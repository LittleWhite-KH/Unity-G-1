using UnityEngine;

/// <summary>
/// "看向目的地"阶段：关卡的核心玩法单元——让玩家转向并看向一个新目标。
///
/// 第一关（看向墓碑）的完整行为：
///   1) 进入时把 SAN 目标压到 <see cref="sanWhenSearching"/>（低位/平）；
///   2) 玩家还没对准目标时 SAN 维持在低位；
///   3) 一旦视线进入 <see cref="angleThreshold"/> 度以内，SAN 目标抬到
///      <see cref="sanWhenFacing"/>，于是 SAN 一路穿过 Low → Mid → Safe；
///   4) 保持对准 <see cref="holdDuration"/> 秒且 SAN 达到 <see cref="requiredStage"/> 后完成；
///   5) 完成时可播放"波形全部变长"（PlayImpact）并/或揭示 HUD。
///
/// 新的一关只要换一个 <see cref="lookTarget"/> 和文案即可，不用写新脚本。
/// </summary>
public class PhaseLookAtTarget : LevelPhaseBase
{
    [Header("目标")]
    [Tooltip("要看向的目的地，例如 Object Sketch/1C1/墓碑_简单")]
    public Transform lookTarget;

    [Tooltip("留空时按这个名字在场景里自动找")]
    public string autoFindTargetName = "";

    [Tooltip("判定为\"面向\"的角度阈值（度）")]
    public float angleThreshold = 5f;

    [Header("SAN 目标")]
    [Tooltip("还没对准目标时的 SAN 目标值")]
    public float sanWhenSearching = 20f;

    [Tooltip("对准目标后抬到的 SAN 目标值（应落在 Mid 或 Safe 段）")]
    public float sanWhenFacing = 65f;

    [Tooltip("SAN 追击速度（值/秒）")]
    public float sanFollowSpeed = 10f;

    [Header("完成条件")]
    [Tooltip("是否需要 SAN 达到指定阶段")]
    public bool requireSanStage = true;

    public SanSystem.Stage requiredStage = SanSystem.Stage.Safe;

    [Tooltip("对准后需要保持的秒数")]
    public float holdDuration = 1.2f;

    [Header("完成演出")]
    [Tooltip("完成时播放\"波形全部变长\"")]
    public bool playImpactOnComplete = true;

    [Tooltip("完成时揭示 HUD（策划案 Part1 第 4 步）")]
    public bool revealHudOnComplete = false;

    [Header("调试")]
    public bool drawGizmos = true;

    private float holdTimer;
    private bool isFacing;

    public override void OnEnter()
    {
        holdTimer = 0f;
        isFacing = false;

        if (lookTarget == null && !string.IsNullOrEmpty(autoFindTargetName))
        {
            GameObject found = GameObject.Find(autoFindTargetName);
            if (found != null) lookTarget = found.transform;
        }

        if (lookTarget == null)
        {
            Debug.LogWarning($"[{phaseName}] 没有指定 lookTarget，阶段将无法完成。");
        }

        if (San != null)
        {
            San.SetFollowSpeed(sanFollowSpeed);
            San.SetTarget(requireSanStage ? sanWhenSearching : sanWhenFacing);
        }

        Log($"开始：看向 {(lookTarget != null ? lookTarget.name : "(未指定)")}，阈值 {angleThreshold}°");
    }

    public override void OnTick(float deltaTime)
    {
        if (lookTarget == null)
        {
            return;
        }

        float angle = AngleToTarget(lookTarget);
        isFacing = angle <= angleThreshold;

        if (San != null)
        {
            San.SetTarget(isFacing ? sanWhenFacing : sanWhenSearching);
        }

        if (!isFacing)
        {
            holdTimer = 0f;
            return;
        }

        bool sanReady = !requireSanStage || (San != null && San.IsAtLeast(requiredStage));

        if (!sanReady)
        {
            holdTimer = 0f;
            return;
        }

        holdTimer += deltaTime;

        if (holdTimer >= holdDuration)
        {
            Complete();
        }
    }

    public override void OnExit()
    {
        if (playImpactOnComplete && SanDisplay != null)
        {
            SanDisplay.PlayImpact();
        }

        if (revealHudOnComplete && Hud != null)
        {
            Hud.Play();
        }

        Log($"完成（对准保持 {holdTimer:0.##}s，SAN {San?.Value:0.#}）");
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos || lookTarget == null)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(lookTarget.position, 0.5f);
    }
}
