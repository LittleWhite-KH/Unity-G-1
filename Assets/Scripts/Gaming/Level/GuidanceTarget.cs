using System;
using UnityEngine;

/// <summary>指引目标的类别（只影响日志/筛选，不影响最终指引效果）。</summary>
public enum GuidanceKind
{
    Waypoint = 0,
    Key = 1,
    Door = 2,
    Ending = 3,
    Interactable = 4
}

/// <summary>
/// 任何"玩家需要知道在哪"的物体挂上它，就自动成为指引目标：
/// 距离计、SAN 方位、地图点位、HUD 文案都通过 <see cref="GuidanceRegistry"/> 找到它。
///
/// 加一个新物体作为指引：
///   1) 场景里给物体挂 GuidanceTarget，填 id / label；
///   2) 在 LevelDefinition 的某个 PhaseDefinition.targetIds 里加上这个 id。
/// 不需要改 LevelDirector，也不需要给每个物体写新脚本。
/// </summary>
[DisallowMultipleComponent]
public class GuidanceTarget : MonoBehaviour
{
    [Header("标识")]
    [Tooltip("全局唯一 id；关卡配置里用它来引用")]
    public string id = "target";

    [Tooltip("地图 / HUD 上显示的名字")]
    public string label = "TARGET";

    public GuidanceKind kind = GuidanceKind.Waypoint;

    [Header("指引")]
    [Tooltip("从物体原点往上抬多少米作为真正的“瞄准点”")]
    public float heightOffset = 1.2f;

    [Tooltip("是否已经在玩家地图上解锁")]
    public bool discovered = true;

    [Tooltip("是否画到地图上")]
    public bool showOnMap = true;

    [Header("拾取")]
    [Tooltip("钥匙类目标：拾取后自动隐藏")]
    public bool isKey = false;

    public bool collected = false;

    public event Action<GuidanceTarget> Collected;

    /// <summary>真正的瞄准点（距离 / 方位 / 地图都用它）。</summary>
    public Vector3 AimPosition => transform.position + Vector3.up * heightOffset;

    private void OnEnable()
    {
        GuidanceRegistry.Register(this);
    }

    private void OnDisable()
    {
        GuidanceRegistry.Unregister(this);
    }

    /// <summary>拾取（钥匙）。重复调用安全。</summary>
    public void Collect()
    {
        if (collected)
        {
            return;
        }

        collected = true;
        Collected?.Invoke(this);
        Debug.Log($"[Guidance] collected {id} ({label})");

        // 钥匙拾取后从场景里消失；其他目标保留。
        if (isKey)
        {
            gameObject.SetActive(false);
        }
    }

    public void ResetTarget()
    {
        collected = false;
        gameObject.SetActive(true);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.13f, 0.42f, 0.96f, 0.8f);
        Gizmos.DrawWireSphere(AimPosition, 0.35f);
        Gizmos.DrawLine(transform.position, AimPosition);
    }
}
