using UnityEngine;

/// <summary>
/// 第一人称走路摇晃 + 眼高锁定。
/// - 相机世界高度固定为 groundY + eyeHeight，不随 CharacterController 被顶起/踩踏而改变；
/// - 按移动速度产生步伐起伏、左右侧摆与轻微 roll；
/// - 玩家不能移动（过场 / 相机被脚本接管）时完全不动相机，避免覆盖过场动画。
/// </summary>
[DisallowMultipleComponent]
public class HeadBob : MonoBehaviour
{
    [Header("引用")]
    public PlayerMovement movement;

    [Header("高度锁定")]
    [Tooltip("把相机世界高度锁死在 groundY + eyeHeight")]
    public bool lockEyeHeight = true;
    [Tooltip("眼睛离地面的绝对高度（m）")]
    public float eyeHeight = 1.36f;
    [Tooltip("地面基准高度（世界 Y），迷宫地面为 0")]
    public float groundY = 0f;

    [Header("步伐")]
    [Tooltip("正常速度下的步频（步/秒）")]
    public float stepRate = 1.85f;
    [Tooltip("垂直起伏幅度（m）")]
    public float bobAmplitude = 0.027f;
    [Tooltip("左右侧摆幅度（m）")]
    public float swayAmplitude = 0.016f;
    [Tooltip("身体 roll 幅度（度）")]
    public float rollAmplitude = 0.32f;
    [Tooltip("启动/停止时摇晃的平滑响应")]
    public float response = 9f;

    private float phase;
    private float bobWeight;
    private bool warned;

    private void Start()
    {
        if (movement == null) movement = GetComponentInParent<PlayerMovement>();
    }

    private void LateUpdate()
    {
        if (movement == null || !movement.canMove)
        {
            bobWeight = 0f;
            return;
        }

        float dt = Time.deltaTime;
        float speed01 = Mathf.Clamp01(Mathf.Abs(movement.CurrentSpeed) / Mathf.Max(0.01f, movement.moveSpeed * 1.35f));
        bobWeight = Mathf.Lerp(bobWeight, speed01, 1f - Mathf.Exp(-response * dt));

        if (bobWeight > 0.001f)
            phase += dt * stepRate * Mathf.PI * 2f;

        float bobY = Mathf.Sin(phase) * bobAmplitude * bobWeight;
        float sway = Mathf.Sin(phase * 0.5f) * swayAmplitude * bobWeight;
        float roll = Mathf.Sin(phase * 0.5f) * rollAmplitude * bobWeight;

        Vector3 root = movement.transform.position;
        Vector3 p = transform.position;
        Vector3 right = transform.right;
        p.x = root.x + right.x * sway;
        p.z = root.z + right.z * sway;
        p.y = (lockEyeHeight ? groundY + eyeHeight : root.y + eyeHeight) + bobY;
        transform.position = p;

        // 保留 MouseLook 的俯仰，叠加走路 roll
        float pitch = transform.localEulerAngles.x;
        if (pitch > 180f) pitch -= 360f;
        transform.localRotation = Quaternion.Euler(pitch, 0f, roll);
    }

    /// <summary>过场等场景可临时关掉摇晃（保留高度锁定）。</summary>
    public void SetBobEnabled(bool on)
    {
        enabled = on;
        if (!on) bobWeight = 0f;
    }
}
