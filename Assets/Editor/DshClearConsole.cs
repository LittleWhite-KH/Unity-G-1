using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>清空 Console（含 Error Pause 的触发源），用于 Play 前做准备。</summary>
public static class DshClearConsole
{
    [MenuItem("Tools/DSH/Tools/Clear Console")]
    public static void Clear()
    {
        var t = System.Type.GetType("UnityEditor.LogEntries, UnityEditor");
        if (t == null) { Debug.LogWarning("[Dsh] LogEntries type not found"); return; }
        var m = t.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (m == null) { Debug.LogWarning("[Dsh] LogEntries.Clear not found"); return; }
        m.Invoke(null, null);
        Debug.Log("[Dsh] console cleared");
    }

    [MenuItem("Tools/DSH/Tools/Clear Console And Play")]
    public static void ClearAndPlay()
    {
        Clear();
        EditorApplication.isPlaying = true;
    }
}
