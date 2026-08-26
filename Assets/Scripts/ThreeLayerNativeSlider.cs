using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ThreeLayerNativeSlider : MonoBehaviour
{
    [Header("原生Slider引用")]
    public Slider slider;
    public Image hintRangeImage;
    public Image bonusChargeImage;

    [Header("数值配置")]
    public float totalMax = 100f;
    public float baseMinValue = 60f;
    public float levelMaxValue = 80f;

    [Header("第四层充能设置")]
    public float bonusStartThreshold = 75f;
    public float bonusFullValue = 80f;
    public float bonusChargeSpeed = 12f;
    public float bonusDecaySpeed = 5f;

    [Header("视角检测")]
    public Transform playerTransform;
    public Transform targetObject;   // 当前目标（由外部动态赋值）

    [Header("距离条件")]
    public float chargeDistanceThreshold = 10f;   // 距离≤10才能充能

    [Header("平滑过渡")]
    public float smoothSpeed = 6f;
    private float smoothValue;

    [Header("数值显示")]
    public TextMeshProUGUI displayText;

    private RectTransform fillAreaRt;
    private RectTransform hintRt;
    private RectTransform bonusRt;
    private float bonusCurrent;

    // 公共属性
    public float BonusCurrent => bonusCurrent;
    public bool IsBonusFull => bonusCurrent >= bonusFullValue - 0.01f;
    public float BonusWidth => bonusRt != null ? bonusRt.sizeDelta.x : 0f;

    // 重置充能
    public void ResetBonus()
    {
        bonusCurrent = 0f;
    }

    void Start()
    {
        slider.minValue = 0;
        slider.maxValue = totalMax;
        smoothValue = baseMinValue;
        bonusCurrent = 0;

        fillAreaRt = slider.fillRect.parent.GetComponent<RectTransform>();
        hintRt = hintRangeImage.rectTransform;
        bonusRt = bonusChargeImage.rectTransform;

        hintRt.anchorMin = new Vector2(0, 0);
        hintRt.anchorMax = new Vector2(0, 1);
        hintRt.pivot = new Vector2(0, 0.5f);

        bonusRt.anchorMin = new Vector2(0, 0);
        bonusRt.anchorMax = new Vector2(0, 1);
        bonusRt.pivot = new Vector2(0, 0.5f);
    }

    void LateUpdate()
    {
        DrawHintRange();
        DrawBonusCharge();

        if (playerTransform == null || targetObject == null) return;

        float targetRawValue = CalcValueByAngle();
        smoothValue = Mathf.Lerp(smoothValue, targetRawValue, Time.deltaTime * smoothSpeed);
        slider.value = smoothValue;

        UpdateBonusCharge();

        if (displayText != null)
        {
            displayText.text = Mathf.Round(smoothValue).ToString();
        }
    }

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
        float val = Mathf.Lerp(baseMinValue, levelMaxValue, t);
        return val;
    }

    void UpdateBonusCharge()
    {
        // 1. 判断视角是否在充电区
        bool inChargeZone = smoothValue >= bonusStartThreshold;

        // 2. 判断距离是否在阈值内
        float distance = Vector3.Distance(playerTransform.position, targetObject.position);
        bool isCloseEnough = distance <= chargeDistanceThreshold;

        // 只有同时满足视角和距离条件，才充电；否则衰减
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
}