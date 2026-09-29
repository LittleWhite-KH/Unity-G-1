// 可靠的 UI 渲染预览：把所有 ScreenSpaceOverlay 画布临时切到相机模式渲染到 2560x1440
// 菜单：Tools/DSH/Render Preview、Tools/DSH/Render Preview (Map Open)
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

internal static class DshPreview
{
    private const int W = 2560;
    private const int H = 1440;

    [MenuItem("Tools/DSH/Render Preview")]
    private static void RenderHud() => Render("preview-hud.png", false);

    [MenuItem("Tools/DSH/Render Preview (Map Open)")]
    private static void RenderMap() => Render("preview-map.png", true);

    private static void Render(string fileName, bool openMap)
    {
        string outPath = Path.Combine(@"E:\DSH_Unity\shots", fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(outPath));

        var switched = new List<Canvas>();
        GameObject camGo = null;
        RenderTexture rt = null;

        GameObject mapGo = openMap ? Find("MapContent") : null;
        bool mapWasActive = mapGo != null && mapGo.activeSelf;

        GameObject panelGo = openMap ? Find("MapPanel") : null;
        CanvasGroup panelGroup = panelGo != null ? panelGo.GetComponent<CanvasGroup>() : null;
        float savedAlpha = panelGroup != null ? panelGroup.alpha : 1f;

        try
        {
            if (mapGo != null) mapGo.SetActive(true);
            if (panelGroup != null) panelGroup.alpha = 1f;   // 地图关闭时 alpha=0，预览要强制可见

            camGo = new GameObject("DshPreviewCamera");
            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            // 编辑模式下 Screen 返回物理屏幕尺寸，而画布尺寸也是按它算的 → 必须用它
            cam.orthographicSize = Screen.height * 0.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.925f, 0.933f, 0.94f, 1f);
            cam.cullingMask = 1 << 5;              // 只渲染 UI 层
            cam.nearClipPlane = 1f;
            cam.farClipPlane = 1000f;
            cam.transform.position = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, -200f);
            cam.transform.rotation = Quaternion.identity;

            rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;

            foreach (Canvas c in Resources.FindObjectsOfTypeAll<Canvas>())
            {
                if (c == null || !c.isRootCanvas) continue;
                if (!c.gameObject.scene.IsValid()) continue;
                if (c.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                if (!c.gameObject.activeInHierarchy) continue;

                switched.Add(c);
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = cam;
                c.planeDistance = 100f;
            }

            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            File.WriteAllBytes(outPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            RenderTexture.active = null;

            Debug.Log($"[DSH] 预览导出：{outPath}（画布 {switched.Count} 个，地图 {(openMap ? "强制打开" : "原状")}）");
        }
        finally
        {
            foreach (Canvas c in switched)
            {
                if (c == null) continue;
                c.renderMode = RenderMode.ScreenSpaceOverlay;
                c.worldCamera = null;
            }

            if (mapGo != null && !mapWasActive) mapGo.SetActive(false);
            if (panelGroup != null) panelGroup.alpha = savedAlpha;

            if (camGo != null)
            {
                Camera cam = camGo.GetComponent<Camera>();
                if (cam != null) cam.targetTexture = null;   // 必须先解绑再释放
            }

            if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
            if (camGo != null) Object.DestroyImmediate(camGo);
        }
    }

    private static GameObject Find(string name)
    {
        foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go != null && go.name == name && go.scene.IsValid() && go.transform.parent != null) return go;
        }

        foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go != null && go.name == name && go.scene.IsValid()) return go;
        }

        return null;
    }
}
