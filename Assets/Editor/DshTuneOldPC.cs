using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 调整两台 Apple II（游戏里显示为 OLD PC 1 / OLD PC 2）：
/// - 放大到 1.4 倍（尺寸 0.61×0.42×0.62 → 0.85×0.59×0.87，底部仍贴 y=0）
/// - 正面转向房间开阔方向：PC1 朝东（玩家从东侧走廊来）、PC2 朝西（玩家从西侧来）
/// </summary>
public static class DshTuneOldPC
{
    const string OutPath = "E:/DSH_Unity/tmp-read/tune-oldpc.txt";
    const float Scale = 1.4f;

    [MenuItem("Tools/DSH/Tune/Old PC Facing And Size")]
    public static void Apply()
    {
        var sb = new StringBuilder();
        var maze = GameObject.Find("MazeRoot").transform;

        // PC1 在 (1.85,9)：东侧开阔 -> 屏幕朝东（yaw 90）
        Apply(sb, maze, "OldPC_1_AppleII", 90f);
        // PC2 在 (-19.85,3)：西侧开阔 -> 屏幕朝西（yaw 270）
        Apply(sb, maze, "OldPC_2_AppleII", 270f);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Directory.CreateDirectory(Path.GetDirectoryName(OutPath));
        File.WriteAllText(OutPath, sb.ToString());
        Debug.Log("[OldPC] " + sb);
    }

    [MenuItem("Tools/DSH/Tune/Old PC Restore")]
    public static void Restore()
    {
        var maze = GameObject.Find("MazeRoot").transform;
        foreach (var n in new[] { "OldPC_1_AppleII", "OldPC_2_AppleII" })
        {
            var t = FindDeep(maze, n);
            if (t == null) continue;
            t.localScale = Vector3.one;
            t.rotation = Quaternion.Euler(0f, 180f, 0f);
        }
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("[OldPC] restored scale=1 rot=180");
    }

    static void Apply(StringBuilder sb, Transform maze, string name, float yaw)
    {
        var t = FindDeep(maze, name);
        if (t == null) { sb.AppendLine(name + ": NOT FOUND"); return; }

        var rends = t.GetComponentsInChildren<Renderer>();
        Bounds before = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) before.Encapsulate(rends[i].bounds);

        t.localScale = Vector3.one * Scale;
        t.rotation = Quaternion.Euler(0f, yaw, 0f);
        Physics.SyncTransforms();

        Bounds after = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) after.Encapsulate(rends[i].bounds);

        sb.AppendLine($"=== {name} ===");
        sb.AppendLine($"  pos={t.position}");
        sb.AppendLine($"  before: size={before.size} minY={before.min.y:F3} maxY={before.max.y:F3}");
        sb.AppendLine($"  after : size={after.size} minY={after.min.y:F3} maxY={after.max.y:F3} yaw={t.eulerAngles.y:F1}");
    }

    static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform c in root)
        {
            var r = FindDeep(c, name);
            if (r != null) return r;
        }
        return null;
    }
}
