using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 设置面板：TAB 打开 / 关闭，锁鼠标并暂停视角与移动。
/// 内容：鼠标灵敏度、反转 Y、FOV、移速、亮度、重置。
/// 运行时自建 UI，风格对齐 HUD（白色面板 + 蓝色标题条 + Menlo）。
/// </summary>
public class SettingsPanel : MonoBehaviour
{
    private const float PanelW = 760f;
    private const float PanelH = 600f;

    private static SettingsPanel instance;

    private Canvas canvas;
    private CanvasGroup group;
    private GameObject root;
    private MouseLook look;
    private PlayerMovement move;
    private Image brightnessMask;

    private Slider sensSlider;
    private Slider fovSlider;
    private Slider speedSlider;
    private Slider brightSlider;
    private TextMeshProUGUI sensValue;
    private TextMeshProUGUI fovValue;
    private TextMeshProUGUI speedValue;
    private TextMeshProUGUI brightValue;
    private TextMeshProUGUI invertLabel;

    private bool open;
    private TMP_FontAsset font;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Ensure(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Ensure(scene);
    }

    private static void Ensure(Scene scene)
    {
        SettingsPanel existing = FindFirstObjectByType<SettingsPanel>();
        if (scene.name == "Start")
        {
            if (existing != null) Destroy(existing.gameObject);
            instance = null;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }
        if (existing != null)
        {
            existing.SetOpen(false);
            existing.PlayerFind();
            existing.enabled = true;
            return;
        }
        GameObject go = new GameObject("SettingsPanel");
        DontDestroyOnLoad(go);
        go.AddComponent<SettingsPanel>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        GameSettings.Load();
        BuildUi();
        ApplyBrightness();
        if (root != null) root.SetActive(false);
        open = false;
    }

    private void Start()
    {
        PlayerFind();
    }

    private void PlayerFind()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null) return;
        look = player.GetComponentInChildren<MouseLook>(true);
        move = player.GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            SetOpen(!open);
        }

        if (open && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SetOpen(false);
        }
    }

    private bool prevLookControl;
    private bool prevMoveControl;

    public void SetOpen(bool value)
    {
        if (open == value) return;

        open = value;
        if (root != null) root.SetActive(value);

        if (look == null || move == null) PlayerFind();

        if (value)
        {
            prevLookControl = look != null && look.canControl;
            prevMoveControl = move != null && move.canMove;
            if (look != null) look.canControl = false;
            if (move != null) move.canMove = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            RefreshValues();
        }
        else
        {
            if (look != null) look.canControl = prevLookControl;
            if (move != null) move.canMove = prevMoveControl;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void RefreshValues()
    {
        if (sensSlider != null)
        {
            sensSlider.SetValueWithoutNotify(GameSettings.Sensitivity);
            fovSlider.SetValueWithoutNotify(GameSettings.Fov);
            speedSlider.SetValueWithoutNotify(GameSettings.MoveSpeed);
            brightSlider.SetValueWithoutNotify(GameSettings.Brightness);
        }

        UpdateLabels();
    }

    private void UpdateLabels()
    {
        if (sensValue != null) sensValue.text = GameSettings.Sensitivity.ToString("0");
        if (fovValue != null) fovValue.text = GameSettings.Fov.ToString("0");
        if (speedValue != null) speedValue.text = GameSettings.MoveSpeed.ToString("0.00") + "x";
        if (brightValue != null) brightValue.text = GameSettings.Brightness.ToString("0.00");
        if (invertLabel != null) invertLabel.text = GameSettings.InvertY ? "ON" : "OFF";
    }

    private void ApplyBrightness()
    {
        if (brightnessMask == null) return;
        float a = Mathf.Clamp01((1.2f - GameSettings.Brightness) / 1.2f) * 0.55f;
        brightnessMask.color = new Color(0f, 0f, 0f, a);
    }

    // ------------------------------------------------------------------
    // UI 构建
    // ------------------------------------------------------------------
    private void BuildUi()
    {
        GameObject canvasGo = new GameObject("SettingsCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 800;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        root = new GameObject("Root");
        root.transform.SetParent(canvasGo.transform, false);
        Stretch(root.AddComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // 变暗遮罩（亮度设置用）
        GameObject maskGo = NewImage("BrightnessMask", root.transform, new Color(0f, 0f, 0f, 0f));
        Stretch(maskGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        brightnessMask = maskGo.GetComponent<Image>();
        brightnessMask.raycastTarget = false;

        // 半透明背景
        GameObject backdrop = NewImage("Backdrop", root.transform, new Color(0.02f, 0.03f, 0.05f, 0.72f));
        Stretch(backdrop.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // 白面板
        GameObject panel = NewImage("Panel", root.transform, new Color(0.965f, 0.97f, 0.975f, 1f));
        RectTransform pr = panel.GetComponent<RectTransform>();
        pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
        pr.sizeDelta = new Vector2(PanelW, PanelH);
        pr.anchoredPosition = Vector2.zero;

        // 标题条
        GameObject header = NewImage("Header", panel.transform, new Color(0.129f, 0.412f, 0.8f, 1f));
        RectTransform hr = header.GetComponent<RectTransform>();
        hr.anchorMin = new Vector2(0f, 1f);
        hr.anchorMax = new Vector2(1f, 1f);
        hr.pivot = new Vector2(0.5f, 1f);
        hr.sizeDelta = new Vector2(0f, 62f);
        hr.anchoredPosition = Vector2.zero;

        MakeText(header.transform, "SETTINGS", 30f, TextAlignmentOptions.Left, new Color(1f, 1f, 1f, 1f),
            new Vector2(30f, 0f), new Vector2(300f, 62f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));

        // 行
        float rowY = 170f;
        const float rowGap = 96f;
        const float leftX = -320f;

        sensValue = MakeSliderRow(panel.transform, "LOOK SENSITIVITY", leftX, rowY, out sensSlider, 1f, 100f,
            GameSettings.Sensitivity, v => { GameSettings.Sensitivity = Mathf.Round(v); GameSettings.Save(); UpdateLabels(); });
        sensSlider.wholeNumbers = true;

        fovValue = MakeSliderRow(panel.transform, "FIELD OF VIEW", leftX, rowY - rowGap, out fovSlider, 55f, 85f,
            GameSettings.Fov, v => { GameSettings.Fov = v; GameSettings.Save(); UpdateLabels(); });

        speedValue = MakeSliderRow(panel.transform, "WALK SPEED", leftX, rowY - rowGap * 2f, out speedSlider, 0.6f, 1.4f,
            GameSettings.MoveSpeed, v => { GameSettings.MoveSpeed = v; GameSettings.Save(); UpdateLabels(); });

        brightValue = MakeSliderRow(panel.transform, "BRIGHTNESS", leftX, rowY - rowGap * 3f, out brightSlider, 0.6f, 1.2f,
            GameSettings.Brightness, v => { GameSettings.Brightness = v; GameSettings.Save(); ApplyBrightness(); UpdateLabels(); });

        // INVERT Y
        GameObject invert = MakeButton(panel.transform, "INVERT Y", -280f, rowY - rowGap * 4f, 200f, 40f,
            new Color(0.9f, 0.92f, 0.95f), new Color(0.15f, 0.16f, 0.18f));

        TextMeshProUGUI invertTitle = invert.GetComponentInChildren<TextMeshProUGUI>();
        if (invertTitle != null)
        {
            invertTitle.alignment = TextAlignmentOptions.Left;
            invertTitle.rectTransform.pivot = new Vector2(0f, 0.5f);
            invertTitle.rectTransform.anchoredPosition = new Vector2(-88f, 0f);
            invertTitle.rectTransform.sizeDelta = new Vector2(180f, 40f);
        }

        invertLabel = MakeText(invert.transform, "OFF", 20f, TextAlignmentOptions.Right, new Color(0.129f, 0.412f, 0.8f),
            new Vector2(82f, 0f), new Vector2(70f, 40f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        invertLabel.rectTransform.pivot = new Vector2(1f, 0.5f);
        invert.GetComponent<Button>().onClick.AddListener(() =>
        {
            GameSettings.InvertY = !GameSettings.InvertY;
            GameSettings.Save();
            UpdateLabels();
        });

        // RESET / BACK
        GameObject reset = MakeButton(panel.transform, "RESET", 40f, -230f, 180f, 46f,
            new Color(0.9f, 0.92f, 0.95f), new Color(0.15f, 0.16f, 0.18f));
        reset.GetComponent<Button>().onClick.AddListener(() =>
        {
            GameSettings.ResetDefaults();
            RefreshValues();
            ApplyBrightness();
        });

        GameObject back = MakeButton(panel.transform, "BACK", 240f, -230f, 180f, 46f,
            new Color(0.129f, 0.412f, 0.8f), new Color(1f, 1f, 1f));
        back.GetComponent<Button>().onClick.AddListener(() => SetOpen(false));

        MakeText(panel.transform, "TAB — CLOSE", 16f, TextAlignmentOptions.Center, new Color(0.5f, 0.52f, 0.55f),
            new Vector2(0f, -278f), new Vector2(300f, 24f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
    }

    private TextMeshProUGUI MakeSliderRow(Transform parent, string label, float x, float y, out Slider slider,
        float min, float max, float value, System.Action<float> onChange)
    {
        TextMeshProUGUI labelText = MakeText(parent, label, 20f, TextAlignmentOptions.Left, new Color(0.15f, 0.16f, 0.18f),
            new Vector2(x, y), new Vector2(320f, 30f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        labelText.rectTransform.pivot = new Vector2(0f, 0.5f);

        TextMeshProUGUI valueText = MakeText(parent, "1.00", 20f, TextAlignmentOptions.Right, new Color(0.129f, 0.412f, 0.8f),
            new Vector2(x + 640f, y), new Vector2(120f, 30f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        valueText.rectTransform.pivot = new Vector2(1f, 0.5f);

        slider = MakeSlider(parent, x, y - 30f, 640f, 14f, min, max, value, onChange);
        return valueText;
    }

    private Slider MakeSlider(Transform parent, float x, float y, float width, float height,
        float min, float max, float value, System.Action<float> onChange)
    {
        GameObject go = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = new Vector2(x, y);

        Slider slider = go.GetComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = value;
        slider.transition = Selectable.Transition.None;

        GameObject bg = NewImage("Background", go.transform, new Color(0.82f, 0.84f, 0.87f, 1f));
        Stretch(bg.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(go.transform, false);
        RectTransform far = fillArea.GetComponent<RectTransform>();
        Stretch(far, Vector2.zero, Vector2.one, new Vector2(0f, 0f), new Vector2(-16f, 0f));

        GameObject fill = NewImage("Fill", fillArea.transform, new Color(0.129f, 0.412f, 0.8f, 1f));
        Stretch(fill.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(go.transform, false);
        Stretch(handleArea.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(8f, 0f), new Vector2(-8f, 0f));

        GameObject handle = NewImage("Handle", handleArea.transform, new Color(0.129f, 0.412f, 0.8f, 1f));
        RectTransform hrt = handle.GetComponent<RectTransform>();
        hrt.sizeDelta = new Vector2(16f, 26f);

        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.handleRect = hrt;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.direction = Slider.Direction.LeftToRight;

        slider.onValueChanged.AddListener(v => onChange(v));
        return slider;
    }

    private GameObject MakeButton(Transform parent, string label, float x, float y, float w, float h, Color bg, Color fg)
    {
        GameObject go = NewImage("Button_" + label, parent, bg);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x, y);

        Button b = go.AddComponent<Button>();
        b.targetGraphic = go.GetComponent<Image>();

        MakeText(go.transform, label, 20f, TextAlignmentOptions.Center, fg,
            Vector2.zero, new Vector2(w, h), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

        return go;
    }

    private TextMeshProUGUI MakeText(Transform parent, string text, float size, TextAlignmentOptions align, Color color,
        Vector2 pos, Vector2 sizeDelta, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = sizeDelta;
        rt.anchoredPosition = pos;

        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.alignment = align;
        t.color = color;
        t.enableWordWrapping = false;
        if (font != null) t.font = font;
        else
        {
            TMP_FontAsset f = TMP_Settings.defaultFontAsset;
            if (f != null) t.font = f;
        }

        return t;
    }

    private static GameObject NewImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.color = color;
        return go;
    }

    private static void Stretch(RectTransform rt, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }
}
