using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 小地图（纯 Unity UI 搭建，不使用切图素材）。
///   · 把玩家在 <see cref="worldMin"/>~<see cref="worldMax"/> 世界范围里的位置，
///     归一化后映射到 <see cref="mapArea"/> 这块矩形里；
///   · <see cref="playerMarker"/> 可选跟随朝向旋转（<see cref="rotateMarker"/>）。
///
/// 房间/走廊的轮廓由场景里预先摆好的小方块充当（装饰用），本脚本只管玩家标记。
/// </summary>
[DisallowMultipleComponent]
public class MiniMapUI : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("地图内容区：世界坐标归一化后映射到这块矩形")]
    public RectTransform mapArea;

    [Tooltip("玩家标记")]
    public RectTransform playerMarker;

    public Transform player;

    [Header("世界范围")]
    public Vector2 worldMin = new Vector2(-40f, -40f);
    public Vector2 worldMax = new Vector2(40f, 40f);

    [Header("行为")]
    [Tooltip("是否让标记跟着玩家朝向旋转")]
    public bool rotateMarker = false;

    [Tooltip("超出范围时把标记钳在边缘")]
    public bool clampToArea = true;

    public float smoothSpeed = 10f;

    [Tooltip("朝向来源，留空则用 Main Camera")]
    public Transform facingSource;

    private Vector2 displayPos = new Vector2(0.5f, 0.5f);

    private void Awake()
    {
        if (player == null)
        {
            GameObject go = GameObject.Find("Player");
            if (go != null) player = go.transform;
        }

        if (facingSource == null && Camera.main != null) facingSource = Camera.main.transform;
    }

    private void Update()
    {
        if (player == null || mapArea == null || playerMarker == null) return;

        Vector3 p = player.position;

        float rx = Mathf.Max(0.01f, worldMax.x - worldMin.x);
        float rz = Mathf.Max(0.01f, worldMax.y - worldMin.y);

        float nx = (p.x - worldMin.x) / rx;
        float nz = (p.z - worldMin.y) / rz;

        if (clampToArea)
        {
            nx = Mathf.Clamp01(nx);
            nz = Mathf.Clamp01(nz);
        }

        Vector2 target = new Vector2(nx, nz);
        displayPos = smoothSpeed > 0f
            ? Vector2.Lerp(displayPos, target, 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime))
            : target;

        playerMarker.anchorMin = displayPos;
        playerMarker.anchorMax = displayPos;
        playerMarker.anchoredPosition = Vector2.zero;

        if (rotateMarker && facingSource != null)
        {
            Vector3 f = facingSource.forward;
            float yaw = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
            playerMarker.localRotation = Quaternion.Euler(0f, 0f, -yaw);
        }
    }

    /// <summary>用场景里现有物体的范围自动适配世界区间（编辑器里调用方便）。</summary>
    public void FitWorldRangeToScene()
    {
        var renderers = FindObjectsOfType<Renderer>();
        if (renderers == null || renderers.Length == 0) return;

        Bounds b = renderers[0].bounds;
        foreach (Renderer r in renderers)
        {
            if (r == null || !r.enabled) continue;
            b.Encapsulate(r.bounds);
        }

        worldMin = new Vector2(b.min.x, b.min.z);
        worldMax = new Vector2(b.max.x, b.max.z);
    }
}
