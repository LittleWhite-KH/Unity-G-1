using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>璇婃柇鍦版澘鐩稿叧娓叉煋锛氭壘鍑轰笌鍦版澘椤堕潰鍏遍潰 / 鍖呭洿鐩掗噸鍙犵殑 MeshRenderer锛岀敤浜庡畾浣?z-fighting銆?/summary>
public static class DshFloorDiag
{
    const string OutPath = "E:/DSH_Unity/tmp-read/floor-diag.txt";

    [MenuItem("Tools/DSH/Diag/Floor Z-Fighting")]
    public static void Run()
    {
        var sb = new StringBuilder();
        var floorGo = GameObject.Find("MazeRoot/MazeArchitecture/Floor");
        if (floorGo == null)
        {
            Debug.LogError("[FloorDiag] MazeRoot/MazeArchitecture/Floor not found (maybe inactive)");
            return;
        }

        var floorR = floorGo.GetComponent<Renderer>();
        Bounds fb = floorR.bounds;
        sb.AppendLine("=== Floor ===");
        sb.AppendLine($"name={floorGo.name} active={floorGo.activeInHierarchy}");
        sb.AppendLine($"worldPos={floorGo.transform.position} euler={floorGo.transform.eulerAngles}");
        sb.AppendLine($"bounds center={fb.center} size={fb.size} min={fb.min} max={fb.max}");
        sb.AppendLine($"materials={MatNames(floorR)}");
        sb.AppendLine($"shadowCasting={floorR.shadowCastingMode} receiveShadows={floorR.receiveShadows}");

        float topY = fb.max.y;
        sb.AppendLine($"floorTopY={topY:F5}");
        sb.AppendLine("");

        var rends = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int n = 0;
        var hits = new List<string>();

        foreach (var r in rends)
        {
            if (r == floorR) continue;
            Bounds b = r.bounds;
            bool xzOverlap = b.max.x > fb.min.x && b.min.x < fb.max.x && b.max.z > fb.min.z && b.min.z < fb.max.z;
            // 椤堕潰 / 搴曢潰璐磋繎鍦版澘椤堕潰锛堝叡闈㈠珜鐤戯級
            bool nearTop = Mathf.Abs(b.max.y - topY) < 0.03f;
            bool nearBottom = Mathf.Abs(b.min.y - topY) < 0.03f;
            bool straddle = b.min.y < topY - 0.001f && b.max.y > topY + 0.001f;

            if (xzOverlap && (nearTop || nearBottom || straddle))
            {
                n++;
                hits.Add($"--- [{n}] {FullPath(r.transform)} active={r.gameObject.activeInHierarchy} " +
                         $"boundsY=[{b.min.y:F4},{b.max.y:F4}] center={b.center} size={b.size}\n" +
                         $"    nearTop={nearTop} nearBottom={nearBottom} straddle={straddle} mats={MatNames(r)} " +
                         $"castShadows={r.shadowCastingMode} receiveShadows={r.receiveShadows} queue={QueueOf(r)}");
            }
        }

        sb.AppendLine($"=== candidates overlapping floor plane (y={topY:F5}): {n} ===");
        foreach (var h in hits) sb.AppendLine(h);

        // 棰濆锛氭墍鏈?MeshRenderer 鐨勬潗璐?renderQueue锛堟壘閫忔槑鐗╀綋锛?        sb.AppendLine("");
        sb.AppendLine("=== non-opaque renderers near floor region ===");
        foreach (var r in rends)
        {
            Bounds b = r.bounds;
            bool xzOverlap = b.max.x > fb.min.x && b.min.x < fb.max.x && b.max.z > fb.min.z && b.min.z < fb.max.z;
            if (!xzOverlap) continue;
            int q = QueueOf(r);
            if (q != -1 && q >= 3000)
                sb.AppendLine($"TRANSPARENT queue={q} {FullPath(r.transform)} mats={MatNames(r)} bounds={b}");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(OutPath));
        File.WriteAllText(OutPath, sb.ToString());
        Debug.Log($"[FloorDiag] wrote {OutPath}\n" + sb.ToString());
    }

    [MenuItem("Tools/DSH/Diag/Coplanar Scan All")]
    public static void CoplanarScan()
    {
        var sb = new StringBuilder();
        var rends = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int n = 0;
        // 绮楁壂锛氫换鎰忎袱涓?renderer 鐨勫寘鍥寸洅鍦ㄦ煇涓酱鍚戜笂"闆跺帤搴﹁创鍚?涓?xz/xy 閲嶅彔
        for (int i = 0; i < rends.Length; i++)
        {
            var a = rends[i];
            if (!a.gameObject.activeInHierarchy) continue;
            Bounds ba = a.bounds;
            for (int j = i + 1; j < rends.Length; j++)
            {
                var b = rends[j];
                if (!b.gameObject.activeInHierarchy) continue;
                Bounds bb = b.bounds;
                bool yz = ba.max.x > bb.min.x && ba.min.x < bb.max.x && ba.max.z > bb.min.z && ba.min.z < bb.max.z;
                if (!yz) continue;
                float dTop = Mathf.Abs(ba.max.y - bb.max.y);
                float dBot = Mathf.Abs(ba.min.y - bb.min.y);
                bool flatA = ba.size.y < 0.05f, flatB = bb.size.y < 0.05f;
                if ((flatA || flatB) && Mathf.Min(dTop, dBot) < 0.02f)
                {
                    n++;
                    sb.AppendLine($"[{n}] dy<0.02  A={FullPath(a.transform)} y=[{ba.min.y:F4},{ba.max.y:F4}]  B={FullPath(b.transform)} y=[{bb.min.y:F4},{bb.max.y:F4}]");
                    if (n > 60) { sb.AppendLine("... truncated"); goto done; }
                }
            }
        }
    done:
        Directory.CreateDirectory(Path.GetDirectoryName(OutPath));
        File.WriteAllText("E:/DSH_Unity/tmp-read/coplanar-scan.txt", sb.ToString());
        Debug.Log($"[FloorDiag] coplanar scan done, {n} pairs\n" + sb.ToString());
    }

    static string FullPath(Transform t)
    {
        var s = t.name;
        var p = t.parent;
        while (p != null) { s = p.name + "/" + s; p = p.parent; }
        return s;
    }

    static string MatNames(Renderer r)
    {
        if (r == null) return "null";
        var ms = r.sharedMaterials;
        if (ms == null || ms.Length == 0) return "none";
        var sb = new StringBuilder();
        foreach (var m in ms) sb.Append(m == null ? "<null>" : (m.name + (m.shader ? "(" + m.shader.name + ")" : ""))).Append(", ");
        return sb.ToString();
    }

    static int QueueOf(Renderer r)
    {
        var m = r.sharedMaterial;
        return m == null ? -1 : m.renderQueue;
    }
}

