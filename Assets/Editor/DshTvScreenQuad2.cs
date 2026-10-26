using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 电视屏幕上门画面的最终方案：
/// 曲面 Screen 网格 UV 不是 0-1，另建一块世界空间 Quad 盖在屏幕前 2~3cm，
/// 朝向玩家（Quad 正面 -Z），贴 door 图。
/// </summary>
public static class DshTvScreenQuad2
{
    const string PngPath = "Assets/Textures/Ending/tv_door.png";
    const string MatPath = "Assets/Materials/Interior/TV/TV_Screen_Door.mat";

    [MenuItem("Tools/DSH/TV/Build Door Quad")]
    public static void Build()
    {
        ApplyToScene("Assets/Scenes/Ending.unity", "Ending_TV", true);
        ApplyToScene("Assets/Scenes/Gaming.unity", "TV_Ending", false);
        EditorSceneManager.OpenScene("Assets/Scenes/Gaming.unity", OpenSceneMode.Single);
        Debug.Log("[TV] door quads built");
    }

    static void ApplyToScene(string scenePath, string tvName, bool isEnding)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
        if (mat == null)
        {
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(PngPath);
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            mat = new Material(shader) { name = "TV_Screen_Door" };
            mat.SetTexture("_BaseMap", tex);
            mat.mainTexture = tex;
            AssetDatabase.CreateAsset(mat, MatPath);
        }

        mat.SetFloat("_Cull", 0f);
        mat.SetColor("_BaseColor", Color.white);
        EditorUtility.SetDirty(mat);

        GameObject old = GameObject.Find("DoorQuad");
        if (old != null) Object.DestroyImmediate(old);

        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "DoorQuad";
        Object.DestroyImmediate(quad.GetComponent<Collider>());

        if (isEnding)
        {
            quad.transform.position = new Vector3(0f, 1.45f, 1.56f);
            quad.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            quad.transform.localScale = new Vector3(2.20f, 1.75f, 1f);
        }
        else
        {
            quad.transform.position = new Vector3(29.015f, 1.545f, 9f);
            quad.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            quad.transform.localScale = new Vector3(0.88f, 0.70f, 1f);
        }

        quad.GetComponent<MeshRenderer>().sharedMaterial = mat;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[TV] {scenePath} quad at {quad.transform.position} rot={quad.transform.eulerAngles} scale={quad.transform.localScale}");
    }
}
