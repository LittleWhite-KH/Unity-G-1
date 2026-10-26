using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>把 Console 内容 dump 到文件（MCP 的 console 读取在 Unity 6 上失效时的备用手段）。</summary>
public static class DshDumpConsole
{
    const string OutPath = "E:/DSH_Unity/tmp-read/console-dump.txt";

    [MenuItem("Tools/DSH/Tools/Dump Console To File")]
    public static void Dump()
    {
        var logEntries = System.Type.GetType("UnityEditor.LogEntries, UnityEditor");
        var logEntry = System.Type.GetType("UnityEditor.LogEntry, UnityEditor");
        if (logEntries == null || logEntry == null)
        {
            File.WriteAllText(OutPath, "LogEntries/LogEntry type not found\n");
            Debug.LogWarning("[Dsh] LogEntries type not found");
            return;
        }

        var getCount = logEntries.GetMethod("GetCount", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        var getEntry = logEntries.GetMethod("GetEntryInternal", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (getCount == null || getEntry == null)
        {
            File.WriteAllText(OutPath, "GetCount/GetEntryInternal not found\n");
            return;
        }

        int count = (int)getCount.Invoke(null, null);
        var entry = System.Activator.CreateInstance(logEntry);
        var msgField = logEntry.GetField("message");
        var modeField = logEntry.GetField("mode");
        var sb = new StringBuilder();
        sb.AppendLine($"total={count}");
        int start = Mathf.Max(0, count - 60);
        for (int i = start; i < count; i++)
        {
            getEntry.Invoke(null, new object[] { i, entry });
            string msg = msgField != null ? (string)msgField.GetValue(entry) : "?";
            int mode = modeField != null ? (int)modeField.GetValue(entry) : -1;
            sb.AppendLine($"[{i}] mode={mode} {msg.Replace("\n", " | ")}");
        }
        Directory.CreateDirectory(Path.GetDirectoryName(OutPath));
        File.WriteAllText(OutPath, sb.ToString());
        Debug.Log("[Dsh] dumped " + count + " entries to " + OutPath);
    }
}
