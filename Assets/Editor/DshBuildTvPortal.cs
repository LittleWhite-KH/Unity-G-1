using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 终局改造：
///   · 电视机里是「白色世界 + 一扇门」，屏幕 = 半透明玻璃罩 + portal 画面；
///   · 白色世界只有白地板、白天空、一扇门（门里黑色，走进去回 Start）；
///   · 电视柜上两个按钮（蓝 = 软盘 / 橙 = 光盘），捡到对应密钥就亮；
///   · 两把密钥统一橙色光效，并把模型对齐到自身轴心（旋转不再偏心）。
/// </summary>
public static class DshBuildTvPortal
{
    const string ScenePath = "Assets/Scenes/Gaming.unity";
    const string StartScenePath = "Assets/Scenes/Start.unity";
    const int ExitLayer = 30;
    const float ExitRoomZ = -200f;
    const float DoorZ = -5f;

    [MenuItem("Tools/DSH/Portal/Build White World")]
    public static void Build()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        foreach (string name in new[] { "TV_ExitRoom", "TV_PortalCamera", "TV_Portal", "TV_PortalScreen", "TV_PortalGlass", "TV_PortalSource", "TV_KeyButtons" })
        {
            GameObject old = GameObject.Find(name);
            if (old != null) Object.DestroyImmediate(old);
        }

        // 旧的门贴图让位给真传送门
        GameObject doorQuad = GameObject.Find("DoorQuad");
        if (doorQuad != null) doorQuad.SetActive(false);

        // ---- 材质 ----
        Material whiteFloor = MatOrCreate("WhiteWorld_Floor", new Color(0.95f, 0.95f, 0.94f), 0.05f, true);
        Material whiteFrame = MatOrCreate("WhiteWorld_Frame", new Color(0.90f, 0.90f, 0.89f), 0.15f, true);
        Material blackMouth = MatOrCreate("WhiteWorld_Mouth", new Color(0.005f, 0.005f, 0.006f), 0f, true);
        Material glass = MatOrCreateGlass("TV_Screen_Glass", new Color(1f, 1f, 1f, 0.18f));

        Material btnBlueOn = MatOrCreateEmissive("Button_BlueOn", new Color(0.15f, 0.45f, 0.9f), new Color(0.25f, 0.65f, 1.6f));
        Material btnBlueOff = MatOrCreateEmissive("Button_BlueOff", new Color(0.05f, 0.09f, 0.16f), new Color(0.01f, 0.02f, 0.04f));
        Material btnOrangeOn = MatOrCreateEmissive("Button_OrangeOn", new Color(0.9f, 0.42f, 0.10f), new Color(1.5f, 0.7f, 0.18f));
        Material btnOrangeOff = MatOrCreateEmissive("Button_OrangeOff", new Color(0.16f, 0.09f, 0.05f), new Color(0.04f, 0.02f, 0.01f));

        // ---- 白色世界 ----
        GameObject root = new GameObject("TV_ExitRoom");
        root.transform.position = new Vector3(0f, 0f, ExitRoomZ);
        SetLayer(root, ExitLayer);

        Box(root.transform, "Floor", new Vector3(0f, -0.1f, 0f), new Vector3(40f, 0.2f, 40f), whiteFloor);

        // 隐形围栏，别让玩家走到白板边缘外
        Box(root.transform, "Fence_N", new Vector3(0f, 1.5f, 19.5f), new Vector3(40f, 3f, 0.4f), null);
        Box(root.transform, "Fence_S", new Vector3(0f, 1.5f, -19.5f), new Vector3(40f, 3f, 0.4f), null);
        Box(root.transform, "Fence_E", new Vector3(19.5f, 1.5f, 0f), new Vector3(0.4f, 3f, 40f), null);
        Box(root.transform, "Fence_W", new Vector3(-19.5f, 1.5f, 0f), new Vector3(0.4f, 3f, 40f), null);

        // 门框
        Box(root.transform, "DoorFrame_L", new Vector3(-1.55f, 2.2f, DoorZ), new Vector3(0.3f, 4.4f, 0.5f), whiteFrame);
        Box(root.transform, "DoorFrame_R", new Vector3(1.55f, 2.2f, DoorZ), new Vector3(0.3f, 4.4f, 0.5f), whiteFrame);
        Box(root.transform, "DoorFrame_T", new Vector3(0f, 4.25f, DoorZ), new Vector3(3.4f, 0.4f, 0.5f), whiteFrame);
        Box(root.transform, "DoorFrame_B", new Vector3(0f, 0.06f, DoorZ), new Vector3(3.4f, 0.12f, 0.5f), whiteFrame);

        // 门里是黑色（走进去就结束）
        GameObject mouth = Box(root.transform, "DoorMouth", new Vector3(0f, 2.15f, DoorZ - 0.1f), new Vector3(2.8f, 4.3f, 0.08f), blackMouth);
        mouth.GetComponent<BoxCollider>().enabled = false;

        // 传送点 / 目标
        GameObject dst = new GameObject("Portal_Destination");
        dst.transform.SetParent(root.transform, false);
        dst.transform.localPosition = new Vector3(0f, 1.45f, 0f);
        dst.transform.localRotation = Quaternion.identity;
        SetLayer(dst, ExitLayer);

        GameObject anchor = new GameObject("Exit_Anchor");
        anchor.transform.SetParent(root.transform, false);
        anchor.transform.localPosition = new Vector3(0f, 0f, -2.2f);
        anchor.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        SetLayer(anchor, ExitLayer);

        GameObject opening = new GameObject("Door_Opening");
        opening.transform.SetParent(root.transform, false);
        opening.transform.localPosition = new Vector3(0f, 2.15f, DoorZ - 0.1f);
        SetLayer(opening, ExitLayer);

        // ---- 电视屏幕 ----
        GameObject tv = GameObject.Find("TV_Ending");
        if (tv == null)
        {
            Debug.LogError("[Portal] TV_Ending not found");
            return;
        }

        Transform screen = tv.transform.Find("Screen");
        if (screen == null)
        {
            Debug.LogError("[Portal] TV_Ending/Screen not found");
            return;
        }

        Bounds sb = screen.GetComponent<Renderer>().bounds;
        Vector3 planeCenter = new Vector3(sb.min.x, sb.center.y, sb.center.z);

        GameObject src = new GameObject("TV_PortalSource");
        src.transform.SetParent(tv.transform, true);
        src.transform.position = planeCenter;
        src.transform.rotation = tv.transform.rotation;
        src.transform.localScale = Vector3.one;
        src.layer = tv.layer;

        GameObject portalScreen = GameObject.CreatePrimitive(PrimitiveType.Quad);
        portalScreen.name = "TV_PortalScreen";
        Object.DestroyImmediate(portalScreen.GetComponent<Collider>());
        portalScreen.transform.position = planeCenter + tv.transform.forward * 0.055f;
        portalScreen.transform.rotation = tv.transform.rotation;
        portalScreen.transform.localScale = new Vector3(1.06f, 0.84f, 1f);
        MeshRenderer mr = portalScreen.GetComponent<MeshRenderer>();
        mr.sharedMaterial = LoadMat("Assets/Materials/Interior/TV/TV_Screen_On.mat");
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.enabled = false;
        portalScreen.layer = screen.gameObject.layer;

        // 半透明玻璃罩
        GameObject glassGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
        glassGo.name = "TV_PortalGlass";
        Object.DestroyImmediate(glassGo.GetComponent<Collider>());
        glassGo.transform.position = planeCenter + tv.transform.forward * 0.062f;
        glassGo.transform.rotation = tv.transform.rotation;
        glassGo.transform.localScale = new Vector3(1.07f, 0.85f, 1f);
        MeshRenderer gr = glassGo.GetComponent<MeshRenderer>();
        gr.sharedMaterial = glass;
        gr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        gr.receiveShadows = false;
        gr.enabled = false;
        glassGo.layer = screen.gameObject.layer;

        // ---- PortalCamera ----
        GameObject camGo = new GameObject("TV_PortalCamera");
        camGo.transform.SetParent(root.transform, true);
        Camera portalCam = camGo.AddComponent<Camera>();
        portalCam.cullingMask = 1 << ExitLayer;
        portalCam.clearFlags = CameraClearFlags.SolidColor;
        portalCam.backgroundColor = new Color(0.96f, 0.96f, 0.95f);
        portalCam.nearClipPlane = 0.03f;
        portalCam.farClipPlane = 120f;
        portalCam.depth = -2;
        portalCam.enabled = false;
        SetLayer(camGo, ExitLayer);

        // ---- 电视柜上的两个槽由 TvKeyButtons 控制亮灭（不再额外做按钮） ----
        GameObject buttonsGo = new GameObject("TV_KeyButtons");
        buttonsGo.AddComponent<TvKeyButtons>();

        // ---- TvPortal ----
        GameObject portalGo = new GameObject("TV_Portal");
        TvPortal portal = portalGo.AddComponent<TvPortal>();

        GameObject player = GameObject.Find("Player");
        portal.player = player != null ? player.transform : null;
        portal.playerCamera = player != null ? player.GetComponentInChildren<Camera>() : null;
        portal.sourcePortal = src.transform;
        portal.portalSurface = mr;
        portal.glassSurface = gr;
        portal.portalCamera = portalCam;
        portal.portalShader = Shader.Find("DSH/TvPortal");
        portal.destinationPortal = dst.transform;
        portal.exitAnchor = anchor.transform;
        portal.exitLookAt = opening.transform;
        portal.doorOpening = opening.transform;
        portal.exitRoomLayer = ExitLayer;
        portal.doorTriggerDistance = 1.6f;

        if (portal.playerCamera != null)
        {
            portal.playerCamera.cullingMask &= ~(1 << ExitLayer);
        }

        FixKeys();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log($"[Portal] white world built. door={ExitRoomZ + DoorZ}, screen={planeCenter}");
    }

    [MenuItem("Tools/DSH/Portal/Build Start Reveal")]
    public static void BuildStartReveal()
    {
        var scene = EditorSceneManager.OpenScene(StartScenePath, OpenSceneMode.Single);

        GameObject props = GameObject.Find("Start_Props");
        if (props == null)
        {
            Debug.LogWarning("[Portal] Start_Props not found in Start scene");
        }

        GameObject old = GameObject.Find("Start_Reveal");
        if (old != null) Object.DestroyImmediate(old);

        GameObject go = new GameObject("Start_Reveal");
        StartSceneReveal reveal = go.AddComponent<StartSceneReveal>();
        reveal.props = props;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[Portal] Start reveal built, props={(props != null ? props.name : "null")}");
    }

    // ------------------------------------------------------------------
    // 钥匙：模型对齐轴心 + 统一橙色光效
    // ------------------------------------------------------------------
    private static void FixKeys()
    {
        Material orange = MatOrCreateEmissive("Key_Glow_Orange", new Color(1f, 0.52f, 0.12f), new Color(1.5f, 0.6f, 0.12f));

        foreach (string keyName in new[] { "Key_A", "Key_B" })
        {
            GameObject key = GameObject.Find(keyName);
            if (key == null) continue;

            Transform model = key.transform.Find("Model_Floppy");
            if (model == null) model = key.transform.Find("Model_Disc");
            if (model != null)
            {
                // 关掉模型自带的投影面
                Transform stage = model.Find("Stage");
                if (stage != null) stage.gameObject.SetActive(false);

                // 计算激活 renderer 的世界包围盒中心，把模型平移到 Key 原点（旋转轴心）
                Renderer[] rs = model.GetComponentsInChildren<Renderer>(true);
                Bounds b = new Bounds(key.transform.position, Vector3.zero);
                bool has = false;
                foreach (Renderer r in rs)
                {
                    if (!r.gameObject.activeInHierarchy) continue;
                    if (!has) { b = r.bounds; has = true; }
                    else b.Encapsulate(r.bounds);
                }

                if (has)
                {
                    Vector3 delta = b.center - key.transform.position;
                    model.position -= delta;
                    Debug.Log($"[Portal] {keyName} model recentered by {delta}");
                }
            }

            // 统一橙色发光
            foreach (Renderer r in key.GetComponentsInChildren<Renderer>(true))
            {
                if (!r.gameObject.activeInHierarchy) continue;
                Material[] mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = orange;
                r.sharedMaterials = mats;
            }

            foreach (Light l in key.GetComponentsInChildren<Light>(true))
            {
                l.color = new Color(1f, 0.58f, 0.22f);
                l.intensity = 1.6f;
            }
        }
    }

    // ------------------------------------------------------------------
    private static GameObject Button(Transform parent, string name, Vector3 worldPos, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent, true);
        go.transform.position = worldPos;
        go.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        go.transform.localScale = new Vector3(0.16f, 0.03f, 0.16f);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        SetLayer(go, 0);

        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = name + "_Ring";
        ring.transform.SetParent(go.transform, false);
        ring.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        ring.transform.localRotation = Quaternion.identity;
        ring.transform.localScale = new Vector3(1.4f, 0.2f, 1.4f);
        Object.DestroyImmediate(ring.GetComponent<Collider>());
        ring.GetComponent<MeshRenderer>().sharedMaterial = mat;
        SetLayer(ring, 0);

        return go;
    }

    private static Mesh BuildCurvedScreen(float width, float height, float bulge, int segX, int segY)
    {
        Mesh mesh = new Mesh { name = "TV_PortalCurve" };
        List<Vector3> verts = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();

        for (int y = 0; y <= segY; y++)
        {
            for (int x = 0; x <= segX; x++)
            {
                float fx = (float)x / segX * 2f - 1f;
                float fy = (float)y / segY * 2f - 1f;
                float px = fx * width * 0.5f;
                float py = fy * height * 0.5f;
                float pz = bulge * (1f - fx * fx) * (1f - fy * fy);
                verts.Add(new Vector3(px, py, pz));
                uvs.Add(new Vector2((fx + 1f) * 0.5f, (fy + 1f) * 0.5f));
            }
        }

        for (int y = 0; y < segY; y++)
        {
            for (int x = 0; x < segX; x++)
            {
                int i = y * (segX + 1) + x;
                tris.Add(i);
                tris.Add(i + segX + 1);
                tris.Add(i + 1);
                tris.Add(i + 1);
                tris.Add(i + segX + 1);
                tris.Add(i + segX + 2);
            }
        }

        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static GameObject Box(Transform parent, string name, Vector3 localPos, Vector3 scale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        if (mat != null)
        {
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }
        else
        {
            go.GetComponent<MeshRenderer>().enabled = false;
        }
        SetLayer(go, ExitLayer);
        return go;
    }

    private static void SetLayer(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
    }

    private static Material LoadMat(string path)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) Debug.LogWarning("[Portal] missing material " + path);
        return mat;
    }

    private static Material MatOrCreate(string name, Color color, float smoothness, bool unlit)
    {
        string path = "Assets/Materials/Interior/TV/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;

        Shader shader = unlit ? Shader.Find("Universal Render Pipeline/Unlit") : Shader.Find("Universal Render Pipeline/Lit");
        mat = new Material(shader) { name = name };
        if (unlit)
        {
            mat.SetColor("_BaseColor", color);
        }
        else
        {
            mat.color = color;
            mat.SetFloat("_Smoothness", smoothness);
        }
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static Material MatOrCreateEmissive(string name, Color baseColor, Color emission)
    {
        Material mat = MatOrCreate(name, baseColor, 0.4f, false);
        mat.color = baseColor;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", emission);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material MatOrCreateGlass(string name, Color color)
    {
        string path = "Assets/Materials/Interior/TV/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            mat = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(mat, path);
        }

        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0f);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        mat.SetColor("_BaseColor", color);
        mat.SetColor("_Color", color);
        mat.SetFloat("_Smoothness", 0.95f);
        mat.SetFloat("_Metallic", 0.1f);
        EditorUtility.SetDirty(mat);
        return mat;
    }
}
