using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class UIWaveform : MaskableGraphic
{
    [Header("波形精度")]
    [Range(10, 500)]
    public int pointCount = 150;

    [Header("线条")]
    public float lineWidth = 3f;

    [Header("整体强度")]
    [Range(0f, 1f)]
    public float intensity = 0.5f;

    public float intensitySmoothSpeed = 5f;

    private float currentIntensity;

    [Header("基础波形")]
    public float amplitude = 40f;
    public float frequency = 3f;
    public float waveSpeed = 2f;

    [Header("随机波动")]
    public float randomStrength = 30f;
    public float randomFrequency = 5f;
    public float randomSpeed = 1f;

    [Header("细节噪声")]
    public float detailStrength = 10f;
    public float detailFrequency = 20f;
    public float detailSpeed = 3f;

    protected override void Awake()
    {
        base.Awake();

        currentIntensity = intensity;

        // 不需要接收鼠标点击
        raycastTarget = false;
    }

    private void Update()
    {
        currentIntensity = Mathf.Lerp(
            currentIntensity,
            intensity,
            Time.deltaTime * intensitySmoothSpeed
        );

        // 通知 Unity 重新生成 UI Mesh
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (pointCount < 2)
            return;

        Rect rect = rectTransform.rect;

        Vector2 previousPoint = Vector2.zero;

        for (int i = 0; i < pointCount; i++)
        {
            float t = i / (float)(pointCount - 1);

            // X 坐标
            float x = Mathf.Lerp(
                rect.xMin,
                rect.xMax,
                t
            );

            // =========================
            // 基础波形
            // =========================

            float sine =
                Mathf.Sin(
                    t * Mathf.PI * 2f * frequency
                    + Time.time * waveSpeed
                );

            // =========================
            // 大范围随机变化
            // =========================

            float noise =
                Mathf.PerlinNoise(
                    t * randomFrequency,
                    Time.time * randomSpeed
                );

            noise = noise * 2f - 1f;

            // =========================
            // 小范围随机细节
            // =========================

            float detail =
                Mathf.PerlinNoise(
                    t * detailFrequency + 100f,
                    Time.time * detailSpeed
                );

            detail = detail * 2f - 1f;

            // =========================
            // 最终高度
            // =========================

            float y =
                sine * amplitude
                + noise * randomStrength
                + detail * detailStrength;

            // 根据强度控制整体波动
            y *= currentIntensity;

            Vector2 point = new Vector2(
                x,
                rect.center.y + y
            );

            if (i > 0)
            {
                AddLine(
                    vh,
                    previousPoint,
                    point,
                    lineWidth
                );
            }

            previousPoint = point;
        }
    }

    /// <summary>
    /// 绘制一段 UI 线条
    /// </summary>
    private void AddLine(
        VertexHelper vh,
        Vector2 start,
        Vector2 end,
        float width
    )
    {
        Vector2 direction = (end - start).normalized;

        Vector2 normal =
            new Vector2(
                -direction.y,
                direction.x
            ) * width * 0.5f;

        int index = vh.currentVertCount;

        UIVertex vertex = UIVertex.simpleVert;

        vertex.color = color;

        vertex.position = start - normal;
        vh.AddVert(vertex);

        vertex.position = start + normal;
        vh.AddVert(vertex);

        vertex.position = end + normal;
        vh.AddVert(vertex);

        vertex.position = end - normal;
        vh.AddVert(vertex);

        vh.AddTriangle(
            index,
            index + 1,
            index + 2
        );

        vh.AddTriangle(
            index,
            index + 2,
            index + 3
        );
    }

    // =========================
    // 对外接口
    // =========================

    public void SetIntensity(float value)
    {
        intensity = Mathf.Clamp01(value);
    }
}