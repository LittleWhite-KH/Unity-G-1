using UnityEngine;
using TMPro;
using System.Collections;

public class Level1Manager : MonoBehaviour
{
    [Header("系统引用")]
    public SanSystem sanSystem;
    public Transform playerTransform;
    public Transform cameraTransform;
    public ThreeLayerNativeSlider threeLayerSlider;

    [Header("目标对象")]
    public GameObject[] tombTargets;   // 第一阶段目标（墓碑）
    public GameObject[] tableTargets;  // 第二阶段目标（桌台）

    [Header("中央提示UI")]
    public CanvasGroup hintCanvasGroup;
    public TextMeshProUGUI hintText;

    [Header("右上角UI（必须挂载CanvasGroup）")]
    public CanvasGroup topRightCanvasGroup;
    public TextMeshProUGUI levelNameText;
    public TextMeshProUGUI progressText;

    [Header("San值参数")]
    public float sanDefault = 60f;
    public float sanLookTarget = 80f;

    [Header("视野角度阈值")]
    public float viewAngleThreshold = 15f;

    [Header("切换阈值（像素宽度）")]
    public float chargeWidthThreshold = 640f;

    [Header("淡入淡出设置")]
    public float fadeDuration = 0.8f;

    private enum Phase { Tomb, Table }
    private Phase currentPhase = Phase.Tomb;

    private bool isFading = false;
    private bool hasSwitchedToTable = false;
    private Coroutine fadeCoroutine = null;

    // 当前阶段的主目标（用于充能检测）
    private Transform currentMainTarget;

    void Start()
    {
        // 自动获取引用
        if (sanSystem == null) sanSystem = FindObjectOfType<SanSystem>();
        if (playerTransform == null) playerTransform = transform;
        if (cameraTransform == null) cameraTransform = Camera.main?.transform;
        if (threeLayerSlider == null) threeLayerSlider = FindObjectOfType<ThreeLayerNativeSlider>();

        ProgressMonitor pm = FindObjectOfType<ProgressMonitor>();
        if (pm != null) pm.enabled = false;

        // 检查引用
        if (hintCanvasGroup == null)
            Debug.LogWarning("hintCanvasGroup 未绑定");
        if (topRightCanvasGroup == null)
            Debug.LogWarning("topRightCanvasGroup 未绑定！右上角淡入淡出将无效。");
        if (levelNameText == null || progressText == null)
            Debug.LogWarning("右上角文本未完全绑定");

        sanSystem.SetTargetSan(sanDefault, true);

        // 设置初始目标（墓碑）
        UpdateCurrentTarget(Phase.Tomb);

        // 初始化UI
        if (hintText != null) hintText.text = "Look at the Tomb";
        if (levelNameText != null) levelNameText.text = "Tomb";
        if (progressText != null) progressText.text = "Look at the Tomb";
        SetAllAlphas(1f);

        // 重置充能
        if (threeLayerSlider != null) threeLayerSlider.ResetBonus();
    }

    void Update()
    {
        // 仅在Tomb阶段且未切换且未淡入淡出中检测切换
        if (currentPhase == Phase.Tomb && !hasSwitchedToTable && !isFading)
        {
            if (threeLayerSlider != null && threeLayerSlider.BonusWidth >= chargeWidthThreshold)
            {
                Debug.Log($"✅ 充能条宽度达到 {threeLayerSlider.BonusWidth:F1}，触发切换！");
                if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
                fadeCoroutine = StartCoroutine(SwitchPhaseWithFade(Phase.Table, "Find the table"));
                hasSwitchedToTable = true;
            }
        }

        // 阶段San值控制
        switch (currentPhase)
        {
            case Phase.Tomb:
                HandleTombPhase();
                break;
            case Phase.Table:
                HandleTablePhase();
                break;
        }
    }

    #region 阶段逻辑

    private void HandleTombPhase()
    {
        bool lookingAtTomb = CheckAnyTargetInSight(tombTargets);
        float targetSan = lookingAtTomb ? sanLookTarget : sanDefault;
        sanSystem.SetTargetSan(targetSan, true);
    }

    private void HandleTablePhase()
    {
        bool lookingAtTable = CheckAnyTargetInSight(tableTargets);
        float targetSan = lookingAtTable ? sanLookTarget : sanDefault;
        sanSystem.SetTargetSan(targetSan, true);
    }

    #endregion

    #region 辅助方法

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
            Vector3 dirToTarget = target.transform.position - playerTransform.position;
            dirToTarget.y = 0;
            if (dirToTarget.sqrMagnitude < 0.001f) continue;
            dirToTarget.Normalize();

            float angle = Vector3.Angle(camForward, dirToTarget);
            if (angle <= viewAngleThreshold) return true;
        }
        return false;
    }

    /// <summary>
    /// 更新当前目标（供ThreeLayerSlider使用）
    /// </summary>
    private void UpdateCurrentTarget(Phase phase)
    {
        if (threeLayerSlider == null) return;

        GameObject targetObj = null;
        if (phase == Phase.Tomb && tombTargets != null && tombTargets.Length > 0)
            targetObj = tombTargets[0];
        else if (phase == Phase.Table && tableTargets != null && tableTargets.Length > 0)
            targetObj = tableTargets[0];

        if (targetObj != null)
        {
            threeLayerSlider.targetObject = targetObj.transform;
            currentMainTarget = targetObj.transform;
        }
        else
        {
            Debug.LogWarning($"阶段 {phase} 没有可用的目标对象！");
        }
    }

    /// <summary>
    /// 带缓出缓入的切换协程
    /// </summary>
    private IEnumerator SwitchPhaseWithFade(Phase newPhase, string newHint)
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

        // 更新阶段、文字、目标、重置充能
        currentPhase = newPhase;
        UpdateCurrentTarget(newPhase);          // 切换目标对象

        if (hintText != null) hintText.text = newHint;
        if (levelNameText != null) levelNameText.text = GetCurrentPhaseName();
        if (progressText != null) progressText.text = newHint;

        // 重置充能条（降至0）
        if (threeLayerSlider != null)
            threeLayerSlider.ResetBonus();

        Debug.Log($"🔄 已切换到阶段: {newPhase}，提示: {newHint}，目标已更新，充能重置");

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

    private float EaseInOut(float t)
    {
        return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
    }

    private void SetAllAlphas(float alpha)
    {
        if (hintCanvasGroup != null) hintCanvasGroup.alpha = alpha;
        if (topRightCanvasGroup != null) topRightCanvasGroup.alpha = alpha;
    }

    #endregion

    #region 对外接口

    public string GetCurrentPhaseName()
    {
        return currentPhase == Phase.Tomb ? "Tomb" : "Table";
    }

    public string GetCurrentHint()
    {
        return hintText != null ? hintText.text : "";
    }

    public void ResetLevel()
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        currentPhase = Phase.Tomb;
        hasSwitchedToTable = false;
        isFading = false;
        UpdateCurrentTarget(Phase.Tomb);
        sanSystem.SetTargetSan(sanDefault, true);
        if (hintText != null) hintText.text = "Look at the Tomb";
        if (levelNameText != null) levelNameText.text = "Tomb";
        if (progressText != null) progressText.text = "Look at the Tomb";
        if (threeLayerSlider != null) threeLayerSlider.ResetBonus();
        SetAllAlphas(1f);
        fadeCoroutine = null;
    }

    #endregion
}