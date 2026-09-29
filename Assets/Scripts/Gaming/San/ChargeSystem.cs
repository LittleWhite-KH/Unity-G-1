using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 充能系统（策划案 Part1 第 2~3 步的新版）：
///
///   · 玩家看向目标 → SAN 上升（<see cref="sanTargetWhenFacing"/>）；
///   · 距离越近，波动越强（<see cref="proximityCurve"/> + <see cref="proximityBoostMax"/>）；
///   · 进入 <see cref="chargeDistance"/> 米内且视角偏差小于 <see cref="angleThreshold"/> 度时，
///     波形的所有柱子**凝聚成一整块矩形**（SanDisplay.SetCoalesce）；
///   · 此时开始判定充能：<see cref="chargeDuration"/> 秒充满（默认 5 秒），
///     蓝色为已充能、白色为未充能，由 <see cref="ChargeBarUI"/> 显示；
///   · 充满后 <see cref="IsComplete"/> 置真，关卡阶段据此进入下一关。
///
/// 离开条件时有 <see cref="graceTime"/> 秒宽容，并且进度按 <see cref="decayPerSecond"/> 缓慢回退
/// （不是瞬间清零，给玩家容错）。
/// </summary>
[DisallowMultipleComponent]
public class ChargeSystem : MonoBehaviour
{
    [Header("目标与玩家")]
    [Tooltip("充能目标，例如墓碑")]
    public Transform target;

    public Transform player;
    public Camera playerCamera;

    [Header("可充能条件")]
    [Tooltip("最大距离（米）")]
    public float chargeDistance = 10f;

    [Tooltip("视角偏差上限（度）")]
    public float angleThreshold = 5f;

    [Tooltip("离开条件后的宽容时间（秒）——避免鼠标微动就中断充能")]
    public float graceTime = 0.25f;

    [Tooltip("是否额外要求 SAN 达到某个阶段")]
    public bool requireSanStage = false;

    public SanSystem.Stage requiredStage = SanSystem.Stage.Safe;

    [Header("充能")]
    [Tooltip("充满所需秒数（策划案：5 秒）")]
    public float chargeDuration = 5f;

    [Tooltip("条件不满足时每秒回退的进度（0.3 ≈ 3.3 秒退完）")]
    public float decayPerSecond = 0.3f;

    [Header("联动")]
    public SanSystem sanSystem;
    public SanDisplay sanDisplay;
    public ChargeBarUI chargeBar;

    [Header("SAN（按距离：越近越高）")]
    [Tooltip("该距离时 SAN = 0（每关起点的距离）")]
    public float sanZeroDistance = 40f;

    [Tooltip("该距离时 SAN = 100")]
    public float sanFullDistance = 10f;

    public float sanFollowSpeed = 12f;

    [Header("方位提示（把波形当成指南针）")]
    [Tooltip("水平视野范围（度）：超出这个角度就完全没有起伏（背对目标时）")]
    public float directionFov = 130f;

    [Tooltip("刚进视野时的最小起伏强度")]
    [Range(0f, 1f)] public float directionMinStrength = 0.25f;

    [Tooltip("最远时参与起伏的柱子比例")]
    [Range(0.01f, 1f)] public float directionMinWidth = 0.06f;

    [Tooltip("最近时参与起伏的柱子比例（越近越多柱子）")]
    [Range(0.01f, 1f)] public float directionMaxWidth = 0.30f;

    [Tooltip("最近时的抖动增益（越大 = 抖得越快越猛）")]
    [Range(0f, 3f)] public float jitterBoostMax = 1.6f;

    [Header("视线遮挡")]
    [Tooltip("开启后：相机与目标之间被挡住时无法充能（不会隔墙充能）")]
    public bool requireLineOfSight = true;

    [Tooltip("参与遮挡检测的层")]
    public LayerMask occlusionMask = ~0;

    [Tooltip("射线终点相对目标中心的抬升（对准墓碑中部）")]
    public float aimHeightOffset = 0.6f;

    [Tooltip("射线提前结束的距离，避免被目标自身表面挡住")]
    public float rayEndPadding = 0.25f;

    [Header("靠近时的波动增益（越近越强）")]
    [Tooltip("超过该距离不再叠加波动")]
    public float proximityStartDistance = 30f;

    [Tooltip("0 = 最远，1 = 最近")]
    public AnimationCurve proximityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Range(0f, 1f)] public float proximityBoostMax = 0.6f;

    [Header("凝聚")]
    [Tooltip("波形收敛成一整块矩形的速度")]
    public float coalesceSpeed = 2.5f;

    [Header("事件")]
    public UnityEvent onChargeStarted;
    public UnityEvent onProgressFull;
    public UnityEvent onChargeCompleted;

    // ---- 运行时状态 ----
    public float Distance { get; private set; }
    public float Angle { get; private set; }
    public bool InRange { get; private set; }
    public bool IsFacing { get; private set; }
    public bool CanCharge { get; private set; }
    public bool HasLineOfSight { get; private set; } = true;
    public bool IsCharging { get; private set; }
    public float Progress01 { get; private set; }
    public bool IsComplete { get; private set; }
    public float Coalesce { get; private set; }

    private float graceTimer;
    private bool progressFullFired;

    private void Awake()
    {
        if (playerCamera == null) playerCamera = Camera.main;

        if (player == null)
        {
            GameObject go = GameObject.Find("Player");
            if (go != null) player = go.transform;
        }
    }

    private void OnDisable()
    {
        // 被禁用时把叠加量清掉，避免波形卡在凝聚/高波动状态
        if (sanDisplay != null)
        {
            sanDisplay.SetProximityBoost(0f);
            sanDisplay.SetCoalesce(0f);
            sanDisplay.SetDirection(0.5f, 0.1f, 0f);
            sanDisplay.SetJitterBoost(0f);
        }
    }

    private void Update()
    {
        if (target == null || player == null)
        {
            return;
        }

        Distance = Vector3.Distance(player.position, target.position);
        Angle = AngleToTarget();
        InRange = Distance <= chargeDistance;
        IsFacing = Angle <= angleThreshold;
        HasLineOfSight = !requireLineOfSight || CheckLineOfSight();

        bool sanOk = !requireSanStage || (sanSystem != null && sanSystem.IsAtLeast(requiredStage));
        CanCharge = InRange && IsFacing && sanOk && HasLineOfSight;

        // 宽容计时
        if (CanCharge) graceTimer = graceTime;
        else graceTimer -= Time.deltaTime;

        bool chargingNow = !IsComplete && graceTimer > 0f;
        if (chargingNow && !IsCharging)
        {
            onChargeStarted?.Invoke();
        }

        IsCharging = chargingNow;

        // SAN：离目标越近越高（每关起点记 0，最高 100，不会低于 0）
        if (sanSystem != null)
        {
            sanSystem.SetFollowSpeed(sanFollowSpeed);

            float proximity = Mathf.InverseLerp(sanZeroDistance, sanFullDistance, Distance);
            sanSystem.SetTarget(Mathf.Clamp01(proximity) * 100f);
        }

        // 方位提示：波形的"哪一侧高"= 目标在这一侧的哪个方向
        UpdateDirectionHint();

        // 波动增益：越近越强（未看向目标时不叠加）
        float coalesceTarget = (InRange && IsFacing && HasLineOfSight) ? 1f : 0f;
        Coalesce = MoveTowardsSmooth(Coalesce, coalesceTarget, coalesceSpeed);

        if (sanDisplay != null)
        {
            float near01 = Mathf.InverseLerp(proximityStartDistance, chargeDistance, Distance);
            float boost = proximityCurve != null ? proximityCurve.Evaluate(Mathf.Clamp01(near01)) : near01;
            boost *= proximityBoostMax;

            // 已经凝成矩形时把增益收回，否则矩形会抖
            boost *= Mathf.Lerp(1f, 0f, Mathf.Clamp01(Coalesce));

            sanDisplay.SetProximityBoost(IsFacing ? boost : 0f);
            sanDisplay.SetCoalesce(Coalesce);
        }

        // 充能进度
        if (!IsComplete)
        {
            if (chargingNow)
            {
                Progress01 += Time.deltaTime / Mathf.Max(0.01f, chargeDuration);

                if (Progress01 >= 1f && !progressFullFired)
                {
                    progressFullFired = true;
                    onProgressFull?.Invoke();
                }
            }
            else
            {
                Progress01 -= decayPerSecond * Time.deltaTime;
            }

            Progress01 = Mathf.Clamp01(Progress01);

            if (chargeBar != null)
            {
                chargeBar.SetProgress(Progress01);
                chargeBar.SetVisible(CanCharge || Progress01 > 0.001f);
            }

            if (Progress01 >= 1f)
            {
                IsComplete = true;
                IsCharging = false;

                if (chargeBar != null) chargeBar.SetProgress(1f);

                Debug.Log($"[ChargeSystem] 充能完成（距离 {Distance:0.##}m，偏差 {Angle:0.##}°）");
                onChargeCompleted?.Invoke();
            }
        }
    }

    /// <summary>
    /// 视线遮挡检测：从相机射向目标，看第一段挡住的是什么。
    /// 命中的是目标自己 → 通视；命中的是别的东西 → 被挡住（不能隔墙充能）。
    /// </summary>
    private bool CheckLineOfSight()
    {
        if (target == null) return true;

        Camera cam = playerCamera != null ? playerCamera : Camera.main;
        if (cam == null) return true;

        Vector3 origin = cam.transform.position;
        Vector3 aimPoint = target.position + Vector3.up * aimHeightOffset;
        Vector3 direction = aimPoint - origin;

        float length = direction.magnitude;
        if (length <= 0.01f) return true;

        float rayLength = Mathf.Max(0.01f, length - rayEndPadding);

        if (!Physics.Raycast(origin, direction.normalized, out RaycastHit hit, rayLength, occlusionMask,
                QueryTriggerInteraction.Ignore))
        {
            // 路上什么都没挡（目标可能没有碰撞体）→ 视为通视
            return true;
        }

        return hit.transform == target || hit.transform.IsChildOf(target);
    }

    /// <summary>把"目标在玩家的哪个方向 + 有多近"翻译成波形的方位参数。</summary>
    private void UpdateDirectionHint()
    {
        if (sanDisplay == null) return;

        Camera cam = playerCamera != null ? playerCamera : Camera.main;
        if (cam == null || target == null) return;

        Vector3 forward = cam.transform.forward;
        forward.y = 0f;

        Vector3 toTarget = target.position - cam.transform.position;
        toTarget.y = 0f;

        if (forward.sqrMagnitude < 0.0001f || toTarget.sqrMagnitude < 0.0001f) return;

        // 正数 = 目标在视角右侧，负数 = 在左侧
        float signedAngle = Vector3.SignedAngle(forward.normalized, toTarget.normalized, Vector3.up);
        float halfRange = Mathf.Max(1f, directionFov * 0.5f);

        // 超出视野范围（基本就是背对目标）→ 完全没起伏
        bool inSight = Mathf.Abs(signedAngle) <= halfRange;

        // 0.5 = 正前方；目标在左 → 小于 0.5（条左侧偏高）
        float center01 = Mathf.Clamp01(0.5f + signedAngle / (halfRange * 2f));

        float near01 = Mathf.Clamp01(Mathf.InverseLerp(sanZeroDistance, sanFullDistance, Distance));

        float strength = inSight ? Mathf.Lerp(directionMinStrength, 1f, near01) : 0f;
        float width = Mathf.Lerp(directionMinWidth, directionMaxWidth, near01);
        float jitter = Mathf.Lerp(0f, jitterBoostMax, near01);

        sanDisplay.SetDirection(center01, width, strength);
        sanDisplay.SetJitterBoost(jitter);
    }

    /// <summary>把进度、凝聚、增益全部复位，供重玩或阶段重启使用。</summary>
    public void ResetCharge()
    {
        Progress01 = 0f;
        IsComplete = false;
        IsCharging = false;
        graceTimer = 0f;
        progressFullFired = false;
        Coalesce = 0f;

        if (chargeBar != null)
        {
            chargeBar.ResetBar();          // 蓝条与红条一起清零
            chargeBar.SetVisible(false);
        }

        if (sanDisplay != null)
        {
            sanDisplay.SetCoalesce(0f);
            sanDisplay.SetProximityBoost(0f);
        }
    }

    private float AngleToTarget()
    {
        Camera cam = playerCamera != null ? playerCamera : Camera.main;
        if (cam == null || target == null || player == null)
        {
            return 180f;
        }

        Vector3 dir = target.position - player.position;
        dir.y = 0f;

        Vector3 forward = cam.transform.forward;
        forward.y = 0f;

        if (dir.sqrMagnitude < 0.0001f || forward.sqrMagnitude < 0.0001f)
        {
            return 180f;
        }

        return Vector3.Angle(forward, dir.normalized);
    }

    /// <summary>与帧率无关的平滑趋近。</summary>
    private static float MoveTowardsSmooth(float current, float targetValue, float speed)
    {
        if (speed <= 0f)
        {
            return targetValue;
        }

        float k = 1f - Mathf.Exp(-speed * Time.deltaTime);
        return Mathf.Lerp(current, targetValue, k);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (target == null) return;

        Gizmos.color = new Color(0.13f, 0.42f, 0.96f, 0.6f);
        Gizmos.DrawWireSphere(target.position, chargeDistance);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(target.position, target.position + Vector3.up * 2f);
    }
#endif
}
