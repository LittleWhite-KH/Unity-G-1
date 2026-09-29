using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// SAN 缩略图：**每秒**对 <see cref="SanBarGraphic"/> 大条做一次"总结"，
/// 压成 <see cref="barCount"/> 根**没有间距**的长方形，实时反映大条的整体形态。
///
/// 采样方式：把大条的柱子等分成 N 段，每段取**峰值**（比取平均更能保留起伏的"包络"）。
/// 秒级更新是刻意的：它像心率监护仪那样"每秒落一格"，而不是跟着大条每帧抖。
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(CanvasRenderer))]
public class SanMiniGraphic : MaskableGraphic
{
    [Header("数据源")]
    [Tooltip("要总结的大条；留空则自动找场景里的 SanBarGraphic")]
    public SanBarGraphic source;

    [Header("样式")]
    [Tooltip("缩略图用几根长方形（无间距）")]
    [Range(2, 32)] public int barCount = 10;

    [Tooltip("每多少秒总结一次")]
    public float updateInterval = 1f;

    public Color barColor = Color.white;

    [Tooltip("最矮也保留一点高度，避免全平的时候整块消失")]
    [Range(0f, 0.2f)] public float minHeightRatio = 0.04f;

    private readonly float[] snapshot = new float[64];
    private float timer;
    private int lastBarCount = -1;

    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;

        if (source == null) source = FindObjectOfType<SanBarGraphic>();
        Sample();
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            if (source == null) source = FindObjectOfType<SanBarGraphic>();
            return;
        }

        timer += Time.deltaTime;
        if (timer < Mathf.Max(0.05f, updateInterval)) return;

        timer = 0f;
        Sample();
        SetVerticesDirty();
    }

    /// <summary>把大条压成 barCount 根：每段取峰值。</summary>
    public void Sample()
    {
        if (source == null) return;

        int srcCount = Mathf.Max(1, source.BarCount);
        int n = Mathf.Clamp(barCount, 2, snapshot.Length);

        for (int i = 0; i < n; i++)
        {
            int from = Mathf.FloorToInt(i * srcCount / (float)n);
            int to = Mathf.Max(from + 1, Mathf.FloorToInt((i + 1) * srcCount / (float)n));

            float peak = 0f;
            for (int s = from; s < to && s < srcCount; s++)
            {
                peak = Mathf.Max(peak, source.GetBarHeight(s));
            }

            snapshot[i] = Mathf.Max(peak, minHeightRatio);
        }

        lastBarCount = n;
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        int n = Mathf.Clamp(barCount, 2, snapshot.Length);
        if (lastBarCount != n)
        {
            Sample();
        }

        Rect r = rectTransform.rect;
        float step = r.width / n;          // 无间距：每根正好占 1/n

        for (int i = 0; i < n; i++)
        {
            float h = Mathf.Clamp01(snapshot[i]) * r.height;
            if (h <= 0.01f) continue;

            float x0 = r.xMin + i * step;
            float x1 = x0 + step;
            float y0 = r.yMin;
            float y1 = r.yMin + h;

            int start = vh.currentVertCount;

            vh.AddVert(new Vector3(x0, y0), barColor, Vector2.zero);
            vh.AddVert(new Vector3(x0, y1), barColor, Vector2.up);
            vh.AddVert(new Vector3(x1, y1), barColor, Vector2.one);
            vh.AddVert(new Vector3(x1, y0), barColor, Vector2.right);

            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        Sample();
    }
#endif
}
