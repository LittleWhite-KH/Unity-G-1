using System.Collections.Generic;
using UnityEngine;

namespace Outdoor.UI
{
    /// <summary>
    /// 地图点位数据（纯数据，不含绘制）。
    /// 挂在 MapPanel 上，由 MapPanelView 读取后画到 MapArea 里。
    ///
    /// 策划案对应关系：
    ///   Part1 第 5 步「地图 UI 显示下一个要去的点位」 → NextTarget
    ///   Part2 第 5 步「拿取碟片后解锁道具位置」        → Unlock(id)
    ///   Part3 第 1 步「用 1.1 的技巧标出门的位置」     → MarkDiscovered(id)
    /// </summary>
    [DisallowMultipleComponent]
    public class MapGraph : MonoBehaviour
    {
        /// <summary>一个地图点位。</summary>
        [System.Serializable]
        public class Node
        {
            [Tooltip("唯一 id；游戏逻辑用它来标记已抵达 / 已解锁")]
            public string id = "node";

            [Tooltip("显示名（出现在 TARGETS 列表里）")]
            public string label = "UNCONFIRMED";

            [Tooltip("世界坐标（只用 XZ）")]
            public Vector3 worldPosition;

            [Tooltip("是不是「目标点」（画菱形，进 TARGETS 列表）")]
            public bool isTarget = false;

            [Tooltip("已抵达：实心 + 实线连过来")]
            public bool visited = false;

            [Tooltip("已确认（非目标点画实心圆；未确认画空心圆）")]
            public bool confirmed = false;

            [Tooltip("是否已经在地图上解锁显示（未解锁的点完全不画）")]
            public bool discovered = true;

            [HideInInspector] public RectTransform marker;
        }

        [Header("点位列表（顺序 = TARGETS 列表顺序）")]
        [SerializeField] private List<Node> nodes = new List<Node>();

        [Header("路径")]
        [Tooltip("把哪些点按顺序连起来（下标指向 nodes）；默认按列表顺序连 visited 的点")]
        [SerializeField] private bool autoChainVisited = true;

        [Header("调试")]
        [SerializeField] private bool logChanges = false;

        public List<Node> Nodes { get { return nodes; } }

        /// <summary>关卡构建工具用：整表替换点位（会触发重画）。</summary>
        public void ConfigureNodes(List<Node> newNodes)
        {
            nodes = newNodes ?? new List<Node>();
            Changed();
        }

        /// <summary>只改某个点的"已解锁"状态。</summary>
        public void SetDiscovered(string id, bool value)
        {
            Node n = Find(id);
            if (n == null || n.discovered == value)
            {
                return;
            }

            n.discovered = value;
            Changed();
        }

        /// <summary>下一个要去的目标点（策划案 Part1 第 5 步）。</summary>
        public Node NextTarget
        {
            get
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    if (nodes[i].isTarget && nodes[i].discovered && !nodes[i].visited) return nodes[i];
                }
                return null;
            }
        }

        public Node Find(string id)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].id == id) return nodes[i];
            }
            return null;
        }

        /// <summary>标记已抵达（走过 → 实心）。</summary>
        public void MarkVisited(string id)
        {
            Node n = Find(id);
            if (n == null || n.visited) return;

            n.visited = true;
            n.confirmed = true;
            n.discovered = true;

            if (logChanges) Debug.Log("[MapGraph] 已抵达 " + id);
            Changed();
        }

        /// <summary>标记已确认（实心圆，但不算"抵达"）。</summary>
        public void MarkConfirmed(string id)
        {
            Node n = Find(id);
            if (n == null || n.confirmed) return;

            n.confirmed = true;

            if (logChanges) Debug.Log("[MapGraph] 已确认 " + id);
            Changed();
        }

        /// <summary>解锁显示（Part2 拿取碟片后）。</summary>
        public void Unlock(string id)
        {
            Node n = Find(id);
            if (n == null || n.discovered) return;

            n.discovered = true;

            if (logChanges) Debug.Log("[MapGraph] 解锁点位 " + id);
            Changed();
        }

        /// <summary>把一串点标为已抵达（用于"玩家沿路走过来"的回填）。</summary>
        public void MarkVisitedRange(int fromIndex, int toIndex)
        {
            bool any = false;
            for (int i = fromIndex; i <= toIndex && i < nodes.Count; i++)
            {
                if (i < 0) continue;
                if (!nodes[i].visited) { nodes[i].visited = true; nodes[i].confirmed = true; nodes[i].discovered = true; any = true; }
            }

            if (any) Changed();
        }

        /// <summary>已抵达的点按列表顺序形成的折线（世界坐标）。</summary>
        public List<Vector3> BuildVisitedPolyline()
        {
            List<Vector3> line = new List<Vector3>();
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].discovered && nodes[i].visited) line.Add(nodes[i].worldPosition);
            }
            return line;
        }

        /// <summary>从最后一个已抵达点指向下一个目标点的"预测路线"。</summary>
        public bool TryBuildPredictedSegment(out Vector3 from, out Vector3 to)
        {
            from = Vector3.zero;
            to = Vector3.zero;

            Vector3? last = null;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].discovered && nodes[i].visited) last = nodes[i].worldPosition;
            }

            Node target = NextTarget;
            if (target == null || !last.HasValue) return false;

            // 目标点和最后一个已抵达点重合就没必要画
            if ((target.worldPosition - last.Value).sqrMagnitude < 0.0001f) return false;

            from = last.Value;
            to = target.worldPosition;
            return true;
        }

        private void Changed()
        {
            if (OnChanged != null) OnChanged();
        }

        /// <summary>数据变化时通知视图重画。</summary>
        public event System.Action OnChanged;
    }
}
