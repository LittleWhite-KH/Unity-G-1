using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Photo-mode style UI switch: press U to fade out every on-screen UI layer
/// (HUD, SAN panel, map, settings, world-space guide text) so the scene can be captured clean.
/// Press U again to bring everything back. Gameplay (movement / look / interaction) keeps working.
/// </summary>
[DisallowMultipleComponent]
public class HudVisibilityToggle : MonoBehaviour
{
    [Header("Key")]
    public Key toggleKey = Key.U;

    [Header("Animation")]
    [Tooltip("淡入淡出时长（秒）")]
    public float duration = 0.22f;

    [Tooltip("HUD 层隐藏时向上滑动的像素")]
    public float slideDistance = 16f;

    [Header("Targets")]
    [Tooltip("同时隐藏世界空间 UI（墙面指引文字、世界空间 Canvas）")]
    public bool includeWorldSpace = true;

    private class CanvasEntry
    {
        public Canvas canvas;
        public CanvasGroup group;
        public bool addedGroup;
        public RectTransform rect;
        public Vector2 home;
        public bool slide;
        public float homeAlpha = 1f;
        public bool homeInteractable = true;
        public bool homeBlocksRaycasts = true;
    }

    private class TextEntry
    {
        public TMP_Text text;
        public float homeAlpha = 1f;
    }

    private readonly List<CanvasEntry> canvases = new List<CanvasEntry>();
    private readonly List<TextEntry> texts = new List<TextEntry>();

    private TvPortal portal;
    private MapPanel map;
    private SettingsPanel settings;
    private CanvasGroup fade;

    private bool hidden;
    private bool collected;
    private float applied = 1f;
    private Coroutine routine;

    private bool mapInputHome = true;
    private bool settingsEnabledHome = true;

    public bool IsHidden => hidden;
    public bool IsReady => collected;

    private void Awake()
    {
        portal = FindAnyObjectByType<TvPortal>();
        map = FindAnyObjectByType<MapPanel>();
        settings = FindAnyObjectByType<SettingsPanel>();

        GameObject fadeGo = GameObject.Find("LevelFade");
        if (fadeGo != null) fade = fadeGo.GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        Prepare();
    }

    /// <summary>收集所有要隐藏的 UI（编辑器预览也会调用）。</summary>
    public void Prepare()
    {
        if (collected) return;
        collected = true;

        canvases.Clear();
        texts.Clear();

        foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (canvas == null) continue;

            string n = canvas.name;
            if (n == "LevelFade") continue;
            if (n.StartsWith("TV_")) continue;
            if (!includeWorldSpace && canvas.renderMode == RenderMode.WorldSpace) continue;

            CanvasGroup group = canvas.GetComponent<CanvasGroup>();
            bool added = false;
            if (group == null)
            {
                group = canvas.gameObject.AddComponent<CanvasGroup>();
                added = true;
            }

            bool slide = canvas.renderMode != RenderMode.WorldSpace;
            RectTransform rect = canvas.transform as RectTransform;

            canvases.Add(new CanvasEntry
            {
                canvas = canvas,
                group = group,
                addedGroup = added,
                rect = rect,
                home = rect != null ? rect.anchoredPosition : Vector2.zero,
                slide = slide,
                homeAlpha = group.alpha,
                homeInteractable = group.interactable,
                homeBlocksRaycasts = group.blocksRaycasts
            });
        }

        // World-space TMP texts that are not inside any canvas (wall guide labels).
        foreach (TMP_Text tmp in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (tmp == null) continue;
            if (tmp.GetComponentInParent<Canvas>(true) != null) continue;

            texts.Add(new TextEntry { text = tmp, homeAlpha = tmp.alpha });
        }

        applied = 1f;

        Debug.Log($"[HudVisibilityToggle] collected {canvases.Count} canvases, {texts.Count} world texts");
    }

    private void Update()
    {
        if (!collected || Keyboard.current == null) return;
        if (!Keyboard.current[toggleKey].wasPressedThisFrame) return;
        if (!CanToggle()) return;

        Debug.Log("[HudVisibilityToggle] U pressed -> hidden=" + (!hidden));
        SetHidden(!hidden);
    }

    private bool CanToggle()
    {
        // Never re-show the HUD once the player is in the white ending world.
        if (portal != null && portal.HasEntered) return false;
        // Skip while a full-screen fade is running.
        if (fade != null && fade.alpha > 0.05f) return false;
        return true;
    }

    /// <summary>切换（animate = false 用于编辑器预览）。</summary>
    public void SetHidden(bool value, bool animate = true)
    {
        if (hidden == value) return;
        hidden = value;
        Debug.Log($"[HudVisibilityToggle] SetHidden({value})");

        if (hidden)
        {
            if (map != null)
            {
                mapInputHome = map.inputEnabled;
                if (map.IsOpen) map.CloseImmediate();
                map.inputEnabled = false;
            }
            if (settings != null)
            {
                settingsEnabledHome = settings.enabled;
                settings.SetOpen(false);
                settings.enabled = false;
            }
        }
        else
        {
            if (map != null) map.inputEnabled = mapInputHome;
            if (settings != null)
            {
                settings.SetOpen(false);
                settings.enabled = settingsEnabledHome;
            }
        }

        float target = hidden ? 0f : 1f;

        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        if (!animate || !isActiveAndEnabled || duration <= 0.001f)
        {
            Apply(target);
            return;
        }

        routine = StartCoroutine(FadeTo(target));
    }

    /// <summary>编辑器预览：只做视觉应用，不触碰运行时面板逻辑。</summary>
    public void EditorPreview(bool hide)
    {
        Prepare();
        hidden = hide;
        Apply(hide ? 0f : 1f);
    }

    private IEnumerator FadeTo(float target)
    {
        float start = applied;
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            k = k * k * (3f - 2f * k);
            Apply(Mathf.Lerp(start, target, k));
            yield return null;
        }

        Apply(target);
        routine = null;
    }

    private void Apply(float k)
    {
        applied = k;

        for (int i = 0; i < canvases.Count; i++)
        {
            CanvasEntry e = canvases[i];
            if (e == null || e.group == null) continue;

            e.group.alpha = e.homeAlpha * k;
            bool interactive = k > 0.99f;
            e.group.interactable = interactive && e.homeInteractable;
            e.group.blocksRaycasts = interactive && e.homeBlocksRaycasts;

            if (e.rect != null && e.slide)
            {
                e.rect.anchoredPosition = e.home + new Vector2(0f, (1f - k) * slideDistance);
            }
        }

        for (int i = 0; i < texts.Count; i++)
        {
            TextEntry e = texts[i];
            if (e == null || e.text == null) continue;
            e.text.alpha = e.homeAlpha * k;
        }
    }
}
