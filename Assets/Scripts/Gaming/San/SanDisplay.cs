using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 把 <see cref="SanSystem"/> 的数值接到波形部件（<see cref="SanBarGraphic"/>）与数字文本上，
/// 并负责两段演出动画：
///   1) 开场：波形"平的 → 平稳阶段"（策划案 Part1-1）
///   2) "全部变长"：波形整体冲高再回落，持续 1.2 秒（策划案 Part1-3 / Part2-3）
///
/// 本组件不做流程判断——什么时候播放由各 Phase 脚本调用。
/// </summary>
[DisallowMultipleComponent]
public class SanDisplay : MonoBehaviour
{
    [Header("数据源")]
    public SanSystem sanSystem;

    [Header("显示目标")]
    public SanBarGraphic bars;

    [Tooltip("数字文本（参考图右下角的 SAN 10）。留空则不更新数字")]
    public TextMeshProUGUI valueText;

    [Tooltip("数字格式，{0} 会被替换成取整后的 SAN 值")]
    public string valueFormat = "{0}";

    [Header("开场：平的 → 平稳")]
    [Tooltip("稳定性从 0 爬到 1 所需秒数")]
    public float stabilizeDuration = 2.5f;

    public AnimationCurve stabilizeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("进入播放时先等待的秒数（给运镜/对白留时间）")]
    public float stabilizeDelay = 0f;

    [Header("全部变长")]
    [Tooltip("冲击峰值倍率")]
    public float impactPeak = 1.9f;

    [Tooltip("冲击持续秒数（策划案要求 1.2 秒）")]
    public float impactDuration = 1.2f;

    [Header("波动联动")]
    [Tooltip("波动强度的平滑速度（越大越紧跟 SAN 分档）")]
    public float fluctuationSmoothSpeed = 3f;

    [Tooltip("额外波动增益：由外部（靠近墓碑的程度）叠加，0 = 不叠加")]
    [Range(0f, 1f)] public float proximityBoost = 0f;

    [Tooltip("凝聚度 0-1：1 = 所有柱子合成一整块矩形（进入范围并看向目标时）")]
    [Range(0f, 1f)] public float coalesce = 0f;

    public float coalesceSpeed = 3f;

    [Header("方位联动（把波形当指南针）")]
    [Tooltip("由 ChargeSystem 打开的方位模式")]
    public bool useDirectionMode = false;

    [Range(0f, 1f)] public float directionCenter = 0.5f;
    [Range(0.01f, 1f)] public float directionWidth = 0.2f;
    [Range(0f, 1f)] public float directionStrength = 1f;
    [Range(0f, 3f)] public float jitterBoost = 0f;

    [Header("可见性")]
    public CanvasGroup canvasGroup;

    private Coroutine stabilizeRoutine;
    private bool isStabilized;
    private float smoothFluctuation = 1f;
    private float smoothCoalesce;

    private void Awake()
    {
        if (bars == null)
        {
            bars = GetComponentInChildren<SanBarGraphic>(true);
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
    }

    private void Start()
    {
        ApplyValue(instant: true);

        // 初始波动强度直接对齐当前 SAN 分档，避免开场时从 1 突然收敛
        if (sanSystem != null)
        {
            smoothFluctuation = Mathf.Clamp01(sanSystem.Fluctuation + proximityBoost);
        }
    }

    private void Update()
    {
        ApplyValue(instant: false);
        ApplyShape(Time.deltaTime);
    }

    /// <summary>把 SAN 的分档波动 + 外部增益（靠近程度）平滑地推给波形部件。</summary>
    private void ApplyShape(float deltaTime)
    {
        if (bars == null)
        {
            return;
        }

        float bandFluctuation = sanSystem != null ? sanSystem.Fluctuation : 1f;
        float targetFluctuation = Mathf.Clamp01(bandFluctuation + proximityBoost);

        float targetCoalesce = Mathf.Clamp01(coalesce);

        if (deltaTime <= 0f)
        {
            smoothFluctuation = targetFluctuation;
            smoothCoalesce = targetCoalesce;
        }
        else
        {
            // 1 - e^(-k·dt) —— 与帧率无关的平滑
            if (fluctuationSmoothSpeed > 0f)
            {
                float k = 1f - Mathf.Exp(-fluctuationSmoothSpeed * deltaTime);
                smoothFluctuation = Mathf.Lerp(smoothFluctuation, targetFluctuation, k);
            }
            else
            {
                smoothFluctuation = targetFluctuation;
            }

            if (coalesceSpeed > 0f)
            {
                float k = 1f - Mathf.Exp(-coalesceSpeed * deltaTime);
                smoothCoalesce = Mathf.Lerp(smoothCoalesce, targetCoalesce, k);
            }
            else
            {
                smoothCoalesce = targetCoalesce;
            }
        }

        bars.SetFluctuation(smoothFluctuation);
        bars.SetCoalesce(smoothCoalesce);

        // 方位模式：哪一侧高 = 目标在哪一侧
        if (useDirectionMode)
        {
            bars.SetDirection(directionCenter, directionWidth, directionStrength);
            bars.SetJitterBoost(jitterBoost);
        }
    }

    /// <summary>方位模式（ChargeSystem 在关卡开始时打开）。</summary>
    public void SetDirectionMode(bool enabled)
    {
        useDirectionMode = enabled;

        if (bars != null)
        {
            bars.directionMode = enabled;
        }
    }

    /// <summary>目标位置 / 影响半宽 / 强度。</summary>
    public void SetDirection(float center01, float width01, float strength01)
    {
        useDirectionMode = true;
        directionCenter = Mathf.Clamp01(center01);
        directionWidth = Mathf.Clamp(width01, 0.01f, 1f);
        directionStrength = Mathf.Clamp01(strength01);
    }

    public void SetJitterBoost(float value)
    {
        jitterBoost = Mathf.Max(0f, value);
    }

    /// <summary>外部叠加的波动增益（例如"离墓碑越近波动越强"）。</summary>
    public void SetProximityBoost(float value01)
    {
        proximityBoost = Mathf.Clamp01(value01);
    }

    /// <summary>设置凝聚度（1 = 合成一整块矩形）。</summary>
    public void SetCoalesce(float value01)
    {
        coalesce = Mathf.Clamp01(value01);
    }

    public float CurrentCoalesce => smoothCoalesce;

    private void ApplyValue(bool instant)
    {
        if (sanSystem == null)
        {
            return;
        }

        if (bars != null)
        {
            bars.SetValue(sanSystem.Value);
        }

        if (valueText != null)
        {
            int rounded = Mathf.RoundToInt(sanSystem.Value);
            string text = string.Format(valueFormat, rounded);
            if (valueText.text != text)
            {
                valueText.text = text;
            }
        }

        if (instant && bars != null)
        {
            bars.SetVerticesDirty();
        }
    }

    // ------------------------------------------------------------------
    // 演出接口
    // ------------------------------------------------------------------

    /// <summary>策划案 Part1-1：波形从"平的"过渡到"平稳阶段"。</summary>
    public void BeginStabilize()
    {
        if (stabilizeRoutine != null)
        {
            StopCoroutine(stabilizeRoutine);
        }

        stabilizeRoutine = StartCoroutine(StabilizeRoutine());
    }

    /// <summary>立刻跳到平稳状态（不做动画）。</summary>
    public void SetStabilized(bool stabilized)
    {
        isStabilized = stabilized;

        if (bars != null)
        {
            bars.SetStability(stabilized ? 1f : 0f);
        }
    }

    public bool IsStabilized => isStabilized;

    /// <summary>策划案 Part1-3 / Part2-3：波形"全部变长"。</summary>
    public void PlayImpact()
    {
        if (bars != null)
        {
            bars.PlayImpact(impactPeak, impactDuration);
        }
    }

    /// <summary>淡入/淡出（供 UI 解锁那一步用）。</summary>
    public void SetAlpha(float alpha)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.Clamp01(alpha);
        }
    }

    private IEnumerator StabilizeRoutine()
    {
        isStabilized = false;

        if (bars != null)
        {
            bars.SetStability(0f);
        }

        if (stabilizeDelay > 0f)
        {
            yield return new WaitForSeconds(stabilizeDelay);
        }

        float duration = Mathf.Max(0.01f, stabilizeDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = stabilizeCurve != null ? stabilizeCurve.Evaluate(t) : t;

            if (bars != null)
            {
                bars.SetStability(eased);
            }

            yield return null;
        }

        if (bars != null)
        {
            bars.SetStability(1f);
        }

        isStabilized = true;
        stabilizeRoutine = null;
    }
}
