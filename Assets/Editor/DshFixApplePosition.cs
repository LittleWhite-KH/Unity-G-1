using UnityEditor;
using UnityEngine;

/// <summary>旧电脑的 Transform 在原点、几何烘在顶点里，所以要用 Renderer.bounds 定位 Apple II。</summary>
public static class DshFixApplePosition
{
    [MenuItem("Tools/DSH/Computers/Fix AppleII Position")]
    public static void Fix()
    {
        GameObject mazeRoot = GameObject.Find("MazeRoot");
        Transform props = mazeRoot != null ? mazeRoot.transform.Find("MazeProps") : null;
        if (props == null) { Debug.LogError("[AppleII] MazeProps not found"); return; }

        foreach (string name in new[] { "OldPC_1", "OldPC_2" })
        {
            Transform old = props.Find(name);
            Transform neu = props.Find(name + "_AppleII");
            if (old == null || neu == null) { Debug.LogWarning($"[AppleII] missing {name}"); continue; }

            bool wasActive = old.gameObject.activeSelf;
            old.gameObject.SetActive(true);

            Renderer[] rs = old.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0)
            {
                Debug.LogWarning($"[AppleII] no renderer on {name}");
                old.gameObject.SetActive(wasActive);
                continue;
            }

            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            old.gameObject.SetActive(wasActive);

            Vector3 target = new Vector3(b.center.x, 0f, b.center.z);
            neu.position = target;
            Debug.Log($"[AppleII] {name} bounds center={b.center} size={b.size} -> apple at {target}");
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
    }
}
