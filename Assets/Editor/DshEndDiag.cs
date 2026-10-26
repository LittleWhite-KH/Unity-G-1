using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

/// <summary>Explicit, one-shot ending diagnostic. Ordinary Play never runs it.</summary>
[InitializeOnLoad]
public static class DshEndDiag
{
    const string ArmedKey = "Outdoor.EndDiag.Armed";
    const string PreviousStartKey = "Outdoor.EndDiag.PreviousStart";
    static readonly string LogPath = Path.GetFullPath("Temp/end-diag.log");
    static int step;
    static double stepStart;
    static Vector3 startPos;
    static Keyboard keyboard;
    static bool pressing;

    static DshEndDiag()
    {
        EditorApplication.playModeStateChanged += OnPlayState;
        if (SessionState.GetBool(ArmedKey, false)) EditorApplication.update += Tick;
    }

    [MenuItem("Tools/DSH/Test/End Diag")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.WriteAllText(LogPath, "== explicit end diag ==\n");
        var previous = EditorSceneManager.playModeStartScene;
        SessionState.SetString(PreviousStartKey, previous != null ? AssetDatabase.GetAssetPath(previous) : "");
        SessionState.SetBool(ArmedKey, true);
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Gaming.unity");
        step = 0;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();
    }

    static void OnPlayState(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(ArmedKey, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            string previous = SessionState.GetString(PreviousStartKey, "");
            EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(previous)
                ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
            stepStart = EditorApplication.timeSinceStartup;
        }
        if (state == PlayModeStateChange.ExitingPlayMode) Cleanup();
    }

    static void Tick()
    {
        if (!SessionState.GetBool(ArmedKey, false) || !EditorApplication.isPlaying || EditorApplication.isPaused) return;
        if (SceneManager.GetActiveScene().name != "Gaming") { Cleanup(); return; }
        double elapsed = EditorApplication.timeSinceStartup - stepStart;
        TvPortal portal = Object.FindAnyObjectByType<TvPortal>();
        if (portal == null || portal.player == null) return;
        switch (step)
        {
            case 0:
                if (elapsed < 2) return;
                Object.FindAnyObjectByType<LevelDirector>()?.JumpTo("tv_enter");
                portal.SetScreenOn(true);
                portal.RemoveScreen();
                Next();
                break;
            case 1:
                if (elapsed < 1) return;
                startPos = portal.player.position;
                keyboard = InputSystem.AddDevice<Keyboard>("EndingDiagnosticKeyboard");
                pressing = true;
                InputSystem.onBeforeUpdate += FeedInput;
                Next();
                break;
            case 2:
                if (elapsed < 1.5) return;
                pressing = false;
                Log("sideways displacement=" + Vector3.Distance(startPos, portal.player.position).ToString("F3"));
                Cleanup();
                break;
        }
    }

    static void FeedInput()
    {
        if (keyboard == null || InputState.currentUpdateType != InputUpdateType.Dynamic) return;
        InputSystem.QueueStateEvent(keyboard, pressing ? new KeyboardState(Key.A) : new KeyboardState());
        keyboard.MakeCurrent();
    }

    static void Cleanup()
    {
        InputSystem.onBeforeUpdate -= FeedInput;
        if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
        keyboard = null;
        pressing = false;
        EditorApplication.update -= Tick;
        string previous = SessionState.GetString(PreviousStartKey, "");
        EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(previous)
            ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
        SessionState.SetBool(ArmedKey, false);
    }

    static void Next() { step++; stepStart = EditorApplication.timeSinceStartup; }
    static void Log(string message)
    {
        Debug.Log("[EndDiag] " + message);
        File.AppendAllText(LogPath, message + "\n");
    }
}
