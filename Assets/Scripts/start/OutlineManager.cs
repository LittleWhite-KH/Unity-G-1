using UnityEngine;
using UnityEngine.InputSystem;

public class OutlineManager : MonoBehaviour
{
    [Header("轮廓闪烁设置")]
    [SerializeField] private Color outlineColor = Color.yellow;
    [SerializeField] private float minWidth = 0.5f;
    [SerializeField] private float maxWidth = 5f;
    [SerializeField] private float blinkSpeed = 2f;

    [Header("调试")]
    [SerializeField] private bool testMode = false; // 勾选后固定为最大宽度

    private Outline outline;
    private bool isHovering = false;
    private float currentWidth = 0f;

    void Start()
    {
        // 获取当前物体上的 Outline 组件
        outline = GetComponent<Outline>();
        if (outline == null)
        {
            Debug.LogError($"OutlineManager: 物体 '{gameObject.name}' 缺少 Outline 组件，请手动添加并勾选 Precompute Outline。");
            enabled = false; // 停止 Update
            return;
        }

        // 初始化轮廓参数
        outline.OutlineMode = Outline.Mode.OutlineAll;
        outline.OutlineColor = outlineColor;
        outline.OutlineWidth = maxWidth; // 初始可见
        outline.enabled = true;

        currentWidth = maxWidth;
        Debug.Log($"OutlineManager 初始化完成，物体: {gameObject.name}");
    }

    void Update()
    {
        // 测试模式：固定最大宽度
        if (testMode)
        {
            if (outline != null)
            {
                outline.OutlineWidth = maxWidth;
                outline.OutlineColor = outlineColor;
                outline.enabled = true;
            }
            return;
        }

        // 鼠标悬停检测（New Input System）
        isHovering = false;
        if (Mouse.current != null)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                // 检测是否击中当前物体（或其子物体，取决于 Collider 设置）
                if (hit.collider.gameObject == gameObject)
                {
                    isHovering = true;
                }
            }
        }

        // 呼吸宽度计算
        if (!isHovering)
        {
            float t = (Mathf.Sin(Time.time * blinkSpeed) + 1f) / 2f; // 0~1
            currentWidth = Mathf.Lerp(minWidth, maxWidth, t);
        }
        else
        {
            // 悬停时保持最亮
            currentWidth = maxWidth;
        }

        // 应用宽度到当前物体的 Outline
        if (outline != null)
        {
            outline.OutlineWidth = currentWidth;
            // 颜色已在初始化设置，无需重复
        }
    }
}