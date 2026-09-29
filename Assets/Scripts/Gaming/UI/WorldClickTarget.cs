using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// 点击场景里的某个物体（例如墓碑）触发事件。
///
/// 用途：充能完成后提示"点击墓碑"，玩家点击后进入下一关。
/// 注意：鼠标悬停在 UI 上时不响应（避免点 BACK 按钮顺带点到世界物体）。
/// </summary>
[DisallowMultipleComponent]
public class WorldClickTarget : MonoBehaviour
{
    [Header("目标")]
    [Tooltip("要点击的物体（它的子物体也算命中）")]
    public Transform target;

    public Camera playerCamera;

    [Tooltip("超过该距离点击无效")]
    public float maxDistance = 15f;

    [Tooltip("是否允许点击（关卡逻辑控制）")]
    public bool clickEnabled = false;

    [Header("事件")]
    public UnityEvent onClicked;

    [Header("调试")]
    public bool logClicks = true;

    public bool Clicked { get; private set; }

    private void Awake()
    {
        if (playerCamera == null) playerCamera = Camera.main;
    }

    private void Update()
    {
        if (!clickEnabled || Clicked)
        {
            return;
        }

        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        // 点在 UI 上就不算
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Camera cam = playerCamera != null ? playerCamera : Camera.main;
        if (cam == null || target == null)
        {
            return;
        }

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, 500f))
        {
            return;
        }

        if (hit.transform != target && !hit.transform.IsChildOf(target))
        {
            return;
        }

        if (maxDistance > 0f)
        {
            float distance = Vector3.Distance(cam.transform.position, hit.point);
            if (distance > maxDistance)
            {
                if (logClicks)
                {
                    Debug.Log($"[WorldClickTarget] 命中了 {hit.transform.name}，但距离 {distance:0.#}m 超过 {maxDistance}m，忽略。");
                }

                return;
            }
        }

        Clicked = true;

        if (logClicks)
        {
            Debug.Log($"[WorldClickTarget] 已点击 {hit.transform.name}");
        }

        onClicked?.Invoke();
    }

    public void Arm()
    {
        clickEnabled = true;
        Clicked = false;
    }

    public void Disarm()
    {
        clickEnabled = false;
    }
}
