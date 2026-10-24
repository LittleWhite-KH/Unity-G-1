using UnityEngine;
using System.Collections;

public class SceneFadeIn : MonoBehaviour
{
    [Header("淡入速度 (Alpha 减少量/秒)")]
    public float fadeSpeed = 1.0f;   // 数值越大，白屏消失越快

    [Tooltip("淡入完成后是否停用遮罩 Canvas（推荐开启：透明全屏 UI 仍会参与射线检测与合批）")]
    public bool deactivateWhenDone = true;

    private UnityEngine.UI.Image fadeImage;
    private GameObject fadeCanvas;

    void Start()
    {
        CreateFadeCanvas();
        StartCoroutine(FadeInCoroutine());
    }

    // 创建全屏白色遮罩 Canvas 和 Image
    private void CreateFadeCanvas()
    {
        fadeCanvas = new GameObject("FadeInCanvas");
        fadeCanvas.transform.SetParent(transform);

        Canvas canvas = fadeCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;   // 确保覆盖所有 UI

        fadeCanvas.AddComponent<UnityEngine.UI.CanvasScaler>();
        fadeCanvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(fadeCanvas.transform);

        fadeImage = imageObj.AddComponent<UnityEngine.UI.Image>();
        fadeImage.color = new Color(1, 1, 1, 1); // 初始为完全不透明（全白）

        // 关键修复：淡入遮罩只负责视觉，绝不能吃鼠标射线。
        // 否则 1 秒淡出后这层全屏透明 Image 会一直挡在地图 BACK、侧栏按钮等所有 UI 上面。
        fadeImage.raycastTarget = false;

        // 拉伸至全屏
        RectTransform rect = fadeImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
    }

    private IEnumerator FadeInCoroutine()
    {
        float alpha = 1f;
        while (alpha > 0f)
        {
            alpha -= fadeSpeed * Time.deltaTime;
            fadeImage.color = new Color(1, 1, 1, alpha);
            yield return null;
        }

        fadeImage.color = new Color(1, 1, 1, 0); // 完全透明

        // 淡入结束后停用整块遮罩，避免它继续参与 UI 的射线检测与 Canvas 重建。
        if (deactivateWhenDone && fadeCanvas != null)
        {
            fadeCanvas.SetActive(false);
        }
    }
}
