using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ThreeLayerNativeSlider : MonoBehaviour
{
    [Header("原生Slider引用")]
    public Slider slider;
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
    public TextMeshProUGUI displayText;

    // 可选：San Slider 和文本
    public Slider sanSlider;
    public TextMeshProUGUI sanDisplayText;

    // ---- 充能完成阈值与外部脚本引用 ----
    [Header("充能完成条件")]
    public float chargeThreshold = 80f;           // 达到此值即完成充能
    public InteractionManager interactionManager; // 拖拽目标脚本

    // ---- 私有参数 ----
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

    // ---- 充能完成标志 ----
    private bool isChargeCompleted = false;

    // ---- 公共属性 ----
    public float BonusCurrent => bonusCurrent;
    public bool IsBonusFull => bonusCurrent >= bonusFullValue - 0.01f;
    public float BonusWidth => bonusRt != null ? bonusRt.sizeDelta.x : 0f;
    public float CurrentSan => currentSan;
    public bool IsChargeCompleted => isChargeCompleted;

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
        smoothValue = baseMinValue;
        slider.value = smoothValue;
        if (displayText != null)
            displayText.text = Mathf.Round(smoothValue).ToString();
    }

    public void SetSanRange(float newMin, float newMax, float newTarget = -1f)
    {
        sanMin = newMin;
        sanMax = newMax;

        if (sanSlider == null)
        {
            TryFindSanSlider();
        }

        if (sanSlider != null)
        {
            sanSlider.minValue = sanMin;
            sanSlider.maxValue = sanMax;
            Debug.Log($"✅ [ThreeLayerNativeSlider] SanSlider 范围已更新: min={sanMin}, max={sanMax}, Slider名称={sanSlider.name}");
        }
        else
        {
            Debug.LogError("❌ [ThreeLayerNativeSlider] sanSlider 仍然为空，无法设置San值范围！请将场景中的San Slider拖拽到Inspector的sanSlider字段，或确保存在名为'SanSlider'的Slider。");
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
        isChargeCompleted = false;
        if (interactionManager != null)
            interactionManager.SetChargeComplete(targetObject.gameObject, false);
    }

    // ---- 辅助方法：查找San Slider ----
    private void TryFindSanSlider()
    {
        GameObject go = GameObject.Find("SanSlider");
        if (go != null)
        {
            Slider found = go.GetComponent<Slider>();
            if (found != null && found != slider)
            {
                sanSlider = found;
                Debug.Log($"🔍 [ThreeLayerNativeSlider] 按名称找到 San Slider: {sanSlider.name}");
                return;
            }
        }

        Slider[] allSliders = FindObjectsOfType<Slider>();
        foreach (Slider s in allSliders)
        {
            if (s != slider)
            {
                sanSlider = s;
                Debug.Log($"🔍 [ThreeLayerNativeSlider] 通过 FindObjectsOfType 找到 San Slider: {sanSlider.name}，请确认此Slider确实是San值显示Slider。");
                return;
            }
        }

        sanSlider = null;
        Debug.LogError("❌ [ThreeLayerNativeSlider] 无法找到用于显示San值的Slider！请手动拖拽或确保场景中存在名为'SanSlider'的Slider。");
    }

    // ---- Unity 生命周期 ----
    void Start()
    {
        if (sanSlider == null)
        {
            TryFindSanSlider();
        }

        if (sanDisplayText == null)
        {
            GameObject textGo = GameObject.Find("SanText");
            if (textGo != null)
            {
                TextMeshProUGUI found = textGo.GetComponent<TextMeshProUGUI>();
                if (found != null)
                {
                    sanDisplayText = found;
                    Debug.Log($"🔍 [ThreeLayerNativeSlider] 按名称找到 San 文本: {sanDisplayText.name}");
                }
            }
            else
            {
                TextMeshProUGUI[] allTexts = FindObjectsOfType<TextMeshProUGUI>();
                foreach (TextMeshProUGUI t in allTexts)
                {
                    if (t.text.Contains("SAN") || t.name.Contains("San"))
                    {
                        sanDisplayText = t;
                        Debug.Log($"🔍 [ThreeLayerNativeSlider] 通过文本内容找到 San 文本: {sanDisplayText.name}");
                        break;
                    }
                }
                if (sanDisplayText == null)
                    Debug.LogWarning("⚠️ [ThreeLayerNativeSlider] 未找到 San 显示文本，将不更新数值显示。");
            }
        }

        slider.minValue = 0;
        slider.maxValue = totalMax;
        smoothValue = baseMinValue;
        slider.value = smoothValue;
        bonusCurrent = 0;
        isChargeCompleted = false;
        currentSan = Mathf.Clamp(0, sanMin, sanMax);
        targetSan = currentSan;

        // 获取 UI 矩形引用（用于绘制）
        fillAreaRt = slider.fillRect.parent.GetComponent<RectTransform>();
        hintRt = hintRangeImage.rectTransform;
        bonusRt = bonusChargeImage.rectTransform;

        // 设置锚点、轴心（确保水平拉伸正确）
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

        // 充能完成标志重置
        if (interactionManager != null)
            interactionManager.SetChargeComplete(targetObject.gameObject, false);
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

    // ---- 充能逻辑（含5度角度判定 + 充能完成锁定） ----
    void UpdateBonusCharge()
    {
        bool inChargeZone = smoothValue >= bonusStartThreshold;
        float distance = Vector3.Distance(playerTransform.position, targetObject.position);
        bool isCloseEnough = distance <= chargeDistanceThreshold;

        // 角度判定（5度以内）
        Vector3 dirTarget = (targetObject.position - playerTransform.position).normalized;
        dirTarget.y = 0;
        dirTarget.Normalize();
        Vector3 playerForward = playerTransform.forward;
        playerForward.y = 0;
        playerForward.Normalize();
        float angle = Vector3.Angle(playerForward, dirTarget);
        bool isAligned = angle <= 5f;

        bool canCharge = inChargeZone && isCloseEnough && isAligned;

        if (isChargeCompleted)
        {
            // 已完成：只允许增加，不允许减少
            if (canCharge)
            {
                bonusCurrent += Time.deltaTime * bonusChargeSpeed;
                bonusCurrent = Mathf.Clamp(bonusCurrent, 0, bonusFullValue);
            }
            // 否则不掉落
        }
        else
        {
            if (canCharge)
            {
                bonusCurrent += Time.deltaTime * bonusChargeSpeed;
                bonusCurrent = Mathf.Clamp(bonusCurrent, 0, bonusFullValue);
            }
            else
            {
                bonusCurrent -= Time.deltaTime * bonusDecaySpeed;
                bonusCurrent = Mathf.Clamp(bonusCurrent, 0, bonusFullValue);
            }

            // 检查是否达到阈值
            if (bonusCurrent >= chargeThreshold && !isChargeCompleted)
            {
                isChargeCompleted = true;
                if (interactionManager != null)
                {
                    interactionManager.SetChargeComplete(targetObject.gameObject, true);
                    Debug.Log("✅ 充能完成！已触发 InteractionManager。");
                }
                else
                {
                    Debug.LogWarning("⚠️ 未绑定 InteractionManager 引用，无法触发。");
                }
            }
        }
    }

    // ---- UI绘制方法（恢复原始实现，基于 fillAreaRt 宽度） ----
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