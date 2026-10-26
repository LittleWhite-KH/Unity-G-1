using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>把穿墙的家具沿 8 个方向尝试推开，直到不再和墙体重叠。</summary>
public static class DshFixFurnitureOverlap
{
    static readonly Vector3[] Dirs =
    {
        new Vector3(1.2f, 0f, 0f), new Vector3(-1.2f, 0f, 0f),
        new Vector3(0f, 0f, 1.2f), new Vector3(0f, 0f, -1.2f),
        new Vector3(1.2f, 0f, 1.2f), new Vector3(-1.2f, 0f, 1.2f),
        new Vector3(1.2f, 0f, -1.2f), new Vector3(-1.2f, 0f, -1.2f),
    };

    [MenuItem("Tools/DSH/Furniture/Fix Overlaps")]
    public static void Fix()
    {
        GameObject root = GameObject.Find("MazeRoot");
        Transform parent = root != null ? root.transform.Find("Interior_Furniture") : null;
        if (parent == null) { Debug.Log("[FurnFix] no furniture"); return; }

        int fixedCount = 0;

        foreach (Transform child in parent)
        {
            if (!Overlaps(child, out Bounds b)) continue;

            Vector3 original = child.position;
            bool moved = false;

            foreach (Vector3 dir in Dirs)
            {
                child.position = original + dir;

                if (!Overlaps(child, out _))
                {
                    moved = true;
                    fixedCount++;
                    Debug.Log($"[FurnFix] {child.name} {original} -> {child.position}");
                    break;
                }
            }

            if (!moved)
            {
                child.position = original;
                Debug.LogWarning($"[FurnFix] {child.name} could not be moved (still overlapping)");
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log($"[FurnFix] fixed {fixedCount}");
    }

    static bool Overlaps(Transform t, out Bounds b)
    {
        b = new Bounds(t.position, Vector3.zero);
        Renderer[] rs = t.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) return false;

        b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);

        Collider[] hits = Physics.OverlapBox(b.center, b.size * 0.45f, Quaternion.identity);
        foreach (Collider c in hits)
        {
            if (c == null || c.transform.IsChildOf(t)) continue;
            if (c.name.Contains("Walls")) return true;
        }

        return false;
    }
}
