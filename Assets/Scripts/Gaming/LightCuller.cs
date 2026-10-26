using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LightCuller : MonoBehaviour
{
    [Header("灯光剔除设置")]
    public float cullDistance = 20f;          // 灯光完全消失的距离
    public float fadeStartDistance = 15f;      // 开始淡出的距离（从该距离到 cullDistance 之间过渡）
    public float fadeDuration = 0.5f;          // 每个灯光淡入淡出持续时间（秒）
    public float checkInterval = 0.2f;         // 检测间隔

    [Header("自动注册")]
    public bool autoFindObjects = true;
    public LayerMask includeLayers = ~0;       // 管理的层（默认所有层）

    private Transform player;
    private List<Light> lights = new List<Light>();

    private class LightOriginal
    {
        public float intensity;
        public float range;
        public float spotAngle;
        public bool enabled;
    }
    private Dictionary<Light, LightOriginal> originalValues = new Dictionary<Light, LightOriginal>();
    private Dictionary<Light, float> currentWeights = new Dictionary<Light, float>();
    private Dictionary<Light, Coroutine> fadeCoroutines = new Dictionary<Light, Coroutine>();

    private bool initialized;

    void Start()
    {
        player = transform;

        if (autoFindObjects)
        {
            Light[] allLights = FindObjectsOfType<Light>();
            foreach (var light in allLights)
            {
                if (!light.enabled || ((1 << light.gameObject.layer) & includeLayers) == 0)
                    continue;

                lights.Add(light);
                LightOriginal orig = new LightOriginal
                {
                    intensity = light.intensity,
                    range = light.range,
                    spotAngle = light.spotAngle,
                    enabled = light.enabled
                };
                originalValues[light] = orig;
                currentWeights[light] = 1f;
            }
        }

        initialized = true;
        UpdateCulling();
        StartCoroutine(CullingLoop());
    }

    private void OnEnable()
    {
        if (initialized) StartCoroutine(CullingLoop());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        fadeCoroutines.Clear();
        foreach (var pair in originalValues)
        {
            if (pair.Key == null) continue;
            pair.Key.intensity = pair.Value.intensity;
            pair.Key.range = pair.Value.range;
            if (pair.Key.type == LightType.Spot) pair.Key.spotAngle = pair.Value.spotAngle;
            pair.Key.enabled = pair.Value.enabled;
            currentWeights[pair.Key] = 1f;
        }
    }

    IEnumerator CullingLoop()
    {
        WaitForSeconds wait = new WaitForSeconds(checkInterval);
        while (true)
        {
            UpdateCulling();
            yield return wait;
        }
    }

    void UpdateCulling()
    {
        Vector3 playerPos = player.position;
        float fadeRange = cullDistance - fadeStartDistance;

        foreach (var light in lights)
        {
            if (light == null) continue;

            float dist = Vector3.Distance(light.transform.position, playerPos);
            float targetWeight = 0f;

            if (dist <= fadeStartDistance)
                targetWeight = 1f;
            else if (dist < cullDistance)
                targetWeight = 1f - (dist - fadeStartDistance) / fadeRange;
            else
                targetWeight = 0f;

            float currentWeight = currentWeights.ContainsKey(light) ? currentWeights[light] : 0f;
            if (Mathf.Abs(currentWeight - targetWeight) > 0.001f)
            {
                if (fadeCoroutines.ContainsKey(light) && fadeCoroutines[light] != null)
                {
                    StopCoroutine(fadeCoroutines[light]);
                    fadeCoroutines.Remove(light);
                }
                Coroutine coroutine = StartCoroutine(FadeLight(light, targetWeight));
                fadeCoroutines[light] = coroutine;
            }
        }
    }

    IEnumerator FadeLight(Light light, float targetWeight)
    {
        float startWeight = currentWeights.ContainsKey(light) ? currentWeights[light] : 0f;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            float smoothT = t * t * (3f - 2f * t); // SmoothStep
            float weight = Mathf.Lerp(startWeight, targetWeight, smoothT);
            ApplyLightWeight(light, weight);
            yield return null;
        }

        ApplyLightWeight(light, targetWeight);
        currentWeights[light] = targetWeight;

        if (fadeCoroutines.ContainsKey(light))
            fadeCoroutines.Remove(light);
    }

    void ApplyLightWeight(Light light, float weight)
    {
        if (!originalValues.ContainsKey(light)) return;

        var orig = originalValues[light];
        light.intensity = orig.intensity * weight;
        light.range = orig.range * weight;
        if (light.type == LightType.Spot)
            light.spotAngle = orig.spotAngle * weight;

        bool shouldEnable = weight > 0.01f;
        if (light.enabled != shouldEnable)
            light.enabled = shouldEnable;

        currentWeights[light] = weight;
    }

    // 动态添加/移除灯光
    public void AddLight(Light light) { /* ... 可参照之前的实现 */ }
    public void RemoveLight(Light light) { /* ... */ }
    public void SetCullDistance(float newCull, float newFadeStart) { /* ... */ }
}