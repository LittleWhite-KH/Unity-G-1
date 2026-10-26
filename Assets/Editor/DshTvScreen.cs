using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 电视屏幕的“门”画面：
///   1) 从走廊入口渲染一张 4:3 图（和玩家进电视后看到的构图一致）；
///   2) 生成 URP Unlit 材质，赋给迷宫电视和 Ending 大电视的 Screen。
/// </summary>
public static class DshTvScreen
{
    const string PngPath = "Assets/Textures/Ending/tv_door.png";
    const string MatPath = "Assets/Materials/Interior/TV/TV_Screen_Door.mat";

    [MenuItem("Tools/DSH/TV/Render Door Image")]
    public static void Render()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Ending.unity", OpenSceneMode.Single);

        GameObject go = new GameObject("DshTmpCam");
        Camera cam = go.AddComponent<Camera>();
        cam.fieldOfView = 60f;
        cam.nearClipPlane = 0.05f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        go.transform.position = new Vector3(0f, 1.45f, -77.4f);
        go.transform.LookAt(new Vector3(0f, 1.1f, -82.94f));

        // 渲染时临时补光，让“门”在屏幕里看得清
        GameObject lamp = new GameObject("DshTmpLamp");
        Light l = lamp.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = 10f;
        l.intensity = 10f;
        l.color = new Color(0.85f, 0.92f, 1f);
        lamp.transform.position = new Vector3(0f, 1.9f, -81.6f);

        const int W = 1024, H = 768;
        RenderTexture rt = RenderTexture.GetTemporary(W, H, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        tex.Apply();

        RenderTexture.active = null;
        cam.targetTexture = null;
        RenderTexture.ReleaseTemporary(rt);
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(lamp);

        Directory.CreateDirectory(Path.GetDirectoryName(PngPath));
        File.WriteAllBytes(PngPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.Refresh();
        Debug.Log($"[TV] rendered door image to {PngPath}");
    }

    [MenuItem("Tools/DSH/TV/Apply Door Image")]
    public static void Apply()
    {
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(PngPath);
        if (tex == null) { Debug.LogError($"[TV] texture not found {PngPath}"); return; }

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            mat = new Material(shader) { name = "TV_Screen_Door" };
            mat.SetTexture("_BaseMap", tex);
            mat.mainTexture = tex;
            AssetDatabase.CreateAsset(mat, MatPath);
            AssetDatabase.SaveAssets();
        }

        ApplyToScene("Assets/Scenes/Ending.unity", "Ending_TV", true);
        ApplyToScene("Assets/Scenes/Gaming.unity", "TV_Ending", false);

        EditorSceneManager.OpenScene("Assets/Scenes/Gaming.unity", OpenSceneMode.Single);
        Debug.Log("[TV] applied door image to both TVs");
    }

    static void ApplyToScene(string scenePath, string tvName, bool fixEntry)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        GameObject tv = GameObject.Find(tvName);
        if (tv == null) { Debug.LogWarning($"[TV] {tvName} not found in {scenePath}"); return; }

        foreach (Renderer r in tv.GetComponentsInChildren<Renderer>(true))
        {
            if (!r.name.ToLower().Contains("screen")) continue;
            r.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
            Debug.Log($"[TV] {scenePath} {r.name} -> screen material");
        }

        if (fixEntry)
        {
            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                Vector3 p = player.transform.position;
                p.z = 1.16f; // 0.36m * 屏幕放大比 3.23
                player.transform.position = p;
                Debug.Log($"[TV] player entry z -> {p.z}");
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
