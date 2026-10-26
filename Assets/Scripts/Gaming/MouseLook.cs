using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    [Header("灵敏度")]
    [Tooltip("基础灵敏度（度/像素）。设置面板里再乘 GameSettings.Sensitivity")]
    public float mouseSensitivity = 0.12f;

    [Header("水平缓动（平滑）")]
    public float horizontalSmoothTime = 0.005f;
    private float mouseXVelocity = 0f;

    [Header("垂直阻尼（俯仰限制）")]
    public float dampStartAngle = 60f;
    public float dampMaxAngle = 80f;

    public Transform cameraTransform;
    public bool canControl = false;

    private float xRotation = 0f;

    private void Start()
    {
        GameSettings.Load();
        Cursor.lockState = CursorLockMode.Locked;

        if (cameraTransform == null)
        {
            Debug.LogError("MouseLook: cameraTransform 未赋值！");
            return;
        }

        // 保留场景里配置的出生朝向（不要强制 identity）
        xRotation = 0f;
        cameraTransform.localRotation = Quaternion.identity;
        ApplyFov();
    }

    private void Update()
    {
        if (!canControl || cameraTransform == null) return;

        ApplyFov();

        float rawMouseX = 0f, rawMouseY = 0f;
        if (Mouse.current != null)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            float scale = mouseSensitivity * GameSettings.SensitivityMultiplier;
            rawMouseX = delta.x * scale;
            rawMouseY = delta.y * scale * (GameSettings.InvertY ? -1f : 1f);
        }

        float targetYaw = transform.eulerAngles.y + rawMouseX;
        float currentYaw = transform.eulerAngles.y;
        float newYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref mouseXVelocity, horizontalSmoothTime);
        transform.rotation = Quaternion.Euler(0f, newYaw, 0f);

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

    private void ApplyFov()
    {
        Camera cam = cameraTransform != null ? cameraTransform.GetComponent<Camera>() : null;
        if (cam != null && !Mathf.Approximately(cam.fieldOfView, GameSettings.Fov))
        {
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, GameSettings.Fov, 1f - Mathf.Exp(-8f * Time.deltaTime));
        }
    }

    public void SetControlEnabled(bool enabled)
    {
        canControl = enabled;
    }
}
