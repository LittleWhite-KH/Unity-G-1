using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Transform))]
public class LightFadeByDistance : MonoBehaviour
{
    [Header("衰减距离区间")]
    public float startFadeDistance = 14f;
    public float endFadeDistance = 20f;

    [Header("扫描性能（秒）")]
    public float scanInterval = 0.2f;

    [Header("排除列表（比如玩家自身的手电筒）")]
    public List<Light> excludeLights = new List<Light>();

    // ========== 🆕 缓动设置 ==========
    [Header("缓动曲线类型")]
    public EasingType easing = EasingType.SmoothStep; // 默认S形缓进缓出

    // 如果你选择 CustomCurve，需要在这里拖入曲线
    public AnimationCurve customCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // 缓动类型枚举
    public enum EasingType
    {
        Linear,      // 线性（无缓动）
        SmoothStep,  // 平滑S形（缓进缓出）
        CustomCurve  // 自定义曲线
    }
    // =================================

    // 缓存数据
    private List<Light> allLights = new List<Light>();
    private Dictionary<Light, LightOriginalData> lightDataMap = new Dictionary<Light, LightOriginalData>();
    private float scanTimer;

    private struct LightOriginalData
    {
        public float intensity;
        public float range;
        public bool wasEnabled;
    }

    void Start()
    {
        RefreshLightList();
        if (startFadeDistance >= endFadeDistance)
        {
            Debug.LogWarning("起始距离应小于结束距离，自动交换数值");
            (startFadeDistance, endFadeDistance) = (endFadeDistance, startFadeDistance);
        }
    }

    public void RefreshLightList()
    {
        Light[] foundLights = FindObjectsOfType<Light>(true);
        allLights.Clear();
        lightDataMap.Clear();

        foreach (Light l in foundLights)
        {
            if (excludeLights.Contains(l)) continue;
            allLights.Add(l);
            lightDataMap[l] = new LightOriginalData
            {
                intensity = l.intensity,
                range = l.range,
                wasEnabled = l.enabled
            };
        }
    }

    void Update()
    {
        scanTimer += Time.deltaTime;
        if (scanTimer >= scanInterval)
        {
            scanTimer = 0f;
            ScanAndUpdateLights();
        }
    }

    void ScanAndUpdateLights()
    {
        Vector3 playerPos = transform.position;

        foreach (Light l in allLights)
        {
            if (l == null) continue;

            float dist = Vector3.Distance(playerPos, l.transform.position);
            LightOriginalData orig = lightDataMap[l];

            float targetIntensity;
            float targetRange;
            bool shouldEnable;

            if (dist <= startFadeDistance)
            {
                targetIntensity = orig.intensity;
                targetRange = orig.range;
                shouldEnable = orig.wasEnabled;
            }
            else if (dist >= endFadeDistance)
            {
                targetIntensity = 0f;
                targetRange = 0f;
                shouldEnable = false;
            }
            else
            {
                // ----- 1. 计算原始线性 t (0~1) -----
                float t = (dist - startFadeDistance) / (endFadeDistance - startFadeDistance);

                // ----- 2. 🆕 应用缓动函数，得到 easedT -----
                float easedT = ApplyEasing(t);

                // ----- 3. 用 easedT 进行插值（缓进缓出）-----
                targetIntensity = Mathf.Lerp(orig.intensity, 0f, easedT);
                targetRange = Mathf.Lerp(orig.range, 0f, easedT);
                shouldEnable = targetIntensity > 0.01f;
            }

            l.intensity = targetIntensity;
            l.range = targetRange;
            if (l.enabled != shouldEnable)
            {
                l.enabled = shouldEnable;
            }
        }
    }

    // 🆕 缓动函数处理器
    private float ApplyEasing(float t)
    {
        switch (easing)
        {
            case EasingType.Linear:
                return t;
            case EasingType.SmoothStep:
                return Mathf.SmoothStep(0f, 1f, t); // 标准 S 形
            case EasingType.CustomCurve:
                return customCurve.Evaluate(t);    // 你自己画的曲线
            default:
                return t;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, startFadeDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, endFadeDistance);
    }
}