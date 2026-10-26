using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Ending 场景总控：
///   1) 玩家走进电视屏幕（越过屏幕平面）→ 白闪 → 传送到电视内部走廊；
///   2) 走廊尽头按 E 开门 → 白闪 → 加载 Start（回到开始界面）。
/// </summary>
public class EndingDirector : MonoBehaviour
{
    [Header("引用")]
    public Transform player;
    public DoorController door;
    public CanvasGroup flash;

    [Header("电视传送")]
    [Tooltip("传送后玩家所在位置（走廊入口）")]
    public Vector3 teleportTarget = new Vector3(0f, 0f, -77.4f);

    [Tooltip("屏幕中心（世界坐标）")]
    public Vector3 screenCenter = new Vector3(0f, 1.45f, 0f);

    [Tooltip("玩家越过这个 z 就触发传送")]
    public float screenTriggerZ = 0.35f;

    [Tooltip("屏幕半宽（允许的横向范围）")]
    public float screenHalfWidth = 1.35f;

    [Header("走廊 / 结束")]
    public Vector3 doorZone = new Vector3(0f, 1.1f, -82.5f);
    public float doorInteractDistance = 2.4f;
    public string endSceneName = "Start";

    private MouseLook look;
    private PlayerMovement move;
    private bool teleported;
    private bool ending;

    private void Start()
    {
        if (player != null)
        {
            look = player.GetComponent<MouseLook>();
            move = player.GetComponent<PlayerMovement>();
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (look != null) look.canControl = true;
        if (move != null) move.canMove = true;
        if (flash != null) flash.alpha = 0f;
    }

    private void Update()
    {
        if (player == null) return;

        if (!teleported)
        {
            Vector3 p = player.position;
            bool insideScreen = Mathf.Abs(p.x) < screenHalfWidth
                                && p.z < screenTriggerZ
                                && p.z > -1.2f
                                && p.y < 2.8f;

            if (insideScreen)
            {
                StartCoroutine(TeleportRoutine());
            }

            return;
        }

        if (!ending && door != null && door.IsOpen)
        {
            StartCoroutine(EndRoutine());
            return;
        }

        if (!ending && door != null && !door.IsOpen)
        {
            float d = Vector3.Distance(player.position, doorZone);
            bool pressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;

            if (pressed)
            {
                Debug.Log($"[Ending] E pressed at door, dist={d:0.##}, limit={doorInteractDistance}");
            }

            // E 交互为主；如果玩家已经贴到门前（<=1.1m）也自动开门，避免输入时序问题卡关
            if (d <= doorInteractDistance && (pressed || d <= 1.1f))
            {
                door.Open();
                Debug.Log($"[Ending] door opening (dist={d:0.##}, pressed={pressed})");
            }
        }
    }

    private IEnumerator TeleportRoutine()
    {
        teleported = true;
        SetControl(false);

        yield return Flash(1f, 0.14f);

        player.position = teleportTarget;
        player.rotation = Quaternion.Euler(0f, 180f, 0f);

        yield return new WaitForSeconds(0.10f);
        yield return Flash(0f, 0.45f);

        SetControl(true);
        Debug.Log("[Ending] walked into the screen, now inside the TV");
    }

    private IEnumerator EndRoutine()
    {
        ending = true;
        SetControl(false);

        yield return new WaitForSeconds(0.55f);
        yield return Flash(1f, 1.1f);
        yield return new WaitForSeconds(0.35f);

        Debug.Log("[Ending] door open, loading " + endSceneName);
        SceneManager.LoadScene(endSceneName);
    }

    private IEnumerator Flash(float target, float duration)
    {
        if (flash == null) yield break;

        float from = flash.alpha;
        float t = 0f;
        duration = Mathf.Max(0.01f, duration);

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            flash.alpha = Mathf.Lerp(from, target, Mathf.Clamp01(t / duration));
            yield return null;
        }

        flash.alpha = target;
    }

    private void SetControl(bool enabled)
    {
        if (look != null) look.canControl = enabled;
        if (move != null) move.canMove = enabled;
    }
}
