using UnityEngine;
using UnityEngine.UI;

public class ThreeLayerNativeSlider : MonoBehaviour
{
    [Header("原生Slider引用")]
    public Slider slider;
    public Image hintRangeImage;
    public Image bonusChargeImage;      //⭐第四层：充能条

    [Header("数值配置")]
    public float totalMax = 100f;
    public float baseMinValue = 60f;
    public float levelMaxValue = 80f;

    [Header("第四层充能设置")]
    public float bonusStartThreshold = 75f;  //大于等于75开始充能
    public float bonusFullValue = 80f;      //充能目标上限
    public float bonusChargeSpeed = 12f;    //充能增长速度
    [Tooltip("离开高能区间是否重置充能")]
    public bool resetBonusWhenLeave = false;

    [Header("视角检测")]
    public Transform playerTransform;
    public Transform targetObject;

    [Header("平滑过渡")]
    public float smoothSpeed = 6f;
    private float smoothValue;

    private RectTransform fillAreaRt;
    private RectTransform hintRt;
    private RectTransform bonusRt;

    private float bonusCurrent; //第四层当前充能数值

    void Start()
    {
        slider.minValue = 0;
        slider.maxValue = totalMax;
        smoothValue = baseMinValue;
        bonusCurrent = 0; //充能初始为0

        fillAreaRt = slider.fillRect.parent.GetComponent<RectTransform>();
        hintRt = hintRangeImage.rectTransform;
        bonusRt = bonusChargeImage.rectTransform;

        //hint锚点
        hintRt.anchorMin = new Vector2(0, 0);
        hintRt.anchorMax = new Vector2(0, 1);
        hintRt.pivot = new Vector2(0, 0.5f);

        //⭐第四层Bonus锚点设置
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

        //⭐第四层充能逻辑
        UpdateBonusCharge();
    }

    /// <summary>
    /// 水平角度计算，忽略Y轴俯仰
    /// </summary>
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

    /// <summary>
    /// 更新第四层充能
    /// </summary>
    void UpdateBonusCharge()
    {
        //判断是否处于75~80区间
        bool inChargeZone = smoothValue >= bonusStartThreshold;

        if (inChargeZone)
        {
            //在区间内，持续充能，上限80
            bonusCurrent += Time.deltaTime * bonusChargeSpeed;
            bonusCurrent = Mathf.Clamp(bonusCurrent, 0, bonusFullValue);
        }
        else
        {
            if (resetBonusWhenLeave)
            {
                bonusCurrent = 0;
            }
            //不重置就保持现有数值不动
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

    /// <summary>
    /// ⭐绘制第四层充能条
    /// </summary>
    void DrawBonusCharge()
    {
        float totalWidth = fillAreaRt.rect.width;
        if (totalWidth < 1) return;

        float pct = bonusCurrent / totalMax;
        bonusRt.sizeDelta = new Vector2(pct * totalWidth, 0);
        bonusRt.anchoredPosition = Vector2.zero;
    }
}
