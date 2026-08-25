using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SanSystem : MonoBehaviour
{
    [Header("San值设置")]
    [Tooltip("初始San值")]
    public float initialSan = 60f;
    public float maxSan = 100f;
    public float minSan = 0f;
    [Tooltip("平滑过渡速度，越大变化越快")]
    public float sanSmoothSpeed = 3f;

    [Header("最佳方向（当不覆盖目标时使用）")]
    public Vector3 sanBestWorldDir = Vector3.forward;

    [Header("UI绑定")]
    public Slider sanSlider;
    public TextMeshProUGUI sanText;

    private float currentSan;
    private float targetSan;
    private bool overrideTarget = false;   // 是否由外部控制目标值

    void Start()
    {
        sanBestWorldDir.Normalize();
        // 初始值设为 initialSan（在 min-max 范围内）
        currentSan = Mathf.Clamp(initialSan, minSan, maxSan);
        targetSan = currentSan;

        if (sanSlider != null)
        {
            sanSlider.minValue = minSan;
            sanSlider.maxValue = maxSan;
            sanSlider.value = currentSan;
        }
        UpdateSanUI();
    }

    void Update()
    {
        // 如果未被外部覆盖，则按方向计算目标值
        if (!overrideTarget)
        {
            CalculateTargetSanByView();
        }
        // 否则 targetSan 由外部通过 SetTargetSan 设置，此处保持不变

        // 平滑插值
        currentSan = Mathf.Lerp(currentSan, targetSan, Time.deltaTime * sanSmoothSpeed);
        // 确保不超出范围（因浮点误差）
        currentSan = Mathf.Clamp(currentSan, minSan, maxSan);
        UpdateSanUI();
    }

    void CalculateTargetSanByView()
    {
        // 获取玩家水平向前方向，剔除垂直
        Vector3 playerForwardFlat = transform.forward;
        playerForwardFlat.y = 0;
        if (playerForwardFlat.sqrMagnitude < 0.001f) return; // 避免归一化零向量
        playerForwardFlat.Normalize();

        Vector3 bestDirFlat = sanBestWorldDir;
        bestDirFlat.y = 0;
        bestDirFlat.Normalize();

        float dot = Vector3.Dot(playerForwardFlat, bestDirFlat);
        // dot 范围 -1~1，映射到 0~1
        float t = (dot + 1f) / 2f;
        targetSan = Mathf.Lerp(minSan, maxSan, t);
    }

    void UpdateSanUI()
    {
        if (sanSlider != null)
            sanSlider.value = currentSan;
        if (sanText != null)
            sanText.text = $"SAN: {Mathf.Round(currentSan)}";
    }

    // --- 外部控制接口 ---
    /// <summary>
    /// 设置目标San值，并决定是否采用外部控制模式
    /// </summary>
    public void SetTargetSan(float value, bool useOverride = true)
    {
        targetSan = Mathf.Clamp(value, minSan, maxSan);
        overrideTarget = useOverride;
    }

    /// <summary>
    /// 获取当前实际San值
    /// </summary>
    public float GetCurrentSan()
    {
        return currentSan;
    }

    /// <summary>
    /// 重置为方向计算模式
    /// </summary>
    public void ResetToDirectionMode()
    {
        overrideTarget = false;
    }

    /// <summary>
    /// 设置最佳方向（用于方向计算模式）
    /// </summary>
    public void SetSanBestDirection(Vector3 newWorldDir)
    {
        sanBestWorldDir = newWorldDir;
        sanBestWorldDir.Normalize();
    }
}