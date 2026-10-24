using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    [Header("灵敏度")]
    public float mouseSensitivity = 75f;

    [Header("水平缓动（平滑）")]
    public float horizontalSmoothTime = 0.005f;
    private float mouseXVelocity = 0f;

    [Header("垂直阻尼（俯仰限制）")]
    public float dampStartAngle = 60f;
    public float dampMaxAngle = 80f;

    public Transform cameraTransform;
    public bool canControl = false;

    private float xRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        if (cameraTransform == null)
        {
            Debug.LogError("MouseLook: cameraTransform 未赋值！");
            return;
        }

        // 初始旋转为水平 0°，俯仰 0°（可根据需要自行调整）
        transform.rotation = Quaternion.identity;
        xRotation = 0f;
        cameraTransform.localRotation = Quaternion.identity;
    }

    void Update()
    {
        if (!canControl || cameraTransform == null) return;

        // ---------- 读取鼠标输入 ----------
        float rawMouseX = 0f, rawMouseY = 0f;
        if (Mouse.current != null)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            rawMouseX = delta.x * mouseSensitivity * Time.deltaTime;
            rawMouseY = delta.y * mouseSensitivity * Time.deltaTime;
        }
        else
        {
            rawMouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            rawMouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        }

        // ---------- 水平旋转（带缓动） ----------
        float targetYaw = transform.eulerAngles.y + rawMouseX;
        float currentYaw = transform.eulerAngles.y;
        float newYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref mouseXVelocity, horizontalSmoothTime);
        transform.rotation = Quaternion.Euler(0f, newYaw, 0f);

        // ---------- 垂直旋转（带阻尼） ----------
        float absAngle = Mathf.Abs(xRotation);
        bool isMovingOutward = false;
        if (xRotation >= 0f && rawMouseY < 0f) isMovingOutward = true;
        else if (xRotation < 0f && rawMouseY > 0f) isMovingOutward = true;

        float dampFactor = 1f;
        if (isMovingOutward && absAngle > dampStartAngle)
        {
            dampFactor = 1f - (absAngle - dampStartAngle) / (dampMaxAngle - dampStartAngle);
            dampFactor = Mathf.Clamp01(dampFactor);
        }

        float verticalDelta = rawMouseY * dampFactor;
        xRotation -= verticalDelta;
        xRotation = Mathf.Clamp(xRotation, -dampMaxAngle, dampMaxAngle);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    public void SetControlEnabled(bool enabled)
    {
        canControl = enabled;
    }
}