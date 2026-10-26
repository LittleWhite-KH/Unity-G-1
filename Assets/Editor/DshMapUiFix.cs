using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 地图 UI 闪烁修复：地图整块是半透明白（Backdrop 0.31 / 卡片 0.78），
/// 背后的地板会透出来，看起来像"地图和地板重合着闪"。
/// 修法：Backdrop 改成深色实底遮罩，卡片接近不透明。
/// </summary>
public static class DshMapUiFix
{
    static string PathOf(Transform t)
    {
        var sb = new StringBuilder(t.name);
        Transform p = t.parent;
        while (p != null)
        {
            sb.Insert(0, p.name + "/");
            p = p.parent;
        }
        return sb.ToString();
    }

    static void Dump(string tag, Transform root, params string[] names)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[MapUI] === {tag} root={(root != null ? root.name : "null")} ===");
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            Image img = t.GetComponent<Image>();
            if (img == null) continue;
            string path = PathOf(t);
            bool hit = names.Length == 0;
            foreach (string n in names)
            {
                if (t.name == n) { hit = true; break; }
            }
            if (hit)
            {
                sb.AppendLine($"[MapUI] {path}  color=({img.color.r:0.###},{img.color.g:0.###},{img.color.b:0.###},{img.color.a:0.###})  raycast={img.raycastTarget}");
            }
        }
        Debug.Log(sb.ToString());
    }

    [MenuItem("Tools/DSH/Map UI/Inspect")]
    public static void Inspect()
    {
        GameObject hud = GameObject.Find("HUD Canvas");
        GameObject map = GameObject.Find("MapPanel");
        Debug.Log($"[MapUI] HUD Canvas = {(hud != null ? "found" : "NULL")}, MapPanel = {(map != null ? "found" : "NULL")}");
        if (hud != null) Dump("HUD", hud.transform, "Backdrop", "MapCard", "MapArea", "LegendCard", "InfoCard", "ObjectiveCard", "StatusCard", "TopBar");
        if (map != null) Dump("MapPanel", map.transform, "Backdrop", "MapCard", "MapArea", "LegendCard", "InfoCard", "ObjectiveCard", "StatusCard", "TopBar");
    }

    [MenuItem("Tools/DSH/Map UI/Fix Blink")]
    public static void Fix()
    {
        GameObject map = GameObject.Find("MapPanel");
        if (map == null)
        {
            Debug.LogError("[MapUI] MapPanel not found");
            return;
        }

        int changed = 0;

        foreach (Transform t in map.GetComponentsInChildren<Transform>(true))
        {
            Image img = t.GetComponent<Image>();
            if (img == null) continue;

            if (t.name == "Backdrop")
            {
                img.color = new Color(0.015f, 0.02f, 0.035f, 0.9f);
                changed++;
                Debug.Log($"[MapUI] Backdrop -> dark solid (alpha 0.90)");
            }
            else if (t.name == "MapCard")
            {
                Color c = img.color;
                c.a = 0.97f;
                img.color = c;
                changed++;
                Debug.Log($"[MapUI] MapCard -> alpha 0.97");
            }
            else if (t.name == "LegendCard" || t.name == "InfoCard" || t.name == "ObjectiveCard" || t.name == "StatusCard")
            {
                Color c = img.color;
                c.a = 0.95f;
                img.color = c;
                changed++;
                Debug.Log($"[MapUI] {t.name} -> alpha 0.95");
            }
            else if (t.name == "MapArea")
            {
                Color c = img.color;
                c.a = 0.06f;
                img.color = c;
                changed++;
                Debug.Log($"[MapUI] MapArea -> alpha 0.06 (subtle grid bed)");
            }
        }

        EditorUtility.SetDirty(map);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(map.scene);
        Debug.Log($"[MapUI] done, changed={changed}");
    }
}
