using UnityEngine;
using TMPro;
using System.Collections;

public class Level1Manager : MonoBehaviour
{
    [System.Serializable]
    public class LevelData
    {
        public string phaseName = "Phase";
        public GameObject[] targets;               // 本关需要注视的目标

        // San 值范围
        public float sanMin = 0f;
        public float sanMax = 20f;

        // Slider 显示范围（baseMin ~ levelMax）
        public float sliderBaseMin = 60f;
        public float sliderLevelMax = 80f;

        // 充能触发阈值和充满值
        public float bonusStartThreshold = 75f;
        public float bonusFullValue = 80f;

        [Tooltip("充能条宽度达到该值后进入下一关")]
        public float chargeThreshold = 160f;

        public string hintText = "Look at the target";

        [Header("顺序切换设置（仅当 targets 有多个时生效）")]
        public bool enableSequentialTargets = false;
        [Tooltip("注视当前目标达到 San 最大值后，保持此秒数才切换到下一个目标")]
        public float holdTimeToSwitch = 1.0f;
    }

    [Header("系统引用")]
    public Transform playerTransform;
    public Transform cameraTransform;
    public ThreeLayerNativeSlider threeLayerSlider;   // 现在负责视角 + San 值

    [Header("关卡配置（按顺序）")]
    public LevelData[] levels;

    [Header("中央提示UI")]
    public CanvasGroup hintCanvasGroup;
    public TextMeshProUGUI hintText;

    [Header("右上角UI")]
    public CanvasGroup topRightCanvasGroup;
    public TextMeshProUGUI levelNameText;
    public TextMeshProUGUI progressText;

    [Header("视野角度阈值")]
    public float viewAngleThreshold = 15f;

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

        // 检测当前关卡是否完成（充能宽度达到阈值）
        if (threeLayerSlider != null && currentLevelIndex < levels.Length - 1)
        {
            float currentWidth = threeLayerSlider.BonusWidth;
            float threshold = levels[currentLevelIndex].chargeThreshold;
            if (currentWidth >= threshold)
            {
                Debug.Log($"✅ 关卡 [{levels[currentLevelIndex].phaseName}] 充能达到 {currentWidth:F1} / {threshold}，进入下一关！");
                if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
                fadeCoroutine = StartCoroutine(SwitchToLevel(currentLevelIndex + 1));
            }
        }

        // 控制 San 值
        if (currentLevelIndex < levels.Length)
        {
            LevelData level = levels[currentLevelIndex];
            if (level.enableSequentialTargets && level.targets != null && level.targets.Length > 1)
            {
                HandleSequentialTargets(level);
            }
            else
            {
                bool lookingAtTarget = CheckAnyTargetInSight(level.targets);
                float targetSan = lookingAtTarget ? level.sanMax : level.sanMin;
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
        bool looking = CheckSpecificTargetInSight(currentTarget);

        if (looking)
        {
            threeLayerSlider?.SetTargetSan(level.sanMax, true);
            float currentSan = threeLayerSlider != null ? threeLayerSlider.CurrentSan : 0f;
            if (currentSan >= level.sanMax - 0.01f)
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
        else
        {
            threeLayerSlider?.SetTargetSan(level.sanMin, true);
            timeLookingAtTarget = 0f;
        }
    }

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
            yield return null;
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

        // 1. 设置目标
        if (threeLayerSlider != null && level.targets != null && level.targets.Length > 0)
        {
            threeLayerSlider.targetObject = level.targets[0].transform;
        }

        // 2. 更新 ThreeLayerNativeSlider 的参数（视角范围、充能阈值）
        if (threeLayerSlider != null)
        {
            threeLayerSlider.SetLevelParameters(
                level.sliderBaseMin,
                level.sliderLevelMax,
                level.bonusStartThreshold,
                level.bonusFullValue
            );
        }

        // 3. 更新 San 值范围（通过 ThreeLayerNativeSlider）
        if (threeLayerSlider != null)
        {
            threeLayerSlider.SetSanRange(level.sanMin, level.sanMax, level.sanMin);
        }

        // 4. 重置充能条
        threeLayerSlider?.ResetBonus();

        // 5. 更新 UI 文字
        if (hintText != null) hintText.text = level.hintText;
        if (levelNameText != null) levelNameText.text = level.phaseName;
        if (progressText != null) progressText.text = level.hintText;

        Debug.Log($"🔁 切换到关卡 [{level.phaseName}]，San范围: {level.sanMin}-{level.sanMax}，通关宽度: {level.chargeThreshold}");
        if (level.enableSequentialTargets && level.targets.Length > 1)
            Debug.Log($"🔀 启用顺序切换，共 {level.targets.Length} 个目标，保持时间 {level.holdTimeToSwitch} 秒");
    }

    private bool CheckAnyTargetInSight(GameObject[] targets)
    {
        if (targets == null || targets.Length == 0 || cameraTransform == null) return false;

        Vector3 camForward = cameraTransform.forward;
        camForward.y = 0;
        if (camForward.sqrMagnitude < 0.001f) return false;
        camForward.Normalize();

        foreach (GameObject target in targets)
        {
            if (target == null) continue;
            if (CheckSpecificTargetInSight(target))
                return true;
        }
        return false;
    }

    private bool CheckSpecificTargetInSight(GameObject target)
    {
        if (target == null || cameraTransform == null || playerTransform == null)
            return false;

        Vector3 camForward = cameraTransform.forward;
        camForward.y = 0;
        if (camForward.sqrMagnitude < 0.001f) return false;
        camForward.Normalize();

        Vector3 dirToTarget = target.transform.position - playerTransform.position;
        dirToTarget.y = 0;
        if (dirToTarget.sqrMagnitude < 0.001f) return false;
        dirToTarget.Normalize();

        float angle = Vector3.Angle(camForward, dirToTarget);
        return angle <= viewAngleThreshold;
    }

    private float EaseInOut(float t)
    {
        return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
    }

    private void SetAllAlphas(float alpha)
    {
        if (hintCanvasGroup != null) hintCanvasGroup.alpha = alpha;
        if (topRightCanvasGroup != null) topRightCanvasGroup.alpha = alpha;
    }

    // 对外接口
    public string GetCurrentPhaseName()
    {
        return currentLevelIndex < levels.Length ? levels[currentLevelIndex].phaseName : "Unknown";
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