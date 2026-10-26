using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 按 blend (maze-blockout-furniture-1026.blend / Interior_New) 重新摆放地图家具：
/// 实例化 MazeInterior1026.fbx，根节点 180°Y 还原 Blender 坐标，
/// 换成 URP Lit 近似材质，按顶层家具加 BoxCollider，停用旧的程序化家具。
/// </summary>
public static class DshImportBlendFurniture
{
    const string FbxPath = "Assets/Models/Interior/MazeInterior1026.fbx";
    const string MatDir = "Assets/Materials/Interior/BlendFurn";
    const string RootName = "Interior_FromBlend";
    const string OldFurniture = "MazeRoot/Interior_Furniture";
    const string OutPath = "E:/DSH_Unity/tmp-read/import-furn.txt";

    static readonly Dictionary<string, Color> Colors = new Dictionary<string, Color>
    {
        { "textile", new Color(0.36f, 0.32f, 0.30f) },
        { "textile.001", new Color(0.38f, 0.34f, 0.31f) },
        { "wood", new Color(0.42f, 0.30f, 0.20f) },
        { "wood.001", new Color(0.42f, 0.30f, 0.20f) },
        { "wood2", new Color(0.38f, 0.26f, 0.17f) },
        { "wood3", new Color(0.46f, 0.34f, 0.23f) },
        { "wood_legs", new Color(0.30f, 0.21f, 0.14f) },
        { "ratan", new Color(0.55f, 0.44f, 0.30f) },
        { "M_Frame", new Color(0.16f, 0.16f, 0.17f) },
        { "M_Frame_02", new Color(0.20f, 0.20f, 0.21f) },
        { "M_Frame_03", new Color(0.12f, 0.12f, 0.13f) },
        { "M_Desk", new Color(0.40f, 0.29f, 0.20f) },
        { "M_Cloth", new Color(0.30f, 0.30f, 0.33f) },
        { "01", new Color(0.72f, 0.71f, 0.67f) },
        { "02", new Color(0.69f, 0.68f, 0.64f) },
        { "03", new Color(0.75f, 0.74f, 0.70f) },
        { "Carpet_A", new Color(0.42f, 0.26f, 0.22f) },
        { "Carpet_A_Ends", new Color(0.34f, 0.20f, 0.17f) },
        { "Chairs_A", new Color(0.42f, 0.32f, 0.24f) },
        { "Radiators", new Color(0.78f, 0.78f, 0.76f) },
        { "Trims_Painted_A", new Color(0.82f, 0.81f, 0.78f) },
        { "Glass_Cupboard_A", new Color(0.62f, 0.70f, 0.72f) },
        { "Table_Kitchen_A", new Color(0.55f, 0.42f, 0.28f) },
        { "1", new Color(0.60f, 0.58f, 0.55f) },
        { "Lit", new Color(0.45f, 0.33f, 0.22f) },
    };

    [MenuItem("Tools/DSH/Blend/Import Furniture From Blend")]
    public static void Run()
    {
        var sb = new StringBuilder();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        if (prefab == null) { Debug.LogError("[FurnImport] cannot load " + FbxPath); return; }

        var mazeRoot = GameObject.Find("MazeRoot");
        if (mazeRoot == null) { Debug.LogError("[FurnImport] MazeRoot missing"); return; }

        var old = FindChild(mazeRoot.transform, RootName);
        if (old != null) Object.DestroyImmediate(old.gameObject);

        if (!AssetDatabase.IsValidFolder(MatDir))
            AssetDatabase.CreateFolder("Assets/Materials/Interior", "BlendFurn");

        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        inst.name = RootName;
        inst.transform.SetParent(mazeRoot.transform, false);
        inst.transform.localPosition = Vector3.zero;
        inst.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        inst.transform.localScale = Vector3.one;

        // ---- materials ----
        var matCache = new Dictionary<string, Material>();
        int rendererCount = 0;
        foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                var src = mats[i];
                if (src == null) continue;
                var key = src.name;
                if (!matCache.TryGetValue(key, out var m))
                {
                    m = CreateMaterial(key);
                    matCache[key] = m;
                }
                mats[i] = m;
            }
            r.sharedMaterials = mats;
            rendererCount++;
        }

        // ---- colliders ----
        int colCount = 0;
        foreach (Transform child in inst.transform)
        {
            if (child.name.StartsWith("Carpet")) continue;
            var rends = child.GetComponentsInChildren<Renderer>(true);
            if (rends.Length == 0) continue;
            bool has = false;
            foreach (var r in rends) if (r.GetComponent<Collider>() != null) has = true;
            if (has) continue;

            var lb = LocalBounds(child);
            if (lb.size.sqrMagnitude < 1e-6f) continue;
            // 底部贴地：低于 0 的部分裁掉，避免薄板沉入地板
            if (lb.min.y < 0f)
            {
                float d = -lb.min.y;
                lb.size = new Vector3(lb.size.x, Mathf.Max(0.02f, lb.size.y - d), lb.size.z);
                lb.center = new Vector3(lb.center.x, lb.center.y + d * 0.5f, lb.center.z);
            }
            var box = child.gameObject.AddComponent<BoxCollider>();
            box.center = lb.center;
            box.size = lb.size;
            colCount++;
        }

        // ---- disable old procedural furniture ----
        var oldFurn = FindChild(mazeRoot.transform, "Interior_Furniture");
        bool hadOld = oldFurn != null && oldFurn.gameObject.activeSelf;
        if (oldFurn != null) oldFurn.gameObject.SetActive(false);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        sb.AppendLine($"instanced {RootName}: children={inst.transform.childCount}, renderers={rendererCount}, collidersAdded={colCount}, materials={matCache.Count}");
        sb.AppendLine($"old furniture '{OldFurniture}' disabled (wasActive={hadOld})");
        foreach (var n in new[] { "Carpet_A", "SM_Bed_01", "Table_Kitchen_A", "sofa_classic_ch_models_", "03", "Stool_A", "Cupboard_Kitchen_A" })
        {
            var t = FindDeep(inst.transform, n);
            if (t != null) sb.AppendLine($"check {n}: world={t.position} bounds={BoundsOf(t)}");
            else sb.AppendLine($"check {n}: NOT FOUND");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(OutPath));
        File.WriteAllText(OutPath, sb.ToString());
        Debug.Log("[FurnImport] " + sb);
    }

    static Bounds LocalBounds(Transform root)
    {
        bool init = false;
        Bounds acc = default;
        var toLocal = root.worldToLocalMatrix;
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            var mf = r.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;
            var mb = mf.sharedMesh.bounds;
            var m = toLocal * r.transform.localToWorldMatrix;
            for (int i = 0; i < 8; i++)
            {
                var p = mb.center + Vector3.Scale(mb.extents, new Vector3(
                    (i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var lp = m.MultiplyPoint3x4(p);
                if (!init) { acc = new Bounds(lp, Vector3.zero); init = true; }
                else acc.Encapsulate(lp);
            }
        }
        return acc;
    }

    static string BoundsOf(Transform t)
    {
        var rs = t.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return "-";
        var b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return $"c={b.center} s={b.size}";
    }

    static Transform FindChild(Transform root, string name)
    {
        foreach (Transform c in root) if (c.name == name) return c;
        return null;
    }

    static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform c in root)
        {
            var r = FindDeep(c, name);
            if (r != null) return r;
        }
        return null;
    }

    static Material CreateMaterial(string key)
    {
        string safe = key.Replace(".", "_").Replace(" ", "_");
        string path = $"{MatDir}/FurnBlend_{safe}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        var m = new Material(shader) { name = "FurnBlend_" + safe };
        Color c = Colors.TryGetValue(key, out var col) ? col : new Color(0.55f, 0.54f, 0.52f);
        m.SetColor("_BaseColor", c);
        m.SetColor("_Color", c);
        m.SetFloat("_Smoothness", 0.15f);
        m.SetFloat("_Metallic", 0f);
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    [MenuItem("Tools/DSH/Blend/Remove Blend Furniture")]
    public static void Remove()
    {
        var mazeRoot = GameObject.Find("MazeRoot");
        var old = mazeRoot ? FindChild(mazeRoot.transform, RootName) : null;
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var oldFurn = mazeRoot ? FindChild(mazeRoot.transform, "Interior_Furniture") : null;
        if (oldFurn != null) oldFurn.gameObject.SetActive(true);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("[FurnImport] removed blend furniture, restored procedural furniture");
    }
}
