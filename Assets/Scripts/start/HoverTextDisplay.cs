using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class HoverTextDisplay : MonoBehaviour
{
    [System.Serializable]
    public class HoverItem
    {
        [Tooltip("需要检测鼠标悬停/点击的物体（必须有 Collider）")]
        public GameObject target;

        [Tooltip("悬停时显示的第一批 TMP 文字。文字内容直接使用 TMP 组件中已填写的内容")]
        public TextMeshProUGUI textComponent;

        [Header("点击扩展")]
        public bool enableClick = true;

        [Tooltip("点击后显示的第二批 TMP 文字。文字内容直接使用 TMP 组件中已填写的内容。鼠标移开后隐藏（不再等松开鼠标）")]
        public TextMeshProUGUI secondaryText;

        [Header("点击后的读条文字")]
        [Tooltip("点击后显示的读条 TMP 文字（例如内容为 / 的那个）。默认隐藏，鼠标移开后隐藏；显示期间循环切换字符模拟读条")]
        public TextMeshProUGUI loadingText;

        [Tooltip("是否把读条 TMP 移动到下面的坐标。取消勾选则保持它自己的位置不动")]
        public bool overrideLoadingTextPosition = true;

        [Tooltip("读条 TMP 的新坐标，对应 Inspector 里 Transform 的 Pos X / Pos Y / Pos Z")]
        public Vector3 loadingTextPosition = Vector3.zero;

        [Header("点击旋转扩展")]
        [Tooltip("点击后物体绕自身轴旋转的角度（增量）")]
        public float rotationAngle = 90f;

        [Tooltip("旋转动画时长（秒），设为 0 则瞬间旋转")]
        public float rotationDuration = 0.5f;

        [Tooltip("旋转动画曲线（X=时间比例 0~1，Y=插值进度 0~1）")]
        public AnimationCurve rotationCurve = AnimationCurve.Linear(0, 0, 1, 1);
    }

    [Header("关联物体与两批 TMP 文字")]
    [SerializeField] private List<HoverItem> items = new List<HoverItem>();

    [Header("文字淡入淡出设置（线性）")]
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("读条动画设置（点击后显示的 loadingText 用）")]
    [Tooltip("读条帧序列，按顺序循环播放。默认 / - \\ - ，模拟转圈读条")]
    [SerializeField] private string[] loadingFrames = new string[] { "/", "-", "\\", "-" };

    [Tooltip("每一帧停留的时间（秒）")]
    [SerializeField] private float loadingFrameInterval = 1f;

    [Header("Default TV prompt")]
    [SerializeField] private bool showDefaultPrompt = true;
    [SerializeField] private int defaultItemIndex = 0;
    private HoverItem defaultItem;
    private bool defaultPromptVisible;

    // 内部状态
    private GameObject currentTarget;
    private TextMeshProUGUI currentText;
    private readonly Dictionary<TextMeshProUGUI, Coroutine> coroutineMap =
        new Dictionary<TextMeshProUGUI, Coroutine>();

    // 已点击激活的项：第二批文字与读条文字只在鼠标移开后隐藏
    private readonly List<HoverItem> activatedItems = new List<HoverItem>();

    // 读条字符切换协程
    private readonly Dictionary<HoverItem, Coroutine> loadingCoroutines =
        new Dictionary<HoverItem, Coroutine>();

    // 管理物体旋转协程
    private readonly Dictionary<GameObject, Coroutine> rotationCoroutines =
        new Dictionary<GameObject, Coroutine>();

    void Start()
    {
        foreach (var item in items)
        {
            if (item.target == null)
            {
                Debug.LogWarning("HoverTextDisplay 中有一项未绑定目标物体，请检查！", this);
                continue;
            }

            // 不再修改 TMP 的 text，只负责隐藏/显示已经排版好的文字。
            if (item.textComponent != null)
            {
                EnsureCanvasGroupAndHide(item.textComponent);
            }
            else
            {
                Debug.LogWarning($"物体 '{item.target.name}' 未绑定第一批 TMP 文字，请检查！", item.target);
            }

            if (item.secondaryText != null)
            {
                EnsureCanvasGroupAndHide(item.secondaryText);
            }

            // 读条文字默认隐藏（它的文字内容由读条动画接管，见 loadingFrames）。
            if (item.loadingText != null)
            {
                EnsureCanvasGroupAndHide(item.loadingText);
            }
        }
    }

    private void UpdateDefaultPrompt(bool visible)
    {
        if (defaultItem == null || defaultPromptVisible == visible) return;
        defaultPromptVisible = visible;
        if (visible) FadeInText(defaultItem.secondaryText);
        else if (!activatedItems.Contains(defaultItem)) FadeOutText(defaultItem.secondaryText);
    }

    void Update()
    {
        if (defaultItem == null && showDefaultPrompt && defaultItemIndex >= 0 && defaultItemIndex < items.Count)
            defaultItem = items[defaultItemIndex];
        if (Mouse.current == null || Camera.main == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0));

        // 本帧鼠标指向的目标：只做一次射线检测，点击与悬停共用。
        HoverItem hoveredItem = null;
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GameObject hitObj = hit.collider.gameObject;
            hoveredItem = items.Find(i => i.target == hitObj);
        }

        // Keep the start label and its SPACE instruction visible at rest.
        bool hasHover = hoveredItem != null;
        if (!hasHover) hoveredItem = defaultItem;
        UpdateDefaultPrompt(hoveredItem == defaultItem);

        // 1. 鼠标左键按下：激活该按钮，显示第二批文字与读条文字。
        if (Mouse.current.leftButton.wasPressedThisFrame &&
            hasHover && hoveredItem != null &&
            hoveredItem.enableClick &&
            !activatedItems.Contains(hoveredItem))
        {
            ActivateItem(hoveredItem);
        }

        // 2. 已激活的项：鼠标移开后隐藏第二批文字与读条文字。
        //    这里不再依赖鼠标松开，隐藏时机与第一批文字的悬停逻辑保持一致。
        for (int i = activatedItems.Count - 1; i >= 0; i--)
        {
            HoverItem item = activatedItems[i];
            if (item != hoveredItem)
            {
                DeactivateItem(item);
            }
        }

        // 3. 悬停检测：显示第一批文字。
        if (hoveredItem != null && hoveredItem.textComponent != null)
        {
            if (currentTarget != hoveredItem.target)
            {
                if (currentText != null)
                    FadeOutText(currentText);

                currentTarget = hoveredItem.target;
                currentText = hoveredItem.textComponent;
                FadeInText(currentText);
            }

            return;
        }

        // 鼠标未悬停在任何目标上。
        if (currentTarget != null)
        {
            if (currentText != null)
                FadeOutText(currentText);

            currentTarget = null;
            currentText = null;
        }
    }

    // 点击处理：不改文字内容、不移动第一批文字，只显示第二批文字与读条文字。
    void ActivateItem(HoverItem item)
    {
        if (item == null || item.target == null) return;

        if (!activatedItems.Contains(item))
            activatedItems.Add(item);

        // 第一批文字保持原位并保持显示。
        if (item.textComponent != null)
        {
            ShowTextImmediately(item.textComponent);
            currentTarget = item.target;
            currentText = item.textComponent;
        }

        // 第二批文字按原有淡入方式显示，位置与内容均不修改。
        if (item.secondaryText != null)
        {
            FadeInText(item.secondaryText);
        }

        // 读条文字：可选的新坐标 + 立即显示第一帧 + 开始循环切换。
        ShowLoadingText(item);

        // 保留原有的物体旋转功能。
        if (item.rotationAngle != 0f)
        {
            StartRotation(item.target, item.rotationAngle, item.rotationDuration, item.rotationCurve);
        }
    }

    // 鼠标移开：第二批文字与读条文字一起隐藏。
    void DeactivateItem(HoverItem item)
    {
        if (item == null) return;

        activatedItems.Remove(item);

        if (item.secondaryText != null)
        {
            FadeOutText(item.secondaryText);
        }

        HideLoadingText(item);
    }

    // 显示读条文字：先应用该 element 指定的新坐标，再显示并启动字符循环。
    void ShowLoadingText(HoverItem item)
    {
        if (item == null || item.loadingText == null) return;

        // 每个 element 都可以给读条 TMP 一个自己的坐标（Inspector 可改）。
        if (item.overrideLoadingTextPosition)
        {
            item.loadingText.rectTransform.localPosition = item.loadingTextPosition;
        }

        item.loadingText.text = GetLoadingFrame(0);
        FadeInText(item.loadingText);

        StopLoadingCycle(item);
        loadingCoroutines[item] = StartCoroutine(LoadingCycleCoroutine(item));
    }

    // 隐藏读条文字：停止字符循环并淡出（与其它文字一致）。
    void HideLoadingText(HoverItem item)
    {
        if (item == null) return;

        StopLoadingCycle(item);

        if (item.loadingText != null)
        {
            FadeOutText(item.loadingText);
        }
    }

    string GetLoadingFrame(int index)
    {
        if (loadingFrames == null || loadingFrames.Length == 0) return string.Empty;

        int count = loadingFrames.Length;
        int safeIndex = ((index % count) + count) % count;
        return loadingFrames[safeIndex];
    }

    void StopLoadingCycle(HoverItem item)
    {
        if (item != null && loadingCoroutines.TryGetValue(item, out Coroutine routine))
        {
            if (routine != null)
                StopCoroutine(routine);

            loadingCoroutines.Remove(item);
        }
    }

    // 读条循环：按 loadingFrameInterval 依次切换 / - \ - …
    IEnumerator LoadingCycleCoroutine(HoverItem item)
    {
        int index = 0;
        float interval = Mathf.Max(0.01f, loadingFrameInterval);

        while (true)
        {
            if (item == null || item.loadingText == null)
                yield break;

            item.loadingText.text = GetLoadingFrame(index);
            index++;

            yield return new WaitForSeconds(interval);
        }
    }

    void EnsureCanvasGroupAndHide(TextMeshProUGUI text)
    {
        if (text == null) return;

        CanvasGroup cg = text.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = text.gameObject.AddComponent<CanvasGroup>();

        cg.alpha = 0f;
        text.gameObject.SetActive(false);
    }

    void ShowTextImmediately(TextMeshProUGUI text)
    {
        if (text == null) return;

        StopCoroutineOnText(text);

        CanvasGroup cg = text.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = text.gameObject.AddComponent<CanvasGroup>();

        text.gameObject.SetActive(true);
        cg.alpha = 1f;
    }

    void FadeInText(TextMeshProUGUI text)
    {
        if (text == null) return;

        CanvasGroup cg = text.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = text.gameObject.AddComponent<CanvasGroup>();

        StopCoroutineOnText(text);
        text.gameObject.SetActive(true);

        if (fadeDuration <= 0f)
        {
            cg.alpha = 1f;
            return;
        }

        StartFade(text, cg, cg.alpha, 1f, false);
    }

    void FadeOutText(TextMeshProUGUI text)
    {
        if (text == null) return;

        CanvasGroup cg = text.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = text.gameObject.AddComponent<CanvasGroup>();

        StopCoroutineOnText(text);

        if (fadeDuration <= 0f)
        {
            cg.alpha = 0f;
            text.gameObject.SetActive(false);
            return;
        }

        StartFade(text, cg, cg.alpha, 0f, true);
    }

    void StopCoroutineOnText(TextMeshProUGUI text)
    {
        if (text != null && coroutineMap.TryGetValue(text, out Coroutine coroutine))
        {
            if (coroutine != null)
                StopCoroutine(coroutine);

            coroutineMap.Remove(text);
        }
    }

    void StartFade(TextMeshProUGUI text, CanvasGroup cg, float from, float to, bool disableAfter)
    {
        StopCoroutineOnText(text);
        Coroutine coroutine = StartCoroutine(FadeAlphaCoroutine(text, cg, from, to, disableAfter));
        coroutineMap[text] = coroutine;
    }

    IEnumerator FadeAlphaCoroutine(
        TextMeshProUGUI text,
        CanvasGroup cg,
        float from,
        float to,
        bool disableAfter)
    {
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            cg.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        cg.alpha = to;

        if (disableAfter && Mathf.Approximately(to, 0f))
            cg.gameObject.SetActive(false);

        coroutineMap.Remove(text);
    }

    // 物体旋转相关方法（保留原有功能）
    void StartRotation(GameObject obj, float angle, float duration, AnimationCurve curve)
    {
        if (obj == null) return;

        StopCoroutineOnObject(obj);
        Coroutine coroutine = StartCoroutine(RotateCoroutine(obj, angle, duration, curve));
        rotationCoroutines[obj] = coroutine;
    }

    void StopCoroutineOnObject(GameObject obj)
    {
        if (obj != null && rotationCoroutines.TryGetValue(obj, out Coroutine coroutine))
        {
            if (coroutine != null)
                StopCoroutine(coroutine);

            rotationCoroutines.Remove(obj);
        }
    }

    IEnumerator RotateCoroutine(GameObject obj, float angle, float duration, AnimationCurve curve)
    {
        // 保持原脚本的实际行为：绕物体自身的局部 Z 轴旋转。
        Vector3 axis = obj.transform.forward;

        if (duration <= 0f)
        {
            obj.transform.RotateAround(obj.transform.position, axis, angle);
            rotationCoroutines.Remove(obj);
            yield break;
        }

        float elapsed = 0f;
        float rotatedSoFar = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curveT = curve != null ? curve.Evaluate(t) : t;
            float targetAngle = Mathf.Lerp(0f, angle, curveT);
            float deltaAngle = targetAngle - rotatedSoFar;

            obj.transform.RotateAround(obj.transform.position, axis, deltaAngle);
            rotatedSoFar = targetAngle;
            yield return null;
        }

        float finalDelta = angle - rotatedSoFar;
        if (Mathf.Abs(finalDelta) > 0.001f)
        {
            obj.transform.RotateAround(obj.transform.position, axis, finalDelta);
        }

        rotationCoroutines.Remove(obj);
    }
}
