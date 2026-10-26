using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 建 Ending 场景：白色虚空 + 放大 4.5 倍的电视（屏幕中心对齐眼高）+ 玩家起始机位。
/// 传送门/走廊/门在下一步追加。
/// </summary>
public static class DshBuildEndingScene
{
    const string TvPath = "Assets/3D/Start/tv.fbx";
    const string ScenePath = "Assets/Scenes/Ending.unity";
    const float TvScale = 4.5f;
    const float EyeHeight = 1.45f;
    const float ScreenCenterInModel = 0.382f;
    const float EntryDistance = 0.36f * TvScale; // 与老场景过场终点一致

    [MenuItem("Tools/DSH/Ending/Build Scene")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ---- 地面（白色虚空）----
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Ending_Floor";
        floor.transform.position = new Vector3(0f, -0.05f, 0f);
        floor.transform.localScale = new Vector3(60f, 0.1f, 60f);
        floor.GetComponent<MeshRenderer>().sharedMaterial = LoadMat("Ending_Floor", new Color(0.92f, 0.92f, 0.90f), 0.9f);

        // ---- 大电视 ----
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TvPath);
        GameObject tv = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        tv.name = "Ending_TV";
        float tvBottom = EyeHeight - ScreenCenterInModel * TvScale;
        tv.transform.position = new Vector3(0f, tvBottom, 0f);
        tv.transform.rotation = Quaternion.identity; // 正面朝 +Z
        tv.transform.localScale = Vector3.one * TvScale;

        Material body = LoadMat("TV_Body", new Color(0.82f, 0.80f, 0.74f), 0.3f);
        Material screen = LoadMat("TV_Screen_Glow", new Color(0.01f, 0.02f, 0.02f), 0.5f);
        foreach (Renderer r in tv.GetComponentsInChildren<Renderer>(true))
        {
            bool isScreen = r.name.ToLower().Contains("screen");
            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = isScreen ? screen : body;
            r.sharedMaterials = mats;
        }

        // ---- 灯光 ----
        GameObject sun = new GameObject("Ending_Sun");
        Light sunLight = sun.AddComponent<Light>();
        sunLight.type = LightType.Directional;
        sunLight.intensity = 1.1f;
        sunLight.color = new Color(1f, 0.96f, 0.90f);
        sun.transform.rotation = Quaternion.Euler(55f, -30f, 0f);

        GameObject fill = new GameObject("Ending_Fill");
        Light fillLight = fill.AddComponent<Light>();
        fillLight.type = LightType.Point;
        fillLight.range = 12f;
        fillLight.intensity = 12f;
        fillLight.color = new Color(1f, 0.93f, 0.82f);
        fill.transform.position = new Vector3(0f, 2.8f, 2.5f);

        // ---- 玩家 ----
        GameObject player = new GameObject("Player");
        CharacterController cc = player.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.center = new Vector3(0f, 1f, 0f);
        cc.radius = 0.5f;
        cc.skinWidth = 0.08f;
        player.AddComponent<PlayerMovement>();

        GameObject camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.SetParent(player.transform, false);
        camGo.transform.localPosition = new Vector3(0f, EyeHeight, 0f);
        Camera cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 60f;
        cam.nearClipPlane = 0.05f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.96f, 0.96f, 0.95f);
        camGo.AddComponent<AudioListener>();

        MouseLook look = player.AddComponent<MouseLook>();
        look.cameraTransform = camGo.transform;

        player.transform.position = new Vector3(0f, 0f, EntryDistance);
        player.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // 朝 -Z 看电视

        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"[Ending] built scene at {ScenePath}, tvScale={TvScale}, entryZ={EntryDistance}, tvBottom={tvBottom}");
    }

    static Material LoadMat(string name, Color color, float smoothness)
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (!AssetDatabase.IsValidFolder("Assets/Materials/Interior")) AssetDatabase.CreateFolder("Assets/Materials", "Interior");
        if (!AssetDatabase.IsValidFolder("Assets/Materials/Interior/TV")) AssetDatabase.CreateFolder("Assets/Materials/Interior", "TV");

        string path = $"Assets/Materials/Interior/TV/{name}.mat";
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
