using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>检查 FBX/预制体结构：层级、Renderer 世界尺寸、材质。</summary>
public static class DshInspectAsset
{
    [MenuItem("Tools/DSH/Audit/Inspect Assets")]
    public static void InspectAll()
    {
        StringBuilder sb = new StringBuilder();
        string[] paths =
        {
            "Assets/Models/Interior/Keyboard.fbx",
            "Assets/Models/Interior/Cup.fbx",
            "Assets/Models/Interior/TableDesk.fbx",
            "Assets/Models/Interior/LowPolyFurniture.fbx",
            "Assets/Models/Interior/AppleII.fbx",
            "Assets/Models/Interior/MazeInterior1026.fbx",
            "Assets/Models/Interior/Floppy.fbx",
            "Assets/Models/Interior/Vinyl.fbx",
        };

        foreach (string path in paths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                sb.AppendLine($"MISSING {path}");
                continue;
            }

            sb.AppendLine($"=== {path} ===");
            Dump(prefab.transform, sb, 0);
        }

        Directory.CreateDirectory("E:/DSH_Unity/tmp-read");
        File.WriteAllText("E:/DSH_Unity/tmp-read/asset-inspect.txt", sb.ToString());
        Debug.Log("[Inspect] written tmp-read/asset-inspect.txt");
    }

    [MenuItem("Tools/DSH/Audit/Inspect Active Scene Renderers")]
    public static void InspectScene()
    {
        StringBuilder sb = new StringBuilder();
        foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            Bounds b = r.bounds;
            sb.AppendLine($"{PathOf(r.transform)} active={r.gameObject.activeInHierarchy} mat={(r.sharedMaterial != null ? r.sharedMaterial.name : "-")} center=({b.center.x:0.###},{b.center.y:0.###},{b.center.z:0.###}) size=({b.size.x:0.###},{b.size.y:0.###},{b.size.z:0.###})");
        }
        Directory.CreateDirectory("E:/DSH_Unity/tmp-read");
        File.WriteAllText("E:/DSH_Unity/tmp-read/scene-renderers.txt", sb.ToString());
        Debug.Log("[Inspect] written tmp-read/scene-renderers.txt");
    }

    static void Dump(Transform t, StringBuilder sb, int depth)
    {
        string pad = new string(' ', depth * 2);
        Renderer r = t.GetComponent<Renderer>();
        string info = "";
        if (r != null)
        {
            Bounds b = r.bounds;
            info = $" mat={(r.sharedMaterial != null ? r.sharedMaterial.name : "-")} boundsCenter=({b.center.x:0.##},{b.center.y:0.##},{b.center.z:0.##}) boundsSize=({b.size.x:0.###},{b.size.y:0.###},{b.size.z:0.###})";
        }

        sb.AppendLine($"{pad}{t.name} localPos=({t.localPosition.x:0.###},{t.localPosition.y:0.###},{t.localPosition.z:0.###}) localScale=({t.localScale.x:0.###},{t.localScale.y:0.###},{t.localScale.z:0.###}){info}");

        for (int i = 0; i < t.childCount; i++) Dump(t.GetChild(i), sb, depth + 1);
    }

    static string PathOf(Transform t)
    {
        string p = t.name;
        while (t.parent != null) { t = t.parent; p = t.name + "/" + p; }
        return p;
    }
}
