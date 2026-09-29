using System;
using UnityEngine;

/// <summary>
/// 全局 SAN 值（0-100）——"整体 SAN 目标"：从游戏开始到结束逐步提升的那一条主轴。
///
/// 三段（按策划案：中/低 = 20，中/安全 = 60）：
///   Low  低   : 0        ~ 20
///   Mid  中   : 20       ~ 60
///   Safe 安全 : 60       ~ 100   （策划案里也写作"高阶段"）
///
/// 本组件只做三件事：持有数值、按阈值判定阶段、广播事件。
/// 它不碰 UI、不碰场景、不碰玩家——那些由 SanDisplay / 各 Phase 脚本订阅事件来完成。
/// </summary>
[DisallowMultipleComponent]
public class SanSystem : MonoBehaviour
{
    public enum Stage
    {
        Low = 0,
        Mid = 1,
        Safe = 2
    }

    /// <summary>
    /// 波动分档（策划案）：0-20 剧烈、20-60 中等、60-100 几乎不波动。
    /// 阈值直接复用 midThreshold / safeThreshold，不额外维护一套数字。
    /// </summary>
    public enum FluctuationBand
    {
        Violent = 0,
        Medium = 1,
        Calm = 2
    }

    [Header("数值（0-100）")]
    [Tooltip("当前 SAN 值。运行时会朝 Target Value 逼近")]
    [SerializeField] private float currentValue = 0f;

    [Tooltip("目标 SAN 值。策划案里\"逐步提升\"指的就是它被一步步抬高")]
    [SerializeField] private float targetValue = 0f;

    [Tooltip("当前值追向目标值的速度（值/秒）。0 附近=很慢的渐变，越大越立刻")]
    [SerializeField] private float followSpeed = 12f;

    [Header("三段阈值")]
    [Tooltip("低于该值为 Low（低）")]
    [SerializeField] private float midThreshold = 20f;

    [Tooltip("达到该值为 Safe（安全 / 高）")]
    [SerializeField] private float safeThreshold = 60f;

    [Header("调试")]
    [SerializeField] private bool logStageChanges = false;

    [Header("波动强度（按上面两档阈值自动分档）")]
    [Tooltip("0-20：剧烈波动")]
    [Range(0f, 1f)] [SerializeField] private float violentFluctuation = 1f;

    [Tooltip("20-60：中等波动")]
    [Range(0f, 1f)] [SerializeField] private float mediumFluctuation = 0.45f;

    [Tooltip("60-100：几乎不波动")]
    [Range(0f, 1f)] [SerializeField] private float calmFluctuation = 0.08f;

    /// <summary>当前波动档位。</summary>
    public FluctuationBand Band => EvaluateBand(currentValue);

    /// <summary>当前波动强度 0-1，供波形部件直接使用。</summary>
    public float Fluctuation
    {
        get
        {
            switch (Band)
            {
                case FluctuationBand.Violent: return violentFluctuation;
                case FluctuationBand.Medium: return mediumFluctuation;
                default: return calmFluctuation;
            }
        }
    }

    /// <summary>波动档位切换时触发。</summary>
    public event Action<FluctuationBand, FluctuationBand> BandChanged;

    private FluctuationBand lastBroadcastBand;

    /// <summary>当前值变化时触发（范围 0-100）。</summary>
    public event Action<float> ValueChanged;

    /// <summary>阶段切换时触发（旧阶段, 新阶段）。</summary>
    public event Action<Stage, Stage> StageChanged;

    public float Value => currentValue;
    public float TargetValue => targetValue;

    /// <summary>归一化到 0-1，方便直接喂给动画/材质。</summary>
    public float Value01 => Mathf.Clamp01(currentValue / 100f);

    public float MidThreshold => midThreshold;
    public float SafeThreshold => safeThreshold;

    public Stage CurrentStage => EvaluateStage(currentValue);

    private Stage lastBroadcastStage;

    private void Awake()
    {
        currentValue = Mathf.Clamp(currentValue, 0f, 100f);
        targetValue = Mathf.Clamp(targetValue, 0f, 100f);
        lastBroadcastStage = CurrentStage;
        lastBroadcastBand = Band;
    }

    private void Update()
    {
        if (!Mathf.Approximately(currentValue, targetValue))
        {
            currentValue = Mathf.MoveTowards(currentValue, targetValue, followSpeed * Time.deltaTime);
            ValueChanged?.Invoke(currentValue);
        }

        Stage stage = CurrentStage;
        if (stage != lastBroadcastStage)
        {
            Stage previous = lastBroadcastStage;
            lastBroadcastStage = stage;

            if (logStageChanges)
            {
                Debug.Log($"[SanSystem] 阶段 {previous} -> {stage}（SAN {currentValue:0.#}）");
            }

            StageChanged?.Invoke(previous, stage);
        }

        FluctuationBand band = Band;
        if (band != lastBroadcastBand)
        {
            FluctuationBand previousBand = lastBroadcastBand;
            lastBroadcastBand = band;

            if (logStageChanges)
            {
                Debug.Log($"[SanSystem] 波动档 {previousBand} -> {band}（SAN {currentValue:0.#}）");
            }

            BandChanged?.Invoke(previousBand, band);
        }
    }

    public FluctuationBand EvaluateBand(float value)
    {
        if (value < midThreshold) return FluctuationBand.Violent;   // 0-20 剧烈
        if (value < safeThreshold) return FluctuationBand.Medium;   // 20-60 中等
        return FluctuationBand.Calm;                                // 60-100 几乎不波动
    }

    public Stage EvaluateStage(float value)
    {
        if (value >= safeThreshold) return Stage.Safe;
        if (value >= midThreshold) return Stage.Mid;
        return Stage.Low;
    }

    public bool IsAtLeast(Stage stage) => CurrentStage >= stage;

    public bool IsWithin(float value, float tolerance = 0.01f)
        => Mathf.Abs(currentValue - value) <= tolerance;

    // ------------------------------------------------------------------
    // 对外接口（给各 Phase 脚本 / 触发器调用）
    // ------------------------------------------------------------------

    /// <summary>抬高/降低目标值（游戏进程中"逐步提升"就靠它）。</summary>
    public void SetTarget(float value)
    {
        targetValue = Mathf.Clamp(value, 0f, 100f);
    }

    /// <summary>在当前目标值上叠加增量。</summary>
    public void AddTarget(float delta)
    {
        SetTarget(targetValue + delta);
    }

    /// <summary>立即设置当前值（目标值同步跟随），用于开场初始化或重置。</summary>
    public void SnapTo(float value)
    {
        currentValue = Mathf.Clamp(value, 0f, 100f);
        targetValue = currentValue;
        lastBroadcastStage = CurrentStage;
        ValueChanged?.Invoke(currentValue);
    }

    /// <summary>带缓动地爬升到指定值（可指定秒数，内部换算成速度）。</summary>
    public void SetTargetWithDuration(float value, float duration)
    {
        float clamped = Mathf.Clamp(value, 0f, 100f);
        float distance = Mathf.Abs(clamped - currentValue);

        if (duration > 0.001f && distance > 0.001f)
        {
            followSpeed = distance / duration;
        }

        SetTarget(clamped);
    }

    public void SetFollowSpeed(float speed)
    {
        followSpeed = Mathf.Max(0.01f, speed);
    }
}
