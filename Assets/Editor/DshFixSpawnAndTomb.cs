using UnityEditor;
using UnityEngine;

/// <summary>修正出生朝向（当前正前方是墙）并清掉墓碑旁的怪家具。</summary>
public static class DshFixSpawnAndTomb
{
    [MenuItem("Tools/DSH/Fix Spawn Facing")]
    public static void FixSpawn()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null) { Debug.LogError("[Fix] Player missing"); return; }

        player.transform.rotation = Quaternion.Euler(0f, 0f, 0f); // 朝北（+Z），面向走廊
        MouseLook look = player.GetComponent<MouseLook>();
        if (look != null) look.canControl = true;

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(player.scene);
        Debug.Log($"[Fix] spawn facing -> {player.transform.eulerAngles}");
    }

    [MenuItem("Tools/DSH/Fix Tomb Furniture")]
    public static void FixTomb()
    {
        GameObject root = GameObject.Find("MazeRoot");
        Transform parent = root != null ? root.transform.Find("Interior_Furniture") : null;
        if (parent == null) { Debug.Log("[Fix] no furniture"); return; }

        Vector3 tomb = new Vector3(-3f, 0f, 11.2f);
        int removed = 0;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            float d = Vector3.Distance(new Vector3(child.position.x, 0f, child.position.z), tomb);
            if (d <= 4.0f)
            {
                Debug.Log($"[Fix] removing {child.name} at {child.position} (d={d:0.##})");
                Object.DestroyImmediate(child.gameObject);
                removed++;
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log($"[Fix] removed {removed} furniture near tomb");
    }
}
