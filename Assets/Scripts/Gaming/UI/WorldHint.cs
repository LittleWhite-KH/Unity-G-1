using UnityEngine;

/// <summary>
/// 世界空间提示（挂在目标物体上的 World Space Canvas + TMP）：
///   · 始终面向玩家相机（billboard）；
///   · 可以淡入淡出；
///   · 超过 <see cref="visibleDistance"/> 米自动淡出，避免远处就看到提示。
///
/// 用途：充能完成后在墓碑上提示"点击墓碑"（策划案 Part1 第 3 步之后）。
/// </summary>
[DisallowMultipleComponent]
public class WorldHint : MonoBehaviour
{
    [Header("结构")]
    public CanvasGroup group;

    [Tooltip("要朝向的目标，通常拖 Main Camera")]
    public Transform faceTarget;

    [Header("行为")]
    public bool billboard = true;

    [Tooltip("只旋转 Y 轴（保持文字竖直，推荐）")]
    public bool yAxisOnly = true;

    [Tooltip("超过该距离自动淡出")]
    public float visibleDistance = 30f;

    public bool hideBeyondDistance = true;

    public float fadeSpeed = 6f;

    [Tooltip("显示时向上漂浮的幅度（像素/秒，0 = 不漂）")]
    public float floatSpeed = 0f;

    [Tooltip("漂浮幅度（像素）")]
    public float floatAmplitude = 6f;

    public bool IsVisible => wantVisible;

    private bool wantVisible;
    private float alpha;
    private Vector3 baseLocalPosition;

    private void Awake()
    {
        if (group == null) group = GetComponent<CanvasGroup>();

        if (faceTarget == null && Camera.main != null)
        {
            faceTarget = Camera.main.transform;
        }

        baseLocalPosition = transform.localPosition;
        alpha = 0f;
        ApplyAlpha();

        if (group != null)
        {
            group.blocksRaycasts = false;
            group.interactable = false;
        }
    }

    private void LateUpdate()
    {
        if (billboard && faceTarget != null)
        {
            Vector3 direction = transform.position - faceTarget.position;
            if (yAxisOnly) direction.y = 0f;

            if (direction.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            }
        }

        // 漂浮
        if (floatSpeed > 0f || floatAmplitude > 0f)
        {
            float offset = Mathf.Sin(Time.time * (floatSpeed <= 0f ? 1f : floatSpeed)) * floatAmplitude;
            transform.localPosition = baseLocalPosition + Vector3.up * offset;
        }

        // 距离淡出
        bool shouldShow = wantVisible;
        if (shouldShow && hideBeyondDistance && faceTarget != null)
        {
            if (Vector3.Distance(transform.position, faceTarget.position) > visibleDistance)
            {
                shouldShow = false;
            }
        }

        float targetAlpha = shouldShow ? 1f : 0f;
        alpha = fadeSpeed > 0f
            ? Mathf.Lerp(alpha, targetAlpha, 1f - Mathf.Exp(-fadeSpeed * Time.deltaTime))
            : targetAlpha;

        ApplyAlpha();
    }

    public void Show()
    {
        wantVisible = true;
    }

    public void Hide()
    {
        wantVisible = false;
    }

    public void SetVisible(bool value)
    {
        wantVisible = value;
    }

    public void SetText(string text)
    {
        var label = GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
        if (label != null) label.text = text;
    }

    private void ApplyAlpha()
    {
        if (group != null) group.alpha = alpha;
    }
}
