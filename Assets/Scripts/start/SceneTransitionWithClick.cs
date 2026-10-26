using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections;

public class SceneTransitionWithClick : MonoBehaviour
{
    [Header("目标物体（需带 Collider）")]
    public GameObject targetObject;          // 鼠标需要点击的物体

    [Header("过渡设置")]
    public float fadeSpeed = 1f;             // 变白速度（Alpha 增量/秒）
    public string nextSceneName;             // 要跳转的场景名称

    private bool objectClicked = false;      // 是否已点击目标
    private bool transitionStarted = false;  // 是否已开始变白
    private UnityEngine.UI.Image fadeImage;  // 全屏白色遮罩

    private void Start()
    {
        CreateFadeCanvas();
    }

    // 创建全屏 Canvas 和 Image（自动覆盖所有 UI）
    private void CreateFadeCanvas()
    {
        GameObject canvasObj = new GameObject("FadeCanvas");
        canvasObj.transform.SetParent(transform);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;   // 确保在最上层

        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(canvasObj.transform);

        fadeImage = imageObj.AddComponent<UnityEngine.UI.Image>();
        fadeImage.raycastTarget = false;
        fadeImage.color = new Color(1, 1, 1, 0); // 全白，但完全透明

        // 拉伸到全屏
        RectTransform rect = fadeImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
    }

    private void Update()
    {
        // 1. 检测鼠标左键点击（使用新输入系统）
        if (!objectClicked && Camera.main != null && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.gameObject == targetObject)
                {
                    objectClicked = true;
                    Debug.Log("目标物体被点击，现在按下空格键触发跳转");
                }
            }
        }

        // 2. 检测空格键按下（必须已点击目标且未开始过渡）
        if (!transitionStarted && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            transitionStarted = true;
            StartCoroutine(FadeToWhiteAndLoad());
        }
    }

    private IEnumerator FadeToWhiteAndLoad()
    {
        float alpha = 0f;
        while (alpha < 1f)
        {
            alpha += Mathf.Max(0.01f, fadeSpeed) * Time.unscaledDeltaTime;
            fadeImage.color = new Color(1, 1, 1, alpha);
            yield return null;
        }
        fadeImage.color = new Color(1, 1, 1, 1); // 确保完全白

        // 跳转场景
        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
        else
            Debug.LogError("未设置下一个场景名称！");
    }
}