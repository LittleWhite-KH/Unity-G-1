using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>把迷宫里的两台旧电脑（OldPC_1 / OldPC_2）换成 Apple II，并把钥匙高度降到机器顶部。</summary>
public static class DshReplaceComputers
{
    const string AppleIIPath = "Assets/Models/Interior/AppleII.fbx";
    const string MatDir = "Assets/Materials/Interior/AppleII";

    static readonly Dictionary<string, Color> MatColors = new Dictionary<string, Color>
    {
        { "appleII_body_mat",     new Color(0.86f, 0.84f, 0.76f) },
        { "appleII_details_mat",  new Color(0.22f, 0.21f, 0.19f) },
        { "appleII_keyboard_mat", new Color(0.82f, 0.80f, 0.72f) },
        { "monitor_base_mat",     new Color(0.84f, 0.82f, 0.74f) },
        { "Monitor_details_mat",  new Color(0.78f, 0.76f, 0.68f) },
        { "monitor_screen_mat",   new Color(0.05f, 0.06f, 0.05f) },
        { "diskII_mat",           new Color(0.84f, 0.82f, 0.74f) },
        { "cable_mat",            new Color(0.28f, 0.28f, 0.26f) },
        { "plugs_mat",            new Color(0.20f, 0.20f, 0.18f) },
    };

    [MenuItem("Tools/DSH/Computers/Replace With AppleII")]
    public static void Replace()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AppleIIPath);
        if (prefab == null) { Debug.LogError("[AppleII] prefab not found"); return; }

        GameObject mazeRoot = GameObject.Find("MazeRoot");
        if (mazeRoot == null) { Debug.LogError("[AppleII] MazeRoot not found"); return; }
        Transform props = mazeRoot.transform.Find("MazeProps");
        if (props == null) { Debug.LogError("[AppleII] MazeProps not found"); return; }

        string[] names = { "OldPC_1", "OldPC_2" };
        foreach (string name in names)
        {
            Transform old = props.Find(name);
            if (old == null) { Debug.LogWarning($"[AppleII] {name} not found"); continue; }

            Vector3 pos = old.position;
            Quaternion rot = old.rotation;

            // 先禁用旧的（保留可回滚）
            old.gameObject.SetActive(false);

            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, props);
            go.name = name + "_AppleII";
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // 正面朝 -Z（房间来向）
            go.transform.localScale = Vector3.one;

            ApplyMaterials(go);
            AddCollider(go);

            Debug.Log($"[AppleII] replaced {name} oldPos={pos} oldRot={rot.eulerAngles} -> newRot=(0,180,0)");
        }

        // 钥匙降到 Apple II 顶部 + 0.22m
        LowerKey("Key_A", 0.64f);
        LowerKey("Key_B", 0.64f);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log("[AppleII] done");
    }

    static void LowerKey(string keyName, float y)
    {
        GameObject key = GameObject.Find("Keys/" + keyName);
        if (key == null)
        {
            // 可能不在 Keys 根下，全局找
            GameObject found = GameObject.Find(keyName);
            if (found == null) { Debug.LogWarning($"[AppleII] key {keyName} not found"); return; }
            key = found;
        }

        Vector3 p = key.transform.position;
        key.transform.position = new Vector3(p.x, y, p.z);

        GuidanceTarget gt = key.GetComponent<GuidanceTarget>();
        if (gt != null)
        {
            gt.heightOffset = 0.10f;
            EditorUtility.SetDirty(gt);
        }

        Debug.Log($"[AppleII] {keyName} y -> {y} (heightOffset 0.10)");
    }

    static void ApplyMaterials(GameObject go)
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null) { Debug.LogError("[AppleII] URP Lit shader not found"); return; }

        if (!AssetDatabase.IsValidFolder("Assets/Materials/Interior")) AssetDatabase.CreateFolder("Assets/Materials", "Interior");
        if (!AssetDatabase.IsValidFolder(MatDir)) AssetDatabase.CreateFolder("Assets/Materials/Interior", "AppleII");

        var cache = new Dictionary<string, Material>();

        foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
        {
            Material[] mats = r.sharedMaterials;
            bool changed = false;

            for (int i = 0; i < mats.Length; i++)
            {
                string matName = mats[i] != null ? mats[i].name : "default";
                if (!cache.TryGetValue(matName, out Material mat))
                {
                    string path = $"{MatDir}/{matName}.mat";
                    mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (mat == null)
                    {
                        mat = new Material(lit) { name = matName };
                        Color c = MatColors.ContainsKey(matName) ? MatColors[matName] : new Color(0.7f, 0.68f, 0.62f);
                        mat.color = c;
                        mat.SetFloat("_Smoothness", 0.25f);
                        mat.SetFloat("_Metallic", 0f);
                        AssetDatabase.CreateAsset(mat, path);
                    }
                    cache[matName] = mat;
                }
                mats[i] = mat;
                changed = true;
            }

            if (changed) r.sharedMaterials = mats;
        }

        AssetDatabase.SaveAssets();
    }

    static void AddCollider(GameObject go)
    {
        if (go.GetComponent<Collider>() != null) return;

        Renderer[] rs = go.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) return;

        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);

        BoxCollider bc = go.AddComponent<BoxCollider>();
        bc.center = go.transform.InverseTransformPoint(b.center);
        bc.size = b.size;

        Debug.Log($"[AppleII] collider center={bc.center} size={bc.size}");
    }
}
