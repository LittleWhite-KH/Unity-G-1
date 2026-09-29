using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 关卡总控：按顺序依次执行各个 <see cref="LevelPhaseBase"/>，负责共享引用注入、文案写入与淡入淡出。
///
/// 设计目标：**加关卡不用改这个文件**。阶段列表要么显式指定，要么自动收集子物体上的
/// LevelPhaseBase（按 order 排序，相同则按场景层级顺序）。
///
/// 每个阶段的核心语义是"让玩家看向一个新的目的地"（第一关：看向墓碑），
/// 但基类不限制玩法，任何脚本都能作为一个阶段。
/// </summary>
[DisallowMultipleComponent]
public class LevelDirector : MonoBehaviour
{
    [Header("阶段来源")]
    [Tooltip("显式列表；留空则自动收集子物体")]
    public List<LevelPhaseBase> phases = new List<LevelPhaseBase>();

    /// <summary>
    /// 当前应该引导玩家去的地方：
    ///   1) 当前阶段自己有目标 → 用它；
    ///   2) 否则往后找最近一个带目标的阶段（过完第一关，距离计就会指向第二关的桌子）。
    /// </summary>
    public Transform CurrentObjective
    {
        get
        {
            if (Current != null && Current.ObjectiveTarget != null) return Current.ObjectiveTarget;

            if (phases != null)
            {
                for (int i = CurrentIndex + 1; i < phases.Count; i++)
                {
                    if (phases[i] != null && phases[i].ObjectiveTarget != null) return phases[i].ObjectiveTarget;
                }
            }

            return null;
        }
    }

    [Tooltip("自动收集本物体子级里的所有 LevelPhaseBase")]
    public bool autoCollectChildren = true;

    [Header("共享引用（自动注入给每个阶段）")]
    public SanSystem sanSystem;
    public ThreatSystem threatSystem;
    public SanDisplay sanDisplay;
    public HudReveal hudReveal;
    public Transform player;
    public Camera playerCamera;

    [Header("关卡 UI")]
    public TextMeshProUGUI levelTitleText;
    public TextMeshProUGUI objectiveText;

    [Tooltip("地图面板：开局自动锁定按键，由某阶段的 unlockMapOnEnter 解锁")]
    public MapPanel mapPanel;

    [Header("过渡")]
    [Tooltip("可选：整屏黑幕（有就淡入淡出，没有就跳过）")]
    public CanvasGroup fadeOverlay;

    public float fadeDuration = 0.6f;

    [Header("调试")]
    public bool logTransitions = true;
    public bool autoStart = true;

    [Tooltip("开局锁定并隐藏鼠标指针（地图打开时会自动解锁）")]
    public bool lockCursorOnStart = true;

    public LevelPhaseBase Current { get; private set; }
    public int CurrentIndex { get; private set; } = -1;
    public int PhaseCount => phases.Count;

    private bool running;
    private bool transitioning;

    private void Awake()
    {
        AutoResolveReferences();
        BuildPhaseList();
    }

    private void Start()
    {
        if (autoStart)
        {
            Begin();
        }
    }

    private void Update()
    {
        if (!running || Current == null)
        {
            return;
        }

        Current.OnTick(Time.deltaTime);

        // transitioning 防止"完成标志持续为真"时重复启动切换协程
        if (Current.IsComplete && !transitioning)
        {
            transitioning = true;
            StartCoroutine(GoToNext());
        }
    }

    // ------------------------------------------------------------------
    // 启动 / 流转
    // ------------------------------------------------------------------

    public void Begin()
    {
        if (running)
        {
            return;
        }

        if (phases.Count == 0)
        {
            Debug.LogError("[LevelDirector] 没有配置任何阶段。");
            return;
        }

        running = true;
        transitioning = true;

        // 开局先锁住地图按键
        if (mapPanel != null)
        {
            mapPanel.SetInputEnabled(false);
        }

        // 开局锁定鼠标（FPS 视角不应该看到指针；开地图时 MapPanel 会解锁）
        if (lockCursorOnStart)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        StartCoroutine(GoToIndex(0, withFade: true));
    }

    public void AdvanceNow()
    {
        if (!running || transitioning) return;
        transitioning = true;
        StartCoroutine(GoToNext());
    }

    public void RestartFromBeginning()
    {
        StopAllCoroutines();
        running = false;

        foreach (LevelPhaseBase phase in phases)
        {
            if (phase != null) phase.ResetPhase();
        }

        Begin();
    }

    private IEnumerator GoToNext()
    {
        if (CurrentIndex + 1 >= phases.Count)
        {
            if (logTransitions)
            {
                Debug.Log("[LevelDirector] 所有阶段已完成。");
            }

            running = false;
            transitioning = false;
            yield break;
        }

        yield return GoToIndex(CurrentIndex + 1, withFade: true);
    }

    private IEnumerator GoToIndex(int index, bool withFade)
    {
        if (index < 0 || index >= phases.Count)
        {
            yield break;
        }

        // 退出当前阶段
        if (Current != null)
        {
            Current.OnExit();
            yield return Fade(1f, 0f, withFade);   // 淡出
        }

        CurrentIndex = index;
        Current = phases[index];
        if (logTransitions)
        {
            Debug.Log($"[LevelDirector] 进入阶段 {index + 1}/{phases.Count}：{Current.phaseName}");
        }

        ApplyPhaseUi(Current);
        Current.ResetPhase();
        Current.OnEnter();

        if (Current.unlockMapOnEnter && mapPanel != null)
        {
            mapPanel.SetInputEnabled(true);

            if (logTransitions)
            {
                Debug.Log("[LevelDirector] 地图按键已解锁（M）");
            }
        }

        yield return Fade(0f, 1f, withFade);       // 淡入

        transitioning = false;
    }

    private void ApplyPhaseUi(LevelPhaseBase phase)
    {
        if (levelTitleText != null && !string.IsNullOrEmpty(phase.levelTitle))
        {
            levelTitleText.text = phase.levelTitle;
        }

        if (objectiveText != null && !string.IsNullOrEmpty(phase.objectiveText))
        {
            objectiveText.text = phase.objectiveText;
        }
    }

    private IEnumerator Fade(float from, float to, bool enabled)
    {
        if (!enabled || fadeOverlay == null || fadeDuration <= 0f)
        {
            if (fadeOverlay != null) fadeOverlay.alpha = to;
            yield break;
        }

        fadeOverlay.blocksRaycasts = true;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeOverlay.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }

        fadeOverlay.alpha = to;
        fadeOverlay.blocksRaycasts = to > 0.99f;
    }

    // ------------------------------------------------------------------
    // 配置
    // ------------------------------------------------------------------

    private void BuildPhaseList()
    {
        if (phases.Count > 0 && !autoCollectChildren)
        {
            SortPhases();
            return;
        }

        if (!autoCollectChildren)
        {
            return;
        }

        var found = new List<LevelPhaseBase>(GetComponentsInChildren<LevelPhaseBase>(true));

        // 只收集"激活中"的阶段 —— 这样直接在 Inspector 里关掉某个阶段物体就能跳过它
        found.RemoveAll(p => p == null || !p.gameObject.activeSelf);

        if (found.Count > 0)
        {
            phases = found;
        }

        SortPhases();

        foreach (LevelPhaseBase phase in phases)
        {
            phase.Bind(this);
        }
    }

    private void SortPhases()
    {
        // order 小的在前；order 相同按场景层级顺序（GetSiblingIndex），保证顺序可预测
        phases.Sort((a, b) =>
        {
            if (a == null || b == null) return 0;
            int byOrder = a.order.CompareTo(b.order);
            if (byOrder != 0) return byOrder;
            return a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex());
        });
    }

    private void AutoResolveReferences()
    {
        if (sanSystem == null) sanSystem = FindInScene<SanSystem>();
        if (threatSystem == null) threatSystem = FindInScene<ThreatSystem>();
        if (sanDisplay == null) sanDisplay = FindInScene<SanDisplay>();
        if (hudReveal == null) hudReveal = FindInScene<HudReveal>();

        if (player == null)
        {
            GameObject go = GameObject.Find("Player");
            if (go != null) player = go.transform;
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    private static T FindInScene<T>() where T : Component
    {
        foreach (T item in Resources.FindObjectsOfTypeAll<T>())
        {
            if (item != null && item.gameObject.scene.IsValid())
            {
                return item;
            }
        }

        return null;
    }
}
