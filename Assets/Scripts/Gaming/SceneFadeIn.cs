using UnityEngine;
using System.Collections;

public class SceneFadeIn : MonoBehaviour
{
    [Header("淡入速度 (Alpha 减少量/秒)")]
    public float fadeSpeed = 1.0f;   // 数值越大，白屏消失越快

    private UnityEngine.UI.Image fadeImage;

    void Start()
    {
        CreateFadeCanvas();
        StartCoroutine(FadeInCoroutine());
    }

    // 创建全屏白色遮罩 Canvas 和 Image
    private void CreateFadeCanvas()
    {
        GameObject canvasObj = new GameObject("FadeInCanvas");
        canvasObj.transform.SetParent(transform);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;   // 确保覆盖所有 UI

        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(canvasObj.transform);

        fadeImage = imageObj.AddComponent<UnityEngine.UI.Image>();
        fadeImage.color = new Color(1, 1, 1, 1); // 初始为完全不透明（全白）

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

        // 可选：如果不再需要遮罩，可以销毁 Canvas，但保留也不影响
        // Destroy(fadeImage.transform.parent.gameObject);
        // 或保留以备后续使用
    }
}