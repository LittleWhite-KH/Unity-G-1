using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>开场：锁操控 → SAN 平稳动画 → 解锁。</summary>
public class IntroRunner : PhaseRunnerBase
{
    public float startDelay = 0.6f;
    public float extraWait = 0.6f;
    public float stabilizeTimeout = 8f;

    private static readonly string[] ControlNames = { "MouseLook", "PlayerMovement" };
    private readonly List<Behaviour> locked = new List<Behaviour>();
    private float elapsed;

    public override void Enter(PhaseDefinition def, LevelContext ctx)
    {
        base.Enter(def, ctx);
        elapsed = 0f;
        SetControl(false);

        if (ctx.SanDisplay != null)
        {
            ctx.SanDisplay.BeginStabilize();
        }
        else
        {
            Log("no SanDisplay, intro will just wait");
        }
    }

    public override void Tick(PhaseDefinition def, LevelContext ctx, float dt)
    {
        elapsed += dt;

        if (elapsed < startDelay)
        {
            return;
        }

        bool stabilizing = ctx.SanDisplay != null && !ctx.SanDisplay.IsStabilized;
        if (stabilizing && elapsed < startDelay + stabilizeTimeout)
        {
            return;
        }

        if (elapsed < startDelay + extraWait)
        {
            return;
        }

        SetControl(true);
        Complete();
    }

    public override void Exit(PhaseDefinition def, LevelContext ctx)
    {
        SetControl(true);
    }

    private void SetControl(bool enable)
    {
        if (Ctx == null || Ctx.Player == null)
        {
            return;
        }

        if (!enable)
        {
            locked.Clear();
            foreach (Behaviour behaviour in Ctx.Player.GetComponentsInChildren<Behaviour>(true))
            {
                if (behaviour == null || !behaviour.enabled)
                {
                    continue;
                }

                if (Matches(behaviour.GetType().Name))
                {
                    behaviour.enabled = false;
                    locked.Add(behaviour);
                }
            }

            Log($"control locked ({locked.Count})");
        }
        else
        {
            foreach (Behaviour behaviour in locked)
            {
                if (behaviour != null)
                {
                    behaviour.enabled = true;
                }
            }

            locked.Clear();
        }
    }

    private static bool Matches(string typeName)
    {
        for (int i = 0; i < ControlNames.Length; i++)
        {
            if (ControlNames[i] == typeName)
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>充能阶段：把本阶段参数写进共享 ChargeSystem，充满即完成。</summary>
public class ChargeRunner : PhaseRunnerBase
{
    private GuidanceTarget target;

    public override Transform ObjectiveTarget => target != null ? target.transform : null;

    public override void Enter(PhaseDefinition def, LevelContext ctx)
    {
        base.Enter(def, ctx);
        target = ResolveTarget();

        if (ctx.Charge == null)
        {
            Log("no ChargeSystem");
            return;
        }

        if (target != null)
        {
            ctx.Charge.target = target.transform;
        }

        ctx.Charge.guidanceOnly = false;
        ctx.Charge.ApplyPhaseSettings(def.sanZeroDistance, def.sanFullDistance, def.chargeDistance, def.angleThreshold, def.chargeDuration);
        ctx.Charge.ResetCharge();
        ctx.Charge.enabled = true;

        if (ctx.Map != null && target != null)
        {
            ctx.Map.SetWaypoint(target.transform);
        }

        Log($"target={(target != null ? target.id : "null")} sanZero={ctx.Charge.sanZeroDistance:0.#} sanFull={ctx.Charge.sanFullDistance:0.#} chargeDist={ctx.Charge.chargeDistance:0.#} angle={ctx.Charge.angleThreshold:0.#} duration={ctx.Charge.chargeDuration:0.#}");
    }

    public override void Tick(PhaseDefinition def, LevelContext ctx, float dt)
    {
        if (ctx.Charge == null || target == null || !ctx.Charge.IsComplete)
        {
            return;
        }

        if (def.playImpactOnComplete && ctx.SanDisplay != null)
        {
            ctx.SanDisplay.PlayImpact();
        }

        if (def.markVisitedOnComplete && ctx.Graph != null && !string.IsNullOrEmpty(def.mapNodeId))
        {
            ctx.Graph.MarkVisited(def.mapNodeId);
        }

        Complete();
    }

    public override void Exit(PhaseDefinition def, LevelContext ctx)
    {
        if (ctx.Charge == null)
        {
            return;
        }

        ctx.Charge.ResetCharge();
        ctx.Charge.enabled = false;
        ctx.Charge.guidanceOnly = false;
    }
}

/// <summary>HUD 揭示 + 等玩家开一次地图 + SAN 回稳。</summary>
public class RevealRunner : PhaseRunnerBase
{
    private GuidanceTarget waypoint;
    private float elapsed;
    private bool opened;
    private bool finished;

    public override Transform ObjectiveTarget => waypoint != null ? waypoint.transform : null;

    public override void Enter(PhaseDefinition def, LevelContext ctx)
    {
        base.Enter(def, ctx);
        waypoint = ResolveWaypoint();
        elapsed = 0f;
        opened = false;
        finished = false;

        if (ctx.Hud != null)
        {
            ctx.Hud.Play();
        }

        if (ctx.MapHintRoot != null)
        {
            ctx.MapHintRoot.SetActive(true);
        }

        if (ctx.Map != null)
        {
            if (def.unlockMapOnEnter)
            {
                ctx.Map.SetInputEnabled(true);
            }

            if (waypoint != null)
            {
                ctx.Map.SetWaypoint(waypoint.transform);
            }
        }

        Log($"reveal hud={(ctx.Hud != null)} waypoint={(waypoint != null ? waypoint.id : "null")}");
    }

    public override void Tick(PhaseDefinition def, LevelContext ctx, float dt)
    {
        if (finished)
        {
            return;
        }

        elapsed += dt;
        if (elapsed < def.holdAfterReveal)
        {
            return;
        }

        if (ctx.Map == null || !def.waitForMapCycle)
        {
            Finish(ctx, def);
            return;
        }

        if (!opened)
        {
            if (ctx.Map.IsOpen)
            {
                opened = true;
                if (ctx.MapHintRoot != null)
                {
                    ctx.MapHintRoot.SetActive(false);
                }
            }

            return;
        }

        if (!ctx.Map.IsOpen)
        {
            Finish(ctx, def);
        }
    }

    private void Finish(LevelContext ctx, PhaseDefinition def)
    {
        finished = true;
        if (ctx.San != null)
        {
            ctx.San.SetTarget(def.sanAfterStable);
        }

        Log("map cycle done, san stable");
        Complete();
    }

    private GuidanceTarget ResolveWaypoint()
    {
        if (Def == null)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(Def.waypointId))
        {
            return GuidanceRegistry.Find(Def.waypointId);
        }

        return ResolveTarget();
    }
}

/// <summary>靠近目标就完成（用于“走到特殊门 → 触发任务”）。</summary>
public class TriggerRunner : PhaseRunnerBase
{
    private GuidanceTarget target;
    private bool wasNear;

    public override Transform ObjectiveTarget => target != null ? target.transform : null;

    public override void Enter(PhaseDefinition def, LevelContext ctx)
    {
        base.Enter(def, ctx);
        target = ResolveTarget();
        wasNear = false;

        if (ctx.Map != null && target != null)
        {
            ctx.Map.SetWaypoint(target.transform);
        }

        if (ctx.Graph != null && target != null)
        {
            ctx.Graph.Unlock(target.id);
        }

        Log($"waiting near {(target != null ? target.id : "null")} (<= {def.triggerDistance:0.#}m)");
    }

    public override void Tick(PhaseDefinition def, LevelContext ctx, float dt)
    {
        if (target == null)
        {
            return;
        }

        bool near = IsNear(target, def.triggerDistance);
        if (near != wasNear)
        {
            wasNear = near;
            if (!string.IsNullOrEmpty(def.objectiveWhenNear) && ctx.Director != null)
            {
                ctx.Director.SetObjective(near ? def.objectiveWhenNear : def.objective);
            }
        }

        if (near)
        {
            if (def.markVisitedOnComplete && ctx.Graph != null && !string.IsNullOrEmpty(def.mapNodeId))
            {
                ctx.Graph.MarkVisited(def.mapNodeId);
            }

            Complete();
        }
    }
}

/// <summary>一组可拾取目标（两把钥匙）：任意顺序，靠近按 E，收齐即完成。</summary>
public class PickupSetRunner : PhaseRunnerBase
{
    private readonly List<GuidanceTarget> targets = new List<GuidanceTarget>();
    private GuidanceTarget nearest;
    private int required = 1;
    private int collected;
    private string lastObjective;
    private bool suppressObjective;

    public override Transform ObjectiveTarget => nearest != null ? nearest.transform : null;

    public override void Enter(PhaseDefinition def, LevelContext ctx)
    {
        base.Enter(def, ctx);
        targets.Clear();
        targets.AddRange(GuidanceRegistry.FindAll(def.targetIds));

        required = def.requiredCount < 0 ? targets.Count : Mathf.Max(1, def.requiredCount);
        collected = 0;
        nearest = null;
        lastObjective = null;

        foreach (GuidanceTarget t in targets)
        {
            if (t == null)
            {
                continue;
            }

            t.discovered = true;
            if (ctx.Graph != null)
            {
                ctx.Graph.Unlock(t.id);
            }
        }

        if (ctx.Charge != null)
        {
            ctx.Charge.ResetCharge();
            ctx.Charge.enabled = true;
            ctx.Charge.guidanceOnly = true;
        }

        UpdateNearest();
        if (ctx.Charge != null && nearest != null)
        {
            ctx.Charge.target = nearest.transform;
        }

        if (ctx.Map != null && nearest != null)
        {
            ctx.Map.SetWaypoint(nearest.transform);
        }

        Log($"pickup set {targets.Count} targets, need {required}");
    }

    public override void Tick(PhaseDefinition def, LevelContext ctx, float dt)
    {
        UpdateNearest();

        if (ctx.Charge != null && nearest != null)
        {
            ctx.Charge.target = nearest.transform;
        }

        if (ctx.Map != null && nearest != null)
        {
            ctx.Map.SetWaypoint(nearest.transform);
        }

        if (nearest != null)
        {
            float distance = DistanceTo(nearest);
            bool near = distance <= def.triggerDistance;

            if (near && InteractPressed())
            {
                nearest.Collect();
                if (ctx.State != null)
                {
                    ctx.State.Add(nearest.id);
                }

                if (ctx.Graph != null)
                {
                    ctx.Graph.MarkVisited(nearest.id);
                }

                UpdateNearest();
            }
        }

        if (collected >= required)
        {
            Complete();
            return;
        }

        UpdateObjective(ctx, def);
    }

    public override void Exit(PhaseDefinition def, LevelContext ctx)
    {
        if (ctx.Charge != null)
        {
            ctx.Charge.enabled = false;
            ctx.Charge.guidanceOnly = false;
            ctx.Charge.ResetCharge();
        }

        if (ctx.Map != null)
        {
            ctx.Map.ClearWaypoint();
        }
    }

    private void UpdateNearest()
    {
        collected = 0;
        nearest = null;
        float bestSq = float.MaxValue;
        Vector3 from = Ctx != null && Ctx.Player != null ? Ctx.Player.position : Vector3.zero;

        foreach (GuidanceTarget t in targets)
        {
            if (t == null)
            {
                continue;
            }

            if (t.collected)
            {
                collected++;
                continue;
            }

            if (!t.gameObject.activeInHierarchy)
            {
                continue;
            }

            float sq = (t.AimPosition - from).sqrMagnitude;
            if (sq < bestSq)
            {
                bestSq = sq;
                nearest = t;
            }
        }
    }

    private void UpdateObjective(LevelContext ctx, PhaseDefinition def)
    {
        if (ctx.Director == null)
        {
            return;
        }

        string objective;
        if (nearest != null && DistanceTo(nearest) <= def.triggerDistance && !string.IsNullOrEmpty(def.objectiveWhenNear))
        {
            objective = def.objectiveWhenNear;
        }
        else if (def.showKeyCounter)
        {
            objective = $"{def.objective} ({collected}/{required})";
        }
        else
        {
            objective = def.objective;
        }

        if (objective != lastObjective)
        {
            lastObjective = objective;
            ctx.Director.SetObjective(objective);
        }
    }
}

/// <summary>门阶段：2 把钥匙收齐后，靠近按 E 开门。</summary>
public class DoorRunner : PhaseRunnerBase
{
    private GuidanceTarget target;
    private DoorController door;
    private bool opened;
    private bool near;

    public override Transform ObjectiveTarget => target != null ? target.transform : null;

    public override void Enter(PhaseDefinition def, LevelContext ctx)
    {
        base.Enter(def, ctx);
        target = ResolveTarget();
        door = target != null ? target.GetComponentInChildren<DoorController>(true) : null;
        opened = false;
        near = false;

        if (ctx.Map != null && target != null)
        {
            ctx.Map.SetWaypoint(target.transform);
        }

        Log($"door={(door != null)} keys={(ctx.State != null ? ctx.State.Count : 0)}");
    }

    public override void Tick(PhaseDefinition def, LevelContext ctx, float dt)
    {
        if (target == null || opened)
        {
            return;
        }

        bool isNear = IsNear(target, def.triggerDistance);
        if (isNear != near)
        {
            near = isNear;
            if (!string.IsNullOrEmpty(def.objectiveWhenNear) && ctx.Director != null)
            {
                ctx.Director.SetObjective(near ? def.objectiveWhenNear : def.objective);
            }
        }

        if (!near || !InteractPressed())
        {
            return;
        }

        opened = true;
        if (door != null)
        {
            door.Open();
        }
        else
        {
            Log("no DoorController, door will stay closed visually");
        }

        if (ctx.State != null)
        {
            ctx.State.doorOpen = true;
        }

        if (ctx.Graph != null)
        {
            ctx.Graph.MarkVisited(!string.IsNullOrEmpty(def.mapNodeId) ? def.mapNodeId : "door");
        }

        Complete();
    }
}

/// <summary>终点：走到 Mainframe，淡出并完成。</summary>
public class EndingRunner : PhaseRunnerBase
{
    private GuidanceTarget target;
    private bool triggered;
    private float fade;
    private bool completed;

    public override Transform ObjectiveTarget => target != null ? target.transform : null;

    public override void Enter(PhaseDefinition def, LevelContext ctx)
    {
        base.Enter(def, ctx);
        target = ResolveTarget();
        triggered = false;
        completed = false;
        fade = 0f;

        if (ctx.Map != null && target != null)
        {
            ctx.Map.SetWaypoint(target.transform);
        }

        Log($"ending target={(target != null ? target.id : "null")}");
    }

    public override void Tick(PhaseDefinition def, LevelContext ctx, float dt)
    {
        if (completed)
        {
            return;
        }

        if (!triggered)
        {
            if (target != null && IsNear(target, def.triggerDistance))
            {
                triggered = true;

                if (ctx.Graph != null && !string.IsNullOrEmpty(def.mapNodeId))
                {
                    ctx.Graph.MarkVisited(def.mapNodeId);
                }

                if (ctx.Director != null)
                {
                    ctx.Director.SetObjective("LEVEL COMPLETE");
                }

                Log("ending reached, fading out");
            }

            return;
        }

        if (!def.fadeOutOnComplete || ctx.FadeOverlay == null)
        {
            completed = true;
            Complete();
            return;
        }

        fade += dt / 2f;
        ctx.FadeOverlay.alpha = Mathf.Clamp01(fade);

        if (fade >= 1f)
        {
            completed = true;
            Complete();
        }
    }
}
