using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 左侧"距离计"：竖直小滑块 + 数字，提示玩家离目标还有多远。
///   · 蓝色 = 已经达到的接近程度（越近越高），白色 = 还没达到的部分；
///   · 数字显示实时距离（米）；
///   · 可指定一个 <see cref="ChargeSystem"/>，自动跟随它的目标与可充能距离。
/// </summary>
[DisallowMultipleComponent]
public class DistanceGaugeUI : MonoBehaviour
{
    [Header("目标")]
    [Tooltip("留空则使用 chargeSystem 的目标")]
    public Transform target;

    public Transform player;

    [Tooltip("留空则自动查找场景里的 ChargeSystem")]
    public ChargeSystem chargeSystem;

    [Header("量程")]
    [Tooltip("滑块的满量程距离（米）：这么远时进度为 0")]
    public float maxDistance = 40f;

    [Tooltip("这么近时进度为 1（通常设成可充能距离）")]
    public float minDistance = 10f;

    [Tooltip("超过满量程后进度保持 0（不显示负数）")]
    public bool clampToRange = true;

    [Header("显示")]
    [Tooltip("蓝色填充（从下往上）")]
    public RectTransform fillRect;

    [Tooltip("可选：白色游标，标出\"曾经到达的最远位置\"")]
    public RectTransform markerRect;

    public TextMeshProUGUI valueText;

    [Tooltip("数字格式，{0} 是距离（米）")]
    public string valueFormat = "{0:0.0}";

    [Tooltip("数字后面追加的单位")]
    public string unit = "m";

    [Header("平滑")]
    public float smoothSpeed = 6f;

    [Header("关卡目标（自动跟随当前关要去的地方）")]
    [Tooltip("留空则自动查找场景里的 LevelDirector；距离会跟着\"当前关的目标\"走，而不是永远锁定第一个目标")]
    public LevelDirector director;

    private float display01;

    // 填充条的原始锚点：填充必须在这段区间里长，绝不能借用父面板的 0/1
    private Vector2 baseMin = new Vector2(0f, 0f);
    private Vector2 baseMax = new Vector2(1f, 1f);
    private bool baseCached;

    private void Awake()
    {
        if (chargeSystem == null) chargeSystem = FindObjectOfType<ChargeSystem>();
        if (director == null) director = FindObjectOfType<LevelDirector>();

        if (player == null)
        {
            GameObject go = GameObject.Find("Player");
            if (go != null) player = go.transform;
        }

        if (target == null && chargeSystem != null) target = chargeSystem.target;

        CacheBaseAnchors();
    }

    private void CacheBaseAnchors()
    {
        if (fillRect == null) return;

        baseMin = fillRect.anchorMin;
        baseMax = fillRect.anchorMax;
        baseCached = true;
    }

    private void Update()
    {
        // 目标 = 当前关要去的地方（过关后自动切到下一关的目标，不再死盯第一个）
        Transform objective = director != null ? director.CurrentObjective : null;
        if (objective != null) target = objective;
        else if (chargeSystem != null && chargeSystem.target != null) target = chargeSystem.target;

        if (target == null || player == null)
        {
            return;
        }

        float distance = Vector3.Distance(player.position, target.position);

        float range = Mathf.Max(0.01f, maxDistance - minDistance);
        float raw01 = 1f - (distance - minDistance) / range;

        if (clampToRange) raw01 = Mathf.Clamp01(raw01);

        display01 = smoothSpeed > 0f
            ? Mathf.Lerp(display01, raw01, 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime))
            : raw01;

        ApplyFill(display01);

        if (valueText != null)
        {
            valueText.text = string.Format(valueFormat, Mathf.Max(0f, distance)) + unit;
        }
    }

    /// <summary>
    /// 填充只在"刻度尺"的区间内从下往上长：
    /// anchorMin.y 保持刻度尺底边不动，只插值 anchorMax.y ——
    /// 这样它永远不会长到数字上面去。
    /// </summary>
    private void ApplyFill(float value01)
    {
        if (fillRect == null) return;
        if (!baseCached) CacheBaseAnchors();

        float t = Mathf.Clamp01(value01);

        Vector2 min = baseMin;
        Vector2 max = baseMax;
        max.y = Mathf.Lerp(baseMin.y, baseMax.y, t);

        fillRect.anchorMin = min;
        fillRect.anchorMax = max;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        if (markerRect != null)
        {
            Vector2 mMin = markerRect.anchorMin;
            Vector2 mMax = markerRect.anchorMax;
            float y = Mathf.Lerp(baseMin.y, baseMax.y, t);
            mMin.y = mMax.y = y;
            markerRect.anchorMin = mMin;
            markerRect.anchorMax = mMax;
        }
    }
}
