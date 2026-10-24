using System;
using UnityEngine;

/// <summary>阶段种类。新增一种玩法 = 新增一个 Runner + 这里加一个枚举值。</summary>
public enum PhaseKind
{
    Intro = 0,
    Charge = 1,
    Reveal = 2,
    Trigger = 3,
    PickupSet = 4,
    Door = 5,
    Ending = 6
}

/// <summary>
/// 一个阶段的数据（纯数据，写在 LevelDefinition 资产里）。
/// 目标一律用 <see cref="GuidanceTarget.id"/> 字符串引用，不直接拖场景引用，
/// 这样换场景/换物体时只要 id 对上即可。
/// </summary>
[Serializable]
public class PhaseDefinition
{
    [Header("标识")]
    public string id = "phase";
    public PhaseKind kind = PhaseKind.Charge;

    [Header("UI 文案")]
    public string levelTitle = "LEVEL 1";
    public string objective = "";

    [Tooltip("靠近目标时替换的提示（留空则不换）")]
    public string objectiveWhenNear = "";

    [Header("目标（GuidanceTarget.id）")]
    public string[] targetIds = new string[0];

    [Tooltip("地图 waypoint 指向的 id（留空则用第一个 target）")]
    public string waypointId = "";

    [Tooltip("完成时要标记的地图点位 id")]
    public string mapNodeId = "";

    public bool markVisitedOnComplete = false;

    [Header("充能（kind=Charge 时生效；-1 = 不改）")]
    public float sanZeroDistance = -1f;
    public float sanFullDistance = -1f;
    public float chargeDistance = -1f;
    public float angleThreshold = -1f;
    public float chargeDuration = -1f;
    public bool requireClick = false;
    public bool playImpactOnComplete = true;

    [Header("触发 / 拾取 / 门")]
    [Tooltip("距离目标多少米内算“到达/可交互”")]
    public float triggerDistance = 4f;

    [Tooltip("PickupSet / Door：需要多少个；-1 = 全部")]
    public int requiredCount = -1;

    public bool showKeyCounter = false;

    [Header("地图 / 解锁")]
    public bool unlockMapOnEnter = false;
    public bool unlockTargetsOnEnter = false;
    public bool discoverTargetsOnEnter = false;

    [Header("Reveal")]
    public float holdAfterReveal = 1f;
    public bool waitForMapCycle = true;
    public float sanAfterStable = 40f;

    [Header("演出")]
    public bool fadeOutOnComplete = false;
}
