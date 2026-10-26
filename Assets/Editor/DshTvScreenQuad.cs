using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 电视屏幕显示“门”的画面：
/// 曲面 Screen 网格的 UV 不是 0-1，直接贴图会花，所以禁用它，用一块双面 Quad 盖在屏幕位置。
/// </summary>
public static class DshTvScreenQuad
{
    const string PngPath = "Assets/Textures/Ending/tv_door.png";
    const string MatPath = "Assets/Materials/Interior/TV/TV_Screen_Door.mat";

    [MenuItem("Tools/DSH/TV/Apply Screen Quad")]
    public static void Apply()
    {
        ApplyToScene("Assets/Scenes/Ending.unity", "Ending_TV");
        ApplyToScene("Assets/Scenes/Gaming.unity", "TV_Ending");
        EditorSceneManager.OpenScene("Assets/Scenes/Gaming.unity", OpenSceneMode.Single);
        Debug.Log("[TV] screen quads applied");
    }

    static void ApplyToScene(string scenePath, string tvName)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        GameObject tv = GameObject.Find(tvName);
        if (tv == null) { Debug.LogWarning($"[TV] {tvName} not found"); return; }

        // 1) 找 Screen 网格并禁用
        Renderer screenRenderer = null;
        foreach (Renderer r in tv.GetComponentsInChildren<Renderer>(true))
        {
            if (r.name.ToLower().Contains("screen")) { screenRenderer = r; break; }
        }

        if (screenRenderer == null) { Debug.LogWarning($"[TV] no screen renderer in {tvName}"); return; }

        Bounds b = screenRenderer.bounds;
        screenRenderer.enabled = false;

        // 2) 旧的 Quad
        Transform old = tv.transform.Find("ScreenQuad");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        // 3) 新 Quad（用模型原始屏幕尺寸：0.53 x 0.42，位于机身正面内侧）
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "ScreenQuad";
        Object.DestroyImmediate(quad.GetComponent<Collider>());
        quad.transform.SetParent(tv.transform, false);
        quad.transform.localPosition = new Vector3(0f, 0.382f, 0.655f);
        quad.transform.localRotation = Quaternion.identity;
        quad.transform.localScale = new Vector3(0.53f, 0.42f, 1f);

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
        if (mat == null)
        {
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(PngPath);
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            mat = new Material(shader) { name = "TV_Screen_Door" };
            mat.SetTexture("_BaseMap", tex);
            mat.mainTexture = tex;
            mat.SetFloat("_Cull", 0f);
            AssetDatabase.CreateAsset(mat, MatPath);
        }
        quad.GetComponent<MeshRenderer>().sharedMaterial = mat;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[TV] {scenePath} quad at {quad.transform.position} size={quad.transform.localScale}");
    }
}
