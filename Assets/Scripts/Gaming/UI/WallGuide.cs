using TMPro;
using UnityEngine;

/// <summary>
/// 墙上的英文指引：玩家进入范围后淡入、停留、淡出。
/// 摆放规则：文字面与墙面保持 2~3cm 距离（防 z-fighting），由摆放工具保证。
/// </summary>
[RequireComponent(typeof(TextMeshPro))]
public class WallGuide : MonoBehaviour
{
    [Header("触发")]
    [Tooltip("玩家进入这个半径后显示（米）")]
    public float triggerRadius = 7f;

    [Tooltip("只显示一次")]
    public bool triggerOnce = true;

    [Tooltip("场景一开始就显示")]
    public bool showOnStart = false;

    [Tooltip("触发后延迟多久开始淡入（秒）")]
    public float delay = 0f;

    [Header("缓动")]
    public float fadeIn = 0.28f;
    public float hold = 4.5f;
    public float fadeOut = 0.45f;

    [Tooltip("淡入时轻微上浮的距离（米）")]
    public float riseDistance = 0.03f;

    [Header("目标")]
    public Transform player;

    private TextMeshPro text;
    private Vector3 homeLocalPosition;
    private float stateTime;
    private int state;          // 0=hidden 1=fadeIn 2=hold 3=fadeOut 4=done
    private bool fired;

    private void Awake()
    {
        text = GetComponent<TextMeshPro>();
        homeLocalPosition = transform.localPosition;
        SetAlpha(0f);
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject go = GameObject.Find("Player");
            if (go != null) player = go.transform;
        }

        if (showOnStart) Fire();
    }

    private void Update()
    {
        if (!fired && player != null)
        {
            if (Vector3.Distance(player.position, transform.position) <= triggerRadius)
            {
                Fire();
            }
        }

        Tick(Time.deltaTime);
    }

    /// <summary>外部（关卡脚本 / 触发器）也能主动打开。</summary>
    public void Fire()
    {
        if (fired && triggerOnce) return;

        fired = true;
        state = 1;
        stateTime = -Mathf.Max(0f, delay);
    }

    private void Tick(float dt)
    {
        if (state == 0 || state == 4) return;

        stateTime += dt;

        if (state == 1)
        {
            if (stateTime < 0f) return;

            float t = fadeIn <= 0.001f ? 1f : Mathf.Clamp01(stateTime / fadeIn);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            SetAlpha(eased);
            transform.localPosition = homeLocalPosition + Vector3.up * (riseDistance * (1f - eased));

            if (t >= 1f)
            {
                state = 2;
                stateTime = 0f;
            }

            return;
        }

        if (state == 2)
        {
            if (stateTime >= hold)
            {
                state = 3;
                stateTime = 0f;
            }

            return;
        }

        if (state == 3)
        {
            float t = fadeOut <= 0.001f ? 1f : Mathf.Clamp01(stateTime / fadeOut);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            SetAlpha(1f - eased);

            if (t >= 1f)
            {
                state = 4;
                gameObject.SetActive(false);
            }
        }
    }

    private void SetAlpha(float a)
    {
        if (text == null) return;

        Color c = text.color;
        c.a = Mathf.Clamp01(a);
        text.color = c;
    }
}
