using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>把 Key_B 的光盘也立起来（与 Key_A 软盘一致），并贴到 Table_PC2 桌面上。</summary>
public static class DshStandDiscKey
{
    const string OutPath = "E:/DSH_Unity/tmp-read/stand-disc.txt";

    [MenuItem("Tools/DSH/Tune/Stand Disc Key")]
    public static void Apply()
    {
        var sb = new StringBuilder();
        var maze = GameObject.Find("MazeRoot").transform;
        var props = FindDeep(maze, "MazeProps");
        var t2 = FindChild(props, "Table_PC2");
        float top2 = t2 != null ? BoundsOf(t2.gameObject).max.y : 0.812f;
        sb.AppendLine($"tablePC2 top={top2:F3}");

        var keyB = FindDeep(GameObject.Find("Keys").transform, "Key_B");
        var disc = keyB.Find("Model_Disc");
        if (disc != null)
        {
            disc.localRotation = Quaternion.Euler(0f, 0f, -90f);

            // 实测立起后的包围盒高度，据此让底部贴桌面
            Physics.SyncTransforms();
            var b = BoundsOf(disc.gameObject);
            float half = b.size.y * 0.5f;
            keyB.position = new Vector3(keyB.position.x, top2 + half + 0.02f, keyB.position.z);
            sb.AppendLine($"disc rot={disc.localEulerAngles} size={b.size} keyB={keyB.position}");
        }
        else sb.AppendLine("Model_Disc missing");

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        File.WriteAllText(OutPath, sb.ToString());
        Debug.Log("[StandDisc] " + sb);
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
