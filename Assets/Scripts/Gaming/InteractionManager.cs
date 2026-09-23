using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class InteractionManager : MonoBehaviour
{
    [Header("全局设置")]
    public Camera playerCamera;

    [Header("交互条目列表")]
    public List<InteractionEntry> entries = new List<InteractionEntry>();

    void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        foreach (var entry in entries)
            entry.Initialize();
    }

    void Update()
    {
        if (playerCamera == null) return;

        var mouse = Mouse.current;
        bool leftPressed = mouse != null && mouse.leftButton.wasPressedThisFrame;
        bool leftReleased = mouse != null && mouse.leftButton.wasReleasedThisFrame;

        foreach (var entry in entries)
            entry.UpdateEntry(playerCamera, leftPressed, leftReleased);
    }

    // ---- 外部设置充能完成（通过目标物体） ----
    public void SetChargeComplete(GameObject target, bool complete)
    {
        foreach (var entry in entries)
            if (entry.targetObject == target)
            {
                entry.SetChargeComplete(complete);
                break;
            }
    }

    // ---- 通过索引设置 ----
    public void SetChargeComplete(int index, bool complete)
    {
        if (index >= 0 && index < entries.Count)
            entries[index].SetChargeComplete(complete);
    }
}

// ============================================================
// 每个交互条目的配置与运行时状态（无协程，全状态机驱动）
// ============================================================
[System.Serializable]
public class InteractionEntry
{
    [Header("前提条件")]
    public GameObject targetObject;
    public float distanceThreshold = 5f;
    public float angleThreshold = 10f;

    [Header("第一文字")]
    public TextMeshProUGUI firstText;
    [TextArea(1, 3)] public string firstContent = "可交互";

    [Header("第二文字")]
    public TextMeshProUGUI secondText;
    [TextArea(1, 3)] public string secondContent = "操作中";

    [Header("移动动画")]
    public float moveOffset = 1f;
    public float moveDuration = 0.5f;
    public AnimationCurve moveCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("淡入淡出")]
    public float fadeDuration = 0.3f;
    public AnimationCurve fadeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("充能依赖")]
    public bool requireCharge = false;
    [HideInInspector] public bool chargeComplete = false;

    // ---- 运行时状态 ----
    private RectTransform firstRect;
    private Vector2 originalPos;
    private bool isConditionMet = false;
    private bool isPressing = false;

    // 移动动画状态
    private Vector2 moveStart, moveTarget;
    private float moveProgress = 1f;   // 1 = 完成
    private bool isMoving = false;

    // 淡入淡出状态（独立管理两个文字）
    private class FadeState
    {
        public TextMeshProUGUI text;
        public float startAlpha, targetAlpha;
        public float progress = 1f;
        public bool isFading = false;
        public bool enableOnComplete = false;
        public AnimationCurve curve;
    }
    private FadeState firstFade = new FadeState();
    private FadeState secondFade = new FadeState();

    // ---- 初始化 ----
    public void Initialize()
    {
        if (firstText == null || secondText == null || targetObject == null)
        {
            Debug.LogError("条目配置不完整！");
            return;
        }

        firstRect = firstText.GetComponent<RectTransform>();
        originalPos = firstRect.anchoredPosition;

        // 初始透明禁用
        SetTextAlpha(firstText, 0f);
        firstText.enabled = false;
        SetTextAlpha(secondText, 0f);
        secondText.enabled = false;
        secondText.text = secondContent;

        // 设置淡入淡出状态引用
        firstFade.text = firstText;
        secondFade.text = secondText;
        firstFade.curve = fadeCurve;
        secondFade.curve = fadeCurve;
        firstFade.progress = 1f;
        secondFade.progress = 1f;
    }

    // ---- 外部设置充能状态 ----
    public void SetChargeComplete(bool complete)
    {
        chargeComplete = complete;
        if (!complete && requireCharge)
            ForceHide();
    }

    // ---- 强制隐藏并复位 ----
    private void ForceHide()
    {
        SetTextAlpha(firstText, 0f);
        firstText.enabled = false;
        SetTextAlpha(secondText, 0f);
        secondText.enabled = false;
        firstRect.anchoredPosition = originalPos;
        isPressing = false;
        isMoving = false;
        firstFade.isFading = false;
        secondFade.isFading = false;
    }

    // ---- 每帧更新 ----
    public void UpdateEntry(Camera camera, bool leftPressed, bool leftReleased)
    {
        if (firstText == null || secondText == null || targetObject == null || camera == null)
            return;

        // 充能未完成则强制隐藏并跳过
        if (requireCharge && !chargeComplete)
        {
            ForceHide();
            return;
        }

        // ---- 1. 条件检测 ----
        Vector3 dir = targetObject.transform.position - camera.transform.position;
        float dist = dir.magnitude;
        float angle = Vector3.Angle(camera.transform.forward, dir);
        bool newCondition = (dist <= distanceThreshold) && (angle <= angleThreshold);

        if (newCondition != isConditionMet)
        {
            isConditionMet = newCondition;
            if (isConditionMet)
                StartFade(firstFade, 1f, fadeDuration, true);
            else
            {
                StartFade(firstFade, 0f, fadeDuration, false);
                if (secondText.enabled || secondText.color.a > 0.01f)
                    StartFade(secondFade, 0f, fadeDuration, false);
                // 复位位置
                isMoving = false;
                firstRect.anchoredPosition = originalPos;
                isPressing = false;
            }
        }

        if (!isConditionMet)
            return;

        // ---- 2. 鼠标交互 ----
        if (leftPressed && !isPressing)
        {
            isPressing = true;
            StartMove(originalPos + new Vector2(0, moveOffset));
            StartFade(secondFade, 1f, fadeDuration, true);
        }
        if (leftReleased && isPressing)
        {
            isPressing = false;
            StartMove(originalPos);
            StartFade(secondFade, 0f, fadeDuration, false);
        }

        // ---- 3. 更新动画 ----
        UpdateMove();
        UpdateFade(firstFade);
        UpdateFade(secondFade);
    }

    // ---- 移动动画 ----
    private void StartMove(Vector2 target)
    {
        moveStart = firstRect.anchoredPosition;
        moveTarget = target;
        moveProgress = 0f;
        isMoving = true;
    }

    private void UpdateMove()
    {
        if (!isMoving) return;
        moveProgress += Time.deltaTime / moveDuration;
        if (moveProgress >= 1f)
        {
            moveProgress = 1f;
            isMoving = false;
        }
        float t = moveCurve.Evaluate(moveProgress);
        firstRect.anchoredPosition = Vector2.Lerp(moveStart, moveTarget, t);
    }

    // ---- 淡入淡出动画 ----
    private void StartFade(FadeState state, float targetAlpha, float duration, bool enableOnComplete)
    {
        if (state.text == null) return;
        if (targetAlpha > 0.5f && enableOnComplete)
            state.text.enabled = true;

        state.startAlpha = state.text.color.a;
        state.targetAlpha = targetAlpha;
        state.progress = 0f;
        state.isFading = true;
        state.enableOnComplete = enableOnComplete;
    }

    private void UpdateFade(FadeState state)
    {
        if (!state.isFading || state.text == null) return;
        state.progress += Time.deltaTime / fadeDuration;
        if (state.progress >= 1f)
        {
            state.progress = 1f;
            state.isFading = false;
        }
        float t = fadeCurve.Evaluate(state.progress);
        float alpha = Mathf.Lerp(state.startAlpha, state.targetAlpha, t);
        SetTextAlpha(state.text, alpha);

        if (!state.isFading) // 动画完成
        {
            if (state.targetAlpha < 0.01f)
                state.text.enabled = false;
            else if (state.enableOnComplete && state.targetAlpha > 0.99f)
                state.text.enabled = true;
        }
    }

    // ---- 辅助方法 ----
    private void SetTextAlpha(TextMeshProUGUI text, float alpha)
    {
        Color c = text.color;
        c.a = alpha;
        text.color = c;
    }
}