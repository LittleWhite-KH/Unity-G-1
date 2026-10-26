using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Start 场景重排：去掉桌子，把电视当显示器放在低台上，
/// 键盘摆在电视前地面，鼠标在右侧，水杯/软盘/光盘散在旁边。
/// </summary>
public static class DshBuildStartDesk2
{
    const string ScenePath = "Assets/Scenes/Start.unity";
    const string MatDir = "Assets/Materials/Interior/StartDesk";

    [MenuItem("Tools/DSH/Start Desk/Build Monitor Layout")]
    public static void Build()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // ---- 清理旧布局 ----
        foreach (string name in new[] { "Start_Desk", "Desk_Keyboard", "Desk_Cup", "Desk_Floppy", "Desk_Disc", "StartDesk_Lamp", "Start_MonitorStand", "Start_Mouse", "Start_Cup", "Start_Floppy", "Start_Disc", "Start_Keyboard" })
        {
            GameObject old = GameObject.Find(name);
            if (old != null) Object.DestroyImmediate(old);
        }

        Material wood = Mat("Desk_Wood", new Color(0.32f, 0.22f, 0.15f), 0.35f);
        Material dark = Mat("Desk_Plastic", new Color(0.12f, 0.12f, 0.13f), 0.4f);
        Material cupMat = Mat("Desk_Cup", new Color(0.86f, 0.86f, 0.84f), 0.6f);
        Material keyMat = Mat("Desk_Keyboard", new Color(0.78f, 0.76f, 0.70f), 0.4f);
        Material floppyMat = Mat("Desk_Floppy", new Color(0.08f, 0.08f, 0.09f), 0.35f);
        Material discMat = Mat("Desk_Disc", new Color(0.55f, 0.57f, 0.6f), 0.7f);

        // ---- 电视当显示器：放到 0.35m 低台上 ----
        GameObject tv = GameObject.Find("tv") ?? FindAnyTv();
        if (tv != null)
        {
            tv.transform.position = new Vector3(0f, 0.35f, 0f);
            tv.transform.rotation = Quaternion.identity;
            Debug.Log($"[Start2] tv -> {tv.transform.position}");
        }
        else
        {
            Debug.LogWarning("[Start2] tv instance not found (check hierarchy)");
        }

        GameObject stand = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stand.name = "Start_MonitorStand";
        stand.transform.position = new Vector3(0f, 0.175f, 0f);
        stand.transform.localScale = new Vector3(0.86f, 0.32f, 0.52f);
        Object.DestroyImmediate(stand.GetComponent<Collider>());
        stand.GetComponent<MeshRenderer>().sharedMaterial = dark;

        // ---- 键盘（电视正前方地面，略偏左）----
        BuildKeyboard(new Vector3(-0.18f, 0.012f, 0.53f), keyMat);

        // ---- 鼠标（键盘右侧）----
        BuildMouse(new Vector3(-0.55f, 0.02f, 0.50f), dark);

        // ---- 水杯 ----
        BuildCup(new Vector3(0.58f, 0f, 0.40f), cupMat);

        // ---- 软盘 / 光盘 ----
        Place("Assets/Models/Interior/Floppy.fbx", new Vector3(-0.18f, 0.012f, 0.69f), 0.13f, floppyMat, "Start_Floppy");
        Place("Assets/Models/Interior/Vinyl.fbx", new Vector3(0.24f, 0.012f, 0.66f), 0.16f, discMat, "Start_Disc");

        // ---- 相机：低机位俯视显示器 + 键鼠 ----
        GameObject camGo = GameObject.Find("Main Camera");
        if (camGo != null)
        {
            camGo.transform.position = new Vector3(0.02f, 1.22f, 2.20f);
            camGo.transform.LookAt(new Vector3(0.02f, 0.46f, 0.32f));
            Debug.Log("[Start2] camera adjusted");
        }

        // ---- 暖光 ----
        GameObject lamp = new GameObject("StartDesk_Lamp");
        Light l = lamp.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = 4.5f;
        l.intensity = 6f;
        l.color = new Color(1f, 0.80f, 0.55f);
        lamp.transform.position = new Vector3(0.15f, 1.55f, 0.95f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Start2] monitor layout built");
    }

    static GameObject FindAnyTv()
    {
        foreach (GameObject go in GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go.name.ToLower().Contains("tv")) return go;
        }
        return null;
    }

    static void BuildCup(Vector3 pos, Material mat)
    {
        GameObject cup = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cup.name = "Start_Cup";
        cup.transform.position = new Vector3(pos.x, 0.062f, pos.z);
        cup.transform.localScale = new Vector3(0.075f, 0.062f, 0.075f);
        Object.DestroyImmediate(cup.GetComponent<Collider>());
        cup.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }
    static void BuildKeyboard(Vector3 center, Material mat)
    {
        GameObject kb = new GameObject("Start_Keyboard");
        Box(kb.transform, "Base", center + new Vector3(0f, 0.012f, 0f), new Vector3(0.46f, 0.024f, 0.16f), mat);

        for (int row = 0; row < 4; row++)
        {
            for (int col = 0; col < 14; col++)
            {
                Vector3 cp = center + new Vector3(-0.20f + col * 0.0305f, 0.028f, -0.048f + row * 0.030f);
                Box(kb.transform, "Key", cp, new Vector3(0.022f, 0.008f, 0.020f), mat);
            }
        }
    }

    static void BuildMouse(Vector3 center, Material mat)
    {
        GameObject mouse = new GameObject("Start_Mouse");

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        body.name = "Body";
        body.transform.SetParent(mouse.transform, true);
        body.transform.position = center + new Vector3(0f, 0.018f, 0f);
        body.transform.localScale = new Vector3(0.065f, 0.036f, 0.10f);
        Object.DestroyImmediate(body.GetComponent<Collider>());
        body.GetComponent<MeshRenderer>().sharedMaterial = mat;

        Box(mouse.transform, "Wheel", center + new Vector3(0f, 0.032f, 0.008f), new Vector3(0.012f, 0.012f, 0.018f), mat);

        // 线
        Box(mouse.transform, "Cable", center + new Vector3(0f, 0.006f, -0.14f), new Vector3(0.008f, 0.006f, 0.18f), mat);
    }

    static void Place(string prefabPath, Vector3 pos, float targetSize, Material mat, string name)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) { Debug.LogWarning($"[Start2] missing {prefabPath}"); return; }

        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = name;

        Renderer[] rs = go.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) return;

        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);

        float maxDim = Mathf.Max(b.size.x, b.size.y, b.size.z);
        if (maxDim > 0.0001f) go.transform.localScale = Vector3.one * (targetSize / maxDim);

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
