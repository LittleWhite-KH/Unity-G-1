using System;
using UnityEngine;

/// <summary>
/// 危险 / 警告值（0-100）——与 SanSystem 相互独立的那一条"用来变化玩家场景"的线。
///
/// 它负责回答"现在有多危险"，由它去驱动场景层面的变化：雾、后处理、灯光、画面抖动、音效、
/// 红色警告 UI 等等（具体效果由订阅 StageChanged 的组件实现，本脚本不直接改场景）。
///
/// 三段（沿用 20 / 60 的切分）：
///   Calm     平静 : 0  ~ 20
///   Warning  警告 : 20 ~ 60
///   Critical 危险 : 60 ~ 100
///
/// 与 SanSystem 的关系：两者独立。SAN 是"恢复进度"，Threat 是"当前紧迫度"；
/// 典型用法是 SAN 缓慢抬升的同时，Threat 被事件/距离推高、再自行衰减回落。
/// </summary>
[DisallowMultipleComponent]
public class ThreatSystem : MonoBehaviour
{
    public enum Stage
    {
        Calm = 0,
        Warning = 1,
        Critical = 2
    }

    [Header("数值（0-100）")]
    [SerializeField] private float currentValue = 0f;
    [SerializeField] private float targetValue = 0f;

    [Tooltip("当前值追向目标值的速度（值/秒）")]
    [SerializeField] private float followSpeed = 8f;

    [Header("自发衰减")]
    [Tooltip("开启后，目标值会以该速度自动回落（用于\"事件推高一下再慢慢平息\"）")]
    [SerializeField] private bool autoDecay = true;
    [SerializeField] private float decaySpeed = 4f;

    [Header("三段阈值")]
    [Tooltip("高于该值进入 Warning（警告）")]
    [SerializeField] private float warningThreshold = 20f;

    [Tooltip("高于该值进入 Critical（危险）")]
    [SerializeField] private float criticalThreshold = 60f;

    [Header("调试")]
    [SerializeField] private bool logStageChanges = false;

    public event Action<float> ValueChanged;
    public event Action<Stage, Stage> StageChanged;

    /// <summary>归一化 0-1，直接可用于后处理权重/雾浓度等。</summary>
    public float Value01 => Mathf.Clamp01(currentValue / 100f);

    public float Value => currentValue;
    public float TargetValue => targetValue;
    public float WarningThreshold => warningThreshold;
    public float CriticalThreshold => criticalThreshold;

    public Stage CurrentStage => EvaluateStage(currentValue);

    private Stage lastBroadcastStage;

    private void Awake()
    {
        currentValue = Mathf.Clamp(currentValue, 0f, 100f);
        targetValue = Mathf.Clamp(targetValue, 0f, 100f);
        lastBroadcastStage = CurrentStage;
    }

    private void Update()
    {
        if (autoDecay && targetValue > 0f)
        {
            targetValue = Mathf.MoveTowards(targetValue, 0f, decaySpeed * Time.deltaTime);
        }

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
                Debug.Log($"[ThreatSystem] 阶段 {previous} -> {stage}（Threat {currentValue:0.#}）");
            }

            StageChanged?.Invoke(previous, stage);
        }
    }

    public Stage EvaluateStage(float value)
    {
        if (value >= criticalThreshold) return Stage.Critical;
        if (value >= warningThreshold) return Stage.Warning;
        return Stage.Calm;
    }

    public bool IsAtLeast(Stage stage) => CurrentStage >= stage;

    // ------------------------------------------------------------------
    // 对外接口
    // ------------------------------------------------------------------

    /// <summary>设定目标危险度。autoDecay 开启时它随后会自行回落。</summary>
    public void SetTarget(float value)
    {
        targetValue = Mathf.Clamp(value, 0f, 100f);
    }

    /// <summary>事件式推高：把目标值抬到"当前值和给定值中的较大者"。</summary>
    public void Push(float value)
    {
        SetTarget(Mathf.Max(targetValue, Mathf.Clamp(value, 0f, 100f)));
    }

    /// <summary>在当前目标值上叠加。</summary>
    public void AddTarget(float delta)
    {
        SetTarget(targetValue + delta);
    }

    /// <summary>立刻设置当前值与目标值（重置 / 调试用）。</summary>
    public void SnapTo(float value)
    {
        currentValue = Mathf.Clamp(value, 0f, 100f);
        targetValue = currentValue;
        lastBroadcastStage = CurrentStage;
        ValueChanged?.Invoke(currentValue);
    }

    public void SetAutoDecay(bool enabled, float speed = -1f)
    {
        autoDecay = enabled;
        if (speed > 0f) decaySpeed = speed;
    }
}
