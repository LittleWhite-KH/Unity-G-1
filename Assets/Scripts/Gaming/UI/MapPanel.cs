using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 地图 UI 面板（框架版，视觉后补）：
///   · OpenWithFlash() —— "闪两下然后放大"打开（策划案 Part1 第 4 步）
///   · 打开时可以标出"下一个要去的点位"（SetWaypoint）
///   · 按键开关（默认 M），玩家可自行关闭（策划案 Part1 第 6 步）
///   · 玩家标记实时跟随玩家世界坐标与朝向
/// </summary>
[DisallowMultipleComponent]
public class MapPanel : MonoBehaviour
{
    [Header("结构")]
    [Tooltip("整块地图的半透明背板/内容组，用来整体淡入淡出")]
    public CanvasGroup group;

    [Tooltip("做缩放动画的根（收起时缩小到 Closed Scale）")]
    public RectTransform scaleRoot;

    [Tooltip("可选：关闭时隐藏的内容根（要放在 scaleRoot 之下）")]
    public GameObject contentRoot;

    [Header("开合动画")]
    [Range(0.5f, 1f)] public float closedScale = 0.86f;
    public float openDuration = 0.35f;
    public float closeDuration = 0.22f;

    [Tooltip("打开前闪几下（策划案要求 2 下）")]
    public int flashCount = 2;

    public float flashInterval = 0.16f;

    [Tooltip("闪烁时的最低透明度")]
    [Range(0f, 1f)] public float flashMinAlpha = 0.12f;

    public AnimationCurve openCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("键位")]
    public bool toggleWithKey = true;
    public Key toggleKey = Key.M;

    [Tooltip("是否允许按键开合（关卡未解锁地图时关掉它）")]
    public bool inputEnabled = true;

    [Header("地图区域与世界映射")]
    [Tooltip("地图绘制区域；点位标记应作为它的子物体")]
    public RectTransform mapArea;

    [Tooltip("世界坐标的 XZ 范围（左下 / 右上）")]
    public Vector2 worldMin = new Vector2(-40f, -40f);
    public Vector2 worldMax = new Vector2(40f, 40f);

    [Tooltip("超出范围时把标记钳制在边缘")]
    public bool clampToArea = true;

    [Header("玩家标记")]
    public Transform playerTransform;
    public RectTransform playerMarker;
    public bool rotatePlayerMarker = true;

    [Header("鼠标与视角")]
    [Tooltip("打开地图时解锁鼠标并显示光标；关闭时重新锁定")]
    public bool controlCursor = true;

    [Tooltip("打开地图时暂停玩家操控（视角旋转 + 移动，按类型名匹配玩家身上的组件）")]
    public bool pauseLookWhileOpen = true;

    [Tooltip("地图打开时要暂停的组件类型名（默认同时暂停 MouseLook 与 PlayerMovement，即地图里按 WASD 不再移动）")]
    [UnityEngine.Serialization.FormerlySerializedAs("lookComponentNames")]
    public string[] pauseComponentNames = { "MouseLook", "PlayerMovement" };

    [Header("点位标记")]
    public RectTransform waypointMarker;

    [Header("调试")]
    public bool logState = false;

    public bool IsOpen { get; private set; }

    [Header("侧栏联动")]
    [Tooltip("地图开合时联动侧栏图标两态；留空则自动找场景里的 SidebarNav")]
    public SidebarNav sidebarNav;

    private Transform waypointTransform;
    private Coroutine routine;
    private readonly System.Collections.Generic.List<Behaviour> pausedLook = new System.Collections.Generic.List<Behaviour>();

    private void Awake()
    {
        ApplyOpenState(false);

        if (playerTransform == null)
        {
            GameObject player = GameObject.Find("Player");
            if (player != null) playerTransform = player.transform;
        }

        if (sidebarNav == null)
        {
            sidebarNav = FindAnyObjectByType<SidebarNav>();
        }
    }

    /// <summary>通知侧栏刷新图标两态（开合协程一结束就调，保证图标立刻跟上）。</summary>
    private void NotifySidebar()
    {
        if (sidebarNav != null) sidebarNav.Refresh();
    }

    private void Update()
    {
        if (toggleWithKey && inputEnabled && Keyboard.current != null)
        {
            if (Keyboard.current[toggleKey].wasPressedThisFrame)
            {
                Toggle();
            }
        }

        if (IsOpen)
        {
            UpdateMarkers();
        }
    }

    /// <summary>关卡流程控制地图按键是否可用（未解锁时关掉，并顺手收起地图）。</summary>
    public void SetInputEnabled(bool value)
    {
        inputEnabled = value;

        if (!value && IsOpen)
        {
            Close();
        }
    }

    // ------------------------------------------------------------------
    // 开合
    // ------------------------------------------------------------------

    public void OpenWithFlash()
    {
        Open(true);
    }

    public void Open(bool withFlash = false)
    {
        if (IsOpen)
        {
            return;
        }

        IsOpen = true;
        NotifySidebar();

        if (contentRoot != null) contentRoot.SetActive(true);

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(OpenRoutine(withFlash));

        if (logState) Debug.Log("[MapPanel] 打开");
    }

    public void Close()
    {
        if (!IsOpen)
        {
            return;
        }

        IsOpen = false;
        NotifySidebar();

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(CloseRoutine());

        if (logState) Debug.Log("[MapPanel] 关闭");
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open(false);
    }

    private IEnumerator OpenRoutine(bool withFlash)
    {
        if (scaleRoot != null) scaleRoot.localScale = Vector3.one * closedScale;

        // 1) 闪两下
        if (withFlash && flashCount > 0)
        {
            SetAlpha(0f);

            for (int i = 0; i < flashCount; i++)
            {
                SetAlpha(1f);
                yield return new WaitForSeconds(flashInterval);

                SetAlpha(flashMinAlpha);
                yield return new WaitForSeconds(flashInterval);
            }
        }

        // 2) 放大到正常尺寸
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, openDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = openCurve != null ? openCurve.Evaluate(t) : t;

            if (scaleRoot != null)
            {
                scaleRoot.localScale = Vector3.one * Mathf.Lerp(closedScale, 1f, eased);
            }

            SetAlpha(eased);
            yield return null;
        }

        ApplyOpenState(true);
        routine = null;

        // 图标会在开合协程结束后才被刷新；编辑器里 Update 可能被限流，所以主动通知一次
        NotifySidebar();
    }

    private IEnumerator CloseRoutine()
    {
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, closeDuration);
        float fromScale = scaleRoot != null ? scaleRoot.localScale.x : 1f;
        float fromAlpha = group != null ? group.alpha : 1f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (scaleRoot != null)
            {
                scaleRoot.localScale = Vector3.one * Mathf.Lerp(fromScale, closedScale, t);
            }

            SetAlpha(Mathf.Lerp(fromAlpha, 0f, t));
            yield return null;
        }

        ApplyOpenState(false);
        routine = null;

        NotifySidebar();
    }

    private void ApplyOpenState(bool open)
    {
        if (scaleRoot != null) scaleRoot.localScale = Vector3.one * (open ? 1f : closedScale);
        SetAlpha(open ? 1f : 0f);

        if (group != null)
        {
            group.blocksRaycasts = open;
            group.interactable = open;
        }

        if (contentRoot != null) contentRoot.SetActive(open);

        ApplyCursorAndLook(open);
    }

    /// <summary>打开地图时解锁鼠标、暂停视角旋转；关闭时还原。</summary>
    private void ApplyCursorAndLook(bool mapOpen)
    {
        if (controlCursor)
        {
            Cursor.lockState = mapOpen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = mapOpen;

            if (logState)
            {
                Debug.Log($"[MapPanel] 光标 {(mapOpen ? "解锁" : "锁定")}");
            }
        }

        if (!pauseLookWhileOpen || playerTransform == null)
        {
            return;
        }

        if (mapOpen)
        {
            pausedLook.Clear();

            foreach (Behaviour behaviour in playerTransform.GetComponentsInChildren<Behaviour>(true))
            {
                if (behaviour == null || !behaviour.enabled) continue;
                if (MatchesLook(behaviour.GetType().Name))
                {
                    behaviour.enabled = false;
                    pausedLook.Add(behaviour);
                }
            }
        }
        else
        {
            foreach (Behaviour behaviour in pausedLook)
            {
                if (behaviour != null) behaviour.enabled = true;
            }

            pausedLook.Clear();
        }
    }

    private bool MatchesLook(string typeName)
    {
        if (pauseComponentNames == null) return false;

        foreach (string name in pauseComponentNames)
        {
            if (!string.IsNullOrEmpty(name) && name == typeName) return true;
        }

        return false;
    }

    private void SetAlpha(float alpha)
    {
        if (group != null) group.alpha = Mathf.Clamp01(alpha);
    }

    // ------------------------------------------------------------------
    // 点位
    // ------------------------------------------------------------------

    /// <summary>标出下一个要去的点位（世界坐标）。传 null 表示清除。</summary>
    public void SetWaypoint(Transform target)
    {
        waypointTransform = target;

        if (waypointMarker != null)
        {
            waypointMarker.gameObject.SetActive(target != null);
        }

        UpdateMarkers();
    }

    public void ClearWaypoint()
    {
        SetWaypoint(null);
    }

    private void UpdateMarkers()
    {
        if (playerTransform != null && playerMarker != null)
        {
            PlaceMarker(playerMarker, playerTransform.position);
        }

        if (waypointTransform != null && waypointMarker != null)
        {
            PlaceMarker(waypointMarker, waypointTransform.position);
        }
    }

    private void PlaceMarker(RectTransform marker, Vector3 world)
    {
        if (marker == null || mapArea == null)
        {
            return;
        }

        float ux = Mathf.InverseLerp(worldMin.x, worldMax.x, world.x);
        float uy = Mathf.InverseLerp(worldMin.y, worldMax.y, world.z);

        if (clampToArea)
        {
            ux = Mathf.Clamp01(ux);
            uy = Mathf.Clamp01(uy);
        }

        marker.anchorMin = new Vector2(ux, uy);
        marker.anchorMax = new Vector2(ux, uy);
        marker.anchoredPosition = Vector2.zero;

        if (rotatePlayerMarker && marker == playerMarker && playerTransform != null)
        {
            marker.localRotation = Quaternion.Euler(0f, 0f, -playerTransform.eulerAngles.y);
        }
    }

    /// <summary>根据当前场景把世界范围自动撑开（把所有渲染物体的包围盒盖住）。</summary>
    [ContextMenu("Fit World Range To Scene")]
    public void FitWorldRangeToScene()
    {
        Renderer[] renderers = FindObjectsOfType<Renderer>();
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers)
        {
            if (r != null) bounds.Encapsulate(r.bounds);
        }

        worldMin = new Vector2(bounds.min.x, bounds.min.z);
        worldMax = new Vector2(bounds.max.x, bounds.max.z);

        Debug.Log($"[MapPanel] 世界范围已设为 X {worldMin.x:0.#}~{worldMax.x:0.#}，Z {worldMin.y:0.#}~{worldMax.y:0.#}");
    }
}
