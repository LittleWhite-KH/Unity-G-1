using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Outdoor.UI
{
    /// <summary>
    /// 地图节点标记：按设计说明画四种图元。
    ///   · FilledCircle   实心圆  —— 已确认/已抵达的点
    ///   · HollowCircle   空心圆  —— 未确认的点
    ///   · FilledDiamond  实心菱形 —— 已抵达的目标点
    ///   · HollowDiamond  空心菱形 —— 未确认的目标点
    ///   · Triangle       三角形  —— 玩家（朝向 = 三角形指向）
    ///
    /// 为什么自绘：uGUI 没有菱形/三角形图元，用 Image + 旋转方块做菱形会在描边上出锯齿；
    /// 自绘一个 4/3 顶点的多边形最干净，且和 SanBarGraphic 一样的路子（1 个对象 1 个 DrawCall）。
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasRenderer))]
    public class MapNodeGraphic : MaskableGraphic
    {
        public enum Shape
        {
            FilledCircle = 0,
            HollowCircle = 1,
            FilledDiamond = 2,
            HollowDiamond = 3,
            Triangle = 4
        }

        [Header("形状")]
        public Shape shape = Shape.FilledCircle;

        [Tooltip("圆/菱形的外接半径（像素）")]
        public float radius = 9f;

        [Tooltip("描边宽度（空心图形用；0 = 用默认）")]
        public float outlineWidth = 2.5f;

        [Tooltip("圆形用多少段近似")]
        [Range(12, 96)] public int circleSegments = 40;

        [Header("外观")]
        [Tooltip("空心图形的内部是否需要挖空（false = 只画描边环）")]
        public bool hollowCenter = true;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            switch (shape)
            {
                case Shape.FilledCircle:
                    AddCircle(vh, radius, 0f);
                    break;

                case Shape.HollowCircle:
                    AddCircle(vh, radius, outlineWidth);
                    break;

                case Shape.FilledDiamond:
                    AddDiamond(vh, radius, 0f);
                    break;

                case Shape.HollowDiamond:
                    AddDiamond(vh, radius, outlineWidth);
                    break;

                case Shape.Triangle:
                    AddTriangle(vh, radius);
                    break;
            }
        }

        /// <summary>在中心 (0,0) 处放一个半径 r 的圆；innerWidth &gt; 0 时挖空成环。</summary>
        private void AddCircle(VertexHelper vh, float r, float innerWidth)
        {
            float rb = r;                       // 外半径
            float ri_circum = innerWidth > 0f ? Mathf.Max(0f, r - innerWidth) : 0f;

            int seg = Mathf.Max(12, circleSegments);
            int start = vh.currentVertCount;

            // 顶点：先中心（或内圈起点），再外圈
            if (innerWidth <= 0f)
            {
                vh.AddVert(Vector3.zero, color, new Vector2(0.5f, 0.5f));
                for (int i = 0; i <= seg; i++)
                {
                    float a = (i / (float)seg) * Mathf.PI * 2f;
                    Vector3 p = new Vector3(Mathf.Cos(a) * rb, Mathf.Sin(a) * rb, 0f);
                    vh.AddVert(p, color, new Vector2(0.5f + Mathf.Cos(a) * 0.5f, 0.5f + Mathf.Sin(a) * 0.5f));
                }

                for (int i = 0; i < seg; i++)
                {
                    vh.AddTriangle(start, start + 1 + i, start + 2 + i);
                }
            }
            else
            {
                // 环：内外两圈交替
                for (int i = 0; i <= seg; i++)
                {
                    float a = (i / (float)seg) * Mathf.PI * 2f;
                    vh.AddVert(new Vector3(Mathf.Cos(a) * rb, Mathf.Sin(a) * rb, 0f), color, Vector2.zero);
                    vh.AddVert(new Vector3(Mathf.Cos(a) * ri_circum, Mathf.Sin(a) * ri_circum, 0f), color, Vector2.zero);
                }

                for (int i = 0; i < seg; i++)
                {
                    int o = start + i * 2;
                    vh.AddTriangle(o, o + 1, o + 3);
                    vh.AddTriangle(o, o + 3, o + 2);
                }
            }
        }

        /// <summary>菱形：顶点在上下左右四个方向（对齐参考图的 ◇）。</summary>
        private void AddDiamond(VertexHelper vh, float r, float innerWidth)
        {
            // 参考图里的菱形是"方正"的菱形，宽高比接近 1:1
            float halfW = r;
            float halfH = r;

            if (innerWidth <= 0f)
            {
                int s = vh.currentVertCount;
                vh.AddVert(new Vector3(0f, halfH, 0f), color, Vector2.zero);
                vh.AddVert(new Vector3(halfW, 0f, 0f), color, Vector2.zero);
                vh.AddVert(new Vector3(0f, -halfH, 0f), color, Vector2.zero);
                vh.AddVert(new Vector3(-halfW, 0f, 0f), color, Vector2.zero);
                vh.AddTriangle(s, s + 1, s + 2);
                vh.AddTriangle(s, s + 2, s + 3);
            }
            else
            {
                // 内菱形按比例缩进，形成等宽描边
                float k = 1f - Mathf.Clamp01(innerWidth / Mathf.Max(0.001f, r));
                float iw = halfW * k;
                float ih = halfH * k;

                int s = vh.currentVertCount;

                // 外四角
                vh.AddVert(new Vector3(0f, halfH, 0f), color, Vector2.zero);
                vh.AddVert(new Vector3(halfW, 0f, 0f), color, Vector2.zero);
                vh.AddVert(new Vector3(0f, -halfH, 0f), color, Vector2.zero);
                vh.AddVert(new Vector3(-halfW, 0f, 0f), color, Vector2.zero);
                // 内四角
                vh.AddVert(new Vector3(0f, ih, 0f), color, Vector2.zero);
                vh.AddVert(new Vector3(iw, 0f, 0f), color, Vector2.zero);
                vh.AddVert(new Vector3(0f, -ih, 0f), color, Vector2.zero);
                vh.AddVert(new Vector3(-iw, 0f, 0f), color, Vector2.zero);

                for (int i = 0; i < 4; i++)
                {
                    int o = s + i;
                    int oNext = s + (i + 1) % 4;
                    int inn = s + 4 + i;
                    int innNext = s + 4 + (i + 1) % 4;

                    vh.AddTriangle(o, inn, innNext);
                    vh.AddTriangle(o, innNext, oNext);
                }
            }
        }

        /// <summary>玩家三角：指向 +Y（配合 RectTransform 的 localRotation 表示朝向）。</summary>
        private void AddTriangle(VertexHelper vh, float r)
        {
            int s = vh.currentVertCount;
            // 等边三角形，顶点朝上
            float half = r;
            float height = r * 1.732f;   // √3

            vh.AddVert(new Vector3(0f, height * 0.577f, 0f), color, Vector2.zero);
            vh.AddVert(new Vector3(half, -height * 0.289f, 0f), color, Vector2.zero);
            vh.AddVert(new Vector3(-half, -height * 0.289f, 0f), color, Vector2.zero);

            vh.AddTriangle(s, s + 1, s + 2);
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
