using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Task 2: rebuild the ending stage behind the white-world door.
///   - dark room floor raised 2 cm above the white floor (kills the z-fighting "material flicker")
///   - door mouth shrunk to the doorway and turned double-sided black (door interior reads pure black)
///   - door frame gets an invisible air wall so the player can never peek behind it
///   - the door leaf gets a real collider and a black material once the player is inside
/// </summary>
public static class DshEndStageFix
{
    const string ScenePath = "Assets/Scenes/Gaming.unity";
    const string ReportPath = "E:/DSH_Unity/tmp-read/end-stage-fix.txt";
    const string BlackMatPath = "Assets/Materials/Interior/TV/DarkRoom_Black.mat";

    [MenuItem("Tools/DSH/Polish/Rebuild End Stage")]
    public static void Rebuild()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var sb = new StringBuilder();

        Material black = AssetDatabase.LoadAssetAtPath<Material>(BlackMatPath);
        if (black == null)
        {
            Debug.LogError("[EndStageFix] DarkRoom_Black.mat missing");
            return;
        }

        RebuildDarkRoom(black, sb);
        AddWhiteWall(sb);
        FixDoorMouth(black, sb);
        AddDoorLeafCollider(sb);
        AddBlockers(sb);
        AddWhiteLight(sb);
        WireEndDoor(black, sb);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        File.WriteAllText(ReportPath, sb.ToString(), Encoding.UTF8);
        Debug.Log("[EndStageFix] -> " + ReportPath);
    }

    // ------------------------------------------------------------------
    // Dark room: raised black floor, tall black box so nothing shows over the door frame.
    // ------------------------------------------------------------------
    static void RebuildDarkRoom(Material black, StringBuilder sb)
    {
        GameObject old = GameObject.Find("TV_DarkRoom");
        if (old != null) Object.DestroyImmediate(old);

        GameObject root = new GameObject("TV_DarkRoom");
        root.layer = 30;
        root.transform.position = new Vector3(0f, 0f, -208.5f);

        // Floor top = +0.02 (white floor top is 0). Front edge stops just behind the door frame
        // face (-204.99), so no black strip can be seen from the white world.
        // Narrow room (2.4 m) so no sight line through the doorway can escape past its side walls.
        AddBox(root.transform, "Floor", new Vector3(0f, -0.08f, 1.18f), new Vector3(2.4f, 0.2f, 4.6f), black, sb);
        AddBox(root.transform, "Ceiling", new Vector3(0f, 4.6f, 1.18f), new Vector3(2.4f, 0.2f, 4.6f), black, sb);
        AddBox(root.transform, "Back", new Vector3(0f, 2.3f, -1.12f), new Vector3(2.4f, 4.6f, 0.2f), black, sb);
        AddBox(root.transform, "Left", new Vector3(-1.2f, 2.3f, 1.18f), new Vector3(0.2f, 4.6f, 4.6f), black, sb);
        AddBox(root.transform, "Right", new Vector3(1.2f, 2.3f, 1.18f), new Vector3(0.2f, 4.6f, 4.6f), black, sb);
        AddBox(root.transform, "FrontLintel", new Vector3(0f, 4.6f, 3.48f), new Vector3(2.4f, 0.2f, 0.2f), black, sb);

        // The room must be pitch black inside: make sure no light sits in it.
        foreach (var light in root.GetComponentsInChildren<Light>(true)) Object.DestroyImmediate(light);

        sb.AppendLine("dark room rebuilt: floor top=+0.02, 2.4 x 4.6 x 4.6, front edge -205.02");
    }

    // ------------------------------------------------------------------
    // White wall around the door: hides the dark room from every angle outside.
    // ------------------------------------------------------------------
    static void AddWhiteWall(StringBuilder sb)
    {
        GameObject room = GameObject.Find("TV_ExitRoom");
        if (room == null) return;

        GameObject old = GameObject.Find("EndDoorWall");
        if (old != null) Object.DestroyImmediate(old);

        Material white = EnsureWallMaterial();
        GameObject root = new GameObject("EndDoorWall");
        root.layer = 30;
        root.transform.SetParent(room.transform, false);
        root.transform.localPosition = Vector3.zero;

        // Local z = -4.975 -> world -204.975; the 6 cm slab sits 2.5 cm proud of the frame face
        // so the frame / wall seam can never show a black sliver behind it.
        const float z = -4.975f;
        float halfFrame = 1.043f;      // door frame outer edge
        float frameTop = 4.43f;        // door frame top edge

        AddWallSlab(root.transform, "Wall_L", new Vector3(-(halfFrame + 5.48f), 3.25f, z), new Vector3(10.96f, 6.5f, 0.06f), white);
        AddWallSlab(root.transform, "Wall_R", new Vector3(halfFrame + 5.48f, 3.25f, z), new Vector3(10.96f, 6.5f, 0.06f), white);
        AddWallSlab(root.transform, "Wall_Top", new Vector3(0f, (frameTop + 6.5f) * 0.5f, z), new Vector3(halfFrame * 2f, 6.5f - frameTop, 0.06f), white);

        // White threshold: covers the 2 cm step between the white floor and the raised dark floor
        // so no white sliver can be seen inside the doorway.
        AddWallSlab(root.transform, "Threshold", new Vector3(0f, 0.01f, -4.76f), new Vector3(2.0f, 0.02f, 0.46f), white);

        sb.AppendLine("white wall added around the door (doorway 2.086 x 4.43) + threshold");
    }

    static Material EnsureWallMaterial()
    {
        const string path = "Assets/Materials/Interior/TV/WhiteWorld_Wall.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        mat = new Material(shader) { name = "WhiteWorld_Wall" };
        mat.SetColor("_BaseColor", new Color(0.96f, 0.96f, 0.95f, 1f));
        mat.SetColor("_Color", new Color(0.96f, 0.96f, 0.95f, 1f));
        mat.SetFloat("_Cull", 0f);
        AssetDatabase.CreateAsset(mat, path);
        AssetDatabase.SaveAssets();
        return mat;
    }

    static void AddWallSlab(Transform parent, string name, Vector3 localPos, Vector3 localScale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.layer = 30;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;
        Object.DestroyImmediate(go.GetComponent<Collider>());

        var renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    // ------------------------------------------------------------------
    // Door mouth: fits inside the doorway only.
    // ------------------------------------------------------------------
    static void FixDoorMouth(Material black, StringBuilder sb)
    {
        GameObject mouth = GameObject.Find("TV_ExitRoom/DoorMouth");
        if (mouth == null)
        {
            sb.AppendLine("DoorMouth missing");
            return;
        }

        mouth.transform.localPosition = new Vector3(0f, 2.15f, -5.3f);   // world z = -205.30
        mouth.transform.localScale = new Vector3(2f, 4.4f, 0.1f);

        var renderer = mouth.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = black;           // URP Unlit, _Cull = 0 (double sided)

        sb.AppendLine("door mouth: 2.00 x 4.40 at z=-205.30, double-sided black");
    }

    // ------------------------------------------------------------------
    // Door leaf collider (blocks the player while the door is closed).
    // ------------------------------------------------------------------
    static void AddDoorLeafCollider(StringBuilder sb)
    {
        GameObject leaf = GameObject.Find("EndDoorHinge/Plane.001");
        if (leaf == null)
        {
            sb.AppendLine("door leaf missing");
            return;
        }

        foreach (var c in leaf.GetComponents<Collider>()) Object.DestroyImmediate(c);

        var renderer = leaf.GetComponent<Renderer>();
        if (renderer == null) return;

        Bounds b = renderer.bounds;
        Vector3 scale = leaf.transform.lossyScale;
        var box = leaf.AddComponent<BoxCollider>();
        box.size = new Vector3(
            b.size.x / Mathf.Max(0.0001f, Mathf.Abs(scale.x)),
            b.size.y / Mathf.Max(0.0001f, Mathf.Abs(scale.y)),
            b.size.z / Mathf.Max(0.0001f, Mathf.Abs(scale.z)));
        box.center = leaf.transform.InverseTransformPoint(b.center);

        sb.AppendLine($"door leaf collider: world size={b.size}");
    }

    // ------------------------------------------------------------------
    // Air walls: the player may only pass through the doorway itself.
    // ------------------------------------------------------------------
    static void AddBlockers(StringBuilder sb)
    {
        GameObject room = GameObject.Find("TV_ExitRoom");
        if (room == null) return;

        GameObject old = GameObject.Find("EndDoorBlockers");
        if (old != null) Object.DestroyImmediate(old);

        GameObject root = new GameObject("EndDoorBlockers");
        root.layer = 30;
        root.transform.SetParent(room.transform, false);
        root.transform.localPosition = Vector3.zero;

        // Local z = -5.15 -> world -205.15 (just in front of the frame).
        AddBlocker(root.transform, "Blocker_L", new Vector3(-10.25f, 1.6f, -5.15f), new Vector3(18.6f, 3.2f, 0.4f));
        AddBlocker(root.transform, "Blocker_R", new Vector3(10.25f, 1.6f, -5.15f), new Vector3(18.6f, 3.2f, 0.4f));

        sb.AppendLine("air walls added: |x| in 0.95..19.55 blocked at z=-205.15");
    }

    // ------------------------------------------------------------------
    static void AddWhiteLight(StringBuilder sb)
    {
        GameObject room = GameObject.Find("TV_ExitRoom");
        if (room == null) return;

        Transform old = room.transform.Find("WhiteLight");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        GameObject go = new GameObject("WhiteLight");
        go.layer = 30;
        go.transform.SetParent(room.transform, false);
        go.transform.localPosition = new Vector3(0f, 3.4f, -2.8f);   // world z = -202.8

        Light light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 10f;
        light.intensity = 3.4f;
        light.color = new Color(1f, 0.97f, 0.92f);
        light.shadows = LightShadows.None;
        light.renderMode = LightRenderMode.ForcePixel;

        sb.AppendLine("white world light added at (0,3.4,-202.8)");
    }

    // ------------------------------------------------------------------
    static void WireEndDoor(Material black, StringBuilder sb)
    {
        GameObject hinge = GameObject.Find("EndDoorHinge");
        if (hinge == null) return;

        var door = hinge.GetComponent<EndDoor>();
        if (door == null)
        {
            sb.AppendLine("EndDoor component missing on hinge");
            return;
        }

        door.darkLeafMaterial = black;
        if (door.doorLeaf == null) door.doorLeaf = hinge.transform.Find("Plane.001");
        if (door.hinge == null) door.hinge = hinge.transform;
        if (door.doorOpening == null)
        {
            GameObject opening = GameObject.Find("TV_ExitRoom/Door_Opening");
            if (opening != null) door.doorOpening = opening.transform;
        }
        if (door.portal == null) door.portal = Object.FindFirstObjectByType<TvPortal>();

        EditorUtility.SetDirty(door);
        sb.AppendLine($"EndDoor wired: leaf={(door.doorLeaf != null)} player={(door.player != null)} portal={(door.portal != null)} open={door.openAngle} push={door.pushDistance}");
    }

    // ------------------------------------------------------------------
    static void AddBox(Transform parent, string name, Vector3 localPos, Vector3 localScale, Material material, StringBuilder sb)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.layer = 30;
        box.transform.SetParent(parent, false);
        box.transform.localPosition = localPos;
        box.transform.localScale = localScale;
        Object.DestroyImmediate(box.GetComponent<Collider>());

        var renderer = box.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        sb.AppendLine($"  box {name} pos={localPos} scale={localScale}");
    }

    static void AddBlocker(Transform parent, string name, Vector3 localPos, Vector3 localScale)
    {
        GameObject go = new GameObject(name);
        go.layer = 30;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;
        go.AddComponent<BoxCollider>();
    }
}
