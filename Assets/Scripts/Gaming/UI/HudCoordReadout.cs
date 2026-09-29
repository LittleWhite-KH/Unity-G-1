using TMPro;
using UnityEngine;

/// <summary>
/// 右下角坐标读数（参考图里的 X / Y / Z）：每帧把目标 Transform 的世界坐标写进 TMP。
/// 纯显示，不做任何逻辑判断。
/// </summary>
[DisallowMultipleComponent]
public class HudCoordReadout : MonoBehaviour
{
    [Tooltip("要读取坐标的目标，通常拖 Player")]
    public Transform target;

    [Tooltip("坐标文本")]
    public TextMeshProUGUI coordText;

    [Tooltip("显示格式，{0}{1}{2} 依次是 X / Y / Z")]
    public string format = "X: {0:0.#}   Y: {1:0.#}   Z: {2:0.#}";

    [Tooltip("多少秒刷新一次（0 = 每帧）")]
    public float interval = 0.1f;

    private float timer;

    private void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.Find("Player");
            if (player != null) target = player.transform;
        }

        Refresh();
    }

    private void Update()
    {
        if (interval > 0f)
        {
            timer += Time.deltaTime;
            if (timer < interval) return;
            timer = 0f;
        }

        Refresh();
    }

    private void Refresh()
    {
        if (target == null || coordText == null) return;

        Vector3 p = target.position;
        coordText.text = string.Format(format, p.x, p.y, p.z);
    }
}
