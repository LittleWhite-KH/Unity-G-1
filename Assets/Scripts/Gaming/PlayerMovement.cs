using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public bool canMove = false;                // 默认关闭，由其他脚本开启
    public float boostMultiplier = 1.2f;        // 加速倍数（20%）

    private CharacterController controller;
    private float currentSpeedMultiplier = 1f;   // 当前速度倍数

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // 检测 F 键是否按住
        if (Keyboard.current != null)
        {
            if (Keyboard.current.fKey.isPressed)
                currentSpeedMultiplier = boostMultiplier;
            else
                currentSpeedMultiplier = 1f;
        }

        if (!canMove) return;

        // 读取 WASD 输入
        Vector2 moveInput = Vector2.zero;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) moveInput.y += 1f;
            if (Keyboard.current.sKey.isPressed) moveInput.y -= 1f;
            if (Keyboard.current.aKey.isPressed) moveInput.x -= 1f;
            if (Keyboard.current.dKey.isPressed) moveInput.x += 1f;
        }

        // 移动方向（基于玩家朝向）
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        // 应用加速倍数
        controller.Move(move * moveSpeed * currentSpeedMultiplier * Time.deltaTime);
    }

    public void SetMoveEnabled(bool enabled)
    {
        canMove = enabled;
    }
}