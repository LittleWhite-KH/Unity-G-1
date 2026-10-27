using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Phase 3 scene work: curved TV portal, dark room behind the end door, lamp check.</summary>
public static class DshPhase3
{
    static readonly string Report = "E:/DSH_Unity/tmp-read/phase3.txt";

    [MenuItem("Tools/DSH/Phase3/Rebuild Curved Portal")]
    public static void RebuildCurvedPortal()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Gaming.unity", OpenSceneMode.Single);
        var sb = new StringBuilder();

        GameObject tv = GameObject.Find("TV_Ending");
        if (tv == null) { Debug.LogError("[Phase3] TV_Ending missing"); return; }

        Transform screen = tv.transform.Find("Screen");
        if (screen == null) { Debug.LogError("[Phase3] TV_Ending/Screen missing"); return; }

        MeshFilter screenFilter = screen.GetComponent<MeshFilter>();
        if (screenFilter == null || screenFilter.sharedMesh == null)
        {
            Debug.LogError("[Phase3] screen mesh missing");
            return;
        }

        Mesh mesh = screenFilter.sharedMesh;
        sb.AppendLine($"screen mesh={mesh.name} verts={mesh.vertexCount} bounds={mesh.bounds.size}");
        sb.AppendLine($"screen local pos={screen.localPosition} rot={screen.localEulerAngles} scale={screen.localScale}");

        GameObject portal = GameObject.Find("TV_PortalScreen");
        GameObject glass = GameObject.Find("TV_PortalGlass");

        if (portal != null) ConfigureSurface(portal, tv.transform, screen, mesh, 0.008f, sb, "portal");
        // The old glass quad covered the whole CRT; on the curved mesh it blocks the portal.
        if (glass != null)
        {
            glass.SetActive(false);
            sb.AppendLine("glass disabled");
        }

        // Hide the CRT mesh: the portal surface is now the visible screen.
        Renderer screenRenderer = screen.GetComponent<Renderer>();
        if (screenRenderer != null)
        {
            screenRenderer.enabled = false;
            sb.AppendLine("screen renderer disabled");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        File.WriteAllText(Report, sb.ToString());
        Debug.Log("[Phase3] curved portal rebuilt");
    }

    static void ConfigureSurface(GameObject go, Transform tv, Transform screen, Mesh mesh, float offset, StringBuilder sb, string label)
    {
        go.transform.SetParent(tv, false);
        go.transform.localPosition = screen.localPosition;
        go.transform.localRotation = screen.localRotation;
        go.transform.localScale = screen.localScale;

        MeshFilter filter = go.GetComponent<MeshFilter>();
        if (filter == null) filter = go.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;

        if (go.GetComponent<MeshRenderer>() == null) go.AddComponent<MeshRenderer>();

        // Push slightly outwards along the TV facing direction so the portal never z-fights the CRT mesh.
        Vector3 outward = tv.forward;
        go.transform.position += outward * offset;

        sb.AppendLine($"{label}: parent={tv.name} localPos={go.transform.localPosition} mesh={mesh.name} offset={offset}");
    }

    // ------------------------------------------------------------------
    [MenuItem("Tools/DSH/Phase3/Build Dark Room")]
    public static void BuildDarkRoom()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Gaming.unity", OpenSceneMode.Single);
        var sb = new StringBuilder();

        GameObject old = GameObject.Find("TV_DarkRoom");
        if (old != null) Object.DestroyImmediate(old);

        Material black = EnsureBlackMaterial();
        GameObject root = new GameObject("TV_DarkRoom");
        root.layer = 30;
        root.transform.position = new Vector3(0f, 0f, -208.5f);

        AddBox(root.transform, "Floor", new Vector3(0f, -0.1f, 0f), new Vector3(6.4f, 0.2f, 6.4f), black, sb);
        AddBox(root.transform, "Ceiling", new Vector3(0f, 4.6f, 0f), new Vector3(6.4f, 0.2f, 6.4f), black, sb);
        AddBox(root.transform, "Back", new Vector3(0f, 2.25f, -3.1f), new Vector3(6.4f, 4.8f, 0.2f), black, sb);
        AddBox(root.transform, "Left", new Vector3(-3.1f, 2.25f, 0f), new Vector3(0.2f, 4.8f, 6.4f), black, sb);
        AddBox(root.transform, "Right", new Vector3(3.1f, 2.25f, 0f), new Vector3(0.2f, 4.8f, 6.4f), black, sb);
        AddBox(root.transform, "FrontLintel", new Vector3(0f, 4.6f, 3.1f), new Vector3(6.4f, 0.2f, 0.2f), black, sb);

        // Door mouth stays as the black opening seen from the white world.
        GameObject mouth = GameObject.Find("DoorMouth");
        if (mouth != null)
        {
            mouth.transform.position = new Vector3(0f, 2.15f, -205.32f);
            mouth.transform.localScale = new Vector3(2.25f, 4.45f, 0.1f);
            sb.AppendLine("door mouth kept");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        File.WriteAllText(Report, sb.ToString());
        Debug.Log("[Phase3] dark room built");
    }

    static void AddBox(Transform parent, string name, Vector3 localPos, Vector3 localScale, Material material, StringBuilder sb)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.layer = 30;
        box.transform.SetParent(parent, false);
        box.transform.localPosition = localPos;
        box.transform.localScale = localScale;
        Object.DestroyImmediate(box.GetComponent<Collider>());
        box.GetComponent<MeshRenderer>().sharedMaterial = material;
        sb.AppendLine($"{name} localscale={localScale}");
    }

    static Material EnsureBlackMaterial()
    {
        const string path = "Assets/Materials/Interior/TV/DarkRoom_Black.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        mat = new Material(shader) { name = "DarkRoom_Black" };
        mat.SetColor("_BaseColor", Color.black);
        mat.SetFloat("_Cull", 0f);          // double sided: visible from inside the box
        mat.SetFloat("_Surface", 0f);
        AssetDatabase.CreateAsset(mat, path);
        AssetDatabase.SaveAssets();
        return mat;
    }

    // ------------------------------------------------------------------
    [MenuItem("Tools/DSH/Phase3/Preview Lamps Top")]
    public static void PreviewLampsTop()
    {
        GameObject camGo = GameObject.Find("Player/Main Camera");
        if (camGo == null) return;

        Transform camT = camGo.transform;
        Vector3 oldPos = camT.position;
        Quaternion oldRot = camT.rotation;
        float oldFov = camGo.GetComponent<Camera>().fieldOfView;

        camT.position = new Vector3(0f, 16f, 0f);
        camT.rotation = Quaternion.Euler(90f, 0f, 0f);
        camGo.GetComponent<Camera>().fieldOfView = 70f;

        DshCapture.Capture("Player/Main Camera", "E:/DSH_Unity/shots/lamps-top.png");

        camT.position = oldPos;
        camT.rotation = oldRot;
        camGo.GetComponent<Camera>().fieldOfView = oldFov;
    }

    [MenuItem("Tools/DSH/Phase3/Planar Portal Fallback")]
    public static void PlanarPortal()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Gaming.unity", OpenSceneMode.Single);
        var sb = new StringBuilder();

        GameObject tv = GameObject.Find("TV_Ending");
        Transform screen = tv != null ? tv.transform.Find("Screen") : null;
        if (screen != null)
        {
            Renderer sr = screen.GetComponent<Renderer>();
            if (sr != null) sr.enabled = false;
            sb.AppendLine("screen disabled");
        }

        GameObject portal = GameObject.Find("TV_PortalScreen");
        if (portal == null) { Debug.LogError("[Phase3] portal missing"); return; }

        portal.transform.SetParent(null, true);
        portal.transform.position = new Vector3(29.135f, 1.731f, 9.057f);
        portal.transform.rotation = Quaternion.Euler(0f, 270f, 0f);
        portal.transform.localScale = new Vector3(1.06f, 0.84f, 1f);

        Mesh quad = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
        MeshFilter filter = portal.GetComponent<MeshFilter>();
        if (filter == null) filter = portal.AddComponent<MeshFilter>();
        filter.sharedMesh = quad;

        GameObject glass = GameObject.Find("TV_PortalGlass");
        if (glass != null) glass.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        File.WriteAllText(Report, sb.ToString());
        Debug.Log("[Phase3] planar portal applied");
    }

    [MenuItem("Tools/DSH/Phase3/Preview Props")]
    public static void PreviewProps()
    {
        Shot("tomb-table", new Vector3(-3f, 1.7f, 6.2f), new Vector3(-3f, 1.1f, 11.2f));
        Shot("table-pc", new Vector3(21f, 1.7f, -6.2f), new Vector3(21f, 1.2f, -11.4f));
    }

    static void Shot(string fileName, Vector3 pos, Vector3 target)
    {
        GameObject camGo = GameObject.Find("Player/Main Camera");
        if (camGo == null) return;

        Transform camT = camGo.transform;
        Vector3 oldPos = camT.position;
        Quaternion oldRot = camT.rotation;
        camT.position = pos;
        camT.rotation = Quaternion.LookRotation(target - pos, Vector3.up);
        DshCapture.Capture("Player/Main Camera", "E:/DSH_Unity/shots/" + fileName + ".png");
        camT.position = oldPos;
        camT.rotation = oldRot;
    }

    [MenuItem("Tools/DSH/Phase3/Preview Door Back")]
    public static void PreviewDoorBack()    {
        GameObject camGo = GameObject.Find("Player/Main Camera");
        TvPortal portal = Object.FindAnyObjectByType<TvPortal>();
        if (camGo == null || portal == null) return;

        Transform camT = camGo.transform;
        Vector3 oldPos = camT.position;
        Quaternion oldRot = camT.rotation;
        int oldMask = camGo.GetComponent<Camera>().cullingMask;

        camT.position = new Vector3(0f, 1.7f, -206.2f);
        camT.rotation = Quaternion.Euler(4f, 0f, 0f);
        camGo.GetComponent<Camera>().cullingMask = 1 << 30;

        DshCapture.Capture("Player/Main Camera", "E:/DSH_Unity/shots/door-back.png");

        camT.position = oldPos;
        camT.rotation = oldRot;
        camGo.GetComponent<Camera>().cullingMask = oldMask;
    }
}
