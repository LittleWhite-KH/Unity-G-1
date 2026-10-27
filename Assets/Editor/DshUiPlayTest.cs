using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>
/// Play-mode check for the U-key UI toggle. Entering play mode reloads editor assemblies, so the
/// tick loop is re-armed from a SessionState flag in [InitializeOnLoadMethod].
/// ScreenCapture is used because screen-space overlay canvases never appear in Camera.Render.
/// </summary>
public static class DshUiPlayTest
{
    const string LogPath = "E:/DSH_Unity/tmp-read/ui-play-test.txt";
    const string ShotDir = "E:/DSH_Unity/shots/";
    const string PendingKey = "DshUiPlayTest.pending";

    static float startTime = -1f;
    static int stage;
    static HudVisibilityToggle toggle;
    static bool releasedU;

    [MenuItem("Tools/DSH/Polish/Play UI Test")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Write("already in play mode");
            return;
        }

        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Gaming.unity", OpenSceneMode.Single);
        EditorSceneManager.SaveScene(scene);
        EditorSceneManager.playModeStartScene = null;
        EditorApplication.isPaused = false;
        PlayerSettings.runInBackground = true;

        SessionState.SetBool(PendingKey, true);
        Write("requesting play mode");
        EditorApplication.isPlaying = true;
    }

    [InitializeOnLoadMethod]
    static void ReArmAfterReload()
    {
        if (!SessionState.GetBool(PendingKey, false)) return;

        SessionState.SetBool(PendingKey, false);
        startTime = -1f;
        stage = 0;
        toggle = null;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        Write("re-armed after domain reload");
    }

    static void Tick()
    {
        if (!Application.isPlaying)
        {
            if (startTime > 0f) Write("play mode ended, stop polling");
            EditorApplication.update -= Tick;
            return;
        }

        if (EditorApplication.isPaused) EditorApplication.isPaused = false;

        if (startTime < 0f)
        {
            startTime = Time.realtimeSinceStartup;
            toggle = Object.FindAnyObjectByType<HudVisibilityToggle>();
            Write("tick started; toggle=" + (toggle != null ? $"ready={toggle.IsReady} hidden={toggle.IsHidden}" : "NULL"));
            return;
        }

        float t = Time.realtimeSinceStartup - startTime;

        switch (stage)
        {
            case 0:
                if (t > 5.0f)
                {
                    ScreenCapture.CaptureScreenshot(ShotDir + "play-ui-0-before.png");
                    Write("shot before t=" + t.ToString("0.0") + " hudHidden=" + (toggle != null && toggle.IsHidden));
                    stage = 1;
                }
                break;

            case 1:
                if (t > 5.6f)
                {
                    PressU();
                    releasedU = false;
                    Write("queued U press t=" + t.ToString("0.0"));
                    stage = 2;
                }
                break;

            case 2:
                if (!releasedU && t > 5.9f)
                {
                    ReleaseU();
                    releasedU = true;
                    Write("released U; hudHidden=" + (toggle != null && toggle.IsHidden));
                }
                if (t > 6.6f)
                {
                    ScreenCapture.CaptureScreenshot(ShotDir + "play-ui-1-hidden.png");
                    Write("shot hidden t=" + t.ToString("0.0") + " hudHidden=" + (toggle != null && toggle.IsHidden));
                    stage = 3;
                }
                break;

            case 3:
                if (t > 7.1f)
                {
                    PressU();
                    Write("queued U press (restore) t=" + t.ToString("0.0"));
                    stage = 4;
                }
                break;

            case 4:
                if (t > 7.4f) ReleaseU();
                if (t > 8.0f)
                {
                    ScreenCapture.CaptureScreenshot(ShotDir + "play-ui-2-restored.png");
                    Write("shot restored t=" + t.ToString("0.0") + " hudHidden=" + (toggle != null && toggle.IsHidden));
                    stage = 5;
                }
                break;

            case 5:
                if (t > 8.5f)
                {
                    stage = 6;
                    Write("exiting play mode");
                    EditorApplication.isPlaying = false;
                }
                break;

            case 6:
                EditorApplication.update -= Tick;
                break;
        }
    }

    static void PressU()
    {
        if (Keyboard.current == null)
        {
            Write("keyboard null");
            return;
        }

        // Let the input system process this on the next frame; calling InputSystem.Update()
        // here would consume wasPressedThisFrame before the game's Update runs.
        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.U));
    }

    static void ReleaseU()
    {
        if (Keyboard.current == null) return;
        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
    }

    static void Write(string message)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(LogPath));
        File.AppendAllText(LogPath, System.DateTime.Now.ToString("HH:mm:ss") + "  " + message + "\n");
    }
}
