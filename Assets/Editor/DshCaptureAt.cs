using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>从指定位置渲染一张检查图（用于确认模型摆放/朝向）。</summary>
public static class DshCaptureAt
{
    [MenuItem("Tools/DSH/Capture/AppleII 1")]
    public static void Shot1() => Shot(new Vector3(1.85f, 1.35f, 7.2f), new Vector3(1.85f, 0.25f, 9f), "E:/DSH_Unity/shots/appleii-1.png");

    [MenuItem("Tools/DSH/Capture/AppleII 2")]
    public static void Shot2() => Shot(new Vector3(-19.85f, 1.35f, 1.2f), new Vector3(-19.85f, 0.25f, 3f), "E:/DSH_Unity/shots/appleii-2.png");

    [MenuItem("Tools/DSH/Capture/Wall Guide")]
    public static void ShotGuide() => Shot(new Vector3(-34.5f, 1.5f, 2.8f), new Vector3(-34.5f, 1.72f, 0.13f), "E:/DSH_Unity/shots/wall-guide.png");

    [MenuItem("Tools/DSH/Capture/TV Wall")]
    public static void ShotTv() => Shot(new Vector3(27.1f, 1.45f, 9f), new Vector3(29.4f, 1.35f, 9f), "E:/DSH_Unity/shots/tv-wall.png");

    [MenuItem("Tools/DSH/Capture/Spawn View")]
    public static void ShotSpawn() => Shot(new Vector3(-34.5f, 1.45f, 3f), new Vector3(-28f, 1.1f, 3f), "E:/DSH_Unity/shots/walk-spawn.png");

    [MenuItem("Tools/DSH/Capture/Tomb View")]
    public static void ShotTomb() => Shot(new Vector3(-3f, 1.45f, 7.6f), new Vector3(-3f, 1.0f, 11.2f), "E:/DSH_Unity/shots/walk-tomb.png");
    [MenuItem("Tools/DSH/Capture/Furn East")]
    public static void ShotFurnEast() => Shot(new Vector3(20.0f, 1.55f, 0.0f), new Vector3(25.0f, 0.7f, 3.2f), "E:/DSH_Unity/shots/furn-east.png");

    [MenuItem("Tools/DSH/Capture/Furn Table")]
    public static void ShotFurnTable() => Shot(new Vector3(18.0f, 1.55f, -6.0f), new Vector3(21.0f, 0.6f, -9.0f), "E:/DSH_Unity/shots/furn-table.png");
    [MenuItem("Tools/DSH/Capture/Ending Wide")]
    public static void ShotEndingWide() => Shot(new Vector3(0f, 1.6f, 9f), new Vector3(0f, 1.2f, 0f), "E:/DSH_Unity/shots/ending-wide.png");

    [MenuItem("Tools/DSH/Capture/Ending Corridor")]
    public static void ShotCorridor() => Shot(new Vector3(0f, 1.45f, -77.4f), new Vector3(0f, 1.1f, -82.6f), "E:/DSH_Unity/shots/ending-corridor.png");

    static void Shot(Vector3 eye, Vector3 look, string outPath)
    {
        GameObject go = new GameObject("DshTempCaptureCam");
        Camera cam = go.AddComponent<Camera>();
        cam.fieldOfView = 60f;
        cam.nearClipPlane = 0.05f;
        go.transform.position = eye;
        go.transform.LookAt(look);

        const int W = 1280, H = 720;
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

        Directory.CreateDirectory(Path.GetDirectoryName(outPath));
        File.WriteAllBytes(outPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        Debug.Log($"[CaptureAt] {outPath}");
    }
}
