using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>给 Ending 走廊的门加把手和门缝光，提升可读性。</summary>
public static class DshAddDoorDetail
{
    [MenuItem("Tools/DSH/Ending/Add Door Detail")]
    public static void Build()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Ending.unity", OpenSceneMode.Single);

        GameObject door = GameObject.Find("Ending_Door");
        if (door == null) { Debug.LogError("[DoorDetail] Ending_Door missing"); return; }

        // 旧件清理
        foreach (string n in new[] { "DoorHandle", "DoorGapLight" })
        {
            Transform old = door.transform.Find(n);
            if (old != null) Object.DestroyImmediate(old.gameObject);
        }

        Material metal = Mat("Door_Handle", new Color(0.55f, 0.55f, 0.58f), 0.75f);
        Material glow = Mat("Door_Gap", new Color(1f, 0.97f, 0.9f), 0.3f);

        // 把手（横向圆柱）
        GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        handle.name = "DoorHandle";
        Object.DestroyImmediate(handle.GetComponent<Collider>());
        handle.transform.SetParent(door.transform, true);
        handle.transform.position = new Vector3(0.42f, 1.02f, door.transform.position.z + 0.06f);
        handle.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        handle.transform.localScale = new Vector3(0.035f, 0.07f, 0.035f);
        handle.GetComponent<MeshRenderer>().sharedMaterial = metal;

        // 门缝光（底部细条）
        GameObject gap = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gap.name = "DoorGapLight";
        Object.DestroyImmediate(gap.GetComponent<Collider>());
        gap.transform.SetParent(door.transform, true);
        gap.transform.position = new Vector3(0f, 0.035f, door.transform.position.z + 0.045f);
        gap.transform.localScale = new Vector3(1.16f, 0.03f, 0.02f);
        gap.GetComponent<MeshRenderer>().sharedMaterial = glow;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[DoorDetail] handle + gap light added");
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
