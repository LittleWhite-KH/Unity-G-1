using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Outdoor.UI
{
    /// <summary>
    /// 地图路径连线：在若干控制点之间画**实线**或**虚线**。
    ///
    /// 设计说明（0.设计说明.pdf 第 3 页）：
    ///   玩家已经过的路线 → 高饱和蓝色实线
    ///   尚未确认的路线   → 浅蓝色虚线
    ///
    /// 自绘原因：uGUI 没有线段图元；用旋转的 Image 拼多段线每段一个对象，
    /// 一条 5 段的路线就是 5 个 GameObject，而且拐角会露出缺口。
    /// 自绘一条多段线只要 1 个对象。
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasRenderer))]
    public class MapPathGraphic : MaskableGraphic
    {
        [Header("控制点（本 RectTransform 的局部坐标）")]
        [SerializeField] private List<Vector2> points = new List<Vector2>();

        [Header("线型")]
        [Tooltip("线宽（像素）")]
        public float thickness = 2.5f;

        [Tooltip("虚线：每段实线长度")]
        public float dashLength = 12f;

        [Tooltip("虚线：每段间隔长度")]
        public float gapLength = 9f;

        [Tooltip("画成虚线（未确认路线）；关闭则实线（已走过路线）")]
        public bool dashed = false;

        public void SetPoints(List<Vector2> newPoints)
        {
            points = newPoints != null ? newPoints : new List<Vector2>();
            SetVerticesDirty();
        }

        public List<Vector2> GetPoints()
        {
            return points;
        }

        public void SetDashed(bool value)
        {
            dashed = value;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            if (points == null || points.Count < 2) return;
            if (thickness <= 0f) return;

            float half = thickness * 0.5f;

            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector2 a = points[i];
                Vector2 b = points[i + 1];

                if (dashed) AddDashedSegment(vh, a, b, half);
                else AddSolidSegment(vh, a, b, half);
            }
        }

        private void AddSolidSegment(VertexHelper vh, Vector2 a, Vector2 b, float half)
        {
            Vector2 d = b - a;
            float len = d.magnitude;
            if (len < 0.0001f) return;

            Vector2 n = new Vector2(-d.y, d.x) / len * half;
            AddQuad(vh, a - n, a + n, b + n, b - n);
        }

        private void AddDashedSegment(VertexHelper vh, Vector2 a, Vector2 b, float half)
        {
            Vector2 d = b - a;
            float len = d.magnitude;
            if (len < 0.0001f) return;

            Vector2 dir = d / len;
            Vector2 n = new Vector2(-dir.y, dir.x) * half;

            float step = Mathf.Max(0.5f, dashLength + gapLength);
            float t = 0f;

            while (t < len)
            {
                float t0 = t;
                float t1 = Mathf.Min(t + dashLength, len);

                Vector2 p0 = a + dir * t0;
                Vector2 p1 = a + dir * t1;

                AddQuad(vh, p0 - n, p0 + n, p1 + n, p1 - n);

                t += step;
            }
        }

        private void AddQuad(VertexHelper vh, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        {
            int s = vh.currentVertCount;
            vh.AddVert(p0, color, Vector2.zero);
            vh.AddVert(p1, color, Vector2.zero);
            vh.AddVert(p2, color, Vector2.zero);
            vh.AddVert(p3, color, Vector2.zero);
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
