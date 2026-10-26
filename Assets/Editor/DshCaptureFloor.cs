using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>地板特写机位，用于验证 z-fighting 修复效果。</summary>
public static class DshCaptureFloor
{
    [MenuItem("Tools/DSH/Capture/Floor Close")]
    public static void ShotClose() => Shot(new Vector3(0f, 1.8f, 2f), new Vector3(0f, 0f, 7f), "E:/DSH_Unity/shots/floor-close.png");

    [MenuItem("Tools/DSH/Capture/Floor Wide")]
    public static void ShotWide() => Shot(new Vector3(-6f, 1.6f, -4f), new Vector3(6f, 0f, 3f), "E:/DSH_Unity/shots/floor-wide.png");

    [MenuItem("Tools/DSH/Capture/Floor Spawn")]
    public static void ShotSpawn() => Shot(new Vector3(-34.2f, 1.45f, 3f), new Vector3(-28f, 0.1f, 3f), "E:/DSH_Unity/shots/floor-spawn.png");

    public static void Shot(Vector3 eye, Vector3 look, string outPath)
    {
        GameObject go = new GameObject("DshTempFloorCam");
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
        Debug.Log($"[CaptureFloor] {outPath}");
    }
}
