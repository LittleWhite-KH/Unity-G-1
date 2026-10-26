using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 进 Play 后捕获 Console 的 error/exception 到文件（编辑器循环驱动，不受 Play 暂停影响）。
/// </summary>
[InitializeOnLoad]
public static class DshPlayLogCapture
{
    const string OutPath = "E:/DSH_Unity/tmp-read/play-log.txt";
    const string KeyArmed = "DshPlayLogCapture.armed";
    static readonly List<string> lines = new List<string>();
    static float captureStart;
    static bool captureArmed;

    [MenuItem("Tools/DSH/Tools/Capture Play Errors")]
    public static void Run()
    {
        lines.Clear();
        captureStart = 0f;
        captureArmed = false;
        SessionState.SetBool(KeyArmed, true);
        Application.logMessageReceived -= OnLog;
        Application.logMessageReceived += OnLog;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.isPlaying = true;
        Debug.Log("[DshCapture] armed, entering play");
    }

    static void OnLog(string message, string stack, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        if (lines.Count < 40) lines.Add($"[{type}] {message}\n{(stack ?? "").Trim()}");
    }

    static void Tick()
    {
        if (!SessionState.GetBool(KeyArmed, false)) { EditorApplication.update -= Tick; return; }
        if (!EditorApplication.isPlaying) return;

        if (EditorApplication.isPaused) EditorApplication.isPaused = false;
        if (!Application.runInBackground) Application.runInBackground = true;

        if (captureStart <= 0f) captureStart = (float)EditorApplication.timeSinceStartup;
        float t = (float)EditorApplication.timeSinceStartup - captureStart;
        if (t < 6f) return;

        SessionState.SetBool(KeyArmed, false);
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= OnLog;

        var sb = new StringBuilder();
        sb.AppendLine($"captured {lines.Count} error(s) in {t:F1}s");
        foreach (var l in lines) sb.AppendLine(l);
        if (lines.Count == 0) sb.AppendLine("(no errors)");
        Directory.CreateDirectory(Path.GetDirectoryName(OutPath));
        File.WriteAllText(OutPath, sb.ToString());
        Debug.Log("[DshCapture] wrote " + OutPath + " errors=" + lines.Count);
        EditorApplication.isPlaying = false;
    }
}
