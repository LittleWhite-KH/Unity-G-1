using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>把场景里的相机渲染成 PNG，用来在编辑模式看画面（不依赖窗口截图）。</summary>
public static class DshCapture
{
    const int W = 1600;
    const int H = 900;

    [MenuItem("Tools/DSH/Capture/Start Camera")]
    public static void CaptureStart() => Capture("Main Camera", "E:/DSH_Unity/shots/start-camera.png");

    [MenuItem("Tools/DSH/Capture/Player Camera")]
    public static void CapturePlayer() => Capture("Player/Main Camera", "E:/DSH_Unity/shots/player-camera.png");

    public static void Capture(string cameraPath, string outPath)
    {
        GameObject go = GameObject.Find(cameraPath);
        if (go == null)
        {
            Debug.LogError($"[DshCapture] camera not found: {cameraPath}");
            return;
        }

        Camera cam = go.GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError($"[DshCapture] no Camera on {cameraPath}");
            return;
        }

        RenderTexture rt = RenderTexture.GetTemporary(W, H, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        RenderTexture prevTarget = cam.targetTexture;
        RenderTexture prevActive = RenderTexture.active;
        RenderTextureDescriptor prevDesc = cam.targetTexture != null ? default : default;

        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        tex.Apply();

        RenderTexture.active = prevActive;
        cam.targetTexture = prevTarget;
        RenderTexture.ReleaseTemporary(rt);

        Directory.CreateDirectory(Path.GetDirectoryName(outPath));
        File.WriteAllBytes(outPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        Debug.Log($"[DshCapture] wrote {outPath} from {cameraPath} (pos={go.transform.position}, fov={cam.fieldOfView})");
    }
}
