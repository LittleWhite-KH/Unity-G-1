using System;
using System.Collections;
using System.Collections.Generic;
using Outdoor.UI;
using TMPro;
using UnityEngine;

/// <summary>
/// 关卡总控。
///
/// 两条路：
///  1) 数据驱动（推荐）：<see cref="levelDefinition"/> 有值时，按 PhaseDefinition 创建 Runner 执行；
///     新关卡/新物体只需要改资产，不用改本文件。
///  2) 旧版兼容：没有资产时，仍然按场景里的 Phase 组件顺序执行（可随时回滚）。
/// </summary>
[DisallowMultipleComponent]
public class LevelDirector : MonoBehaviour
{
    [Header("阶段来源（旧版：场景 Phase 组件）")]
    [Tooltip("显式列表；留空则自动收集子物体")]
    public List<LevelPhaseBase> phases = new List<LevelPhaseBase>();

    [Tooltip("自动收集本物体子级里的所有 LevelPhaseBase")]
    public bool autoCollectChildren = true;

    [Header("数据驱动（优先）")]
    [Tooltip("有 LevelDefinition 时按资产跑；没有则退回旧版 Phase")]
    public LevelDefinition levelDefinition;

    public bool useDataDriven = true;

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

    [Tooltip("\"OPEN MAP\" 侧栏提示（Reveal 阶段自动显示/收起）")]
    public GameObject mapHintRoot;

    [Header("过渡")]
    public CanvasGroup fadeOverlay;
    public float fadeDuration = 0.6f;

    [Header("调试")]
    public bool logTransitions = true;
    public bool autoStart = true;

    [Tooltip("开局锁定并隐藏鼠标指针（地图打开时会自动解锁）")]
    public bool lockCursorOnStart = true;

    public LevelPhaseBase Current { get; private set; }
    public int CurrentIndex { get; private set; } = -1;
    public int PhaseCount => UseDataDriven ? levelDefinition.phases.Count : phases.Count;
    public bool IsRunning => running;
    public LevelState State => state;

    public bool UseDataDriven =>
        useDataDriven && levelDefinition != null &&
        levelDefinition.phases != null && levelDefinition.phases.Count > 0;

    /// <summary>当前阶段的目标（距离计 / 地图用）。</summary>
    public Transform CurrentObjective
    {
        get
        {
            if (UseDataDriven)
            {
                if (CurrentIndex >= 0 && CurrentIndex < runners.Count)
                {
                    return runners[CurrentIndex].ObjectiveTarget;
                }

                return null;
            }

            if (Current != null && Current.ObjectiveTarget != null)
            {
                return Current.ObjectiveTarget;
            }

            if (phases != null)
            {
                for (int i = CurrentIndex + 1; i < phases.Count; i++)
                {
                    if (phases[i] != null && phases[i].ObjectiveTarget != null)
                    {
                        return phases[i].ObjectiveTarget;
                    }
                }
            }

            return null;
        }
    }

    /// <summary>阶段进入事件（数据驱动时带 PhaseDefinition）。</summary>
    public event Action<PhaseDefinition> PhaseEntered;

    /// <summary>整关完成事件。</summary>
    public event Action<LevelDefinition> LevelCompleted;

    private readonly List<IPhaseRunner> runners = new List<IPhaseRunner>();
    private LevelContext context;
    private LevelState state;
    private bool running;
    private bool transitioning;
    private string baseObjective = "";

    private void Awake()
    {
        AutoResolveReferences();

        if (UseDataDriven)
        {
            BuildDataDriven();
        }
        else
        {
            BuildPhaseList();
        }
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
        if (!running)
        {
            return;
        }

        if (UseDataDriven)
        {
            if (CurrentIndex < 0 || CurrentIndex >= runners.Count)
            {
                return;
            }

            PhaseDefinition def = levelDefinition.phases[CurrentIndex];
            IPhaseRunner runner = runners[CurrentIndex];

            if (!runner.IsComplete)
            {
                runner.Tick(def, context, Time.deltaTime);
            }

            if (runner.IsComplete && !transitioning)
            {
                transitioning = true;
                StartCoroutine(GoToNextData());
            }

            return;
        }

        if (Current == null)
        {
            return;
        }

        Current.OnTick(Time.deltaTime);

        if (Current.IsComplete && !transitioning)
        {
            transitioning = true;
            StartCoroutine(GoToNext());
        }
    }

    // ------------------------------------------------------------------
    // 启动 / 流转（数据驱动）
    // ------------------------------------------------------------------

    public void Begin()
    {
        if (running)
        {
            return;
        }

        if (UseDataDriven)
        {
            if (context == null || runners.Count != levelDefinition.phases.Count)
            {
                BuildDataDriven();
            }

            if (runners.Count == 0)
            {
                Debug.LogError("[Level] LevelDefinition 没有可执行的阶段。");
                return;
            }

            running = true;
            transitioning = true;

            if (mapPanel != null)
            {
                mapPanel.SetInputEnabled(false);
            }

            if (lockCursorOnStart)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            PrepareRun();
            StartCoroutine(GoToIndexData(0, true));
            return;
        }

        // ---- 旧版 ----
        if (phases.Count == 0)
        {
            Debug.LogError("[Level] 没有配置任何阶段。");
            return;
        }

        running = true;
        transitioning = true;

        if (mapPanel != null)
        {
            mapPanel.SetInputEnabled(false);
        }

        if (lockCursorOnStart)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        StartCoroutine(GoToIndex(0, true));
    }

    public void AdvanceNow()
    {
        if (!running || transitioning)
        {
            return;
        }

        transitioning = true;

        if (UseDataDriven)
        {
            StartCoroutine(GoToNextData());
        }
        else
        {
            StartCoroutine(GoToNext());
        }
    }

    public void JumpTo(string phaseId)
    {
        if (!UseDataDriven || string.IsNullOrEmpty(phaseId))
        {
            return;
        }

        int index = levelDefinition.phases.FindIndex(p => p != null && p.id == phaseId);
        if (index < 0)
        {
            Debug.LogWarning($"[Level] JumpTo: 找不到阶段 {phaseId}");
            return;
        }

        StopAllCoroutines();

        if (CurrentIndex >= 0 && CurrentIndex < runners.Count)
        {
            runners[CurrentIndex].Exit(levelDefinition.phases[CurrentIndex], context);
        }

        CurrentIndex = -1;
        running = true;
        transitioning = true;
        StartCoroutine(GoToIndexData(index, false));
    }

    public void RestartFromBeginning()
    {
        StopAllCoroutines();
        running = false;
        transitioning = false;
        CurrentIndex = -1;

        foreach (IPhaseRunner runner in runners)
        {
            runner.Reset();
        }

        foreach (LevelPhaseBase phase in phases)
        {
            if (phase != null)
            {
                phase.ResetPhase();
            }
        }

        if (state != null)
        {
            state.Reset();
        }

        Begin();
    }

    private void PrepareRun()
    {
        if (state == null)
        {
            state = new LevelState();
        }

        state.Reset();

        foreach (IPhaseRunner runner in runners)
        {
            runner.Reset();
        }

        if (context != null && context.Charge != null)
        {
            context.Charge.ResetCharge();
            context.Charge.guidanceOnly = false;
            context.Charge.target = null;
            context.Charge.enabled = false;
        }

        if (context != null && context.Map != null)
        {
            context.Map.ClearWaypoint();
        }
    }

    private IEnumerator GoToNextData()
    {
        int next = CurrentIndex + 1;

        if (next >= runners.Count)
        {
            running = false;
            transitioning = false;

            if (logTransitions)
            {
                Debug.Log("[Level] 所有阶段已完成");
            }

            LevelCompleted?.Invoke(levelDefinition);
            yield break;
        }

        yield return GoToIndexData(next, true);
    }

    private IEnumerator GoToIndexData(int index, bool withFade)
    {
        if (index < 0 || index >= runners.Count)
        {
            yield break;
        }

        if (CurrentIndex >= 0 && CurrentIndex < runners.Count)
        {
            runners[CurrentIndex].Exit(levelDefinition.phases[CurrentIndex], context);
            yield return Fade(0f, 1f, withFade); // 淡出到黑
        }

        CurrentIndex = index;
        PhaseDefinition def = levelDefinition.phases[index];
        ApplyPhaseUi(def);

        IPhaseRunner runner = runners[index];
        runner.Enter(def, context);

        if (mapPanel != null && def.unlockMapOnEnter)
        {
            mapPanel.SetInputEnabled(true);
        }

        if (logTransitions)
        {
            Debug.Log($"[Level] 进入阶段 {index + 1}/{runners.Count}：{def.id} ({def.kind})");
        }

        PhaseEntered?.Invoke(def);

        yield return Fade(1f, 0f, withFade); // 从黑淡入
        transitioning = false;
    }

    // ------------------------------------------------------------------
    // 旧版流转（保留兼容）
    // ------------------------------------------------------------------

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

        if (Current != null)
        {
            Current.OnExit();
            yield return Fade(1f, 0f, withFade);
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
        }

        yield return Fade(0f, 1f, withFade);
        transitioning = false;
    }

    // ------------------------------------------------------------------
    // 配置
    // ------------------------------------------------------------------

    private void BuildDataDriven()
    {
        runners.Clear();

        if (state == null)
        {
            state = new LevelState();
        }

        context = new LevelContext
        {
            Director = this,
            San = sanSystem,
            SanDisplay = sanDisplay,
            Threat = threatSystem,
            Hud = hudReveal,
            Player = player,
            PlayerCamera = playerCamera,
            Charge = ResolveChargeSystem(),
            Map = mapPanel != null ? mapPanel : FindFirstObjectByType<MapPanel>(),
            Graph = FindFirstObjectByType<MapGraph>(),
            State = state,
            MapHintRoot = ResolveMapHintRoot(),
            FadeOverlay = fadeOverlay
        };

        if (mapPanel == null)
        {
            mapPanel = context.Map;
        }

        if (levelDefinition == null || levelDefinition.phases == null)
        {
            return;
        }

        foreach (PhaseDefinition def in levelDefinition.phases)
        {
            if (def == null)
            {
                continue;
            }

            runners.Add(PhaseRunnerFactory.Create(def.kind));
        }

        if (logTransitions)
        {
            Debug.Log($"[Level] 数据驱动关卡 {levelDefinition.levelId}，共 {runners.Count} 个阶段");
        }
    }

    private void BuildPhaseList()
    {
        if (phases.Count > 0 && !autoCollectChildren)
        {
            SortPhases();

            foreach (LevelPhaseBase phase in phases)
            {
                if (phase != null)
                {
                    phase.Bind(this);
                }
            }

            return;
        }

        if (!autoCollectChildren)
        {
            return;
        }

        var found = new List<LevelPhaseBase>(GetComponentsInChildren<LevelPhaseBase>(true));

        // 只收"激活中"的阶段：关掉某个阶段物体就能跳过它。
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
        phases.Sort((a, b) =>
        {
            if (a == null || b == null)
            {
                return 0;
            }

            int byOrder = a.order.CompareTo(b.order);
            if (byOrder != 0)
            {
                return byOrder;
            }

            return a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex());
        });
    }

    private void ApplyPhaseUi(LevelPhaseBase phase)
    {
        if (phase == null)
        {
            return;
        }

        if (levelTitleText != null && !string.IsNullOrEmpty(phase.levelTitle))
        {
            levelTitleText.text = phase.levelTitle;
        }

        baseObjective = phase.objectiveText;

        if (objectiveText != null && !string.IsNullOrEmpty(phase.objectiveText))
        {
            objectiveText.text = phase.objectiveText;
        }
    }

    private void ApplyPhaseUi(PhaseDefinition def)
    {
        if (def == null)
        {
            return;
        }

        if (levelTitleText != null && !string.IsNullOrEmpty(def.levelTitle))
        {
            levelTitleText.text = def.levelTitle;
        }

        baseObjective = def.objective;

        if (objectiveText != null)
        {
            objectiveText.text = def.objective;
        }
    }

    /// <summary>Runner 可以临时改目标文案（例如 "PRESS E — TAKE KEY"）。</summary>
    public void SetObjective(string text)
    {
        if (objectiveText != null && !string.IsNullOrEmpty(text))
        {
            objectiveText.text = text;
        }
    }

    public void RestoreObjective()
    {
        if (objectiveText != null)
        {
            objectiveText.text = baseObjective;
        }
    }

    private ChargeSystem ResolveChargeSystem()
    {
        ChargeSystem cs = GetComponentInChildren<ChargeSystem>(true);
        if (cs == null)
        {
            cs = FindFirstObjectByType<ChargeSystem>();
        }

        return cs;
    }

    private GameObject ResolveMapHintRoot()
    {
        if (mapHintRoot != null)
        {
            return mapHintRoot;
        }

        PhaseHudReveal[] reveals = GetComponentsInChildren<PhaseHudReveal>(true);
        foreach (PhaseHudReveal reveal in reveals)
        {
            if (reveal != null && reveal.mapHintRoot != null)
            {
                mapHintRoot = reveal.mapHintRoot;
                return mapHintRoot;
            }
        }

        GameObject found = GameObject.Find("HUD Canvas/HudRoot/Sidebar/Item_MAP/MapKeyHint");
        if (found != null)
        {
            mapHintRoot = found;
        }

        return mapHintRoot;
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

        if (mapPanel == null)
        {
            mapPanel = FindFirstObjectByType<MapPanel>();
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

    private IEnumerator Fade(float from, float to, bool enabled)
    {
        if (!enabled || fadeOverlay == null || fadeDuration <= 0f)
        {
            if (fadeOverlay != null)
            {
                fadeOverlay.alpha = to;
            }

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
}
