using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 在 Start（开始界面）里加一张温馨桌面：桌子 + 水杯 + 键盘 + 软盘 + 光盘。
/// 同时把相机稍微拉远，让桌面入镜。
/// </summary>
public static class DshBuildStartDesk
{
    const string ScenePath = "Assets/Scenes/Start.unity";
    const string MatDir = "Assets/Materials/Interior/StartDesk";

    [MenuItem("Tools/DSH/Start Desk/Build")]
    public static void Build()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Material wood = Mat("Desk_Wood", new Color(0.32f, 0.22f, 0.15f), 0.35f);
        Material cupMat = Mat("Desk_Cup", new Color(0.86f, 0.86f, 0.84f), 0.6f);
        Material keyMat = Mat("Desk_Keyboard", new Color(0.72f, 0.70f, 0.64f), 0.4f);
        Material floppyMat = Mat("Desk_Floppy", new Color(0.08f, 0.08f, 0.09f), 0.35f);
        Material discMat = Mat("Desk_Disc", new Color(0.55f, 0.57f, 0.6f), 0.7f);

        // ---- 桌子 ----
        GameObject oldDesk = GameObject.Find("Start_Desk");
        if (oldDesk != null) Object.DestroyImmediate(oldDesk);

        GameObject desk = new GameObject("Start_Desk");
        float topY = 0.74f;
        Box(desk.transform, "Top", new Vector3(1.38f, topY, 0.62f), new Vector3(1.25f, 0.07f, 0.74f), wood);

        float legH = topY - 0.03f;
        float lx = 0.60f, lz = 0.36f;
        for (int i = 0; i < 4; i++)
        {
            float x = 1.38f + ((i == 0 || i == 3) ? -lx : lx);
            float z = 0.62f + ((i == 0 || i == 1) ? -lz : lz);
            Box(desk.transform, "Leg" + i, new Vector3(x, legH / 2f, z), new Vector3(0.075f, legH, 0.075f), wood);
        }

        // ---- 桌上道具 ----
        Place("Assets/Models/Interior/Cup.fbx", new Vector3(0.95f, topY + 0.035f, 0.42f), 0.17f, cupMat, "Desk_Cup");
        BuildKeyboard(new Vector3(1.38f, topY + 0.035f, 0.66f), keyMat);
        Place("Assets/Models/Interior/Floppy.fbx", new Vector3(1.02f, topY + 0.035f, 0.92f), 0.17f, floppyMat, "Desk_Floppy");
        Place("Assets/Models/Interior/Vinyl.fbx", new Vector3(1.78f, topY + 0.035f, 0.76f), 0.22f, discMat, "Desk_Disc");

        // ---- 相机（稍微拉远，让桌面入镜）----
        GameObject camGo = GameObject.Find("Main Camera");
        if (camGo != null)
        {
            camGo.transform.position = new Vector3(0.10f, 1.55f, 3.05f);
            camGo.transform.LookAt(new Vector3(0.80f, 0.42f, 0.35f));
            Debug.Log("[StartDesk] camera adjusted");
        }

        // ---- 暖光 ----
        GameObject oldLamp = GameObject.Find("StartDesk_Lamp");
        if (oldLamp != null) Object.DestroyImmediate(oldLamp);

        GameObject lamp = new GameObject("StartDesk_Lamp");
        Light l = lamp.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = 4f;
        l.intensity = 5f;
        l.color = new Color(1f, 0.78f, 0.5f);
        lamp.transform.position = new Vector3(0.75f, 1.45f, 0.85f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[StartDesk] built");
    }

    static void Place(string prefabPath, Vector3 pos, float targetSize, Material mat, string name)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) { Debug.LogWarning($"[StartDesk] missing {prefabPath}"); return; }

        GameObject existing = GameObject.Find(name);
        if (existing != null) Object.DestroyImmediate(existing);

        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = name;

        Renderer[] rs = go.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) return;

        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);

        float maxDim = Mathf.Max(b.size.x, b.size.y, b.size.z);
        float scale = maxDim > 0.0001f ? targetSize / maxDim : 1f;
        go.transform.localScale = Vector3.one * scale;

        // 重新算一次缩放后的包围盒，把物体底部放到 pos.y、中心放到 pos.x/z
        b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        go.transform.position += new Vector3(pos.x - b.center.x, pos.y - b.min.y, pos.z - b.center.z);

        foreach (Renderer r in rs)
        {
            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            r.sharedMaterials = mats;
        }
    }

    static void BuildKeyboard(Vector3 pos, Material mat)
    {
        GameObject old = GameObject.Find("Desk_Keyboard");
        if (old != null) Object.DestroyImmediate(old);

        GameObject kb = new GameObject("Desk_Keyboard");
        Box(kb.transform, "Base", pos + new Vector3(0f, 0.012f, 0f), new Vector3(0.46f, 0.024f, 0.16f), mat);

        for (int row = 0; row < 4; row++)
        {
            for (int col = 0; col < 14; col++)
            {
                Vector3 cp = pos + new Vector3(-0.20f + col * 0.0305f, 0.028f, -0.052f + row * 0.032f);
                Box(kb.transform, "Key", cp, new Vector3(0.022f, 0.008f, 0.022f), mat);
            }
        }
    }
    static void Box(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, true);
        go.transform.position = pos;
        go.transform.localScale = scale;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    static Material Mat(string name, Color color, float smoothness)
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (!AssetDatabase.IsValidFolder("Assets/Materials/Interior")) AssetDatabase.CreateFolder("Assets/Materials", "Interior");
        if (!AssetDatabase.IsValidFolder(MatDir)) AssetDatabase.CreateFolder("Assets/Materials/Interior", "StartDesk");

        string path = $"{MatDir}/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(lit) { name = name };
            mat.color = color;
            mat.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(mat, path);
        }
        return mat;
    }
}
