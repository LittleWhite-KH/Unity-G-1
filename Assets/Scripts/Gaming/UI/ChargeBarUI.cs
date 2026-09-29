using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 充能条（屏幕靠下的细长条）：
///   · 蓝色 = 已充能，白色（底槽）= 未充能；
///   · 红色 = "曾经充到过"的警告条：只在充能**回退**时露出来，下降速度比蓝色慢，
///     藏在蓝色下面，用来提示"你刚才的进度掉了"。
///
/// 用锚点做填充（不需要 sprite）：fill.anchorMin.x = 0，fill.anchorMax.x = 进度。
/// </summary>
[DisallowMultipleComponent]
public class ChargeBarUI : MonoBehaviour
{
    [Header("结构")]
    [Tooltip("已充能部分（蓝色）")]
    public RectTransform fillRect;

    [Tooltip("警告部分（红色）：会被驱动到\"历史最高进度\"，然后缓慢回落")]
    public RectTransform warningRect;

    [Tooltip("整条的整体透明度控制")]
    public CanvasGroup group;

    [Header("行为")]
    public bool hideWhenIdle = true;
    public float fadeSpeed = 8f;

    [Tooltip("蓝条追赶速度")]
    public float smoothSpeed = 14f;

    [Header("红色警告条")]
    [Tooltip("红条的回落速度（进度/秒）——必须明显小于蓝条的回落速度才会\"慢慢下降\"")]
    public float warningDecaySpeed = 0.10f;

    [Tooltip("红条自身的平滑速度")]
    public float warningSmoothSpeed = 8f;

    [Tooltip("蓝条领先红条超过该值才算\"发生回退\"，避免误触")]
    public float warningShowThreshold = 0.005f;

    [Header("调试")]
    [Range(0f, 1f)] public float previewProgress = 0f;

    private float targetProgress;
    private float displayProgress;
    private float warningProgress;
    private bool visible;

    private void Awake()
    {
        targetProgress = previewProgress;
        displayProgress = previewProgress;
        warningProgress = previewProgress;
        ApplyFill(fillRect, displayProgress);
        ApplyFill(warningRect, warningProgress);

        if (group != null && hideWhenIdle && previewProgress <= 0.001f)
        {
            group.alpha = 0f;
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        // 蓝条：追赶目标
        displayProgress = Smooth(displayProgress, targetProgress, smoothSpeed, dt);

        // 红条：只跟"历史最高"（蓝条掉下去时红条留在原地，然后缓慢回落）
        if (warningProgress < displayProgress)
        {
            warningProgress = displayProgress;
        }
        else if (warningProgress > displayProgress)
        {
            warningProgress = Mathf.Max(displayProgress, warningProgress - warningDecaySpeed * dt);
        }

        warningProgress = Smooth(warningProgress, Mathf.Max(warningProgress, displayProgress), warningSmoothSpeed, dt);

        ApplyFill(fillRect, displayProgress);

        bool showWarning = warningRect != null && (warningProgress - displayProgress) > warningShowThreshold;
        if (warningRect != null)
        {
            warningRect.gameObject.SetActive(showWarning);
            if (showWarning) ApplyFill(warningRect, warningProgress);
        }

        if (group != null && hideWhenIdle)
        {
            float targetAlpha = visible ? 1f : 0f;
            group.alpha = Smooth(group.alpha, targetAlpha, fadeSpeed, dt);
        }
    }

    /// <summary>设置充能进度 0-1。</summary>
    public void SetProgress(float progress01)
    {
        targetProgress = Mathf.Clamp01(progress01);
    }

    /// <summary>控制整条显隐（条件满足或进度不为 0 时显示）。</summary>
    public void SetVisible(bool value)
    {
        visible = value;

        if (group != null && !hideWhenIdle)
        {
            group.alpha = value ? 1f : 0f;
        }
    }

    /// <summary>完全复位（含红条）。</summary>
    public void ResetBar()
    {
        targetProgress = 0f;
        displayProgress = 0f;
        warningProgress = 0f;
        ApplyFill(fillRect, 0f);
        if (warningRect != null)
        {
            ApplyFill(warningRect, 0f);
            warningRect.gameObject.SetActive(false);
        }
    }

    private static void ApplyFill(RectTransform rect, float progress01)
    {
        if (rect == null) return;

        Vector2 min = rect.anchorMin;
        Vector2 max = rect.anchorMax;
        min.x = 0f;
        max.x = Mathf.Clamp01(progress01);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = new Vector2(0f, rect.offsetMin.y);
        rect.offsetMax = new Vector2(0f, rect.offsetMax.y);
    }

    private static float Smooth(float current, float target, float speed, float dt)
    {
        if (speed <= 0f) return target;
        return Mathf.Lerp(current, target, 1f - Mathf.Exp(-speed * dt));
    }
}
