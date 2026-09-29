using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 侧栏导航（四个按钮：MAP / EMAIL / SETTING / LOG）。
///
/// 策划案硬性约束「四个侧栏按钮打开四个界面」+ 素材只有「常态 / 打开态」两套图标，
/// 所以本组件负责：
///   1) **图标两态**：某一项打开时，只把那一行换成 active 图标，其余保持 normal；关闭换回。
///   2) **互斥**：同一时间只允许一个界面打开（打开新的会收起旧的）。
///   3) **点击**：为每个 Item 自动补一个 Button，点开 / 再点关闭。
///
/// 状态检测用**轮询**而不是事件：MapPanel 有按键、关卡脚本、按钮三条打开路径，
/// 轮询能同时覆盖，且不需要改动 MapPanel。
/// </summary>
[DisallowMultipleComponent]
public class SidebarNav : MonoBehaviour
{
    /// <summary>一个侧栏条目：常态图标 / 打开态图标 / 它对应的界面。</summary>
    [System.Serializable]
    public class Entry
    {
        [Tooltip("侧栏行（可点区域，通常挂 Button）")]
        public GameObject item;

        [Tooltip("常态图标 Image（收在 HudSprites 下）")]
        public GameObject iconNormal;

        [Tooltip("打开态图标 Image（收在 HudSpritesActive 下）")]
        public GameObject iconActive;

        [Tooltip("对应的界面根对象（打开 = SetActive(true)）")]
        public GameObject panel;

        [Tooltip("界面上的 MapPanel（可选；有就用它的 Open/Close/IsOpen，兼容开合动画）")]
        public MapPanel mapPanel;

        [Tooltip("本行是否可点（关卡未解锁时可关掉）")]
        public bool interactable = true;

        [HideInInspector] public Button button;

        /// <summary>上一次应用到图标上的开合状态；null = 还没同步过。</summary>
        [HideInInspector] public bool? lastOpen;
    }

    [Header("四个条目（顺序 = 屏幕上从上到下）")]
    [SerializeField] private List<Entry> entries = new List<Entry>();

    [Header("行为")]
    [Tooltip("同一时间只允许一个界面打开")]
    [SerializeField] private bool exclusive = true;

    [Tooltip("点击侧栏行开关界面")]
    [SerializeField] private bool clickToToggle = true;

    [Tooltip("界面对象 activeSelf 变化时的收放时长（仅用于直接 SetActive，不用 MapPanel 时）")]
    [Range(0f, 0.5f)] [SerializeField] private float fadeDuration = 0.12f;

    [SerializeField] private bool logState = false;

    private readonly List<Coroutine> running = new List<Coroutine>();

#if UNITY_EDITOR
    /// <summary>调试用：Update 实际被调用的次数。</summary>
    public int DebugUpdateCount { get; private set; }
#endif

    private void Awake()
    {
        WireButtons();

        // 进入场景先按实际开合状态刷一次，避免首帧闪一下
        SyncIcons();
    }

    private void Update()
    {
#if UNITY_EDITOR
        DebugUpdateCount++;
#endif
        // 走 UI 打开/关闭时在这里立刻同步一次。
        // 轮询在真机上最多差一帧，但编辑器在后台时 Update 会被限流到每秒一次左右，
        // 主动同步能保证"状态已变、图标已换"在任何帧率下都成立。
        SyncIcons();
    }

    private void OnDestroy()
    {
        UnwireButtons();
    }

    // ------------------------------------------------------------------
    // 状态
    // ------------------------------------------------------------------

    /// <summary>该条目对应的界面当前是否打开。</summary>
    public bool IsOpen(Entry entry)
    {
        if (entry == null) return false;

        if (entry.mapPanel != null) return entry.mapPanel.IsOpen;
        return entry.panel != null && entry.panel.activeSelf;
    }

    /// <summary>当前打开的条目下标；没有则 -1。</summary>
    public int OpenIndex()
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (IsOpen(entries[i])) return i;
        }

        return -1;
    }

    /// <summary>按图标应有的状态刷一遍（每帧调用，开销可忽略）。</summary>
    private void SyncIcons()
    {
        for (int i = 0; i < entries.Count; i++)
        {
            Entry e = entries[i];
            if (e == null) continue;

            // 界面没接线的条目不要动它的图标，免得把"未实现"显示成"已打开"
            bool wired = e.mapPanel != null || e.panel != null;
            bool open = wired && IsOpen(e);

            bool iconsMatch = e.iconActive != null && e.iconActive.activeSelf == open
                              && e.iconNormal != null && e.iconNormal.activeSelf == !open;

            // 状态没变、而且图标也确实是对的时候，跳过。
            // 不能只看"状态没变"就跳过：MapPanel.Awake/OnEnable 会先跑一次开合协程，
            // 若在那期间把图标点亮，协程结束后没有新的状态变化，图标会永久停在 active。
            if (e.lastOpen == open && iconsMatch) continue;

            e.lastOpen = open;
            SetActiveIfChanged(e.iconActive, open, e);
            SetActiveIfChanged(e.iconNormal, !open, e);
        }
    }

    /// <summary>
    /// 外部（MapPanel 等）状态变化后主动通知一次。
    /// 编辑器在后台时 Update 会被限流到每秒一次左右，主动通知能让图标立刻跟上。
    /// </summary>
    public void Refresh()
    {
        SyncIcons();
    }

    private void SetActiveIfChanged(GameObject go, bool want, Entry e)
    {
        if (go == null || go.activeSelf == want) return;

        go.SetActive(want);
        if (logState) Debug.Log($"[SidebarNav] {Name(e)} {go.name} → {want}");
    }

    // ------------------------------------------------------------------
    // 点击
    // ------------------------------------------------------------------

    private void WireButtons()
    {
        for (int i = 0; i < entries.Count; i++)
        {
            Entry e = entries[i];
            if (e == null || e.item == null) continue;

            e.button = e.item.GetComponent<Button>();
            if (e.button == null) e.button = e.item.AddComponent<Button>();

            // 让整行都可点：Image 必须接收射线
            Graphic[] graphics = e.item.GetComponentsInChildren<Graphic>(true);
            foreach (Graphic g in graphics)
            {
                if (!g.raycastTarget) g.raycastTarget = true;
            }

            int index = i;
            e.button.onClick.RemoveAllListeners();
            e.button.onClick.AddListener(() => OnClick(index));
        }
    }

    private void UnwireButtons()
    {
        foreach (Entry e in entries)
        {
            if (e != null && e.button != null) e.button.onClick.RemoveAllListeners();
        }
    }

    private void OnClick(int index)
    {
        if (index < 0 || index >= entries.Count) return;

        Entry e = entries[index];
        if (e == null || !e.interactable || !clickToToggle) return;

        if (IsOpen(e)) CloseEntry(e);
        else OpenEntry(e);
    }

    // ------------------------------------------------------------------
    // 对外控制
    // ------------------------------------------------------------------

    /// <summary>打开某条（可被按钮/关卡脚本调用）。</summary>
    public void Open(int index)
    {
        if (index < 0 || index >= entries.Count) return;
        OpenEntry(entries[index]);
    }

    /// <summary>关闭某条。</summary>
    public void Close(int index)
    {
        if (index < 0 || index >= entries.Count) return;
        CloseEntry(entries[index]);
    }

    /// <summary>开关某条。</summary>
    public void Toggle(int index)
    {
        if (index < 0 || index >= entries.Count) return;

        if (IsOpen(entries[index])) CloseEntry(entries[index]);
        else OpenEntry(entries[index]);
    }

    /// <summary>全部关闭。</summary>
    public void CloseAll()
    {
        foreach (Entry e in entries) CloseEntry(e);
    }

    /// <summary>关卡解锁用：控制某一行是否可点。</summary>
    public void SetInteractable(int index, bool value)
    {
        if (index < 0 || index >= entries.Count) return;

        Entry e = entries[index];
        if (e == null) return;

        e.interactable = value;
        if (e.button != null) e.button.interactable = value;
    }

    private void OpenEntry(Entry entry)
    {
        if (entry == null) return;

        if (exclusive)
        {
            foreach (Entry other in entries)
            {
                if (other != null && other != entry && IsOpen(other)) CloseEntry(other);
            }
        }

        SetActive(entry, true);
        SyncIcons();

        if (logState) Debug.Log($"[SidebarNav] 打开 {Name(entry)}");
    }

    private void CloseEntry(Entry entry)
    {
        if (entry == null) return;

        SetActive(entry, false);
        SyncIcons();

        if (logState) Debug.Log($"[SidebarNav] 关闭 {Name(entry)}");
    }

    private void SetActive(Entry entry, bool open)
    {
        // 优先走 MapPanel：它有"闪两下再放大"的开合动画与光标处理
        if (entry.mapPanel != null)
        {
            if (open) entry.mapPanel.Open(false);
            else entry.mapPanel.Close();
            return;
        }

        if (entry.panel == null) return;

        if (open)
        {
            entry.panel.SetActive(true);
            StartFade(entry.panel, 1f);
        }
        else if (entry.panel.activeSelf && entry.panel.activeInHierarchy && fadeDuration > 0f)
        {
            StartFade(entry.panel, 0f, true);
        }
        else
        {
            entry.panel.SetActive(false);
        }
    }

    private void StartFade(GameObject panel, float target, bool deactivateAtEnd = false)
    {
        CanvasGroup group = panel.GetComponent<CanvasGroup>();
        if (group == null) group = panel.AddComponent<CanvasGroup>();

        if (running.Count > 63) running.RemoveAll(c => c == null);

        Coroutine c = StartCoroutine(FadeRoutine(group, panel, target, deactivateAtEnd));
        running.Add(c);
    }

    private IEnumerator FadeRoutine(CanvasGroup group, GameObject panel, float target, bool deactivateAtEnd)
    {
        float from = group.alpha;
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, fadeDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, target, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        group.alpha = target;

        if (deactivateAtEnd && panel != null && target <= 0f)
        {
            panel.SetActive(false);
        }
    }

    private static string Name(Entry e)
    {
        if (e == null) return "(null)";
        if (e.item != null) return e.item.name;
        if (e.panel != null) return e.panel.name;
        return "(unnamed)";
    }

#if UNITY_EDITOR
    public List<Entry> EditorEntries
    {
        get { return entries; }
    }
#endif
}
