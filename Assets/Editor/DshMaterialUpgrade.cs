using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Task 3: give the whole project a consistent material treatment.
///   - every opaque URP Lit material moves to DSH/Surface (procedural detail / grunge / wood / fabric patterns)
///   - maze shell gets triplanar world-space mapping + panel lines
///   - volume gets ACES tonemapping, bloom, colour grading, vignette, film grain
///   - maze lights / ambient / fog are rebalanced
/// </summary>
public static class DshMaterialUpgrade
{
    const string ReportPath = "E:/DSH_Unity/tmp-read/material-upgrade.txt";

    [MenuItem("Tools/DSH/Polish/Upgrade Materials")]
    public static void UpgradeAll()
    {
        var sb = new StringBuilder();
        Shader surface = Shader.Find("DSH/Surface");
        if (surface == null)
        {
            Debug.LogError("[MatUpgrade] DSH/Surface shader missing");
            return;
        }

        EditorSceneManager.OpenScene("Assets/Scenes/Gaming.unity", OpenSceneMode.Single);

        UpgradeShells(surface, sb);
        UpgradeFurniture(surface, sb);
        UpgradeDoor(surface, sb);
        UpgradeLightPanel(surface, sb);

        UpgradeVolume(sb);
        UpgradeLighting(sb);

        AssetDatabase.SaveAssets();
        File.WriteAllText(ReportPath, sb.ToString(), Encoding.UTF8);
        Debug.Log("[MatUpgrade] -> " + ReportPath);
    }

    // ------------------------------------------------------------------
    [MenuItem("Tools/DSH/Polish/Debug Wall Lit")]
    public static void DebugWallLit()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Gaming.unity", OpenSceneMode.Single);
        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Maze/Maze_Wall.mat");
        if (mat == null) return;

        Shot("debug-wall-surface", new Vector3(21f, 1.7f, -6.2f), new Vector3(21f, 1.2f, -11.4f));

        mat.shader = Shader.Find("Universal Render Pipeline/Lit");
        mat.SetColor("_BaseColor", new Color(0.87f, 0.82f, 0.6f));
        mat.SetFloat("_Smoothness", 0.15f);
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
        Shot("debug-wall-lit", new Vector3(21f, 1.7f, -6.2f), new Vector3(21f, 1.2f, -11.4f));

        // restore the surface version
        var sb = new StringBuilder();
        Upgrade("Assets/Materials/Maze/Maze_Wall.mat", Shader.Find("DSH/Surface"), sb,
            detail: 0.38f, grunge: 0.50f, pattern: 0, triplanar: 1, tiling: 0.5f, panel: 0f,
            smoothness: 0.16f, metallic: 0f, tint: new Color(0.60f, 0.55f, 0.44f));
        AssetDatabase.SaveAssets();
        Debug.Log("[MatUpgrade] debug wall lit done");
    }

    static void Shot(string fileName, Vector3 pos, Vector3 target)
    {
        var cam = GameObject.Find("Player/Main Camera")?.GetComponent<Camera>();
        if (cam == null) return;
        Transform t = cam.transform;
        Vector3 oldPos = t.position;
        Quaternion oldRot = t.rotation;
        t.position = pos;
        t.rotation = Quaternion.LookRotation(target - pos, Vector3.up);
        DshCapture.Capture("Player/Main Camera", "E:/DSH_Unity/shots/" + fileName + ".png");
        t.position = oldPos;
        t.rotation = oldRot;
    }

    // ------------------------------------------------------------------
    static void UpgradeShells(Shader shader, StringBuilder sb)
    {
        // triplanar world mapping on the big boxes; detail noise + grunge do most of the work
        Upgrade("Assets/Materials/Maze/Maze_Wall.mat", shader, sb,
            detail: 0.38f, grunge: 0.50f, pattern: 0, triplanar: 1, tiling: 0.5f, panel: 0f,
            smoothness: 0.16f, metallic: 0f, tint: new Color(0.78f, 0.72f, 0.58f));

        Upgrade("Assets/Materials/Maze/Maze_Floor.mat", shader, sb,
            detail: 0.45f, grunge: 0.55f, pattern: 0, triplanar: 1, tiling: 0.35f, panel: 1.1f,
            smoothness: 0.26f, metallic: 0f, tint: new Color(0.48f, 0.44f, 0.37f));

        Upgrade("Assets/Materials/Maze/Maze_Ceiling.mat", shader, sb,
            detail: 0.30f, grunge: 0.35f, pattern: 0, triplanar: 1, tiling: 0.3f, panel: 0.3f,
            smoothness: 0.10f, metallic: 0f, tint: new Color(0.26f, 0.26f, 0.27f));

        Upgrade("Assets/Materials/Maze/Maze_TBar.mat", shader, sb,
            detail: 0.18f, grunge: 0.20f, pattern: 0, triplanar: 1, tiling: 1.2f, panel: 0f,
            smoothness: 0.55f, metallic: 0.85f, tint: new Color(0.36f, 0.35f, 0.33f));

        Upgrade("Assets/Materials/Maze/Maze_Prop.mat", shader, sb,
            detail: 0.28f, grunge: 0.30f, pattern: 1, triplanar: 1, tiling: 1.2f, panel: 0f,
            smoothness: 0.35f, metallic: 0.15f, tint: new Color(0.36f, 0.28f, 0.20f));
    }

    // ------------------------------------------------------------------
    static void UpgradeFurniture(Shader shader, StringBuilder sb)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Materials/Interior/BlendFurn", "Assets/Materials/Interior/Furn" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null || mat.shader == null) continue;
            if (!mat.shader.name.StartsWith("Universal Render Pipeline/Lit")) continue;

            string name = mat.name.ToLowerInvariant();
            if (name.Contains("glass")) continue;

            int pattern = 0;
            float metallic = 0f;
            float smoothness = mat.HasProperty("_Smoothness") ? mat.GetFloat("_Smoothness") : 0.3f;

            if (name.Contains("wood") || name.Contains("desk") || name.Contains("table") || name.Contains("drawer") || name.Contains("dectable"))
            {
                pattern = 1;
                smoothness = Mathf.Clamp(smoothness + 0.1f, 0.05f, 0.8f);
            }
            else if (name.Contains("carpet") || name.Contains("textile") || name.Contains("cloth") || name.Contains("chair") || name.Contains("rug"))
            {
                pattern = 2;
                smoothness = Mathf.Clamp(smoothness - 0.05f, 0.05f, 0.6f);
            }
            else if (name.Contains("frame") || name.Contains("radiator"))
            {
                metallic = 0.45f;
                smoothness = Mathf.Clamp(smoothness + 0.12f, 0.1f, 0.85f);
            }

            Upgrade(path, shader, sb,
                detail: 0.26f, grunge: 0.24f, pattern: pattern, triplanar: 0, tiling: 2f, panel: 0f,
                smoothness: smoothness, metallic: metallic, tint: null);
        }
    }

    // ------------------------------------------------------------------
    static void UpgradeDoor(Shader shader, StringBuilder sb)
    {
        Upgrade("Assets/Models/Furniture/DoorEnd/EndDoor_Wood.mat", shader, sb,
            detail: 0.24f, grunge: 0.15f, pattern: 0, triplanar: 0, tiling: 2.6f, panel: 0f,
            smoothness: 0.32f, metallic: 0.05f, tint: new Color(0.34f, 0.21f, 0.13f));
    }

    static void UpgradeLightPanel(Shader shader, StringBuilder sb)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Maze/Maze_LightPanel.mat");
        if (mat == null) { sb.AppendLine("missing light panel mat"); return; }

        mat.shader = shader;
        mat.SetColor("_BaseColor", new Color(0.85f, 0.84f, 0.80f));
        mat.SetColor("_EmissionColor", new Color(2.6f, 2.45f, 2.2f));
        mat.SetFloat("_Metallic", 0f);
        mat.SetFloat("_Smoothness", 0.2f);
        mat.SetFloat("_DetailStrength", 0.05f);
        mat.SetFloat("_GrungeStrength", 0f);
        mat.SetFloat("_Pattern", 0f);
        mat.SetFloat("_Triplanar", 0f);
        mat.SetFloat("_PanelScale", 0f);
        mat.SetFloat("_WearStrength", 0f);
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        EditorUtility.SetDirty(mat);
        sb.AppendLine("light panel -> DSH/Surface + emission 2.6");
    }

    // ------------------------------------------------------------------
    static void Upgrade(string path, Shader shader, StringBuilder sb,
        float detail, float grunge, int pattern, float triplanar, float tiling, float panel,
        float smoothness, float metallic, Color? tint)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            sb.AppendLine("missing " + path);
            return;
        }

        Texture baseMap = mat.HasProperty("_BaseMap") ? mat.GetTexture("_BaseMap") : null;
        Texture bumpMap = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;
        Color oldColor = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : Color.white;

        mat.shader = shader;

        mat.SetTexture("_BaseMap", baseMap);
        if (bumpMap != null) mat.SetTexture("_NormalMap", bumpMap);
        mat.SetColor("_BaseColor", tint ?? oldColor);
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Smoothness", smoothness);
        mat.SetFloat("_DetailStrength", detail);
        mat.SetFloat("_DetailScale", 10f);
        mat.SetFloat("_GrungeStrength", grunge);
        mat.SetFloat("_Pattern", pattern);
        mat.SetFloat("_PatternScale", 6f);
        mat.SetFloat("_PatternStrength", pattern > 0 ? 0.55f : 0f);
        mat.SetFloat("_WorldTiling", tiling);
        mat.SetFloat("_Triplanar", triplanar);
        mat.SetFloat("_PanelScale", panel);
        mat.SetFloat("_PanelStrength", panel > 0f ? 0.55f : 0f);
        mat.SetFloat("_WearStrength", 0.14f);
        mat.SetColor("_EmissionColor", Color.black);
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;

        EditorUtility.SetDirty(mat);
        sb.AppendLine($"upgraded {mat.name}: pattern={pattern} detail={detail} grunge={grunge} triplanar={triplanar}");
    }

    // ------------------------------------------------------------------
    static void UpgradeVolume(StringBuilder sb)
    {
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/SampleSceneProfile.asset");
        if (profile == null)
        {
            sb.AppendLine("volume profile missing");
            return;
        }

        var tonemapping = GetOrAdd<Tonemapping>(profile);
        tonemapping.active = true;
        tonemapping.mode.value = TonemappingMode.Neutral;

        var bloom = GetOrAdd<Bloom>(profile);
        bloom.active = true;
        bloom.threshold.value = 0.85f;
        bloom.intensity.value = 0.85f;
        bloom.scatter.value = 0.72f;
        bloom.tint.value = new Color(1f, 0.96f, 0.90f);

        var color = GetOrAdd<ColorAdjustments>(profile);
        color.active = true;
        color.postExposure.value = 0.45f;
        color.contrast.value = 10f;
        color.saturation.value = 6f;

        var vignette = GetOrAdd<Vignette>(profile);
        vignette.active = true;
        vignette.intensity.value = 0.3f;
        vignette.smoothness.value = 0.45f;
        vignette.color.value = new Color(0.015f, 0.015f, 0.02f);

        var grain = GetOrAdd<FilmGrain>(profile);
        grain.active = true;
        grain.type.value = FilmGrainLookup.Thin1;
        grain.intensity.value = 0.12f;
        grain.response.value = 0.85f;

        var whiteBalance = GetOrAdd<WhiteBalance>(profile);
        whiteBalance.active = true;
        whiteBalance.temperature.value = 6f;
        whiteBalance.tint.value = -2f;

        EditorUtility.SetDirty(profile);
        sb.AppendLine("volume: ACES + bloom 0.85 + grading + vignette + grain");
    }

    static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (profile.TryGet<T>(out T existing)) return existing;
        return profile.Add<T>(true);
    }

    // ------------------------------------------------------------------
    static void UpgradeLighting(StringBuilder sb)
    {
        // The asset was authored with "Per Vertex" additional lights, so spot lights never lit
        // any per-pixel material. Force per-pixel lighting.
        var rpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
        if (rpAsset != null)
        {
            var so = new SerializedObject(rpAsset);
            var mode = so.FindProperty("m_AdditionalLightsRenderingMode");
            if (mode != null)
            {
                mode.intValue = 2;
                so.ApplyModifiedProperties();
                sb.AppendLine("PC_RPAsset: additional lights -> per pixel");
            }
        }

        int lights = 0;
        foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (light == null) continue;
            if (light.name != "MazeLight") continue;
            light.intensity = 160f;
            light.color = new Color(1f, 0.94f, 0.86f);
            lights++;
        }

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.62f, 0.62f, 0.66f);
        RenderSettings.ambientEquatorColor = new Color(0.36f, 0.34f, 0.31f);
        RenderSettings.ambientGroundColor = new Color(0.14f, 0.13f, 0.12f);
        RenderSettings.fog = false;
        RenderSettings.fogColor = new Color(0.075f, 0.075f, 0.085f);

        DynamicGI.UpdateEnvironment();

        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        sb.AppendLine($"lighting: {lights} maze lights @160, trilight ambient, fog off, neutral tonemap");
    }
}
