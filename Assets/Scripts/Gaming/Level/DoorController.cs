using UnityEngine;

/// <summary>
/// 特殊门：开门时禁用门下所有碰撞体，并把门板向上滑动。
/// 挂在门的 GUID 根物体上，leaf 指向门板（留空则滑动整扇门）。
/// </summary>
public class DoorController : MonoBehaviour
{
    [Header("门板")]
    public Transform leaf;

    [Tooltip("开门后门板相对初始位置的位移（本地坐标）")]
    public Vector3 openOffset = new Vector3(0f, 2.4f, 0f);

    public float openDuration = 1.2f;
    public bool logOpen = true;

    public bool IsOpen { get; private set; }

    private Vector3 closedWorldPosition;
    private Collider[] colliders;
    private float progress;

    private void Awake()
    {
        if (leaf == null)
        {
            leaf = transform;
        }

        closedWorldPosition = leaf.position;
        colliders = GetComponentsInChildren<Collider>(true);
    }

    public void Open()
    {
        if (IsOpen)
        {
            return;
        }

        IsOpen = true;
        progress = 0f;

        if (colliders != null)
        {
            foreach (Collider c in colliders)
            {
                if (c != null)
                {
                    c.enabled = false;
                }
            }
        }

        if (logOpen)
        {
            Debug.Log($"[Door] opening {name} (leaf={(leaf != null ? leaf.name : "null")}, colliders={(colliders != null ? colliders.Length : 0)})");
        }
    }

    private void Update()
    {
        if (!IsOpen || leaf == null || progress >= 1f)
        {
            return;
        }

        progress += Time.deltaTime / Mathf.Max(0.01f, openDuration);
        float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress));
        leaf.position = Vector3.Lerp(closedWorldPosition, closedWorldPosition + openOffset, t);
    }
}
