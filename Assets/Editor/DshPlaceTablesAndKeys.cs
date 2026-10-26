using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 把素材库的桌子（TableDesk.fbx）摆到两台旧电脑下面，并把密钥移到电脑前桌面、玩家顺手能拿的位置。
/// - 桌子长边沿 Z（垂直于电脑朝向），缩放只放大平面、高度保持 0.84
/// - 电脑抬到桌面高度
/// - Key_A/Key_B 移到电脑正前方桌沿
/// </summary>
public static class DshPlaceTablesAndKeys
{
    const string FbxPath = "Assets/Models/Interior/TableDesk.fbx";
    const string MatPath = "Assets/Materials/Interior/Furn/TableDesk.mat";
    const string OutPath = "E:/DSH_Unity/tmp-read/tables-keys.txt";

    const float TableScalePc1 = 2.2f;   // 平面放大（长 3.19 / 深 1.32）
    const float TableScalePc2 = 1.3f;   // 平面放大（长 1.89 / 深 0.78）
    const float Pc1Scale = 2.0f;        // 用户原来 2.93，桌深撑不住，收到 2.0
    const float Pc2Scale = 1.4f;
    const float KeyHeightAboveTable = 0.11f;

    [MenuItem("Tools/DSH/Tune/Place Tables And Keys")]
    public static void Apply()
    {
        var sb = new StringBuilder();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        if (prefab == null) { Debug.LogError("[Tables] cannot load " + FbxPath); return; }

        var maze = GameObject.Find("MazeRoot").transform;
        var props = FindDeep(maze, "MazeProps");
        var mat = GetOrCreateMaterial();

        // ---- PC1 朝东，PC2 朝西 ----
        var pc1 = FindDeep(maze, "OldPC_1_AppleII");
        var pc2 = FindDeep(maze, "OldPC_2_AppleII");
        var keyA = FindDeep(GameObject.Find("Keys").transform, "Key_A");
        var keyB = FindDeep(GameObject.Find("Keys").transform, "Key_B");

        // 固定原始站位：否则重复执行时桌子/电脑会跟着上一次的偏移累积漂移
        pc1.position = new Vector3(1.8777f, 0f, 8.9564f);
        pc2.position = new Vector3(-19.85f, 0f, 3f);
        Physics.SyncTransforms();

        PlaceTable(sb, prefab, props, mat, "Table_PC1", pc1, TableScalePc1, out float top1, out float half1);
        PlaceTable(sb, prefab, props, mat, "Table_PC2", pc2, TableScalePc2, out float top2, out float half2);

        // 记录桌面中心（电脑原本位置），电脑要往桌后挪，好在前沿给钥匙留位置
        Vector3 tableCenter1 = pc1.position;
        Vector3 tableCenter2 = pc2.position;

        // 电脑：缩放 + 抬到桌面 + 往桌后挪一点，让电脑不会盖住桌前沿的钥匙
        pc1.localScale = Vector3.one * Pc1Scale;
        Vector3 fwd1 = pc1.forward;
        pc1.position = tableCenter1 - fwd1 * 0.18f + Vector3.up * (top1 - tableCenter1.y);
        pc2.localScale = Vector3.one * Pc2Scale;
        Vector3 fwd2 = pc2.forward;
        pc2.position = tableCenter2 - fwd2 * 0.22f + Vector3.up * (top2 - tableCenter2.y);
        Physics.SyncTransforms();

        // 密钥：桌前沿正前方，玩家站在桌前顺手能拿
        MoveKey(keyA, tableCenter1 + fwd1 * (half1 - 0.06f), top1 + KeyHeightAboveTable, "Key_A");
        MoveKey(keyB, tableCenter2 + fwd2 * (half2 - 0.06f), top2 + KeyHeightAboveTable, "Key_B");

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        sb.AppendLine($"pc1 scale={Pc1Scale} tableTop={top1:F3} keyA={keyA.position}");
        sb.AppendLine($"pc2 scale={Pc2Scale} tableTop={top2:F3} keyB={keyB.position}");

        Directory.CreateDirectory(Path.GetDirectoryName(OutPath));
        File.WriteAllText(OutPath, sb.ToString());
        Debug.Log("[Tables] " + sb);
    }

    static void MoveKey(Transform key, Vector3 worldPos, float y, string name)
    {
        key.position = new Vector3(worldPos.x, y, worldPos.z);
        Debug.Log($"[Tables] {name} -> {key.position}");
    }

    static void PlaceTable(StringBuilder sb, GameObject prefab, Transform parent, Material mat, string name,
        Transform pc, float planarScale, out float topHeight, out float halfDepth)
    {
        var old = FindChild(parent, name);
        if (old != null) Object.DestroyImmediate(old.gameObject);

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        // 保留 FBX 根节点自带的轴向旋转（重置成 identity 会让桌子躺倒）
        Vector3 localUp = go.transform.InverseTransformDirection(Vector3.up);
        Vector3 scale = Vector3.one * planarScale;
        if (Mathf.Abs(localUp.y) > 0.9f) scale.y = 1f;
        else if (Mathf.Abs(localUp.x) > 0.9f) scale.x = 1f;
        else scale.z = 1f;
        go.transform.localScale = scale;
        Physics.SyncTransforms();

        // 让长边沿 Z（垂直于电脑朝向）
        var b = BoundsOf(go);
        if (b.size.x > b.size.z)
        {
            go.transform.Rotate(0f, 90f, 0f, Space.World);
            Physics.SyncTransforms();
            b = BoundsOf(go);
        }

        // 落位：桌面中心对准电脑 x/z，底部贴 y=0
        Vector3 delta = new Vector3(pc.position.x - b.center.x, -b.min.y, pc.position.z - b.center.z);
        go.transform.position += delta;
        Physics.SyncTransforms();
        b = BoundsOf(go);

        // 材质
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            r.sharedMaterials = mats;
        }

        // 碰撞（整体一个盒子，从地面到桌面）
        var box = go.GetComponent<BoxCollider>();
        if (box == null) box = go.AddComponent<BoxCollider>();
        var lb = LocalBounds(go.transform);
        box.center = lb.center;
        box.size = lb.size;

        topHeight = b.max.y;
        halfDepth = b.size.x * 0.5f;
        sb.AppendLine($"{name}: size={b.size} top={b.max.y:F3} bounds={b.center}");
    }

    static Bounds BoundsOf(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        var b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }

    static Bounds LocalBounds(Transform root)
    {
        bool init = false;
        Bounds acc = default;
        var toLocal = root.worldToLocalMatrix;
        foreach (var r in root.GetComponentsInChildren<Renderer>())
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

    static Material GetOrCreateMaterial()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
        if (existing != null) return existing;
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        var m = new Material(shader) { name = "TableDesk" };
        var c = new Color(0.38f, 0.26f, 0.17f);
        m.SetColor("_BaseColor", c);
        m.SetColor("_Color", c);
        m.SetFloat("_Smoothness", 0.2f);
        m.SetFloat("_Metallic", 0f);
        AssetDatabase.CreateAsset(m, MatPath);
        return m;
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

    static Transform FindChild(Transform root, string name)
    {
        foreach (Transform c in root) if (c.name == name) return c;
        return null;
    }

    [MenuItem("Tools/DSH/Tune/Remove Tables")]
    public static void Remove()
    {
        var maze = GameObject.Find("MazeRoot").transform;
        var props = FindDeep(maze, "MazeProps");
        foreach (var n in new[] { "Table_PC1", "Table_PC2" })
        {
            var t = FindChild(props, n);
            if (t != null) Object.DestroyImmediate(t.gameObject);
        }
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("[Tables] removed");
    }
}
