using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Opt-in integration checks using the real player loop and Input System.</summary>
[InitializeOnLoad]
public static class DshBugRegression
{
    const string Armed = "Outdoor.Regression.Armed";
    const string Prefix = "Outdoor.Regression.";
    static readonly string Output = Path.GetFullPath("Temp/OutdoorRegression");
    static int stage;
    static double since;
    static double wallSince;
    static Keyboard keyboard;
    static Mouse mouse;
    static Key held;
    static Vector2 mouseDelta;
    static Vector3 samplePosition;
    static Quaternion sampleRotation;
    static int failures;
    static int checks;
    static int runtimeErrors;

    static DshBugRegression()
    {
        EditorApplication.playModeStateChanged += PlayChanged;
        if (SessionState.GetBool(Armed, false)) EditorApplication.update += Tick;
    }

    [MenuItem("Tools/DSH/Regression/Audit and Fix Scene Configuration")]
    public static void Audit()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save scene edits before audit.");
        Directory.CreateDirectory(Output);
        File.WriteAllText(Path.Combine(Output, "scene-audit.txt"), "Scene audit\n");
        string previous = SceneManager.GetActiveScene().path;
        foreach (var entry in EditorBuildSettings.scenes.Where(s => s.enabled))
        {
            Scene scene = EditorSceneManager.OpenScene(entry.path);
            bool changed = false;
            int missing = 0, invalidMaterials = 0, fixedLights = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                    // NewGame retained an obsolete copy of TMPRadian; the current copy is still attached.
                    if (count == 1 && scene.name == "Start" && t.name == "NewGame" && t.GetComponent<TMPRadian>() != null)
                    {
                        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                        EditorUtility.SetDirty(t.gameObject);
                        changed = true;
                    }
                    else missing += count;
                }
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                    invalidMaterials += renderer.sharedMaterials.Count(m => m == null || m.shader == null);
                foreach (var light in root.GetComponentsInChildren<Light>(true))
                {
                    if ((light.type == LightType.Rectangle || light.type == LightType.Disc)
                        && light.lightmapBakeType != LightmapBakeType.Baked)
                    {
                        light.lightmapBakeType = LightmapBakeType.Baked;
                        EditorUtility.SetDirty(light);
                        fixedLights++;
                        changed = true;
                    }
                }
            }
            TvPortal portal = Object.FindAnyObjectByType<TvPortal>();
            if (portal != null)
            {
                if (portal.portalShader == null)
                {
                    portal.portalShader = Shader.Find("DSH/TvPortal");
                    EditorUtility.SetDirty(portal);
                    changed = true;
                }
                if (portal.glassSurface != null && portal.portalSurface != null)
                {
                    Vector3 expected = portal.portalSurface.transform.position + portal.sourcePortal.forward * 0.007f;
                    if (Vector3.Distance(expected, portal.glassSurface.transform.position) > 0.001f)
                    {
                        portal.glassSurface.transform.position = expected;
                        EditorUtility.SetDirty(portal.glassSurface.transform);
                        changed = true;
                    }
                }
            }
            string line = $"{scene.name}: missingScripts={missing}, invalidMaterials={invalidMaterials}, fixedAreaLights={fixedLights}";
            File.AppendAllText(Path.Combine(Output, "scene-audit.txt"), line + "\n");
            Debug.Log("[Regression] " + line);
            if (changed) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
        }
        if (!string.IsNullOrEmpty(previous)) EditorSceneManager.OpenScene(previous);
    }

    [MenuItem("Tools/DSH/Regression/Run Full Flow")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save scene edits before regression.");
        Directory.CreateDirectory(Output);
        File.WriteAllText(Path.Combine(Output, "results.txt"), "OUTDOOR integration regression\n");
        SessionState.SetString(Prefix + "Scene", SceneManager.GetActiveScene().path);
        var previous = EditorSceneManager.playModeStartScene;
        SessionState.SetString(Prefix + "PlayStart", previous != null ? AssetDatabase.GetAssetPath(previous) : "");
        SavePref("outdoor.finished");
        SavePref("dsh.flowprobe");
        PlayerPrefs.DeleteKey("outdoor.finished");
        PlayerPrefs.SetInt("dsh.flowprobe", 0);
        PlayerPrefs.Save();
        SessionState.SetBool(Armed, true);
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Start.unity");
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();
    }

    static void PlayChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Armed, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            stage = 0; checks = failures = runtimeErrors = 0;
            since = Time.timeAsDouble;
            wallSince = EditorApplication.timeSinceStartup;
            keyboard = InputSystem.AddDevice<Keyboard>("OutdoorRegressionKeyboard");
            mouse = InputSystem.AddDevice<Mouse>("OutdoorRegressionMouse");
            InputSystem.onBeforeUpdate += FeedInput;
            Application.logMessageReceived += CaptureError;
            RestorePlayStart();
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            InputSystem.onBeforeUpdate -= FeedInput;
            Application.logMessageReceived -= CaptureError;
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            RestorePref("outdoor.finished");
            RestorePref("dsh.flowprobe");
            PlayerPrefs.Save();
            RestorePlayStart();
            EditorApplication.update -= Tick;
        }
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(Armed, false);
            string previous = SessionState.GetString(Prefix + "Scene", "");
            if (!string.IsNullOrEmpty(previous)) EditorSceneManager.OpenScene(previous);
        }
    }

    static void FeedInput()
    {
        if (InputState.currentUpdateType != InputUpdateType.Dynamic) return;
        InputSystem.QueueStateEvent(keyboard, held == Key.None ? new KeyboardState() : new KeyboardState(held));
        keyboard.MakeCurrent();
        InputSystem.QueueStateEvent(mouse, new MouseState { position = Vector2.zero, delta = mouseDelta });
        mouse.MakeCurrent();
    }

    static void Tick()
    {
        if (!SessionState.GetBool(Armed, false) || !EditorApplication.isPlaying) return;
        if (EditorApplication.isPaused) { Finish("Editor paused during test"); return; }
        try { Step(); }
        catch (Exception ex) { Finish(ex.ToString()); }
    }

    static void Step()
    {
        double t = Time.timeAsDouble - since;
        if (EditorApplication.timeSinceStartup - wallSince > 90)
        {
            Finish("Timeout at stage " + stage + ", phase=" + Phase() + ", gameTime=" + t.ToString("F2"));
            return;
        }
        LevelDirector director = Object.FindAnyObjectByType<LevelDirector>();
        TvPortal portal = Object.FindAnyObjectByType<TvPortal>();
        // HideHud disables the HUD canvas during the teleport, so the panel can be inactive.
        MapPanel map = Object.FindAnyObjectByType<MapPanel>(FindObjectsInactive.Include);
        switch (stage)
        {
            case 0:
                if (t < 4) return;
                Check(SceneManager.GetActiveScene().name == "Start", "Ordinary Start remains Start (no diagnostic hijack)");
                Check(VisibleText("NewGame") && VisibleText("gamestart"), "Default TV prompt and SPACE instruction visible");
                Check(GameObject.Find("Start_Props") == null || !GameObject.Find("Start_Props").activeInHierarchy,
                    "Minimal title hides decorations before finishing");
                Shot("01-start"); held = Key.Space; Next(); break;
            case 1:
                if (t < 0.2) return;
                held = Key.None;
                if (director == null || Phase() != "charge_vase") return;
                Check(true, "SPACE enters Gaming without prior click; intro completes");
                PlaceNear("vase", new Vector3(0, 0, -3)); Next(); break;
            case 2:
                if (Phase() != "reveal_map" || t < 1.5) return;
                Check(true, "Vase charges and reaches map reveal");
                held = Key.M; Next(); break;
            case 3:
                if (t < 0.6) return;
                held = Key.None;
                Check(map.IsOpen, "M opens map");
                Check(!portal.player.GetComponent<PlayerMovement>().enabled, "Map pauses player movement");
                Shot("02-map"); Next(); break;
            case 4:
                if (t < 0.2) return;
                held = Key.M; Next(); break;
            case 5:
                if (t < 0.5) return;
                held = Key.None;
                if (Phase() != "charge_terminal") return;
                Check(!map.IsOpen && portal.player.GetComponent<PlayerMovement>().enabled, "Map closes and restores movement");
                PlaceNear("terminal", new Vector3(0, 0, 3)); Next(); break;
            case 6:
                if (Phase() != "tv_offline") return;
                Check(true, "Table charges and reaches offline TV");
                PlaceAtTV(portal); Next(); break;
            case 7:
                if (Phase() != "find_keys") return;
                Check(Slot("Slot_Floppy") < 0.01f && Slot("Slot_Disc") < 0.01f, "Both key slots initially dark");
                PlaceNear("key_a", new Vector3(0, 0, -2)); Next(); break;
            case 8:
                if (t < 0.4) return;
                held = Key.E; Next(); break;
            case 9:
                if (t < 0.4) return;
                held = Key.None;
                Check(director.State.Has("key_a"), "E collects floppy through pickup runner");
                Check(Slot("Slot_Floppy") > 0.5f && Slot("Slot_Disc") < 0.01f, "Only floppy slot lights after floppy pickup");
                PlaceNear("key_b", new Vector3(0, 0, 2)); Next(); break;
            case 10:
                if (t < 0.4) return;
                held = Key.E; Next(); break;
            case 11:
                if (t < 0.4) return;
                held = Key.None;
                if (Phase() != "tv_unlock") return;
                Check(director.State.Has("key_b") && Slot("Slot_Disc") > 0.5f, "Disc collected and orange slot lights");
                PlaceAtTV(portal); Next(); break;
            case 12:
                if (t < 0.5) return;
                held = Key.E; Next(); break;
            case 13:
                if (t < 0.5) return;
                held = Key.None;
                if (Phase() != "tv_enter") return;
                Check(portal.portalSurface.enabled, "Submit activates portal screen");
                Check(GameObject.Find("TV_ExitRoom").GetComponentsInChildren<Renderer>()
                    .Where(r => !r.name.StartsWith("Fence_", StringComparison.Ordinal)).All(r => r.enabled),
                    "Remote room remains visible to portal camera (invisible fences excluded)");
                CapturePortal(portal); Shot("03-tv-portal"); Next(); break;
            case 14:
                if (t < 1) return;
                held = Key.E; Next(); break;
            case 15:
                if (t < 0.25) return;
                held = Key.None;
                if (!portal.HasEntered || t < 1.8) return;
                Check(Vector3.Distance(portal.player.position, portal.exitAnchor.position) < 0.5f, "Screen removal teleports to white room");
                Check(portal.player.GetComponent<PlayerMovement>().canMove && portal.player.GetComponent<PlayerMovement>().enabled, "Movement restored after cinematic");
                Check(portal.player.GetComponent<MouseLook>().canControl, "Mouse look restored after cinematic");
                Check(GameObject.Find("TV_DarkRoom") != null, "Dark room exists behind the end door");
                GameObject doorPrompt = GameObject.Find("TV_EndDoorPrompt");
                CanvasGroup promptGroup = doorPrompt != null ? doorPrompt.GetComponent<CanvasGroup>() : null;
                Check(promptGroup != null && promptGroup.alpha > 0.1f, "End door prompt visible from the white room entry");
                samplePosition = portal.player.position; held = Key.A; Next(); break;
            case 16:
                if (t < 1.8) return;
                held = Key.None;
                Check(Vector3.Distance(samplePosition, portal.player.position) > 3.2f, "Real WASD moves beyond old 3m reset radius: " + Vector3.Distance(samplePosition, portal.player.position).ToString("F2") + "m");
                Shot("04-white-room"); sampleRotation = portal.player.rotation; mouseDelta = new Vector2(20, 0); Next(); break;
            case 17:
                if (t < 0.3) return;
                mouseDelta = Vector2.zero;
                Check(Quaternion.Angle(sampleRotation, portal.player.rotation) > 1, "Mouse input turns player in white room");
                Teleport(new Vector3(0f, 0.1f, -203.4f), portal.doorOpening.position);
                held = Key.E; Next(); break;
            case 18:
                if (SceneManager.GetActiveScene().name != "Start")
                {
                    if (t > 9) Check(false, "Pressing E at the end door did not lead back to Start");
                    return;
                }
                held = Key.None;
                Check(PlayerPrefs.GetInt("outdoor.finished") == 1, "End door push animation writes finish flag and returns Start");
                Next(); break;
            case 19:
                if (t < 4) return;
                Check(SceneManager.GetActiveScene().name == "Start", "Returned title stays Start");
                Check(Object.FindAnyObjectByType<SettingsPanel>() == null && Cursor.lockState == CursorLockMode.None, "Old settings panel removed and title cursor unlocked");
                Check(GameObject.Find("Start_Props") != null && GameObject.Find("Start_Props").activeInHierarchy && VisibleText("gamestart"),
                    "Title decorations and prompt revealed after completing the game");
                Shot("05-completed-title"); held = Key.Space; Next(); break;
            case 20:
                if (t < 0.2) return;
                held = Key.None;
                if (director == null || Phase() != "charge_vase") return;
                SettingsPanel settings = Object.FindAnyObjectByType<SettingsPanel>();
                Check(settings != null, "Second game creates fresh settings panel");
                settings.SetOpen(true);
                Check(!portal.player.GetComponent<PlayerMovement>().canMove, "Settings bind and pause the new player");
                settings.SetOpen(false);
                Check(portal.player.GetComponent<PlayerMovement>().canMove, "Settings restore new player's control");
                director.JumpTo("tv_enter"); map.inputEnabled = true; map.Open(); Next(); break;
            case 21:
                if (t < 0.6) return;
                Check(!portal.player.GetComponent<PlayerMovement>().enabled, "Open map disables player component before teleport");
                portal.RemoveScreen(); Next(); break;
            case 22:
                if (t < 0.5) return;
                Check(portal.player.GetComponent<PlayerMovement>().enabled && portal.player.GetComponent<MouseLook>().enabled && !map.IsOpen, "Teleport with map open restores both components and closes map");
                Check(runtimeErrors == 0, "Zero runtime errors/exceptions (count=" + runtimeErrors + ")");
                Finish(null); break;
        }
    }

    static void Next()
    {
        stage++; since = Time.timeAsDouble; wallSince = EditorApplication.timeSinceStartup;
        File.AppendAllText(Path.Combine(Output, "results.txt"), "STAGE " + stage + "\n");
    }
    static string Phase()
    {
        var d = Object.FindAnyObjectByType<LevelDirector>();
        return d != null && d.CurrentIndex >= 0 ? d.levelDefinition.phases[d.CurrentIndex].id : "none";
    }
    static void PlaceNear(string id, Vector3 offset)
    {
        var target = GuidanceRegistry.Find(id);
        Vector3 foot = target.transform.position + offset; foot.y = 0.1f;
        Teleport(foot, target.AimPosition);
    }
    static void PlaceAtTV(TvPortal portal)
    {
        Vector3 foot = portal.sourcePortal.position + portal.sourcePortal.forward * 2.2f;
        foot.y = 0.1f;
        Teleport(foot, portal.sourcePortal.position);
    }
    static void Teleport(Vector3 position, Vector3 target)
    {
        var portal = Object.FindAnyObjectByType<TvPortal>();
        var cc = portal.player.GetComponent<CharacterController>();
        cc.enabled = false;
        portal.player.position = position;
        Vector3 horizontal = target - position; horizontal.y = 0;
        portal.player.rotation = Quaternion.LookRotation(horizontal);
        portal.player.GetComponent<MouseLook>().ResetLook();
        portal.player.GetComponent<PlayerMovement>().ResetMotion();
        cc.enabled = true; Physics.SyncTransforms();
    }
    static float Slot(string name) => GameObject.Find(name).GetComponent<Renderer>().sharedMaterial.GetColor("_EmissionColor").maxColorComponent;
    static bool VisibleText(string name)
    {
        var text = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(x => x.name == name);
        return text != null && text.gameObject.activeInHierarchy && text.GetComponent<CanvasGroup>().alpha > 0.9f;
    }
    static void CapturePortal(TvPortal portal)
    {
        var rt = portal.portalCamera.targetTexture;
        File.AppendAllText(Path.Combine(Output, "results.txt"),
            $"PORTALCAM pos={portal.portalCamera.transform.position} rot={portal.portalCamera.transform.eulerAngles} fov={portal.portalCamera.fieldOfView} mask=0x{portal.portalCamera.cullingMask:X} rt={rt.width}x{rt.height}\n");
        GameObject leaf = GameObject.Find("Plane.001");
        if (leaf != null)
        {
            var leafRenderer = leaf.GetComponent<Renderer>();
            if (leafRenderer != null)
            {
                File.AppendAllText(Path.Combine(Output, "results.txt"),
                    $"LEAF bounds={leafRenderer.bounds.center} size={leafRenderer.bounds.size}\n");
            }
        }
        var old = RenderTexture.active; RenderTexture.active = rt;
        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply(); RenderTexture.active = old;
        Color32[] pixels = tex.GetPixels32();
        Check(pixels.Count(c => c.r > 160 && c.g > 160 && c.b > 160) > pixels.Length / 2, "Portal RT shows bright white world");
        Check(pixels.Count(c => c.r < 60 && c.g < 60 && c.b < 60) > pixels.Length / 50, "Portal RT contains the dark doorway");
        File.WriteAllBytes(Path.Combine(Output, "portal-rt.png"), tex.EncodeToPNG()); Object.Destroy(tex);
    }
    static void Shot(string name) { ScreenCapture.CaptureScreenshot(Path.Combine(Output, name + ".png")); }
    static void Check(bool success, string message)
    {
        checks++; if (!success) failures++;
        string line = (success ? "PASS " : "FAIL ") + message;
        File.AppendAllText(Path.Combine(Output, "results.txt"), line + "\n");
        Debug.Log("[Regression] " + line);
        if (!success) throw new InvalidOperationException(message);
    }
    static void CaptureError(string condition, string trace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
        {
            runtimeErrors++;
            File.AppendAllText(Path.Combine(Output, "results.txt"), "RUNTIME ERROR " + condition + "\n" + trace + "\n");
        }
    }
    static void Finish(string error)
    {
        held = Key.None; mouseDelta = Vector2.zero;
        if (error != null) { failures++; File.AppendAllText(Path.Combine(Output, "results.txt"), "ABORT " + error + "\n"); }
        File.AppendAllText(Path.Combine(Output, "results.txt"), $"RESULT checks={checks} failures={failures} runtimeErrors={runtimeErrors}\n");
        EditorApplication.update -= Tick;
        EditorApplication.ExitPlaymode();
    }
    static void SavePref(string key)
    {
        SessionState.SetBool(Prefix + key + ".Present", PlayerPrefs.HasKey(key));
        SessionState.SetInt(Prefix + key, PlayerPrefs.GetInt(key));
    }
    static void RestorePref(string key)
    {
        if (SessionState.GetBool(Prefix + key + ".Present", false)) PlayerPrefs.SetInt(key, SessionState.GetInt(Prefix + key, 0));
        else PlayerPrefs.DeleteKey(key);
    }
    static void RestorePlayStart()
    {
        string previous = SessionState.GetString(Prefix + "PlayStart", "");
        EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(previous) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
    }
}
