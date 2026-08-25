using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

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

    // ----- 开场动画参数（新添加） -----
    [Header("开场动画")]
    public float startPitch = 90f;     // 起始俯仰角（上下），单位度
    public float startYaw = -90f;      // 起始偏航角（左右），单位度
    public float endPitch = -90f;      // 结束俯仰角
    public float endYaw = -90f;        // 结束偏航角（如果需要变化，否则保持与起始一致）
    public float introDuration = 3f;   // 动画时长（秒）
    public float holdDuration = 1f;    // 停顿时间（秒）

    private float xRotation = 0f;
    private bool isIntroPlaying = true;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        if (cameraTransform == null)
        {
            Debug.LogError("MouseLook: cameraTransform 未赋值！");
            return;
        }

        // 设置初始旋转为 (startPitch, startYaw, 0)
        transform.rotation = Quaternion.Euler(startPitch, startYaw, 0f);
        // 同时设置 xRotation 为起始俯仰，以便后续同步
        xRotation = startPitch;
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // 启动开场协程
        StartCoroutine(IntroRoutine());
    }

    void Update()
    {
        if (!canControl || isIntroPlaying || cameraTransform == null) return;

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
        transform.rotation = Quaternion.Euler(0f, newYaw, 0f); // 注意：俯仰由 cameraTransform 控制

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

    IEnumerator IntroRoutine()
    {
        isIntroPlaying = true;

        float elapsed = 0f;
        float startPitchVal = startPitch;
        float endPitchVal = endPitch;

        // 如果希望偏航也变化，可以同时插值，这里保持固定为 startYaw，然后渐变到 endYaw（但通常仅俯仰变化）
        // 为了简单，偏航在开场期间保持不变（除非你明确要求变）
        transform.rotation = Quaternion.Euler(startPitch, startYaw, 0f);

        while (elapsed < introDuration)
        {
            float t = elapsed / introDuration;
            // 缓入曲线（t^2）
            float easeT = t * t;
            float currentPitch = Mathf.Lerp(startPitchVal, endPitchVal, easeT);
            // 插值偏航：若想同时变，可添加，这里保持恒定
            float currentYaw = Mathf.Lerp(startYaw, endYaw, easeT); // 如果不想变偏航，将 endYaw 设为 startYaw

            transform.rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
            xRotation = currentPitch;  // 同步

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 确保最终角度精确
        transform.rotation = Quaternion.Euler(endPitch, endYaw, 0f);
        xRotation = endPitch;

        // 停顿
        yield return new WaitForSeconds(holdDuration);

        // 结束开场，允许控制
        isIntroPlaying = false;
        canControl = true;
        Debug.Log("开场结束，玩家可控制摄像机。");
    }

    public void SetControlEnabled(bool enabled)
    {
        if (!isIntroPlaying)
            canControl = enabled;
    }
}