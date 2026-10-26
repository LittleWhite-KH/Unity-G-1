using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 简化版电视亮屏：直接给 Screen 网格一个自发光浅蓝材质，先保证“电视是开着的”观感。
/// 门画面（曲面 UV 问题）后面再单独处理。
/// </summary>
public static class DshTvScreenSimple
{
    const string MatPath = "Assets/Materials/Interior/TV/TV_Screen_On.mat";

    [MenuItem("Tools/DSH/TV/Simple Screen On")]
    public static void Apply()
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            mat = new Material(shader) { name = "TV_Screen_On" };
            mat.color = new Color(0.45f, 0.62f, 0.85f);
            mat.SetTexture("_BaseMap", null);
            AssetDatabase.CreateAsset(mat, MatPath);
            AssetDatabase.SaveAssets();
        }

        ApplyToScene("Assets/Scenes/Ending.unity", "Ending_TV");
        ApplyToScene("Assets/Scenes/Gaming.unity", "TV_Ending");
        EditorSceneManager.OpenScene("Assets/Scenes/Gaming.unity", OpenSceneMode.Single);
        Debug.Log("[TV] simple screen on applied");
    }

    static void ApplyToScene(string scenePath, string tvName)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        GameObject tv = GameObject.Find(tvName);
        if (tv == null) { Debug.LogWarning($"[TV] {tvName} not found"); return; }

        Transform quad = tv.transform.Find("ScreenQuad");
        if (quad != null) Object.DestroyImmediate(quad.gameObject);

        foreach (Renderer r in tv.GetComponentsInChildren<Renderer>(true))
        {
            if (!r.name.ToLower().Contains("screen")) continue;
            r.enabled = true;
            r.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
