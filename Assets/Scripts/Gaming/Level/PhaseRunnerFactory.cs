using UnityEngine;

/// <summary>按 PhaseKind 创建执行器。新增玩法时在这里加一行。</summary>
public static class PhaseRunnerFactory
{
    public static IPhaseRunner Create(PhaseKind kind)
    {
        switch (kind)
        {
            case PhaseKind.Intro:
                return new IntroRunner();
            case PhaseKind.Charge:
                return new ChargeRunner();
            case PhaseKind.Reveal:
                return new RevealRunner();
            case PhaseKind.Trigger:
                return new TriggerRunner();
            case PhaseKind.PickupSet:
                return new PickupSetRunner();
            case PhaseKind.Door:
                return new DoorRunner();
            case PhaseKind.Ending:
                return new EndingRunner();
            case PhaseKind.Submit:
                return new SubmitRunner();
            case PhaseKind.ScreenDoor:
                return new ScreenDoorRunner();
            default:
                Debug.LogWarning($"[Level] unknown phase kind {kind}, fallback to TriggerRunner");
                return new TriggerRunner();
        }
    }
}
