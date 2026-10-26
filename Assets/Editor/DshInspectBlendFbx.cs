using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>检查从 blend 导出的家具 FBX 在 Unity 里的坐标映射（是否 180°Y 后才能对上 Blender 位置）。</summary>
public static class DshInspectBlendFbx
{
    const string FbxPath = "Assets/Models/Interior/MazeInterior1026.fbx";
    const string OutPath = "E:/DSH_Unity/tmp-read/fbx-inspect.txt";

    [MenuItem("Tools/DSH/Blend/Inspect Furniture FBX")]
    public static void Run()
    {
        var root = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        if (root == null) { Debug.LogError("[FbxInspect] cannot load " + FbxPath); return; }

        var sb = new StringBuilder();
        sb.AppendLine($"root={root.name} localPos={root.transform.localPosition} localRot={root.transform.localEulerAngles} localScale={root.transform.localScale}");
        sb.AppendLine($"childCount={root.transform.childCount}");

        int n = 0;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t == root.transform) continue;
            var r = t.GetComponent<Renderer>();
            string b = "";
            if (r != null)
            {
                var bb = r.bounds;
                b = $" boundsC={Fmt(bb.center)} boundsS={Fmt(bb.size)}";
            }
            string mats = "";
            if (r != null)
            {
                var ms = r.sharedMaterials;
                var list = new List<string>();
                foreach (var m in ms) list.Add(m == null ? "<null>" : m.name);
                mats = " mats=" + string.Join("|", list);
            }
            sb.AppendLine($"[{n}] {t.name} lp={Fmt(t.localPosition)} lr={Fmt(t.localEulerAngles)} ls={Fmt(t.localScale)} wp={Fmt(t.position)}{b}{mats}");
            n++;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(OutPath));
        File.WriteAllText(OutPath, sb.ToString());
        Debug.Log("[FbxInspect] wrote " + OutPath + "\n" + sb.ToString());
    }

    static string Fmt(Vector3 v) => $"({v.x:F2},{v.y:F2},{v.z:F2})";
}
