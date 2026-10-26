using System;
using System.Collections;
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
    [Tooltip("钥匙类目标：拾取后播放上浮缩小动画并隐藏")]
    public bool isKey = false;

    public bool collected = false;

    [Header("拾取缓动")]
    public float collectDuration = 0.55f;
    public float collectRise = 0.35f;
    public float collectSpin = 720f;

    public event Action<GuidanceTarget> Collected;

    public Vector3 AimPosition => transform.position + Vector3.up * heightOffset;

    private void OnEnable()
    {
        GuidanceRegistry.Register(this);
    }

    private void OnDisable()
    {
        GuidanceRegistry.Unregister(this);
    }

    /// <summary>拾取。重复调用安全。</summary>
    public void Collect()
    {
        if (collected)
        {
            return;
        }

        collected = true;
        Collected?.Invoke(this);
        Debug.Log($"[Guidance] collected {id} ({label})");

        if (isKey)
        {
            StartCoroutine(CollectRoutine());
        }
    }

    /// <summary>上浮 + 自转加速 + 缩小，然后隐藏。</summary>
    private IEnumerator CollectRoutine()
    {
        KeyBob bob = GetComponent<KeyBob>();
        if (bob != null) bob.enabled = false;

        Vector3 startPos = transform.position;
        Vector3 startScale = transform.localScale;
        float t = 0f;
        float duration = Mathf.Max(0.05f, collectDuration);

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float eased = Mathf.SmoothStep(0f, 1f, k);

            transform.position = startPos + Vector3.up * (collectRise * eased);
            transform.localScale = Vector3.Lerp(startScale, startScale * 0.12f, eased);
            transform.Rotate(Vector3.up, collectSpin * Time.deltaTime, Space.Self);

            yield return null;
        }

        gameObject.SetActive(false);
    }

    public void ResetTarget()
    {
        collected = false;
        transform.localScale = Vector3.one;
        gameObject.SetActive(true);

        KeyBob bob = GetComponent<KeyBob>();
        if (bob != null) bob.enabled = true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.13f, 0.42f, 0.96f, 0.8f);
        Gizmos.DrawWireSphere(AimPosition, 0.35f);
        Gizmos.DrawLine(transform.position, AimPosition);
    }
}
