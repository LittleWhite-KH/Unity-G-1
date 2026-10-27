using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Inspect / build the end door model (door-with-frame FBX).</summary>
public static class DshBuildEndDoor
{
    const string FbxPath = "Assets/Models/Furniture/DoorEnd/EndDoor.fbx";
    const string TexPath = "Assets/Models/Furniture/DoorEnd/Door_C.jpeg";
    const string MatPath = "Assets/Models/Furniture/DoorEnd/EndDoor_Wood.mat";
    static readonly string Report = "E:/DSH_Unity/tmp-read/end-door.txt";

    [MenuItem("Tools/DSH/Phase2/Inspect Door Model")]
    public static void Inspect()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        var sb = new StringBuilder();
        if (prefab == null)
        {
            Debug.LogError("[EndDoor] FBX missing: " + FbxPath);
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.position = Vector3.zero;

        sb.AppendLine("EndDoor.fbx hierarchy");
        Walk(instance.transform, 0, sb);

        Object.DestroyImmediate(instance);
        Directory.CreateDirectory(Path.GetDirectoryName(Report));
        File.WriteAllText(Report, sb.ToString(), Encoding.UTF8);
        Debug.Log("[EndDoor] inspect -> " + Report);
    }

    [MenuItem("Tools/DSH/Phase2/Build End Door")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Gaming.unity", OpenSceneMode.Single);

        foreach (string name in new[] { "EndDoorRoot", "EndDoorHinge" })
        {
            GameObject old = GameObject.Find(name);
            if (old != null) Object.DestroyImmediate(old);
        }
        foreach (string name in new[] { "DoorFrame_L", "DoorFrame_R", "DoorFrame_T", "DoorFrame_B" })
        {
            GameObject old = GameObject.Find(name);
            if (old != null) Object.DestroyImmediate(old);
        }

        Material wood = EnsureWood();
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        if (prefab == null)
        {
            Debug.LogError("[EndDoor] FBX missing");
            return;
        }

        GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        root.name = "EndDoorRoot";
        root.transform.position = Vector3.zero;
        root.transform.rotation = Quaternion.Euler(270f, 0f, 0f);
        root.transform.localScale = Vector3.one * 215f;

        Transform leaf = root.transform.Find("Plane.001");
        if (leaf == null)
        {
            Debug.LogError("[EndDoor] door leaf (Plane.001) missing");
            return;
        }

        Renderer leafRenderer = leaf.GetComponent<Renderer>();
        Bounds leafBounds = leafRenderer.bounds;
        Vector3 targetCentre = new Vector3(0f, 2.15f, -205.02f);
        root.transform.position += targetCentre - leafBounds.center;
        Physics.SyncTransforms();

        // One wood material for frame and door leaf (handle included for now).
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            Material[] mats = renderer.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = wood;
            renderer.sharedMaterials = mats;
        }

        foreach (Collider collider in root.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 30;

        // Hinge on the right edge of the leaf, so the door swings inward.
        Bounds currentLeaf = leafRenderer.bounds;
        Vector3 hingePosition = new Vector3(currentLeaf.max.x - 0.02f, currentLeaf.center.y, currentLeaf.center.z);
        GameObject hinge = new GameObject("EndDoorHinge");
        hinge.transform.position = hingePosition;
        hinge.transform.rotation = Quaternion.identity;
        hinge.layer = 30;
        leaf.SetParent(hinge.transform, true);

        EndDoor endDoor = hinge.AddComponent<EndDoor>();
        TvPortal portal = Object.FindAnyObjectByType<TvPortal>();
        GameObject player = GameObject.Find("Player");
        GameObject opening = GameObject.Find("Door_Opening");
        endDoor.player = player != null ? player.transform : null;
        endDoor.hinge = hinge.transform;
        endDoor.doorLeaf = leaf;
        endDoor.doorOpening = opening != null ? opening.transform : null;
        endDoor.portal = portal;

        if (portal != null) portal.waitForDoorInteraction = true;

        GameObject mouth = GameObject.Find("DoorMouth");
        if (mouth != null)
        {
            mouth.transform.position = new Vector3(0f, 2.15f, -205.32f);
            mouth.transform.localScale = new Vector3(2.25f, 4.45f, 0.1f);
            mouth.layer = 30;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[EndDoor] built at {targetCentre}, leaf size={currentLeaf.size}, hinge={hingePosition}");
    }

    [MenuItem("Tools/DSH/Phase2/Preview End Door")]
    public static void PreviewDoor()
    {
        GameObject camGo = GameObject.Find("Player/Main Camera");
        TvPortal portal = Object.FindAnyObjectByType<TvPortal>();
        if (camGo == null || portal == null) { Debug.LogError("[EndDoor] preview refs missing"); return; }

        Transform player = portal.player;
        Camera cam = portal.playerCamera;
        Vector3 oldPos = player.position;
        Quaternion oldRot = player.rotation;
        Vector3 oldCamPos = cam.transform.localPosition;
        Quaternion oldCamRot = cam.transform.localRotation;
        int oldMask = cam.cullingMask;

        player.position = portal.exitAnchor.position;
        Vector3 dir = portal.doorOpening.position - player.position;
        dir.y = 0f;
        player.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        cam.transform.localPosition = new Vector3(0f, 1.36f, 0f);
        cam.transform.localRotation = Quaternion.identity;
        cam.cullingMask = 1 << portal.exitRoomLayer;

        DshCapture.Capture("Player/Main Camera", "E:/DSH_Unity/shots/end-door.png");

        player.position = oldPos;
        player.rotation = oldRot;
        cam.transform.localPosition = oldCamPos;
        cam.transform.localRotation = oldCamRot;
        cam.cullingMask = oldMask;
    }

    [MenuItem("Tools/DSH/Phase2/Check End Door")]
    public static void CheckDoor()
    {
        var sb = new StringBuilder();
        foreach (string name in new[] { "EndDoorRoot", "EndDoorHinge", "Plane.001", "DoorMouth", "Door_Opening" })
        {
            GameObject go = GameObject.Find(name);
            if (go == null) { sb.AppendLine(name + ": missing"); continue; }
            sb.AppendLine($"{name} layer={go.layer} active={go.activeInHierarchy} lossyScale={F(go.transform.lossyScale)} pos={F(go.transform.position)} rot={F(go.transform.eulerAngles)}");
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                sb.AppendLine($"  renderer enabled={renderer.enabled} active={renderer.gameObject.activeInHierarchy} mat={(renderer.sharedMaterial != null ? renderer.sharedMaterial.name + " / " + renderer.sharedMaterial.shader.name : "null")}");
                sb.AppendLine($"  bounds center={F(renderer.bounds.center)} size={F(renderer.bounds.size)}");
            }
            foreach (Renderer child in go.GetComponentsInChildren<Renderer>(true))
            {
                if (child == renderer) continue;
                sb.AppendLine($"  child {child.name} layer={child.gameObject.layer} enabled={child.enabled} mat={(child.sharedMaterial != null ? child.sharedMaterial.name : "null")} bounds={F(child.bounds.center)} size={F(child.bounds.size)}");
            }
        }

        TvPortal portal = Object.FindAnyObjectByType<TvPortal>();
        if (portal != null)
        {
            sb.AppendLine($"portal waitForDoorInteraction={portal.waitForDoorInteraction} roomLayer={portal.exitRoomLayer}");
            if (portal.portalCamera != null)
            {
                Camera cam = portal.portalCamera;
                sb.AppendLine($"portalCamera pos={F(cam.transform.position)} rot={F(cam.transform.eulerAngles)} mask=0x{cam.cullingMask:X} near={cam.nearClipPlane} far={cam.farClipPlane}");
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Report));
        File.WriteAllText(Report, sb.ToString(), Encoding.UTF8);
        Debug.Log("[EndDoor] check -> " + Report);
    }

    static Material EnsureWood()
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
        if (mat != null) return mat;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        mat = new Material(shader) { name = "EndDoor_Wood" };
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexPath);
        if (tex != null) mat.SetTexture("_BaseMap", tex);
        mat.SetColor("_BaseColor", Color.white);
        mat.SetFloat("_Smoothness", 0.32f);
        AssetDatabase.CreateAsset(mat, MatPath);
        AssetDatabase.SaveAssets();
        return mat;
    }

    static void Walk(Transform t, int depth, StringBuilder sb)
    {
        sb.Append(' ', depth * 2);
        sb.Append(t.name).Append(" localPos=").Append(F(t.localPosition))
          .Append(" localRot=").Append(F(t.localEulerAngles))
          .Append(" localScale=").Append(F(t.localScale));
        var renderer = t.GetComponent<Renderer>();
        if (renderer != null)
        {
            sb.Append(" mat=").Append(renderer.sharedMaterial != null ? renderer.sharedMaterial.name : "-");
            sb.Append(" worldBounds=").Append(F(renderer.bounds.center)).Append(" size=").Append(F(renderer.bounds.size));
        }
        sb.AppendLine();
        for (int i = 0; i < t.childCount; i++) Walk(t.GetChild(i), depth + 1, sb);
    }

    static string F(Vector3 v) => $"({v.x:0.####},{v.y:0.####},{v.z:0.####})";
}
