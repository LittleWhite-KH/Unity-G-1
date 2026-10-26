using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 1) 把用户手调过的 Table_PC1 的朝向/比例套用到 Table_PC2（按电脑尺寸比例缩 X/Z，Y 保持一致）；
/// 2) 把 Key_A 的软盘立起来（盘面朝玩家），KeyBob 自转时就变成"竖着转"。
/// </summary>
public static class DshApplyTableTweak
{
    const string OutPath = "E:/DSH_Unity/tmp-read/table-tweak.txt";

    [MenuItem("Tools/DSH/Tune/Apply Table Tweak To PC2")]
    public static void Apply()
    {
        var sb = new StringBuilder();
        var maze = GameObject.Find("MazeRoot").transform;
        var props = FindDeep(maze, "MazeProps");
        var t1 = FindChild(props, "Table_PC1");
        var t2 = FindChild(props, "Table_PC2");
        var pc1 = FindDeep(maze, "OldPC_1_AppleII");
        var pc2 = FindDeep(maze, "OldPC_2_AppleII");

        if (t1 == null || t2 == null) { Debug.LogError("[TableTweak] tables missing"); return; }

        // 电脑尺寸比例：PC2(1.4) / PC1(2.0) = 0.7
        float ratio = pc2.localScale.x / pc1.localScale.x;
        sb.AppendLine($"PC1 rot={t1.localEulerAngles} scale={t1.localScale}");
        sb.AppendLine($"PC2 before rot={t2.localEulerAngles} scale={t2.localScale}");
        sb.AppendLine($"ratio={ratio:F3}");

        // 套用 PC1 的朝向与比例（X/Z 按电脑比例缩，Y 保持一致让桌面同高）
        t2.localRotation = t1.localRotation;
        t2.localScale = new Vector3(t1.localScale.x * ratio, t1.localScale.y, t1.localScale.z * ratio);
        Physics.SyncTransforms();

        // 对齐：桌面中心对准 PC2 电脑 x/z，底部贴地
        Vector3 pc2pos = new Vector3(-19.85f, 0f, 3f);
        var b = BoundsOf(t2.gameObject);
        t2.position += new Vector3(pc2pos.x - b.center.x, -b.min.y, pc2pos.z - b.center.z);
        Physics.SyncTransforms();
        b = BoundsOf(t2.gameObject);

        float top2 = b.max.y;
        sb.AppendLine($"PC2 after rot={t2.localEulerAngles} scale={t2.localScale} bounds={b.size} top={top2:F3}");

        // PC2 电脑抬到新桌面
        pc2.position = new Vector3(pc2.position.x, top2, pc2.position.z);
        Physics.SyncTransforms();
        sb.AppendLine($"pc2 pos={pc2.position}");

        // 软盘立起来：盘面朝玩家，KeyBob 的 Y 自转就变成竖着转
        var keyA = FindDeep(GameObject.Find("Keys").transform, "Key_A");
        var floppy = keyA.Find("Model_Floppy");
        if (floppy != null)
        {
            floppy.localRotation = Quaternion.Euler(0f, 0f, -90f);
            // 立起来后半高 ~0.10，贴桌面
            float top1 = BoundsOf(t1.gameObject).max.y;
            keyA.position = new Vector3(keyA.position.x, top1 + 0.10f, keyA.position.z);
            sb.AppendLine($"floppy rot={floppy.localEulerAngles} keyA={keyA.position} (top1={top1:F3})");
        }

        // 光盘保持平放，底部贴新桌面
        var keyB = FindDeep(GameObject.Find("Keys").transform, "Key_B");
        keyB.position = new Vector3(keyB.position.x, top2 + 0.02f, keyB.position.z);
        sb.AppendLine($"keyB={keyB.position}");

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        File.WriteAllText(OutPath, sb.ToString());
        Debug.Log("[TableTweak] " + sb);
    }

    static Bounds BoundsOf(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        var b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
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

    static Transform FindChild(Transform root, string name)
    {
        foreach (Transform c in root) if (c.name == name) return c;
        return null;
    }
}
