using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>检查家具包围盒是否和迷宫墙体（MeshCollider）重叠，输出需要挪动的家具。</summary>
public static class DshCheckFurniture
{
    [MenuItem("Tools/DSH/Furniture/Check Overlap")]
    public static void Check()
    {
        GameObject root = GameObject.Find("MazeRoot");
        Transform parent = root != null ? root.transform.Find("Interior_Furniture") : null;
        if (parent == null) { Debug.Log("[FurnCheck] no furniture"); return; }

        var sb = new StringBuilder();
        int bad = 0;

        foreach (Transform child in parent)
        {
            Renderer[] rs = child.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) continue;

            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);

            Collider[] hits = Physics.OverlapBox(b.center, b.size * 0.45f, Quaternion.identity);
            foreach (Collider c in hits)
            {
                if (c == null) continue;
                if (c.transform.IsChildOf(child)) continue;
                if (!c.name.Contains("Walls")) continue;

                sb.AppendLine($"[FurnCheck] OVERLAP {child.name} at {child.position} size={b.size} with {c.name}");
                bad++;
                break;
            }
        }

        sb.AppendLine($"[FurnCheck] total={parent.childCount} overlapping={bad}");
        Debug.Log(sb.ToString());
    }
}
