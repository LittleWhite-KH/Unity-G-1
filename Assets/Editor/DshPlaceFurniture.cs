using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 把 low-poly 家具包里的几件摆进迷宫（第一批）。
/// 用已有家具/道具附近的安全位置，避免穿墙。
/// </summary>
public static class DshPlaceFurniture
{
    const string FbxPath = "Assets/Models/Interior/LowPolyFurniture.fbx";
    const string ParentName = "Interior_Furniture";

    [MenuItem("Tools/DSH/Furniture/Place Batch 1")]
    public static void PlaceBatch1()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        if (prefab == null) { Debug.LogError("[Furn] lowpoly fbx missing"); return; }

        GameObject source = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        source.name = "TMP_FurnitureSource";
        source.SetActive(false);

        Transform parent = GetParent();

        // name, 世界位置, 朝向(度), 目标尺寸(0=保持原尺寸)
        var items = new List<(string, Vector3, float, float)>
        {
            ("Base_Low",    new Vector3(-29.8f, 0f, 1.1f), 180f, 0f),
            ("Cube.008",    new Vector3(1.00f, 0f, 10.25f), 160f, 0f),
            ("Cube.008",    new Vector3(-19.05f, 0f, 4.05f), 200f, 0f),
            ("Cube.017",    new Vector3(11.60f, 0f, -1.40f), 0f, 0f),
            ("Cube.005",    new Vector3(20.00f, 0f, -7.40f), 90f, 0f),
            ("Cube.009",    new Vector3(-14.00f, 0f, -0.60f), 0f, 0f),
        };

        for (int i = 0; i < items.Count; i++)
        {
            var (name, pos, yaw, targetSize) = items[i];
            Transform child = FindDeep(source.transform, name);
            if (child == null) { Debug.LogWarning($"[Furn] {name} not found"); continue; }

            GameObject go = Object.Instantiate(child.gameObject);
            go.name = "Furn_" + name.Replace(".", "_") + "_" + i;
            go.transform.SetParent(parent, true);
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            // low-poly 包里的缩放不统一：保留原始比例，只把最大边限制到 2m 以内
            Bounds pre = GetBounds(go);
            float preMax = Mathf.Max(pre.size.x, pre.size.y, pre.size.z);
            if (preMax > 2.0f)
            {
                go.transform.localScale *= 2.0f / preMax;
            }

            if (targetSize > 0.01f)
            {
                Bounds b0 = GetBounds(go);
                float maxDim = Mathf.Max(b0.size.x, b0.size.y, b0.size.z);
                if (maxDim > 0.01f) go.transform.localScale *= targetSize / maxDim;
            }

            Bounds b = GetBounds(go);
            go.transform.position = new Vector3(pos.x, pos.y - b.min.y, pos.z);

            ApplyMaterial(go, i);
            AddCollider(go);
            Debug.Log($"[Furn] placed {go.name} at {go.transform.position} size={GetBounds(go).size}");
        }

        Object.DestroyImmediate(source);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log("[Furn] batch 1 done");
    }

    [MenuItem("Tools/DSH/Furniture/Place Batch 2")]
    public static void PlaceBatch2()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        if (prefab == null) { Debug.LogError("[Furn] lowpoly fbx missing"); return; }

        GameObject source = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        source.name = "TMP_FurnitureSource2";
        source.SetActive(false);

        Transform parent = GetParent();

        var items = new List<(string, Vector3, float, float)>
        {
            ("Cube.010", new Vector3(-29.4f, 0f, -2.6f), 0f, 0f),
            ("Cube.018", new Vector3(10.95f, 0f, -1.85f), 0f, 0f),
            ("Cube.013", new Vector3(20.05f, 0f, -8.55f), 40f, 0f),
            ("Cube.013", new Vector3(21.95f, 0f, -8.55f), -40f, 0f),
            ("Cube.011", new Vector3(-4.60f, 0f, 10.05f), 0f, 0f),
            ("Cube.019", new Vector3(-33.20f, 0f, 4.60f), 90f, 0f),
            ("Cube.012", new Vector3(28.35f, 0f, 10.40f), 0f, 0f),
            ("Cylinder.002", new Vector3(12.60f, 0f, -4.60f), 0f, 0f),
        };

        for (int i = 0; i < items.Count; i++)
        {
            var (name, pos, yaw, targetSize) = items[i];
            Transform child = FindDeep(source.transform, name);
            if (child == null) { Debug.LogWarning($"[Furn] {name} not found"); continue; }

            GameObject go = Object.Instantiate(child.gameObject);
            go.name = "Furn2_" + name.Replace(".", "_") + "_" + i;
            go.transform.SetParent(parent, true);
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            // low-poly 包里的缩放不统一：保留原始比例，只把最大边限制到 2m 以内
            Bounds pre = GetBounds(go);
            float preMax = Mathf.Max(pre.size.x, pre.size.y, pre.size.z);
            if (preMax > 2.0f)
            {
                go.transform.localScale *= 2.0f / preMax;
            }

            Bounds b = GetBounds(go);
            go.transform.position = new Vector3(pos.x, pos.y - b.min.y, pos.z);

            ApplyMaterial(go, i);
            AddCollider(go);
            Debug.Log($"[Furn2] placed {go.name} at {go.transform.position} size={GetBounds(go).size}");
        }

        Object.DestroyImmediate(source);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log("[Furn] batch 2 done");
    }
    [MenuItem("Tools/DSH/Furniture/Place Batch 3")]
    public static void PlaceBatch3()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        if (prefab == null) { Debug.LogError("[Furn] lowpoly fbx missing"); return; }

        GameObject source = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        source.name = "TMP_FurnitureSource3";
        source.SetActive(false);

        Transform parent = GetParent();

        var items = new List<(string, Vector3, float, float)>
        {
            ("Cube.012", new Vector3(-33.0f, 0f, 1.4f), 90f, 0f),
            ("Cube.013", new Vector3(0.65f, 0f, 8.35f), 140f, 0f),
            ("Cube.013", new Vector3(-21.0f, 0f, 2.05f), 220f, 0f),
            ("Cube.020", new Vector3(22.5f, 0f, -9.6f), 0f, 0f),
            ("Cube.012", new Vector3(28.0f, 0f, 7.9f), 90f, 0f),
            ("Cube.015", new Vector3(8.0f, 0f, -4.6f), 90f, 0f),
            ("Cube.014", new Vector3(10.0f, 0f, 0.2f), 0f, 0f),
            ("Cube.016", new Vector3(-8.0f, 0f, 0.6f), 0f, 0f),
            ("Cylinder.001", new Vector3(-25.0f, 0f, -6.0f), 0f, 0f),
            ("Base_Low.002", new Vector3(17.0f, 0f, 6.1f), 180f, 0f),
        };

        for (int i = 0; i < items.Count; i++)
        {
            var (name, pos, yaw, targetSize) = items[i];
            Transform child = FindDeep(source.transform, name);
            if (child == null) { Debug.LogWarning($"[Furn] {name} not found"); continue; }

            GameObject go = Object.Instantiate(child.gameObject);
            go.name = "Furn3_" + name.Replace(".", "_") + "_" + i;
            go.transform.SetParent(parent, true);
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            // low-poly 包里的缩放不统一：保留原始比例，只把最大边限制到 2m 以内
            Bounds pre = GetBounds(go);
            float preMax = Mathf.Max(pre.size.x, pre.size.y, pre.size.z);
            if (preMax > 2.0f)
            {
                go.transform.localScale *= 2.0f / preMax;
            }

            Bounds b = GetBounds(go);
            go.transform.position = new Vector3(pos.x, pos.y - b.min.y, pos.z);

            ApplyMaterial(go, i);
            AddCollider(go);
            Debug.Log($"[Furn3] placed {go.name} at {go.transform.position} size={GetBounds(go).size}");
        }

        Object.DestroyImmediate(source);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log("[Furn] batch 3 done");
    }
    [MenuItem("Tools/DSH/Furniture/Place Batch 4")]
    public static void PlaceBatch4()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        if (prefab == null) { Debug.LogError("[Furn] lowpoly fbx missing"); return; }

        GameObject source = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        source.name = "TMP_FurnitureSource4";
        source.SetActive(false);

        Transform parent = GetParent();

        var items = new List<(string, Vector3, float, float)>
        {
            ("Cube.012", new Vector3(24.0f, 0f, 3.2f), 0f, 0f),
            ("Cube.015", new Vector3(26.0f, 0f, -3.0f), 90f, 0f),
            ("Cube.016", new Vector3(14.0f, 0f, -8.0f), 0f, 0f),
            ("Cube.013", new Vector3(18.0f, 0f, 3.0f), 30f, 0f),
            ("Cube.010", new Vector3(30.0f, 0f, 2.0f), 0f, 0f),
            ("Base_Low.001", new Vector3(6.0f, 0f, 6.0f), 180f, 0f),
            ("Cube.013", new Vector3(-6.0f, 0f, -6.0f), 90f, 0f),
            ("Cube.012", new Vector3(-2.0f, 0f, 3.0f), 180f, 0f),
            ("Cube.020", new Vector3(4.0f, 0f, -3.0f), 0f, 0f),
            ("Cube.010", new Vector3(12.0f, 0f, 4.0f), 0f, 0f),
        };

        for (int i = 0; i < items.Count; i++)
        {
            var (name, pos, yaw, targetSize) = items[i];
            Transform child = FindDeep(source.transform, name);
            if (child == null) { Debug.LogWarning($"[Furn] {name} not found"); continue; }

            GameObject go = Object.Instantiate(child.gameObject);
            go.name = "Furn4_" + name.Replace(".", "_") + "_" + i;
            go.transform.SetParent(parent, true);
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            // low-poly 包里的缩放不统一：保留原始比例，只把最大边限制到 2m 以内
            Bounds pre = GetBounds(go);
            float preMax = Mathf.Max(pre.size.x, pre.size.y, pre.size.z);
            if (preMax > 2.0f)
            {
                go.transform.localScale *= 2.0f / preMax;
            }

            Bounds b = GetBounds(go);
            go.transform.position = new Vector3(pos.x, pos.y - b.min.y, pos.z);

            ApplyMaterial(go, i);
            AddCollider(go);
            Debug.Log($"[Furn4] placed {go.name} at {go.transform.position} size={GetBounds(go).size}");
        }

        Object.DestroyImmediate(source);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log("[Furn] batch 4 done");
    }
    [MenuItem("Tools/DSH/Furniture/Clear All")]
    public static void ClearAll()
    {
        GameObject root = GameObject.Find("MazeRoot");
        Transform parent = root != null ? root.transform.Find(ParentName) : null;
        if (parent == null) { Debug.Log("[Furn] nothing to clear"); return; }

        int n = 0;
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(parent.GetChild(i).gameObject);
            n++;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log($"[Furn] cleared {n} furniture objects");
    }
    [MenuItem("Tools/DSH/Furniture/Replace Odd Shapes")]
    public static void ReplaceOdd()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        if (prefab == null) { Debug.LogError("[Furn] lowpoly fbx missing"); return; }

        GameObject root = GameObject.Find("MazeRoot");
        Transform parent = root != null ? root.transform.Find(ParentName) : null;
        if (parent == null) { Debug.Log("[Furn] no furniture parent"); return; }

        // 形状奇怪的低模：栏杆/衣架/复合件 -> 换成规整沙发/柜子/茶几
        var replaceMap = new System.Collections.Generic.Dictionary<string, string>
        {
            { "Cube_015", "Base_Low.001" },
            { "Cube_016", "Cube.009" },
            { "Cube_012", "Cube.018" },
            { "Cube_011", "Base_Low.002" },
        };

        var pending = new List<(string replaceName, Vector3 pos, Quaternion rot)>();
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            foreach (var kv in replaceMap)
            {
                if (!child.name.Contains(kv.Key)) continue;

                pending.Add((kv.Value, child.position, child.rotation));
                Object.DestroyImmediate(child.gameObject);
                break;
            }
        }

        if (pending.Count == 0) { Debug.Log("[Furn] nothing to replace"); return; }

        GameObject source = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        source.name = "TMP_FurnitureSourceReplace";
        source.SetActive(false);

        int index = 0;
        foreach (var (replaceName, pos, rot) in pending)
        {
            Transform src = FindDeep(source.transform, replaceName);
            if (src == null) { Debug.LogWarning($"[Furn] replace target {replaceName} missing"); continue; }

            GameObject go = Object.Instantiate(src.gameObject);
            go.name = "FurnR_" + replaceName.Replace(".", "_") + "_" + index;
            index++;
            go.transform.SetParent(parent, true);
            go.transform.rotation = rot;

            Bounds pre = GetBounds(go);
            float preMax = Mathf.Max(pre.size.x, pre.size.y, pre.size.z);
            if (preMax > 2.0f) go.transform.localScale *= 2.0f / preMax;

            Bounds b = GetBounds(go);
            go.transform.position = new Vector3(pos.x, 0f - b.min.y + 0f, pos.z);

            ApplyMaterial(go, 0);
            AddCollider(go);
        }

        Object.DestroyImmediate(source);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log($"[Furn] replaced {pending.Count} odd shapes");
    }
    [MenuItem("Tools/DSH/Furniture/Place Batch 5")]
    public static void PlaceBatch5()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        if (prefab == null) { Debug.LogError("[Furn] lowpoly fbx missing"); return; }

        GameObject source = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        source.name = "TMP_FurnitureSource5";
        source.SetActive(false);

        Transform parent = GetParent();

        var items = new List<(string, Vector3, float, float)>
        {
            ("Cube.008", new Vector3(-27.5f, 0f, 2.5f), 30f, 0f),
            ("Cube.018", new Vector3(-28.0f, 0f, -6.0f), 0f, 0f),
            ("Cube.010", new Vector3(-22.0f, 0f, 6.5f), 0f, 0f),
            ("Cube.008", new Vector3(-16.0f, 0f, 2.0f), 210f, 0f),
            ("Cube.013", new Vector3(7.0f, 0f, -6.5f), 120f, 0f),
            ("Cube.019", new Vector3(4.0f, 0f, 8.0f), 0f, 0f),
            ("Base_Low.001", new Vector3(13.0f, 0f, 7.0f), 90f, 0f),
            ("Cube.008", new Vector3(18.0f, 0f, -3.0f), 250f, 0f),
            ("Cube.020", new Vector3(26.0f, 0f, 6.0f), 0f, 0f),
            ("Cube.018", new Vector3(31.0f, 0f, -2.0f), 90f, 0f),
        };

        for (int i = 0; i < items.Count; i++)
        {
            var (name, pos, yaw, targetSize) = items[i];
            Transform child = FindDeep(source.transform, name);
            if (child == null) { Debug.LogWarning($"[Furn] {name} not found"); continue; }

            GameObject go = Object.Instantiate(child.gameObject);
            go.name = "Furn5_" + name.Replace(".", "_") + "_" + i;
            go.transform.SetParent(parent, true);
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            Bounds pre = GetBounds(go);
            float preMax = Mathf.Max(pre.size.x, pre.size.y, pre.size.z);
            if (preMax > 2.0f) go.transform.localScale *= 2.0f / preMax;

            Bounds b = GetBounds(go);
            go.transform.position = new Vector3(pos.x, pos.y - b.min.y, pos.z);

            ApplyMaterial(go, i);
            AddCollider(go);
            Debug.Log($"[Furn5] placed {go.name} at {go.transform.position} size={GetBounds(go).size}");
        }

        Object.DestroyImmediate(source);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log("[Furn] batch 5 done");
    }
    static Transform GetParent()
    {
        GameObject root = GameObject.Find("MazeRoot");
        if (root == null) root = new GameObject("MazeRoot");

        Transform existing = root.transform.Find(ParentName);
        if (existing != null) return existing;

        GameObject go = new GameObject(ParentName);
        go.transform.SetParent(root.transform, false);
        return go.transform;
    }

    static Transform FindDeep(Transform root, string name)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == name) return t;
        }
        return null;
    }

    static Bounds GetBounds(GameObject go)
    {
        Renderer[] rs = go.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);

        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }

    static void ApplyMaterial(GameObject go, int index)
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        Material mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Interior/Furn/InteriorFurn.mat");
        if (mat == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Materials/Interior")) AssetDatabase.CreateFolder("Assets/Materials", "Interior");
            if (!AssetDatabase.IsValidFolder("Assets/Materials/Interior/Furn")) AssetDatabase.CreateFolder("Assets/Materials/Interior", "Furn");
            mat = new Material(lit) { name = "InteriorFurn" };
            mat.color = new Color(0.36f, 0.30f, 0.25f);
            mat.SetFloat("_Smoothness", 0.25f);
            AssetDatabase.CreateAsset(mat, "Assets/Materials/Interior/Furn/InteriorFurn.mat");
            AssetDatabase.SaveAssets();
        }

        foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
        {
            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            r.sharedMaterials = mats;
        }
    }

    static void AddCollider(GameObject go)
    {
        if (go.GetComponent<Collider>() != null) return;
        Bounds b = GetBounds(go);

        BoxCollider bc = go.AddComponent<BoxCollider>();
        bc.center = go.transform.InverseTransformPoint(b.center);
        bc.size = b.size;
    }
}
