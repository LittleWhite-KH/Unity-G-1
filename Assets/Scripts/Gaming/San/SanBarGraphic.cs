using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// SAN 波形部件：自绘 <see cref="barCount"/> 根竖状长方形，用来表示 SAN 0-100。
///
/// 为什么自绘而不是 100 个 Image：100 个 Image = 100 个 GameObject + 100 个 CanvasRenderer，
/// 改一次形状要动 100 个组件；自绘只需要 1 个对象、1 个 DrawCall、400 个顶点。
///
/// 高度公式（每个参数都在 Inspector 里可调）：
///   amplitude = (对称 ? 半高 : 全高) × (value/100) × heightScale
///               × envelope(t) × (1 + noise) × impact
///
/// 三种"形态"分别由谁控制：
///   · value      —— SAN 数值（0-100），整体高度
///   · stability  —— 平的 → 平稳波形（开场动画：0 → 1）
///   · impact     —— "全部变长"那一下（策划案 Part1-3 / Part2-3）
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(CanvasRenderer))]
public class SanBarGraphic : MaskableGraphic
{
    [Header("柱体")]
    [Range(2, 200)] public int barCount = 100;

    [Tooltip("柱宽占间距的比例（0.5 ≈ 一半柱子一半空隙）")]
    [Range(0.05f, 1f)] public float barWidthRatio = 0.55f;

    [Tooltip("以中线为轴对称（参考图的样子）；关闭则从底部长上来")]
    public bool symmetric = true;

    [Tooltip("整体垂直偏移（像素）")]
    public float verticalOffset = 0f;

    [Header("数值映射（0-100）")]
    [Range(0f, 100f)] public float value = 0f;

    [Tooltip("柱子占半高的比例，1 = 满值时顶满上下边界")]
    [Range(0.1f, 1f)] public float heightScale = 1f;

    [Header("包络（波形轮廓）")]
    [Tooltip("每根柱子的相对高度轮廓，横轴 = 从左到右")]
    public AnimationCurve envelope = new AnimationCurve(
        new Keyframe(0f, 0.30f),
        new Keyframe(0.12f, 0.92f),
        new Keyframe(0.30f, 1f),
        new Keyframe(0.55f, 0.55f),
        new Keyframe(0.80f, 0.28f),
        new Keyframe(1f, 0.18f));

    [Tooltip("0 = 忽略包络（所有柱子等高，即\"平的\"）；1 = 完全按包络起伏")]
    [Range(0f, 1f)] public float envelopeStrength = 1f;

    [Tooltip("稳定性：0 = 平，1 = 完全进入平稳波形。开场\"平的 → 平稳\"动画驱动它")]
    [Range(0f, 1f)] public float stability = 0f;

    [Tooltip("波动强度 0-1：0 = 几乎不波动（柱子等高），1 = 剧烈波动。由 SanSystem 的分档驱动")]
    [Range(0f, 1f)] public float fluctuation = 1f;

    [Tooltip("波动为 0 时仍保留的最低抖动比例（保证任何时刻都有随机性）")]
    [Range(0f, 0.5f)] public float minJitterFloor = 0.15f;

    [Tooltip("凝聚度 0-1：1 = 所有柱子等高、无缝、拉满，合成一整块矩形（进入墓碑范围并看向它时）")]
    [Range(0f, 1f)] public float coalesce = 0f;

    [Header("方位模式（把整条柱子当成\"指南针\"）")]
    [Tooltip("开启后柱子高度由下面的方位参数决定（哪一侧高 = 目标在哪一侧），不再用包络")]
    public bool directionMode = false;

    [Tooltip("目标在整条上的位置：0.5 = 正前方，<0.5 = 在左侧，>0.5 = 在右侧")]
    [Range(0f, 1f)] public float directionCenter = 0.5f;

    [Tooltip("受影响范围的半宽（相对整条宽度）：越大 = 越多的柱子参与起伏")]
    [Range(0.01f, 1f)] public float directionWidth = 0.20f;

    [Tooltip("方位起伏强度 0-1：背对目标时它为 0 → 几乎没有起伏")]
    [Range(0f, 1f)] public float directionStrength = 1f;

    [Tooltip("抖动增益：越靠近目标越大 → 更多柱子、更快地抖动")]
    [Range(0f, 3f)] public float jitterBoost = 0f;

    [Tooltip("方位模式下的最小高度比例：SAN 再低也能看到方位提示（远处也要醒目）")]
    [Range(0f, 0.9f)] public float directionMinHeight = 0.45f;

    [Header("基础抖动（任何状态下都存在，保证\"像波形图\"）")]
    [Tooltip("绝对抖动强度：相对\"满值半高\"的比例。与 SAN 数值无关，所以平的状态下也有随机起伏")]
    [Range(0f, 0.5f)] public float baseJitter = 0.10f;

    [Tooltip("基础抖动频率（越高越细密）")]
    public float baseJitterFrequency = 22f;

    [Tooltip("基础抖动变化速度")]
    public float baseJitterSpeed = 2.5f;

    [Header("平稳阶段叠加的噪声")]
    [Range(0f, 1f)] public float noiseAmount = 0.25f;
    public float noiseFrequency = 9f;
    public float noiseSpeed = 1.2f;
    public float noiseSeed = 0f;

    [Header("中线")]
    public bool drawCenterLine = true;

    [Range(1f, 12f)] public float centerLineThickness = 2f;

    [Range(0f, 1f)] public float centerLineAlpha = 0.35f;

    [Header("冲击（\"全部变长\"）")]
    [Tooltip("当前冲击倍率，1 = 常态")]
    public float impact = 1f;

    [Header("性能")]
    [Tooltip("噪声随时间动，所以默认每帧重绘；关掉后只在参数变化时重绘")]
    public bool animateEveryFrame = true;

    // 内部
    private float lastValue;
    private float lastStability;
    private float lastImpact;
    private float lastFluctuation;
    private float lastCoalesce;
    private float noiseTime;

    // 柱高快照（0-1，相对满幅）：给"缩略图"每秒采样一次用
    private readonly float[] barHeights = new float[512];

    public int BarCount => barCount;

    /// <summary>第 i 根柱子当前的归一化高度（0-1）。供缩略图采样。</summary>
    public float GetBarHeight(int index)
    {
        if (index < 0 || index >= barCount || index >= barHeights.Length) return 0f;
        return barHeights[index];
    }

    protected override void Awake()
    {
        base.Awake();

        // 波形不需要接收鼠标点击
        raycastTarget = false;

        lastValue = value;
        lastStability = stability;
        lastImpact = impact;
    }

    private void Update()
    {
        // 噪声时间自己累加：这样改 jitterBoost 时不会因为 Time.time 跳变而抖一下
        noiseTime += Time.deltaTime * (1f + Mathf.Max(0f, jitterBoost));

        if (animateEveryFrame
            || !Mathf.Approximately(lastValue, value)
            || !Mathf.Approximately(lastStability, stability)
            || !Mathf.Approximately(lastImpact, impact)
            || !Mathf.Approximately(lastFluctuation, fluctuation)
            || !Mathf.Approximately(lastCoalesce, coalesce))
        {
            lastValue = value;
            lastStability = stability;
            lastImpact = impact;
            lastFluctuation = fluctuation;
            lastCoalesce = coalesce;
            SetVerticesDirty();
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (barCount < 1)
        {
            return;
        }

        Rect rect = rectTransform.rect;
        if (rect.width <= 0f || rect.height <= 0f)
        {
            return;
        }

        float pitch = rect.width / barCount;
        float halfHeight = rect.height * 0.5f;
        float centerY = rect.center.y + verticalOffset;
        float valueScale = Mathf.Clamp01(value / 100f) * heightScale;
        float fullAmplitude = (symmetric ? halfHeight : rect.height);

        float c = Mathf.Clamp01(coalesce);

        // 形态量：开场稳定性 与 SAN 波动强度 取较小者；凝聚时归零（→ 所有柱子等高）
        float shapeAmount = Mathf.Clamp01(Mathf.Min(stability, fluctuation));
        shapeAmount = Mathf.Lerp(shapeAmount, 0f, c);

        // 凝聚时柱宽填满间距、数值拉满 —— 视觉上合成一整块矩形
        float widthRatioEff = Mathf.Lerp(Mathf.Clamp01(barWidthRatio), 1f, c);
        float barWidth = Mathf.Max(1f, pitch * widthRatioEff);

        // 方位模式下给一个最小高度：SAN 很低时也看得到"目标在哪一侧"
        float valueScaleEff = directionMode ? Mathf.Max(valueScale, directionMinHeight) : valueScale;
        float maxAmplitude = fullAmplitude * Mathf.Lerp(valueScaleEff, 1f, c);

        // 抖动：方位模式下由每根柱子自己的 profile 决定强弱；凝聚时清零
        float coalesceFade = 1f - c;
        float baseJitterPixels = fullAmplitude * Mathf.Clamp01(baseJitter) * coalesceFade;

        for (int i = 0; i < barCount; i++)
        {
            float t = barCount == 1 ? 0.5f : i / (float)(barCount - 1);

            // 每根柱子的"形态"：方位模式下 = 以目标位置为中心的钟形；否则用包络
            float profile;
            if (directionMode)
            {
                float distanceFromCenter = Mathf.Abs(t - directionCenter);
                float falloff = directionWidth <= 0.0001f
                    ? 0f
                    : Mathf.Clamp01(1f - distanceFromCenter / directionWidth);

                falloff = falloff * falloff * (3f - 2f * falloff);   // smoothstep
                profile = falloff * Mathf.Clamp01(directionStrength);
            }
            else
            {
                float envelopeValue = Mathf.Max(0f, envelope != null ? envelope.Evaluate(t) : 1f);
                profile = Mathf.Lerp(1f, envelopeValue, Mathf.Clamp01(envelopeStrength) * shapeAmount);
            }

            // 凝聚时把所有柱子拉平成等高 —— 否则方位模式下只有目标附近几根高，
            // 永远拼不成一整块矩形（这是"到了墓碑前还是不变矩形"的根因）
            profile = Mathf.Lerp(profile, 1f, c);

            // 基础抖动：任何时刻都在；方位模式下按 profile 与 jitterBoost 放大
            float jitter = 0f;
            if (baseJitterPixels > 0f)
            {
                float jn = Mathf.PerlinNoise(t * baseJitterFrequency + noiseSeed, noiseTime * baseJitterSpeed + noiseSeed * 0.37f);
                float jitterScaleHere = Mathf.Max(profile, minJitterFloor) * (1f + Mathf.Max(0f, jitterBoost));
                jitter = (jn * 2f - 1f) * baseJitterPixels * jitterScaleHere;
            }

            // 附加噪声：随形态量增强
            float stageNoise = 0f;
            if (noiseAmount > 0f)
            {
                float n = Mathf.PerlinNoise(t * noiseFrequency + noiseSeed + 50f, noiseTime * noiseSpeed + noiseSeed);
                float noiseScaleHere = directionMode ? profile : shapeAmount;
                stageNoise = (n * 2f - 1f) * noiseAmount * noiseScaleHere;
            }

            float amplitude = maxAmplitude * profile * (1f + stageNoise) * Mathf.Max(0f, impact) + jitter;
            amplitude = Mathf.Max(0f, amplitude);

            // 记录快照，供缩略图采样
            if (i < barHeights.Length)
            {
                barHeights[i] = fullAmplitude > 0.01f ? Mathf.Clamp01(amplitude / fullAmplitude) : 0f;
            }

            float x0 = rect.xMin + i * pitch + (pitch - barWidth) * 0.5f;
            float x1 = x0 + barWidth;

            float y0;
            float y1;

            if (symmetric)
            {
                y0 = centerY - amplitude;
                y1 = centerY + amplitude;
            }
            else
            {
                y0 = rect.yMin;
                y1 = rect.yMin + amplitude;
            }

            AddQuad(vh, x0, x1, y0, y1, color);
        }

        if (drawCenterLine && centerLineAlpha > 0f)
        {
            Color lineColor = color;
            lineColor.a *= centerLineAlpha;

            float half = Mathf.Max(0.5f, centerLineThickness * 0.5f);
            AddQuad(vh, rect.xMin, rect.xMax, centerY - half, centerY + half, lineColor);
        }
    }

    private static void AddQuad(VertexHelper vh, float x0, float x1, float y0, float y1, Color32 vertexColor)
    {
        if (x1 <= x0 || y1 <= y0)
        {
            return;
        }

        int index = vh.currentVertCount;

        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = vertexColor;
        vertex.uv0 = new Vector2(0.5f, 0.5f);

        vertex.position = new Vector3(x0, y0, 0f);
        vh.AddVert(vertex);

        vertex.position = new Vector3(x0, y1, 0f);
        vh.AddVert(vertex);

        vertex.position = new Vector3(x1, y1, 0f);
        vh.AddVert(vertex);

        vertex.position = new Vector3(x1, y0, 0f);
        vh.AddVert(vertex);

        vh.AddTriangle(index, index + 1, index + 2);
        vh.AddTriangle(index, index + 2, index + 3);
    }

    // ------------------------------------------------------------------
    // 对外接口
    // ------------------------------------------------------------------

    /// <summary>设置 SAN 数值（0-100）。</summary>
    public void SetValue(float sanValue)
    {
        value = Mathf.Clamp(sanValue, 0f, 100f);
    }

    /// <summary>设置稳定性（0 = 平，1 = 平稳波形）。</summary>
    public void SetStability(float value01)
    {
        stability = Mathf.Clamp01(value01);
    }

    /// <summary>设置波动强度（0 = 几乎不波动，1 = 剧烈）。</summary>
    public void SetFluctuation(float value01)
    {
        fluctuation = Mathf.Clamp01(value01);
    }

    /// <summary>设置凝聚度（1 = 合成一整块矩形）。</summary>
    public void SetCoalesce(float value01)
    {
        coalesce = Mathf.Clamp01(value01);
    }

    /// <summary>方位模式：目标在整条上的位置 / 影响半宽 / 强度。</summary>
    public void SetDirection(float center01, float width01, float strength01)
    {
        directionMode = true;
        directionCenter = Mathf.Clamp01(center01);
        directionWidth = Mathf.Clamp(width01, 0.01f, 1f);
        directionStrength = Mathf.Clamp01(strength01);
    }

    /// <summary>抖动增益（越靠近目标越大：更多柱子、更快抖动）。</summary>
    public void SetJitterBoost(float value)
    {
        jitterBoost = Mathf.Max(0f, value);
    }

    /// <summary>设置冲击倍率（1 = 常态）。</summary>
    public void SetImpact(float multiplier)
    {
        impact = Mathf.Max(0f, multiplier);
    }

    /// <summary>一次性"全部变长"：冲到 peak 再回到 1。策划案里持续 1.2 秒。</summary>
    public void PlayImpact(float peak = 1.9f, float duration = 1.2f)
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        StopAllCoroutines();
        StartCoroutine(ImpactRoutine(peak, duration));
    }

    private IEnumerator ImpactRoutine(float peak, float duration)
    {
        float half = Mathf.Max(0.01f, duration * 0.5f);

        float elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            impact = Mathf.Lerp(1f, peak, Mathf.SmoothStep(0f, 1f, elapsed / half));
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            impact = Mathf.Lerp(peak, 1f, Mathf.SmoothStep(0f, 1f, elapsed / half));
            yield return null;
        }

        impact = 1f;
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        SetVerticesDirty();
    }
#endif
}
