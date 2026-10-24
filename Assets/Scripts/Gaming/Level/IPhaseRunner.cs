using UnityEngine;

/// <summary>阶段执行器接口：Enter / Tick / Exit / IsComplete。</summary>
public interface IPhaseRunner
{
    void Enter(PhaseDefinition def, LevelContext ctx);
    void Tick(PhaseDefinition def, LevelContext ctx, float dt);
    void Exit(PhaseDefinition def, LevelContext ctx);

    bool IsComplete { get; }

    /// <summary>当前阶段玩家该去的地方（距离计 / 地图用）。</summary>
    Transform ObjectiveTarget { get; }

    void Reset();
}
