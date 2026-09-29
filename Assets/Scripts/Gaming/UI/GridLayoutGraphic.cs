using UnityEngine;
using UnityEngine.UI;

namespace Outdoor.UI
{
    /// <summary>
    /// 地图网格：在自身矩形内画等距的横竖细线（参考图 9.Map.PNG 里那层很淡的方格）。
    ///
    /// 自绘而不是拼 Image：一张地图要 20+ 条线，拼 GameObject 不划算。
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasRenderer))]
    public class GridLayoutGraphic : MaskableGraphic
    {
        [Tooltip("格边长（像素）")]
        public float cellSize = 53f;

        [Tooltip("线宽")]
        public float lineThickness = 1f;

        [Tooltip("线颜色（含透明度；参考图是很淡的灰蓝）")]
        public Color lineColor = new Color(0.478f, 0.51f, 0.565f, 0.13f);

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect r = rectTransform.rect;
            if (r.width <= 1f || r.height <= 1f) return;

            float cell = Mathf.Max(4f, cellSize);
            float half = Mathf.Max(0.5f, lineThickness) * 0.5f;

            int cols = Mathf.CeilToInt(r.width / cell);
            int rows = Mathf.CeilToInt(r.height / cell);

            // 从中心向外对齐（参考图的网格是和面板居中对齐的）
            float x0 = r.center.x - cols * cell * 0.5f;
            float y0 = r.center.y - rows * cell * 0.5f;

            for (int i = 0; i <= cols; i++)
            {
                float x = x0 + i * cell;
                AddQuad(vh, new Vector2(x - half, r.yMin), new Vector2(x + half, r.yMax));
            }

            for (int j = 0; j <= rows; j++)
            {
                float y = y0 + j * cell;
                AddQuad(vh, new Vector2(r.xMin, y - half), new Vector2(r.xMax, y + half));
            }
        }

        private void AddQuad(VertexHelper vh, Vector2 a, Vector2 b)
        {
            int s = vh.currentVertCount;
            vh.AddVert(new Vector3(a.x, a.y, 0f), lineColor, Vector2.zero);
            vh.AddVert(new Vector3(b.x, a.y, 0f), lineColor, Vector2.zero);
            vh.AddVert(new Vector3(b.x, b.y, 0f), lineColor, Vector2.zero);
            vh.AddVert(new Vector3(a.x, b.y, 0f), lineColor, Vector2.zero);
            vh.AddTriangle(s, s + 1, s + 2);
            vh.AddTriangle(s, s + 2, s + 3);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetVerticesDirty();
        }
#endif
    }
}
