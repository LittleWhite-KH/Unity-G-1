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

        [Tooltip("悬停时显示的主文字组件（First Text）")]
        public TextMeshProUGUI textComponent;

        [Tooltip("悬停时显示的文字内容")]
        public string displayText;

        [Header("点击扩展")]
        public bool enableClick = true;

        [Tooltip("点击后主文字上升的偏移量（Y轴）")]
        public float riseOffset = 50f;

        [Tooltip("上升/下降动画时长")]
        public float riseDuration = 0.5f;

        [Tooltip("上升位置动画曲线（X=时间比例 0~1，Y=插值进度 0~1）")]
        public AnimationCurve riseCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [Tooltip("点击后显示的第二个文字组件（位置不变，仅淡入淡出）")]
        public TextMeshProUGUI secondaryText;

        [Tooltip("第二个文字显示的内容")]
        public string secondaryDisplayText;

        // ===== 新增：点击旋转扩展 =====
        [Header("点击旋转扩展")]
        [Tooltip("点击后物体绕自身 Y 轴旋转的角度（增量）")]
        public float rotationAngle = 90f;

        [Tooltip("旋转动画时长（秒），设为 0 则瞬间旋转")]
        public float rotationDuration = 0.5f;

        [Tooltip("旋转动画曲线（X=时间比例 0~1，Y=插值进度 0~1）")]
        public AnimationCurve rotationCurve = AnimationCurve.Linear(0, 0, 1, 1);

        // 运行时记录主文字原始位置
        [HideInInspector] public Vector2 originalPosition;
    }

    [Header("关联物体、文字组件及显示内容")]
    [SerializeField] private List<HoverItem> items = new List<HoverItem>();

    [Header("主文字及 Secondary 淡入淡出设置（线性）")]
    [SerializeField] private float fadeDuration = 0.5f;

    // 内部状态
    private GameObject currentTarget;
    private TextMeshProUGUI currentText;
    private Dictionary<TextMeshProUGUI, Coroutine> coroutineMap = new Dictionary<TextMeshProUGUI, Coroutine>();
    private HashSet<GameObject> clickedObjects = new HashSet<GameObject>();
    private HoverItem activeClickedItem = null;

    // ===== 新增：管理物体旋转协程的字典 =====
    private Dictionary<GameObject, Coroutine> rotationCoroutines = new Dictionary<GameObject, Coroutine>();

    void Start()
    {
        foreach (var item in items)
        {
            // 主文字初始化
            if (item.textComponent == null)
            {
                Debug.LogWarning($"物体 '{item.target.name}' 未绑定主文字，请检查！");
                continue;
            }
            EnsureCanvasGroupAndHide(item.textComponent);
            item.originalPosition = item.textComponent.rectTransform.anchoredPosition;

            // 第二个文字初始化
            if (item.secondaryText != null)
            {
                EnsureCanvasGroupAndHide(item.secondaryText);
                item.secondaryText.text = item.secondaryDisplayText;
            }
        }
    }

    void Update()
    {
        if (Mouse.current == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0));
        RaycastHit hit;

        // ---- 1. 鼠标左键按下（点击） ----
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (Physics.Raycast(ray, out hit))
            {
                GameObject hitObj = hit.collider.gameObject;
                HoverItem item = items.Find(i => i.target == hitObj);
                if (item != null && item.enableClick && !clickedObjects.Contains(hitObj))
                {
                    ClickItem(item);
                    return;
                }
            }
        }

        // ---- 2. 鼠标左键释放（复位） ----
        if (Mouse.current.leftButton.wasReleasedThisFrame && activeClickedItem != null)
        {
            ReleaseItem(activeClickedItem);
            activeClickedItem = null;
            // 释放后继续执行悬停检测（本帧可能已有悬停）
        }

        // ---- 3. 悬停检测 ----
        if (Physics.Raycast(ray, out hit))
        {
            GameObject hitObj = hit.collider.gameObject;
            // 若已被点击但未释放，忽略悬停
            if (clickedObjects.Contains(hitObj))
                return;

            HoverItem item = items.Find(i => i.target == hitObj);
            if (item != null && item.textComponent != null)
            {
                if (currentTarget != hitObj)
                {
                    // 隐藏上一个文字
                    if (currentText != null)
                        FadeOutText(currentText);

                    // 显示新文字
                    item.textComponent.text = item.displayText;
                    currentTarget = hitObj;
                    currentText = item.textComponent;
                    FadeInText(currentText);
                }
                return;
            }
        }

        // 鼠标未悬停在任何目标上
        if (currentTarget != null)
        {
            if (currentText != null)
                FadeOutText(currentText);
            currentTarget = null;
            currentText = null;
        }
    }

    // ---- 点击处理 ----
    void ClickItem(HoverItem item)
    {
        // 若当前悬停的正是该物体，清除悬停状态（避免冲突）
        if (currentTarget == item.target)
        {
            if (currentText != null)
            {
                StopCoroutineOnText(currentText);
                currentText = null;
            }
            currentTarget = null;
        }

        clickedObjects.Add(item.target);
        activeClickedItem = item;

        // ---- 处理主文字 ----
        TextMeshProUGUI mainText = item.textComponent;
        CanvasGroup mainCg = mainText.GetComponent<CanvasGroup>();
        if (mainCg == null) mainCg = mainText.gameObject.AddComponent<CanvasGroup>();

        StopCoroutineOnText(mainText);
        mainText.gameObject.SetActive(true);
        mainCg.alpha = 1f;

        // 上升动画（使用缓动曲线）
        StartCoroutine(RiseCoroutine(mainText, item.originalPosition, item.riseOffset, item.riseDuration, item.riseCurve));

        // ---- 处理第二个文字（仅淡入，位置不变） ----
        if (item.secondaryText != null)
        {
            item.secondaryText.text = item.secondaryDisplayText;
            FadeInText(item.secondaryText); // 使用线性淡入
        }

        // ===== 新增：物体旋转 =====
        if (item.rotationAngle != 0f)
        {
            StartRotation(item.target, item.rotationAngle, item.rotationDuration, item.rotationCurve);
        }
    }

    // ---- 释放复位 ----
    void ReleaseItem(HoverItem item)
    {
        clickedObjects.Remove(item.target);

        bool isHovering = IsMouseOverTarget(item.target);

        // ---- 主文字复位（下降） ----
        TextMeshProUGUI mainText = item.textComponent;
        CanvasGroup mainCg = mainText.GetComponent<CanvasGroup>();

        if (mainCg != null)
        {
            StopCoroutineOnText(mainText);
            // 下降动画：若悬停则不隐藏，否则隐藏
            StartCoroutine(ReturnCoroutine(mainText, item.originalPosition, item.riseDuration, !isHovering, item.riseCurve));
        }

        // ---- 第二个文字淡出（不管是否悬停都消失） ----
        if (item.secondaryText != null)
        {
            FadeOutText(item.secondaryText);
        }

        // ---- 若鼠标仍悬停，恢复悬停状态 ----
        if (isHovering)
        {
            currentTarget = item.target;
            currentText = mainText;
            // 确保文本内容为悬停显示
            mainText.text = item.displayText;
            // 确保透明度和激活状态正确（下降动画未改变透明度）
            if (mainCg != null)
            {
                mainCg.alpha = 1f;
                mainText.gameObject.SetActive(true);
            }
        }
        else
        {
            // 不悬停，清除状态（由 ReturnCoroutine 隐藏）
            if (currentTarget == item.target)
            {
                currentTarget = null;
                currentText = null;
            }
        }
    }

    // ---- 判断鼠标是否悬停于某物体 ----
    bool IsMouseOverTarget(GameObject target)
    {
        if (Mouse.current == null || target == null) return false;
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0));
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            return hit.collider.gameObject == target;
        }
        return false;
    }

    // ---- 主文字上升协程（带缓动） ----
    IEnumerator RiseCoroutine(TextMeshProUGUI text, Vector2 origin, float offset, float duration, AnimationCurve curve)
    {
        RectTransform rt = text.rectTransform;
        Vector2 startPos = origin;
        Vector2 targetPos = new Vector2(origin.x, origin.y + offset);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curveT = curve.Evaluate(t);
            rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, curveT);
            yield return null;
        }
        rt.anchoredPosition = targetPos;
    }

    // ---- 主文字下降复位协程（带缓动） ----
    IEnumerator ReturnCoroutine(TextMeshProUGUI text, Vector2 origin, float duration, bool disableAfter, AnimationCurve curve)
    {
        RectTransform rt = text.rectTransform;
        Vector2 startPos = rt.anchoredPosition;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curveT = curve != null ? curve.Evaluate(t) : t;
            rt.anchoredPosition = Vector2.Lerp(startPos, origin, curveT);
            yield return null;
        }
        rt.anchoredPosition = origin;

        if (disableAfter)
        {
            CanvasGroup cg = text.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 0f;
                text.gameObject.SetActive(false);
            }
        }
    }

    // ---- 辅助：确保有 CanvasGroup 并隐藏 ----
    void EnsureCanvasGroupAndHide(TextMeshProUGUI text)
    {
        if (text == null) return;
        CanvasGroup cg = text.GetComponent<CanvasGroup>();
        if (cg == null) cg = text.gameObject.AddComponent<CanvasGroup>();
        text.gameObject.SetActive(false);
        cg.alpha = 0f;
    }

    // ---- 淡入（线性） ----
    void FadeInText(TextMeshProUGUI text)
    {
        if (text == null) return;
        CanvasGroup cg = text.GetComponent<CanvasGroup>();
        if (cg == null) return;

        StopCoroutineOnText(text);
        text.gameObject.SetActive(true);
        cg.alpha = 0f;
        StartFade(text, cg, 0f, 1f, false);
    }

    // ---- 淡出（线性） ----
    void FadeOutText(TextMeshProUGUI text)
    {
        if (text == null) return;
        CanvasGroup cg = text.GetComponent<CanvasGroup>();
        if (cg == null) return;

        StopCoroutineOnText(text);
        StartFade(text, cg, cg.alpha, 0f, true);
    }

    void StopCoroutineOnText(TextMeshProUGUI text)
    {
        if (coroutineMap.ContainsKey(text))
        {
            StopCoroutine(coroutineMap[text]);
            coroutineMap.Remove(text);
        }
    }

    void StartFade(TextMeshProUGUI text, CanvasGroup cg, float from, float to, bool disableAfter)
    {
        StopCoroutineOnText(text);
        Coroutine cor = StartCoroutine(FadeAlphaCoroutine(text, cg, from, to, disableAfter));
        coroutineMap[text] = cor;
    }

    IEnumerator FadeAlphaCoroutine(TextMeshProUGUI text, CanvasGroup cg, float from, float to, bool disableAfter)
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            cg.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }
        cg.alpha = to;

        if (disableAfter && to == 0f)
            cg.gameObject.SetActive(false);

        if (coroutineMap.ContainsKey(text))
            coroutineMap.Remove(text);
    }

    // ===== 新增：物体旋转相关方法 =====

    // ---- 启动旋转（管理协程） ----
    void StartRotation(GameObject obj, float angle, float duration, AnimationCurve curve)
    {
        StopCoroutineOnObject(obj);
        Coroutine cor = StartCoroutine(RotateCoroutine(obj, angle, duration, curve));
        rotationCoroutines[obj] = cor;
    }

    // ---- 停止物体上的旋转协程 ----
    void StopCoroutineOnObject(GameObject obj)
    {
        if (rotationCoroutines.ContainsKey(obj))
        {
            if (rotationCoroutines[obj] != null)
                StopCoroutine(rotationCoroutines[obj]);
            rotationCoroutines.Remove(obj);
        }
    }

    // ===== 修改：旋转协程（使用局部轴，围绕自身枢轴点） =====
    IEnumerator RotateCoroutine(GameObject obj, float angle, float duration, AnimationCurve curve)
    {
        // 若已有旋转协程，先停止（已经在 StartRotation 中处理，但为了安全也在这里判断）
        StopCoroutineOnObject(obj);

        // 使用物体自身的局部 Z 轴方向（世界空间表示）作为旋转轴
        Vector3 axis = obj.transform.forward; // 局部 Y 轴

        if (duration <= 0f)
        {
            // 瞬间旋转：直接应用增量旋转（围绕枢轴点，绕局部 Y 轴）
            obj.transform.RotateAround(obj.transform.position, axis, angle);
            yield break;
        }

        float elapsed = 0f;
        float rotatedSoFar = 0f; // 记录已经旋转的总角度

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curveT = curve.Evaluate(t);

            // 计算这一帧应达到的目标总角度
            float targetAngle = Mathf.Lerp(0, angle, curveT);
            // 计算本帧需要增加的增量角度
            float deltaAngle = targetAngle - rotatedSoFar;

            // 应用增量旋转，围绕物体自身的枢轴点（transform.position），绕局部 Y 轴
            obj.transform.RotateAround(obj.transform.position, axis, deltaAngle);

            rotatedSoFar = targetAngle;
            yield return null;
        }

        // 确保最终旋转到位（避免浮点数误差）
        float finalDelta = angle - rotatedSoFar;
        if (Mathf.Abs(finalDelta) > 0.001f)
        {
            obj.transform.RotateAround(obj.transform.position, axis, finalDelta);
        }

        // 协程结束后从字典移除
        if (rotationCoroutines.ContainsKey(obj))
            rotationCoroutines.Remove(obj);
    }
}