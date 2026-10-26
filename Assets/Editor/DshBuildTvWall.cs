using UnityEditor;
using UnityEngine;

/// <summary>
/// 第 3 批：迷宫终点改造。
/// 1) 禁用旧的特殊门 GT_Door；
/// 2) 用墙块补上 x=30 的门洞；
/// 3) 墙前放矮柜 + tv.fbx（屏幕中心对齐眼高）；
/// 4) 柜面加两个钥匙槽（软盘 / 光盘）。
/// </summary>
public static class DshBuildTvWall
{
    const string TvPath = "Assets/3D/Start/tv.fbx";
    const string MatDir = "Assets/Materials/Interior/TV";

    [MenuItem("Tools/DSH/TV/Build TV Wall")]
    public static void Build()
    {
        // ---- 1. 旧门 ----
        GameObject door = GameObject.Find("LevelProps/GT_Door");
        if (door != null)
        {
            door.SetActive(false);
            Debug.Log("[TV] disabled LevelProps/GT_Door");
        }

        // ---- 2. 补墙（门洞 z 8.2~9.8，高 2.2，墙厚 0.24）----
        GameObject oldPatch = GameObject.Find("TV_WallPatch");
        if (oldPatch != null) Object.DestroyImmediate(oldPatch);

        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "TV_WallPatch";
        wall.transform.position = new Vector3(30f, 1.1f, 9f);
        wall.transform.localScale = new Vector3(0.24f, 2.2f, 1.6f);

        GameObject wallsRoot = GameObject.Find("MazeRoot/MazeArchitecture/Walls");
        if (wallsRoot != null)
        {
            MeshRenderer src = wallsRoot.GetComponent<MeshRenderer>();
            if (src != null) wall.GetComponent<MeshRenderer>().sharedMaterial = src.sharedMaterial;
        }

        // ---- 3. 矮柜 ----
        GameObject oldCab = GameObject.Find("TV_Cabinet");
        if (oldCab != null) Object.DestroyImmediate(oldCab);

        GameObject cab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cab.name = "TV_Cabinet";
        cab.transform.position = new Vector3(29.45f, 0.475f, 9f);
        cab.transform.localScale = new Vector3(0.9f, 0.95f, 1.3f);
        cab.GetComponent<MeshRenderer>().sharedMaterial = GetMaterial("TV_Cabinet", new Color(0.16f, 0.135f, 0.11f), 0.15f);

        // ---- 4. 电视 ----
        GameObject oldTv = GameObject.Find("TV_Ending");
        if (oldTv != null) Object.DestroyImmediate(oldTv);

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TvPath);
        if (prefab == null) { Debug.LogError($"[TV] prefab not found {TvPath}"); return; }

        GameObject tv = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        tv.name = "TV_Ending";
        tv.transform.position = new Vector3(29.52f, 0.95f, 9f);
        tv.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
        tv.transform.localScale = Vector3.one * 1.4f;

        Material body = GetMaterial("TV_Body", new Color(0.80f, 0.78f, 0.72f), 0.25f);
        Material screen = GetMaterial("TV_Screen", new Color(0.02f, 0.03f, 0.03f), 0.1f);

        foreach (Renderer r in tv.GetComponentsInChildren<Renderer>(true))
        {
            Debug.Log($"[TV] renderer {r.name} mats={r.sharedMaterials.Length}");
            bool isScreen = r.name.ToLower().Contains("screen");
            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = isScreen ? screen : body;
            r.sharedMaterials = mats;
        }

        // 交互指引点（挂在电视上，瞄准屏幕中心）
        GuidanceTarget gt = tv.GetComponent<GuidanceTarget>();
        if (gt == null) gt = tv.AddComponent<GuidanceTarget>();
        gt.id = "tv";
        gt.label = "THE SCREEN";
        gt.kind = GuidanceKind.Interactable;
        gt.heightOffset = 0.535f;
        gt.discovered = true;
        gt.isKey = false;

        // ---- 5. 两个钥匙槽（柜面靠前，避开电视）----
        BuildSlot("Slot_Floppy", new Vector3(28.96f, 0.70f, 8.72f), new Color(0.35f, 0.75f, 1f));
        BuildSlot("Slot_Disc", new Vector3(28.96f, 0.70f, 9.28f), new Color(1f, 0.55f, 0.25f));

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("[TV] built tv wall");
    }

    static void BuildSlot(string name, Vector3 pos, Color color)
    {
        GameObject old = GameObject.Find(name);
        if (old != null) Object.DestroyImmediate(old);

        GameObject slot = GameObject.CreatePrimitive(PrimitiveType.Cube);
        slot.name = name;
        slot.transform.position = pos;
        slot.transform.localScale = new Vector3(0.03f, 0.12f, 0.12f);
        Object.DestroyImmediate(slot.GetComponent<Collider>());

        Material mat = GetMaterial("TV_" + name, color, 0.2f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", color * 1.6f);
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        slot.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    static Material GetMaterial(string name, Color color, float smoothness)
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (!AssetDatabase.IsValidFolder("Assets/Materials/Interior")) AssetDatabase.CreateFolder("Assets/Materials", "Interior");
        if (!AssetDatabase.IsValidFolder(MatDir)) AssetDatabase.CreateFolder("Assets/Materials/Interior", "TV");

        string path = $"{MatDir}/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(lit) { name = name };
            mat.color = color;
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", 0f);
            AssetDatabase.CreateAsset(mat, path);
        }

        return mat;
    }
}
