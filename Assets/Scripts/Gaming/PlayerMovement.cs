using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 3.4f;
    public bool canMove = false;
    public float boostMultiplier = 1.35f;

    [Header("缓动启停")]
    public float acceleration = 14f;
    public float deceleration = 18f;

    [Header("重力")]
    [Tooltip("重力加速度（负数）。没有重力时踩到东西会被 stepOffset 顶上去且下不来")]
    public float gravity = -22f;
    [Tooltip("贴地时保持下落用的速度，避免走下坡时漂浮")]
    public float groundStick = -2f;

    private CharacterController controller;
    private float currentSpeed;
    private Vector3 currentVelocity;
    private float verticalVelocity;

    /// <summary>当前水平速度（m/s），供 HeadBob 等读取。</summary>
    public float CurrentSpeed => Mathf.Abs(currentSpeed);

    private void Start()
    {
        GameSettings.Load();
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        if (!canMove)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * dt);
            currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, 1f - Mathf.Exp(-18f * dt));
            ApplyGravity(dt);
            return;
        }

        Vector2 moveInput = Vector2.zero;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) moveInput.y += 1f;
            if (Keyboard.current.sKey.isPressed) moveInput.y -= 1f;
            if (Keyboard.current.aKey.isPressed) moveInput.x -= 1f;
            if (Keyboard.current.dKey.isPressed) moveInput.x += 1f;
        }

        if (moveInput.sqrMagnitude > 1f) moveInput.Normalize();

        float boost = Keyboard.current != null && Keyboard.current.fKey.isPressed ? boostMultiplier : 1f;
        float target = moveSpeed * boost * Mathf.Max(0.2f, GameSettings.MoveSpeed);

        if (moveInput.sqrMagnitude > 0.001f)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, target, acceleration * dt);
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * dt);
        }

        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        if (move.sqrMagnitude > 1f) move.Normalize();

        // 方向缓动：避免瞬间变向的僵硬感
        Vector3 desired = move * currentSpeed * dt;
        currentVelocity = Vector3.Lerp(currentVelocity, desired, 1f - Mathf.Exp(-18f * dt));

        ApplyGravity(dt);
    }

    private void ApplyGravity(float dt)
    {
        if (controller == null) return;
        // 组件/对象被临时禁用时不要 Move（编辑器里会报 inactive controller）
        if (!controller.enabled || !controller.gameObject.activeInHierarchy) return;

        // CharacterController 对 MeshCollider 地板的 isGrounded 偶尔会是 false，
        // 用 collisionFlags.Below 兜底，避免竖直速度一直累积成"掉穿地板"。
        bool grounded = controller.isGrounded || (controller.collisionFlags & CollisionFlags.Below) != 0;
        if (grounded && verticalVelocity < 0f) verticalVelocity = groundStick;

        verticalVelocity += gravity * dt;
        if (verticalVelocity < -30f) verticalVelocity = -30f;

        Vector3 delta = currentVelocity;
        delta.y = verticalVelocity * dt;
        controller.Move(delta);
    }

    public void SetMoveEnabled(bool enabled)
    {
        canMove = enabled;
    }
}
