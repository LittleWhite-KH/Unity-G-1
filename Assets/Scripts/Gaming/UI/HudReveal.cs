using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// HUD 整体揭示动画：每个元素从指定方向滑入 + 淡入，支持逐个延迟。
///
/// 对应策划案 Part1 第 4 步"解锁所有 UI 效果（渐入）"——
/// 右侧 UI 从右往左滑入、上方 UI 从上往下滑入，以此类推。
///
/// 用法：
///   reveal.Play();   // 播放揭示动画
///   reveal.Hide();   // 立刻回到隐藏态（可重复播放）
/// </summary>
[DisallowMultipleComponent]
public class HudReveal : MonoBehaviour
{
    public enum From
    {
        None = 0,
        Right = 1,
        Left = 2,
        Top = 3,
        Bottom = 4
    }

    [Serializable]
    public class Element
    {
        [Tooltip("要动的 UI 元素")]
        public RectTransform target;

        [Tooltip("滑入方向：Right = 从屏幕右侧滑入")]
        public From from = From.Right;

        [Tooltip("滑入距离（像素，2560x1440 画布下的绝对值）")]
        public float distance = 260f;

        [Tooltip("相对整体开始的延迟（秒）")]
        public float delay = 0f;

        public float duration = 0.6f;

        public AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [NonSerialized] public Vector2 basePosition;
        [NonSerialized] public CanvasGroup group;
        [NonSerialized] public bool cached;
    }

    [Header("元素列表")]
    [SerializeField] private List<Element> elements = new List<Element>();

    [Header("行为")]
    [Tooltip("Awake 时自动进入隐藏态")]
    [SerializeField] private bool hideOnAwake = true;

    [Tooltip("测试用：进入 Play 就自动播放一次揭示动画（正式流程里由 Phase 脚本调用 Play()）")]
    [SerializeField] private bool playOnStart = false;

    [Tooltip("整体播放速度倍率")]
    [SerializeField] private float speedScale = 1f;

    [SerializeField] private bool logPlayback = false;

    public bool IsPlaying { get; private set; }

    private Coroutine routine;

    private void Awake()
    {
        CacheElements();

        if (hideOnAwake)
        {
            ApplyHidden();
        }
    }

    private void Start()
    {
        if (playOnStart)
        {
            Play();
        }
    }

    /// <summary>缓存初始位置与 CanvasGroup（缺 CanvasGroup 会自动补一个）。</summary>
    public void CacheElements()
    {
        foreach (Element element in elements)
        {
            if (element == null || element.target == null)
            {
                continue;
            }

            if (!element.cached)
            {
                element.basePosition = element.target.anchoredPosition;

                element.group = element.target.GetComponent<CanvasGroup>();
                if (element.group == null)
                {
                    element.group = element.target.gameObject.AddComponent<CanvasGroup>();
                }

                element.cached = true;
            }
        }
    }

    /// <summary>播放揭示动画。</summary>
    public void Play()
    {
        CacheElements();

        if (routine != null)
        {
            StopCoroutine(routine);
        }

        routine = StartCoroutine(PlayRoutine());
    }

    /// <summary>立刻隐藏（同时停止正在播放的动画）。</summary>
    public void Hide()
    {
        CacheElements();

        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        ApplyHidden();
        IsPlaying = false;
    }

    /// <summary>立刻显示到最终状态。</summary>
    public void ShowImmediate()
    {
        CacheElements();

        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        foreach (Element element in elements)
        {
            ApplyProgress(element, 1f);
        }

        IsPlaying = false;
    }

    private void ApplyHidden()
    {
        foreach (Element element in elements)
        {
            ApplyProgress(element, 0f);
        }
    }

    private IEnumerator PlayRoutine()
    {
        IsPlaying = true;

        float speed = Mathf.Max(0.05f, speedScale);
        float total = 0f;

        foreach (Element element in elements)
        {
            if (element == null || element.target == null) continue;
            total = Mathf.Max(total, (element.delay + element.duration) / speed);
        }

        if (logPlayback)
        {
            Debug.Log($"[HudReveal] 开始播放，共 {elements.Count} 个元素，总时长 {total:0.##}s");
        }

        float elapsed = 0f;
        while (elapsed < total)
        {
            elapsed += Time.deltaTime;

            foreach (Element element in elements)
            {
                if (element == null || element.target == null) continue;

                float local = (elapsed - element.delay / speed) / (Mathf.Max(0.01f, element.duration) / speed);
                ApplyProgress(element, Mathf.Clamp01(local));
            }

            yield return null;
        }

        foreach (Element element in elements)
        {
            ApplyProgress(element, 1f);
        }

        IsPlaying = false;
        routine = null;
    }

    private void ApplyProgress(Element element, float progress01)
    {
        if (element == null || element.target == null)
        {
            return;
        }

        if (!element.cached)
        {
            CacheElements();
        }

        float eased = element.curve != null ? element.curve.Evaluate(progress01) : progress01;

        Vector2 direction = DirectionOf(element.from);
        element.target.anchoredPosition = element.basePosition + direction * element.distance * (1f - eased);

        if (element.group != null)
        {
            element.group.alpha = eased;
        }
    }

    private static Vector2 DirectionOf(From from)
    {
        switch (from)
        {
            case From.Right: return new Vector2(1f, 0f);
            case From.Left: return new Vector2(-1f, 0f);
            case From.Top: return new Vector2(0f, 1f);
            case From.Bottom: return new Vector2(0f, -1f);
            default: return Vector2.zero;
        }
    }

#if UNITY_EDITOR
    /// <summary>供编辑器工具批量填充元素。</summary>
    public void EditorSetElements(List<Element> newElements)
    {
        elements = newElements;
    }

    public List<Element> EditorGetElements()
    {
        return elements;
    }
#endif
}
