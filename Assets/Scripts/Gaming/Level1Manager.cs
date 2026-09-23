using UnityEngine;
using TMPro;
using System.Collections;

public class Level1Manager : MonoBehaviour
{
    [System.Serializable]
    public class LevelData
    {
        public string phaseName = "Phase";
        public GameObject[] targets;

        public float sanMin = 0f;
        public float sanMax = 20f;

        public float sliderBaseMin = 60f;
        public float sliderLevelMax = 80f;

        public float bonusStartThreshold = 75f;
        public float bonusFullValue = 80f;

        [Tooltip("充能条宽度达到该值后进入下一关")]
        public float chargeThreshold = 160f;

        public string hintText = "Look at the target";

        [Header("顺序切换设置")]
        public bool enableSequentialTargets = false;
        public float holdTimeToSwitch = 1.0f;

        [Header("San 值角度控制")]
        [Tooltip("玩家视线与目标夹角超过此角度时，San 值降至最低（sanMin）；0°时达到最高（sanMax）")]
        public float sanAngleThreshold = 5f;
    }

    [Header("系统引用")]
    public Transform playerTransform;
    public Transform cameraTransform;
    public ThreeLayerNativeSlider threeLayerSlider;

    [Header("关卡配置")]
    public LevelData[] levels;

    [Header("UI")]
    public CanvasGroup hintCanvasGroup;
    public TextMeshProUGUI hintText;
    public CanvasGroup topRightCanvasGroup;
    public TextMeshProUGUI levelNameText;
    public TextMeshProUGUI progressText;

    [Header("淡入淡出设置")]
    public float fadeDuration = 0.8f;

    private int currentLevelIndex = 0;
    private int currentTargetIndex = 0;
    private float timeLookingAtTarget = 0f;
    private bool isFading = false;
    private Coroutine fadeCoroutine = null;

    void Start()
    {
        if (playerTransform == null) playerTransform = transform;
        if (cameraTransform == null) cameraTransform = Camera.main?.transform;
        if (threeLayerSlider == null) threeLayerSlider = FindObjectOfType<ThreeLayerNativeSlider>();

        ProgressMonitor pm = FindObjectOfType<ProgressMonitor>();
        if (pm != null) pm.enabled = false;

        if (levels == null || levels.Length == 0)
        {
            Debug.LogError("未配置任何关卡！");
            return;
        }

        ApplyLevel(currentLevelIndex, instant: true);
        SetAllAlphas(1f);
        threeLayerSlider?.ResetBonus();
    }

    void Update()
    {
        if (isFading) return;

        // 检测通关
        if (threeLayerSlider != null && currentLevelIndex < levels.Length - 1)
        {
            float currentWidth = threeLayerSlider.BonusWidth;
            float threshold = levels[currentLevelIndex].chargeThreshold;
            if (currentWidth >= threshold)
            {
                Debug.Log($"✅ 关卡 [{levels[currentLevelIndex].phaseName}] 完成");
                if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
                fadeCoroutine = StartCoroutine(SwitchToLevel(currentLevelIndex + 1));
            }
        }

        // 控制 San 值（基于角度）
        if (currentLevelIndex < levels.Length)
        {
            LevelData level = levels[currentLevelIndex];
            if (level.enableSequentialTargets && level.targets != null && level.targets.Length > 1)
            {
                HandleSequentialTargets(level);
            }
            else
            {
                float minAngle = GetMinAngleToTargets(level.targets);
                float targetSan = CalculateSanFromAngle(minAngle, level.sanAngleThreshold, level.sanMin, level.sanMax);
                threeLayerSlider?.SetTargetSan(targetSan, true);
            }
        }
    }

    private void HandleSequentialTargets(LevelData level)
    {
        if (currentTargetIndex >= level.targets.Length)
        {
            threeLayerSlider?.SetTargetSan(level.sanMin, true);
            return;
        }

        GameObject currentTarget = level.targets[currentTargetIndex];
        float angle = GetAngleToSingleTarget(currentTarget);
        float targetSan = CalculateSanFromAngle(angle, level.sanAngleThreshold, level.sanMin, level.sanMax);
        threeLayerSlider?.SetTargetSan(targetSan, true);

        if (angle < 0.5f && threeLayerSlider.CurrentSan >= level.sanMax - 0.01f)
        {
            timeLookingAtTarget += Time.deltaTime;
            if (timeLookingAtTarget >= level.holdTimeToSwitch)
            {
                currentTargetIndex++;
                timeLookingAtTarget = 0f;
                threeLayerSlider?.SetTargetSan(level.sanMin, true);
                if (currentTargetIndex < level.targets.Length)
                {
                    threeLayerSlider.targetObject = level.targets[currentTargetIndex].transform;
                    Debug.Log($"🔄 切换到下一个目标: {level.targets[currentTargetIndex].name}");
                }
                else
                {
                    Debug.Log($"🏁 所有目标已注视完成！");
                }
            }
        }
        else
        {
            timeLookingAtTarget = 0f;
        }
    }

    // ---- 协程切换关卡 ----
    private IEnumerator SwitchToLevel(int newIndex)
    {
        isFading = true;
        float halfDuration = fadeDuration / 2f;

        // 淡出
        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            float alpha = Mathf.Lerp(1f, 0f, EaseInOut(t));
            SetAllAlphas(alpha);
            yield return null;  // 必需，确保是协程
        }
        SetAllAlphas(0f);

        ApplyLevel(newIndex, instant: false);

        // 淡入
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            float alpha = Mathf.Lerp(0f, 1f, EaseInOut(t));
            SetAllAlphas(alpha);
            yield return null;
        }
        SetAllAlphas(1f);

        isFading = false;
        fadeCoroutine = null;
    }

    private void ApplyLevel(int index, bool instant)
    {
        if (index < 0 || index >= levels.Length)
        {
            Debug.LogWarning($"关卡索引 {index} 超出范围，停在当前关");
            return;
        }

        currentLevelIndex = index;
        currentTargetIndex = 0;
        timeLookingAtTarget = 0f;
        LevelData level = levels[index];

        if (threeLayerSlider != null && level.targets != null && level.targets.Length > 0)
        {
            threeLayerSlider.targetObject = level.targets[0].transform;
        }

        threeLayerSlider?.SetLevelParameters(
            level.sliderBaseMin,
            level.sliderLevelMax,
            level.bonusStartThreshold,
            level.bonusFullValue
        );

        threeLayerSlider?.SetSanRange(level.sanMin, level.sanMax, level.sanMin);
        threeLayerSlider?.ResetBonus();

        if (hintText != null) hintText.text = level.hintText;
        if (levelNameText != null) levelNameText.text = level.phaseName;
        if (progressText != null) progressText.text = level.hintText;

        Debug.Log($"🔁 切换到关卡 [{level.phaseName}]，San范围: {level.sanMin}-{level.sanMax}，角度阈值: {level.sanAngleThreshold}°");
    }

    // ---- 角度计算 ----
    private float GetMinAngleToTargets(GameObject[] targets)
    {
        if (targets == null || targets.Length == 0 || cameraTransform == null || playerTransform == null)
            return 180f;

        Vector3 camForward = cameraTransform.forward;
        camForward.y = 0;
        if (camForward.sqrMagnitude < 0.001f) return 180f;
        camForward.Normalize();

        float minAngle = 180f;
        foreach (GameObject target in targets)
        {
            if (target == null) continue;
            Vector3 dirToTarget = target.transform.position - playerTransform.position;
            dirToTarget.y = 0;
            if (dirToTarget.sqrMagnitude < 0.001f) continue;
            dirToTarget.Normalize();

            float angle = Vector3.Angle(camForward, dirToTarget);
            if (angle < minAngle) minAngle = angle;
        }
        return minAngle;
    }

    private float GetAngleToSingleTarget(GameObject target)
    {
        if (target == null || cameraTransform == null || playerTransform == null)
            return 180f;

        Vector3 camForward = cameraTransform.forward;
        camForward.y = 0;
        if (camForward.sqrMagnitude < 0.001f) return 180f;
        camForward.Normalize();

        Vector3 dirToTarget = target.transform.position - playerTransform.position;
        dirToTarget.y = 0;
        if (dirToTarget.sqrMagnitude < 0.001f) return 180f;
        dirToTarget.Normalize();

        return Vector3.Angle(camForward, dirToTarget);
    }

    private float CalculateSanFromAngle(float angle, float threshold, float minSan, float maxSan)
    {
        float t = Mathf.Clamp01(1f - (angle / threshold));
        return Mathf.Lerp(minSan, maxSan, t);
    }

    // ---- 辅助 ----
    private float EaseInOut(float t)
    {
        return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
    }

    private void SetAllAlphas(float alpha)
    {
        if (hintCanvasGroup != null) hintCanvasGroup.alpha = alpha;
        if (topRightCanvasGroup != null) topRightCanvasGroup.alpha = alpha;
    }

    // ---- 对外接口 ----
    public string GetCurrentPhaseName()
    {
        if (currentLevelIndex < levels.Length)
            return levels[currentLevelIndex].phaseName;
        else
            return "Unknown";
    }

    public void ResetLevel()
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        isFading = false;
        currentTargetIndex = 0;
        timeLookingAtTarget = 0f;
        ApplyLevel(0, instant: true);
        SetAllAlphas(1f);
        fadeCoroutine = null;
    }
}