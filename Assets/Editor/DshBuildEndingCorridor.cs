using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 在 Ending 场景里搭“电视内部”走廊 + 门 + 白闪 + EndingDirector。
/// 走廊放在 z=-80 的远处，从电视前看不到。
/// </summary>
public static class DshBuildEndingCorridor
{
    const string ScenePath = "Assets/Scenes/Ending.unity";
    const float CorridorZ = -80f;
    const float CorridorLength = 6f;

    [MenuItem("Tools/DSH/Ending/Build Corridor")]
    public static void Build()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Material wall = Mat("Ending_CorridorWall", new Color(0.10f, 0.11f, 0.12f), 0.35f);
        Material floor = Mat("Ending_CorridorFloor", new Color(0.06f, 0.065f, 0.07f), 0.6f);
        Material doorMat = Mat("Ending_Door", new Color(0.18f, 0.19f, 0.20f), 0.4f);
        Material lightMat = Mat("Ending_DoorLight", new Color(1f, 0.98f, 0.92f), 0.2f);

        GameObject root = new GameObject("Ending_Corridor");
        root.transform.position = Vector3.zero;

        Box(root, "Floor", new Vector3(0f, -0.05f, CorridorZ), new Vector3(1.8f, 0.1f, CorridorLength), floor);
        Box(root, "Ceiling", new Vector3(0f, 2.65f, CorridorZ), new Vector3(1.8f, 0.1f, CorridorLength), wall);
        Box(root, "Wall_L", new Vector3(-0.9f, 1.35f, CorridorZ), new Vector3(0.1f, 2.7f, CorridorLength), wall);
        Box(root, "Wall_R", new Vector3(0.9f, 1.35f, CorridorZ), new Vector3(0.1f, 2.7f, CorridorLength), wall);

        // 尽头墙 + 门洞（1.2 x 2.2）
        float endZ = CorridorZ - CorridorLength / 2f + 0.06f;
        Box(root, "Wall_End_L", new Vector3(-0.75f, 1.1f, endZ), new Vector3(0.3f, 2.2f, 0.12f), wall);
        Box(root, "Wall_End_R", new Vector3(0.75f, 1.1f, endZ), new Vector3(0.3f, 2.2f, 0.12f), wall);
        Box(root, "Wall_End_Top", new Vector3(0f, 2.45f, endZ), new Vector3(1.8f, 0.5f, 0.12f), wall);

        // 门后白光
        GameObject glow = GameObject.CreatePrimitive(PrimitiveType.Quad);
        glow.name = "DoorGlow";
        glow.transform.SetParent(root.transform, true);
        glow.transform.position = new Vector3(0f, 1.1f, endZ - 0.6f);
        glow.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        glow.transform.localScale = new Vector3(1.6f, 2.4f, 1f);
        Object.DestroyImmediate(glow.GetComponent<Collider>());
        Material glowMat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "DoorGlow" };
        glowMat.color = new Color(1f, 0.98f, 0.94f);
        AssetDatabase.CreateAsset(glowMat, "Assets/Materials/Interior/TV/DoorGlow.mat");
        glow.GetComponent<MeshRenderer>().sharedMaterial = glowMat;

        // 门（侧滑）
        GameObject oldDoor = GameObject.Find("Ending_Door");
        if (oldDoor != null) Object.DestroyImmediate(oldDoor);

        GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
        door.name = "Ending_Door";
        door.transform.SetParent(root.transform, true);
        door.transform.position = new Vector3(0f, 1.1f, endZ + 0.02f);
        door.transform.localScale = new Vector3(1.2f, 2.2f, 0.08f);
        door.GetComponent<MeshRenderer>().sharedMaterial = doorMat;
        DoorController dc = door.AddComponent<DoorController>();
        dc.leaf = door.transform;
        dc.openOffset = new Vector3(1.45f, 0f, 0f);
        dc.openDuration = 1.3f;
        dc.logOpen = true;

        // 走廊冷光
        GameObject lamp = new GameObject("Ending_CorridorLight");
        lamp.transform.SetParent(root.transform, true);
        lamp.transform.position = new Vector3(0f, 2.4f, CorridorZ + 1.2f);
        Light l = lamp.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = 9f;
        l.intensity = 8f;
        l.color = new Color(0.72f, 0.82f, 1f);

        // 白闪 UI
        GameObject oldCanvas = GameObject.Find("Ending_Flash");
        if (oldCanvas != null) Object.DestroyImmediate(oldCanvas);

        GameObject canvasGo = new GameObject("Ending_Flash");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();
        CanvasGroup group = canvasGo.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;

        GameObject imgGo = new GameObject("White");
        imgGo.transform.SetParent(canvasGo.transform, false);
        Image img = imgGo.AddComponent<Image>();
        img.color = Color.white;
        RectTransform rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Director
        GameObject directorGo = GameObject.Find("Ending_Director");
        if (directorGo == null) directorGo = new GameObject("Ending_Director");
        EndingDirector dir = directorGo.GetComponent<EndingDirector>();
        if (dir == null) dir = directorGo.AddComponent<EndingDirector>();

        GameObject player = GameObject.Find("Player");
        GameObject tv = GameObject.Find("Ending_TV");
        dir.player = player != null ? player.transform : null;
        dir.teleportTarget = new Vector3(0f, 0f, CorridorZ + CorridorLength / 2f - 0.6f);
        dir.screenCenter = new Vector3(0f, 1.45f, 0f);
        dir.door = dc;
        dir.flash = group;
        dir.doorZone = new Vector3(0f, 1.1f, endZ + 0.9f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/Start.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Gaming.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Ending.unity", true),
        };

        Debug.Log($"[Ending] corridor built at z={CorridorZ}, doorZ={endZ}, teleport={dir.teleportTarget}");
    }

    static void Box(GameObject parent, string name, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent.transform, true);
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    static Material Mat(string name, Color color, float smoothness)
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
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
