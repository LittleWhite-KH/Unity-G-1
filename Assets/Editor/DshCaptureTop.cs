using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>临时隐藏天花板/灯板，从正上方俯拍迷宫，检查家具分布。</summary>
public static class DshCaptureTop
{
    [MenuItem("Tools/DSH/Capture/Maze Top Down")]
    public static void Capture()
    {
        string[] hideNames = { "Ceiling", "CeilingGrid", "LightPanels" };
        var disabled = new System.Collections.Generic.List<Renderer>();

        foreach (string n in hideNames)
        {
            GameObject go = GameObject.Find("MazeRoot/MazeArchitecture/" + n);
            if (go == null) continue;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (r.enabled) { r.enabled = false; disabled.Add(r); }
            }
        }

        GameObject camGo = new GameObject("DshTopCam");
        Camera cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 55f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 200f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.09f, 0.11f);
        camGo.transform.position = new Vector3(0f, 78f, 0f);
        camGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        GameObject sunGo = new GameObject("DshTopSun");
        Light sun = sunGo.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.2f;
        sun.color = Color.white;
        sunGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        const int W = 1920, H = 900;
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
        Object.DestroyImmediate(camGo);
        Object.DestroyImmediate(sunGo);

        foreach (Renderer r in disabled) r.enabled = true;

        Directory.CreateDirectory("E:/DSH_Unity/shots");
        string path = "E:/DSH_Unity/shots/maze-top-furnished.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        Debug.Log($"[TopCam] wrote {path}");
    }
}
