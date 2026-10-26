using UnityEditor;
using UnityEngine;

/// <summary>按名字关键字删除指定家具（用于清掉 low-poly 里形状奇怪的复合件）。</summary>
public static class DshRemoveFurniture
{
    [MenuItem("Tools/DSH/Furniture/Remove Near Table")]
    public static void RemoveNearTable()
    {
        GameObject root = GameObject.Find("MazeRoot");
        Transform parent = root != null ? root.transform.Find("Interior_Furniture") : null;
        if (parent == null) return;

        Vector3 target = new Vector3(20.6f, 0f, -8.4f);
        int removed = 0;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            float d = Vector3.Distance(new Vector3(child.position.x, 0f, child.position.z), target);
            if (d <= 3.2f)
            {
                Debug.Log($"[FurnRemove] near-table {child.name} at {child.position} d={d:0.##}");
                Object.DestroyImmediate(child.gameObject);
                removed++;
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log($"[FurnRemove] removed near table {removed}");
    }
    [MenuItem("Tools/DSH/Furniture/Remove Odd Compl")]
    public static void RemoveOdd()
    {
        GameObject root = GameObject.Find("MazeRoot");
        Transform parent = root != null ? root.transform.Find("Interior_Furniture") : null;
        if (parent == null) { Debug.Log("[FurnRemove] no furniture"); return; }

        string[] keys = { "Cube_013", "Cube_008", "Cube_011", "Cube_012", "Cube_015", "Cube_016" };
        int removed = 0;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            foreach (string k in keys)
            {
                if (!child.name.Contains(k)) continue;
                Debug.Log($"[FurnRemove] {child.name} at {child.position}");
                Object.DestroyImmediate(child.gameObject);
                removed++;
                break;
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log($"[FurnRemove] removed {removed}");
    }
}
