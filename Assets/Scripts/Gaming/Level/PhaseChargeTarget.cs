using UnityEngine;

/// <summary>
/// 充能阶段（策划案 Part1 第 2~3 步）：玩法交给 <see cref="ChargeSystem"/>，本阶段只负责编排：
///
///   1) 启用充能系统（可选地改指向另一个目标 —— 第二关就把 target 换成"桌台_简单"）；
///   2) 等充能充满；
///   3) 充满后在目标上显示世界空间提示（"点击墓碑"）并开放点击；
///   4) 玩家点击目标后阶段完成 → 关卡控制器进入下一关。
/// </summary>
public class PhaseChargeTarget : LevelPhaseBase
{
    [Header("充能系统")]
    public ChargeSystem chargeSystem;

    [Tooltip("本阶段要充能的目标；留空则沿用 chargeSystem 自己的 target")]
    public Transform targetOverride;

    public bool enableOnEnter = true;

    [Tooltip("离开阶段时停用充能系统（其 OnDisable 会清掉凝聚与波动增益）")]
    public bool disableOnExit = true;

    [Header("充能完成后的交互")]
    [Tooltip("充满后显示的 UI 提示（放在 UI 画布上，比世界空间 TMP 清楚）")]
    public GameObject uiHintRoot;

    [Tooltip("充满后显示的世界空间提示（已改为可选，效果不佳时可以留空）")]
    public WorldHint worldHint;

    [Tooltip("充满后开放点击的物体")]
    public WorldClickTarget clickTarget;

    [Tooltip("是否必须点击后才算完成（关掉则充满即完成、直接进下一关）")]
    public bool requireClickToComplete = true;

    [Header("调试")]
    public bool logProgress = false;

    /// <summary>本阶段的目标（距离计与地图点位靠它跟随关卡推进）。</summary>
    public override Transform ObjectiveTarget
    {
        get
        {
            if (targetOverride != null) return targetOverride;
            return chargeSystem != null ? chargeSystem.target : null;
        }
    }

    private float logTimer;
    private bool interactionOpened;

    public override void OnEnter()
    {
        interactionOpened = false;

        if (chargeSystem == null)
        {
            Debug.LogWarning($"[{phaseName}] 没有绑定 ChargeSystem，阶段无法完成。");
            return;
        }

        if (targetOverride != null)
        {
            chargeSystem.target = targetOverride;
        }

        chargeSystem.ResetCharge();
        chargeSystem.enabled = enableOnEnter;

        if (worldHint != null) worldHint.Hide();
        if (uiHintRoot != null) uiHintRoot.SetActive(false);
        if (clickTarget != null) clickTarget.Disarm();

        Log($"开始充能：目标 {(chargeSystem.target != null ? chargeSystem.target.name : "(未指定)")}，" +
            $"距离上限 {chargeSystem.chargeDistance}m，角度上限 {chargeSystem.angleThreshold}°，需 {chargeSystem.chargeDuration}s");
    }

    public override void OnTick(float deltaTime)
    {
        if (chargeSystem == null)
        {
            return;
        }

        if (logProgress)
        {
            logTimer += deltaTime;
            if (logTimer >= 1f)
            {
                logTimer = 0f;
                Log($"距离 {chargeSystem.Distance:0.#}m  偏差 {chargeSystem.Angle:0.#}°  " +
                    $"可充能 {chargeSystem.CanCharge}  进度 {chargeSystem.Progress01 * 100f:0}%");
            }
        }

        // 充满之后：需要点击才弹提示，不需要就直接进下一关
        if (chargeSystem.IsComplete && !interactionOpened)
        {
            interactionOpened = true;

            if (requireClickToComplete)
            {
                if (uiHintRoot != null) uiHintRoot.SetActive(true);
                if (worldHint != null) worldHint.Show();
                if (clickTarget != null) clickTarget.Arm();

                Log("充能完成，已显示点击提示");
            }
            else
            {
                if (uiHintRoot != null) uiHintRoot.SetActive(false);
                if (worldHint != null) worldHint.Hide();
                if (clickTarget != null) clickTarget.Disarm();

                Log("充能完成，直接进入下一关");
            }
        }

        if (!chargeSystem.IsComplete)
        {
            return;
        }

        bool clickRequired = requireClickToComplete && clickTarget != null;
        if (!clickRequired || clickTarget.Clicked)
        {
            Complete();
        }
    }

    public override void OnExit()
    {
        if (worldHint != null) worldHint.Hide();
        if (uiHintRoot != null) uiHintRoot.SetActive(false);
        if (clickTarget != null) clickTarget.Disarm();

        if (chargeSystem != null && disableOnExit)
        {
            chargeSystem.ResetCharge();    // 一关结束充能归零（蓝条 + 红条一起清）
            chargeSystem.enabled = false;  // OnDisable 里会清掉凝聚/波动叠加
        }
    }
}
