using UnityEngine;

/// <summary>
/// 让 UI 元素做呼吸式脉冲（用于"按 M 打开地图"这类提示）。
/// </summary>
[DisallowMultipleComponent]
public class UiPulseEffect : MonoBehaviour
{
    [Tooltip("要脉冲的 CanvasGroup；留空则用本物体上的")]
    public CanvasGroup group;

    [Range(0f, 1f)] public float minAlpha = 0.30f;
    [Range(0f, 1f)] public float maxAlpha = 1f;

    [Tooltip("每秒脉冲次数")]
    public float speed = 1.2f;

    [Tooltip("是否同时做轻微缩放")]
    public bool scalePulse = false;

    [Range(0f, 0.5f)] public float scaleAmount = 0.06f;

    private RectTransform rect;

    private void Awake()
    {
        if (group == null) group = GetComponent<CanvasGroup>();
        rect = transform as RectTransform;
    }

    private void Update()
    {
        float t = (Mathf.Sin(Time.unscaledTime * speed * Mathf.PI * 2f) + 1f) * 0.5f;

        if (group != null)
        {
            group.alpha = Mathf.Lerp(minAlpha, maxAlpha, t);
        }

        if (scalePulse && rect != null)
        {
            float scale = 1f + (t - 0.5f) * 2f * scaleAmount;
            rect.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
