using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>两台旧电脑的正面特写机位（从房间开阔侧看）。</summary>
public static class DshCaptureOldPC
{
    [MenuItem("Tools/DSH/Capture/Old PC 1 Front")]
    public static void Shot1()
    {
        Shot(new Vector3(3.35f, 1.40f, 8.96f), new Vector3(2.30f, 0.92f, 8.96f), "E:/DSH_Unity/shots/oldpc1-front.png");
        Shot(new Vector3(1.25f, 1.35f, 12.2f), new Vector3(1.85f, 0.35f, 9.0f), "E:/DSH_Unity/shots/oldpc1-side.png");
    }

    [MenuItem("Tools/DSH/Capture/Old PC 2 Front")]
    public static void Shot2()
    {
        Shot(new Vector3(-21.35f, 1.40f, 3.0f), new Vector3(-20.05f, 0.92f, 3.0f), "E:/DSH_Unity/shots/oldpc2-front.png");
        Shot(new Vector3(-19.85f, 1.35f, 6.2f), new Vector3(-19.85f, 0.35f, 3.0f), "E:/DSH_Unity/shots/oldpc2-side.png");
    }

    static void Shot(Vector3 eye, Vector3 look, string outPath)
    {
        var go = new GameObject("DshTempPcCam");
        var cam = go.AddComponent<Camera>();
        cam.fieldOfView = 55f;
        cam.nearClipPlane = 0.05f;
        go.transform.position = eye;
        go.transform.LookAt(look);

        const int W = 1280, H = 720;
        var rt = RenderTexture.GetTemporary(W, H, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        tex.Apply();

        RenderTexture.active = null;
        cam.targetTexture = null;
        RenderTexture.ReleaseTemporary(rt);
        Object.DestroyImmediate(go);

        Directory.CreateDirectory(Path.GetDirectoryName(outPath));
        File.WriteAllBytes(outPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        Debug.Log("[CaptureOldPC] " + outPath);
    }
}
