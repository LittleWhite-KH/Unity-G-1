using System.Collections;
using UnityEngine;

/// <summary>
/// 关卡阶段基类：一个阶段 = 一个独立脚本 + 场景里一个空物体。
///
/// 加新关卡的流程（不用改 <see cref="LevelDirector"/> 一行代码）：
///   1. 新建一个继承本类的脚本，实现 OnEnter / OnTick / <see cref="Complete"/>；
///   2. 场景里在 Director 下挂一个空物体，把这个脚本加上去；
///   3. 填好它自己的数值（目标、时长、SAN 目标…）。
///
/// 通用能力（子类直接用）：等待、延时、设置 SAN/Threat 目标、驱动关卡标题与目标文字、播放 HUD 揭示。
/// </summary>
public abstract class LevelPhaseBase : MonoBehaviour
{
    [Header("阶段信息")]
    [Tooltip("顺序，小的先执行；相同则按场景层级顺序")]
    public int order = 0;

    [Tooltip("阶段名（调试用）")]
    public string phaseName = "Phase";

    [Header("进入时写入的 UI 文案（留空则不改）")]
    public string levelTitle = "";
    public string objectiveText = "";

    [Header("进入时的解锁")]
    [Tooltip("进入本阶段时解锁地图按键（M 键）")]
    public bool unlockMapOnEnter = false;

    private bool completed;

    /// <summary>由 Director 在启动时注入。</summary>
    public LevelDirector Director { get; private set; }

    public bool IsComplete => completed;

    protected SanSystem San => Director != null ? Director.sanSystem : null;
    protected ThreatSystem Threat => Director != null ? Director.threatSystem : null;
    protected SanDisplay SanDisplay => Director != null ? Director.sanDisplay : null;
    protected HudReveal Hud => Director != null ? Director.hudReveal : null;
    protected Transform Player => Director != null ? Director.player : null;
    protected Camera PlayerCamera => Director != null ? Director.playerCamera : null;

    internal void Bind(LevelDirector director)
    {
        Director = director;
    }

    internal void ResetPhase()
    {
        completed = false;
        OnReset();
    }

    protected virtual void OnReset()
    {
    }

    /// <summary>阶段开始（Director 已注入引用、UI 文案已写入）。</summary>
    /// <summary>
    /// 本阶段要去的地方（距离计 / 地图点位用它）。
    /// 没有目标的阶段返回 null，关卡控制器会自动往后找最近一个带目标的阶段 ——
    /// 这样"过关后距离计自动指向下一关的目标"就是自然行为。
    /// </summary>
    public virtual Transform ObjectiveTarget => null;

    public virtual void OnEnter()
    {
    }

    /// <summary>每帧调用，只有当前阶段会收到。</summary>
    public virtual void OnTick(float deltaTime)
    {
    }

    /// <summary>阶段结束。</summary>
    public virtual void OnExit()
    {
    }

    /// <summary>子类完成条件满足时调用它。</summary>
    protected void Complete()
    {
        completed = true;
    }

    // ------------------------------------------------------------------
    // 常用小工具
    // ------------------------------------------------------------------

    protected float AngleToTarget(Transform target)
    {
        if (target == null || PlayerCamera == null)
        {
            return 180f;
        }

        Vector3 origin = Player != null ? Player.position : PlayerCamera.transform.position;
        Vector3 dir = target.position - origin;
        dir.y = 0f;

        Vector3 forward = PlayerCamera.transform.forward;
        forward.y = 0f;

        if (dir.sqrMagnitude < 0.0001f || forward.sqrMagnitude < 0.0001f)
        {
            return 180f;
        }

        return Vector3.Angle(forward, dir.normalized);
    }

    protected IEnumerator WaitSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
    }

    protected void Log(string message)
    {
        Debug.Log($"[Level:{phaseName}] {message}");
    }
}
