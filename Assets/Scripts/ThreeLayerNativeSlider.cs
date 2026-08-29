using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ThreeLayerNativeSlider : MonoBehaviour
{
    [Header("原生Slider引用")]
    public Slider slider;                 // 视角角度 Slider
    public Image hintRangeImage;
    public Image bonusChargeImage;

    [Header("视角检测")]
    public Transform playerTransform;
    public Transform targetObject;

    [Header("距离条件")]
    public float chargeDistanceThreshold = 10f;

    [Header("平滑过渡")]
    public float smoothSpeed = 6f;

    [Header("数值显示")]
    public TextMeshProUGUI displayText;   // 显示视角角度值

    // 可选：独立显示 San 值的 Slider 和文本（由外部绑定）
    public Slider sanSlider;
    public TextMeshProUGUI sanDisplayText;

    // ---- 私有配置参数（由外部通过方法设置） ----
    private float totalMax = 100f;
    private float baseMinValue = 60f;
    private float levelMaxValue = 80f;
    private float bonusStartThreshold = 75f;
    private float bonusFullValue = 80f;
    private float bonusChargeSpeed = 12f;
    private float bonusDecaySpeed = 5f;
    private float sanMin = 0f;
    private float sanMax = 20f;
    private float sanSmoothSpeed = 3f;

    // ---- 内部状态 ----
    private RectTransform fillAreaRt;
    private RectTransform hintRt;
    private RectTransform bonusRt;
    private float bonusCurrent;
    private float smoothValue;
    private float currentSan;
    private float targetSan;
    private bool overrideSan = false;

    // ---- 公共属性 ----
    public float BonusCurrent => bonusCurrent;
    public bool IsBonusFull => bonusCurrent >= bonusFullValue - 0.01f;
    public float BonusWidth => bonusRt != null ? bonusRt.sizeDelta.x : 0f;
    public float CurrentSan => currentSan;

    // ---- 外部设置接口 ----
    public void SetTotalMax(float max)
    {
        totalMax = max;
        slider.maxValue = totalMax;
    }

    public void SetChargeSpeeds(float chargeSpeed, float decaySpeed)
    {
        bonusChargeSpeed = chargeSpeed;
        bonusDecaySpeed = decaySpeed;
    }

    public void SetLevelParameters(float baseMin, float levelMax, float startThreshold, float fullValue)
    {
        baseMinValue = baseMin;
        levelMaxValue = levelMax;
        bonusStartThreshold = startThreshold;
        bonusFullValue = fullValue;
        // 立即更新平滑值和 Slider 显示
        smoothValue = baseMinValue;
        slider.value = smoothValue;
        if (displayText != null)
            displayText.text = Mathf.Round(smoothValue).ToString();
    }

    public void SetSanRange(float newMin, float newMax, float newTarget = -1f)
    {
        sanMin = newMin;
        sanMax = newMax;
        if (sanSlider != null)
        {
            sanSlider.minValue = sanMin;
            sanSlider.maxValue = sanMax;
        }
        currentSan = Mathf.Clamp(currentSan, sanMin, sanMax);
        targetSan = (newTarget >= 0) ? Mathf.Clamp(newTarget, sanMin, sanMax) : Mathf.Clamp(targetSan, sanMin, sanMax);
        overrideSan = true;
        UpdateSanUI();
    }

    public void SetTargetSan(float value, bool useOverride = true)
    {
        if (value < sanMin || value > sanMax)
        {
            Debug.LogWarning($"目标 San 值 {value} 超出范围 [{sanMin}, {sanMax}]，已自动钳制");
            value = Mathf.Clamp(value, sanMin, sanMax);
        }
        targetSan = value;
        overrideSan = useOverride;
    }

    public float GetCurrentSan() => currentSan;

    public void ResetBonus()
    {
        bonusCurrent = 0f;
    }

    // ---- Unity 生命周期 ----
    void Start()
    {
        slider.minValue = 0;
        slider.maxValue = totalMax;
        smoothValue = baseMinValue;
        slider.value = smoothValue;
        bonusCurrent = 0;
        currentSan = Mathf.Clamp(0, sanMin, sanMax);
        targetSan = currentSan;

        fillAreaRt = slider.fillRect.parent.GetComponent<RectTransform>();
        hintRt = hintRangeImage.rectTransform;
        bonusRt = bonusChargeImage.rectTransform;

        hintRt.anchorMin = new Vector2(0, 0);
        hintRt.anchorMax = new Vector2(0, 1);
        hintRt.pivot = new Vector2(0, 0.5f);

        bonusRt.anchorMin = new Vector2(0, 0);
        bonusRt.anchorMax = new Vector2(0, 1);
        bonusRt.pivot = new Vector2(0, 0.5f);

        if (sanSlider != null)
        {
            sanSlider.minValue = sanMin;
            sanSlider.maxValue = sanMax;
            sanSlider.value = currentSan;
        }
        UpdateSanUI();
    }

    void LateUpdate()
    {
        DrawHintRange();
        DrawBonusCharge();

        if (playerTransform != null && targetObject != null)
        {
            float targetRawValue = CalcValueByAngle();
            smoothValue = Mathf.Lerp(smoothValue, targetRawValue, Time.deltaTime * smoothSpeed);
            slider.value = smoothValue;
            if (displayText != null)
                displayText.text = Mathf.Round(smoothValue).ToString();
        }

        UpdateBonusCharge();

        // San 值平滑更新
        currentSan = Mathf.Lerp(currentSan, targetSan, Time.deltaTime * sanSmoothSpeed);
        currentSan = Mathf.Clamp(currentSan, sanMin, sanMax);
        UpdateSanUI();
    }

    // ---- 核心计算方法 ----
    float CalcValueByAngle()
    {
        Vector3 dirTarget = (targetObject.position - playerTransform.position).normalized;
        dirTarget.y = 0;
        dirTarget.Normalize();

        Vector3 playerForward = playerTransform.forward;
        playerForward.y = 0;
        playerForward.Normalize();

        float dot = Vector3.Dot(playerForward, dirTarget);
        float t = Mathf.InverseLerp(-1f, 1f, dot);
        return Mathf.Lerp(baseMinValue, levelMaxValue, t);
    }

    void UpdateBonusCharge()
    {
        bool inChargeZone = smoothValue >= bonusStartThreshold;
        float distance = Vector3.Distance(playerTransform.position, targetObject.position);
        bool isCloseEnough = distance <= chargeDistanceThreshold;

        if (inChargeZone && isCloseEnough)
        {
            bonusCurrent += Time.deltaTime * bonusChargeSpeed;
            bonusCurrent = Mathf.Clamp(bonusCurrent, 0, bonusFullValue);
        }
        else
        {
            bonusCurrent -= Time.deltaTime * bonusDecaySpeed;
            bonusCurrent = Mathf.Clamp(bonusCurrent, 0, bonusFullValue);
        }
    }

    void DrawHintRange()
    {
        float totalWidth = fillAreaRt.rect.width;
        if (totalWidth < 1) return;

        float startPct = baseMinValue / totalMax;
        float endPct = levelMaxValue / totalMax;

        float startX = startPct * totalWidth;
        float width = (endPct - startPct) * totalWidth;

        hintRt.anchoredPosition = new Vector2(startX, 0);
        hintRt.sizeDelta = new Vector2(width, 0);
    }

    void DrawBonusCharge()
    {
        float totalWidth = fillAreaRt.rect.width;
        if (totalWidth < 1) return;

        float pct = bonusCurrent / totalMax;
        bonusRt.sizeDelta = new Vector2(pct * totalWidth, 0);
        bonusRt.anchoredPosition = Vector2.zero;
    }

    void UpdateSanUI()
    {
        if (sanSlider != null)
            sanSlider.value = currentSan;
        if (sanDisplayText != null)
            sanDisplayText.text = $"SAN: {Mathf.Round(currentSan)}";
    }
}