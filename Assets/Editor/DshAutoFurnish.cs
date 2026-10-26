using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 在 6m 网格点上自动补家具：
///   - 用 CheckBox 检测该格中心 2×2m 范围内有没有墙/家具；
///   - 只在空位摆规整家具（沙发/椅子/茶几/柜子），并随机朝向；
///   - 全部贴地、加 BoxCollider。
/// </summary>
public static class DshAutoFurnish
{
    const string FbxPath = "Assets/Models/Interior/LowPolyFurniture.fbx";
    const string ParentName = "Interior_Furniture";

    static readonly string[] Pool =
    {
        "Base_Low", "Base_Low.001", "Base_Low.002",
        "Cube.010", "Cube.014", "Cube.017", "Cube.018", "Cube.019", "Cube.020",
    };

    [MenuItem("Tools/DSH/Furniture/Auto Furnish Grid")]
    public static void AutoFurnish()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        if (prefab == null) { Debug.LogError("[AutoFurn] fbx missing"); return; }

        GameObject source = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        source.name = "TMP_AutoFurnSource";
        source.SetActive(false);

        GameObject root = GameObject.Find("MazeRoot");
        Transform parent = root != null ? root.transform.Find(ParentName) : null;
        if (parent == null)
        {
            GameObject go = new GameObject(ParentName);
            go.transform.SetParent(root != null ? root.transform : null, false);
            parent = go.transform;
        }

        int placed = 0;
        int index = 0;

        for (int i = 0; i < 12; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                Vector3 center = new Vector3(-33f + 6f * i, 0f, -9f + 6f * j);

                // 该格中心 2.2×2.2m、离地 0.5~2.0m 的范围里不能有墙/家具
                if (Physics.CheckBox(center + Vector3.up * 1.2f, new Vector3(1.1f, 0.8f, 1.1f), Quaternion.identity))
                {
                    continue;
                }

                // 周围 2.5m 内不要已经有家具
                if (HasNearbyFurniture(parent, center, 2.5f)) continue;

                string name = Pool[index % Pool.Length];
                index++;

                Transform src = FindDeep(source.transform, name);
                if (src == null) continue;

                GameObject go = Object.Instantiate(src.gameObject);
                go.name = "FurnA_" + name.Replace(".", "_") + "_" + index;
                go.transform.SetParent(parent, true);
                go.transform.rotation = Quaternion.Euler(0f, (index * 67) % 360, 0f);

                Bounds pre = GetBounds(go);
                float preMax = Mathf.Max(pre.size.x, pre.size.y, pre.size.z);
                if (preMax > 2.0f) go.transform.localScale *= 2.0f / preMax;

                Bounds b = GetBounds(go);
                go.transform.position = new Vector3(center.x, -b.min.y, center.z);

                ApplyMaterial(go);
                AddCollider(go);
                placed++;
            }
        }

        Object.DestroyImmediate(source);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log($"[AutoFurn] placed {placed}");
    }

    static bool HasNearbyFurniture(Transform parent, Vector3 pos, float radius)
    {
        foreach (Transform child in parent)
        {
            Vector3 p = child.position;
            if (Vector3.Distance(new Vector3(p.x, 0f, p.z), new Vector3(pos.x, 0f, pos.z)) < radius)
            {
                return true;
            }
        }

        return false;
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

    static void ApplyMaterial(GameObject go)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Interior/Furn/InteriorFurn.mat");
        if (mat == null) return;

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
