using UnityEngine;

/// <summary>Runner 基类：保存 Def/Ctx，统一完成标志与日志前缀。</summary>
public abstract class PhaseRunnerBase : IPhaseRunner
{
    protected PhaseDefinition Def;
    protected LevelContext Ctx;

    public bool IsComplete { get; protected set; }
    public virtual Transform ObjectiveTarget => null;

    public virtual void Enter(PhaseDefinition def, LevelContext ctx)
    {
        Def = def;
        Ctx = ctx;
        IsComplete = false;
    }

    public virtual void Tick(PhaseDefinition def, LevelContext ctx, float dt)
    {
    }

    public virtual void Exit(PhaseDefinition def, LevelContext ctx)
    {
    }

    public virtual void Reset()
    {
        IsComplete = false;
    }

    protected void Complete()
    {
        if (IsComplete)
        {
            return;
        }

        IsComplete = true;
        Log("complete");
    }

    protected GuidanceTarget ResolveTarget(int index = 0)
    {
        if (Def == null || Def.targetIds == null || Def.targetIds.Length <= index)
        {
            return null;
        }

        GuidanceTarget target = GuidanceRegistry.Find(Def.targetIds[index]);
        if (target == null)
        {
            Debug.LogWarning($"[Level:{Def.id}] 找不到 GuidanceTarget id={Def.targetIds[index]}");
        }

        return target;
    }

    protected float DistanceTo(GuidanceTarget target)
    {
        if (target == null || Ctx == null || Ctx.Player == null)
        {
            return float.MaxValue;
        }

        return Vector3.Distance(Ctx.Player.position, target.AimPosition);
    }

    protected bool IsNear(GuidanceTarget target, float distance)
    {
        return target != null && DistanceTo(target) <= distance;
    }

    protected static bool InteractPressed()
    {
        return UnityEngine.InputSystem.Keyboard.current != null &&
               UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame;
    }

    protected void Log(string message)
    {
        Debug.Log($"[Level:{(Def != null ? Def.id : "?")}] {message}");
    }
}
