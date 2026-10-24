using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Outdoor.UI
{
    /// <summary>
    /// 地图视图：把 <see cref="MapGraph"/> 的点位/路径画进 <see cref="mapArea"/>。
    ///
    /// 世界坐标 → 地图局部坐标：X 轴向右、Z 轴向上（俯视图），
    /// 由 <see cref="worldMin"/>/<see cref="worldMax"/> 线性映射，并等比缩放保持长宽比。
    ///
    /// 节点画法（照 0.设计说明.pdf 第 3 页的图例）：
    ///   已抵达的目标点 = 实心菱形      未抵达的目标点 = 空心菱形
    ///   已确认的普通点 = 实心圆        未确认的普通点 = 空心圆
    ///   玩家          = 蓝色三角（指向朝向）
    /// 路径画法：
    ///   走过的那段 = 高饱和蓝实线      从最后抵达点指向下一目标 = 浅蓝虚线
    /// </summary>
    [DisallowMultipleComponent]
    public class MapPanelView : MonoBehaviour
    {
        [Header("数据")]
        public MapGraph graph;

        [Tooltip("玩家（用来画三角并读朝向）")]
        public Transform playerTransform;

        [Header("绘制容器")]
        [Tooltip("点位/路径画在这个 RectTransform 里（通常是 MapArea）")]
        public RectTransform mapArea;

        [Tooltip("路径层（会自动创建一个 MapPathGraphic）")]
        public MapPathGraphic pathLayer;

        [Tooltip("节点层：所有节点标记的父物体")]
        public RectTransform nodeLayer;

        [Tooltip("玩家三角")]
        public MapNodeGraphic playerMarker;

        [Header("世界范围（只用 XZ）")]
        public Vector2 worldMin = new Vector2(-40f, -40f);
        public Vector2 worldMax = new Vector2(40f, 40f);

        [Tooltip("在地图区内再留一点边距（像素）")]
        public float padding = 24f;

        [Header("外观")]
        public Color traveledColor = new Color(0.129f, 0.412f, 0.800f, 1f);      // 主蓝 #2169CC
        public Color predictedColor = new Color(0.129f, 0.412f, 0.800f, 0.45f);  // 浅蓝虚线
        public Color nodeBlue = new Color(0.129f, 0.412f, 0.800f, 1f);
        public Color nodeGrey = new Color(0.55f, 0.60f, 0.66f, 1f);

        [Header("尺寸")]
        public float nodeRadius = 7f;
        public float targetRadius = 9f;
        public float playerRadius = 13f;
        public float pathThickness = 2.5f;

        [Header("调试")]
        public bool logRebuild = false;

        private readonly List<GameObject> spawned = new List<GameObject>();
        // 说明：spawned 已不再用于 PathLayer/NodeLayer（那两层必须长期存在），保留字段仅为兼容旧序列化。

        private void Awake()
        {
            if (graph == null) graph = GetComponent<MapGraph>();
            if (graph == null) graph = GetComponentInParent<MapGraph>();
        }

        private void OnEnable()
        {
            if (graph != null) graph.OnChanged += Rebuild;
            Rebuild();
        }

        private void OnDisable()
        {
            if (graph != null) graph.OnChanged -= Rebuild;
        }

        private void LateUpdate()
        {
            UpdatePlayerMarker();
        }

        // ------------------------------------------------------------------
        // 绘制
        // ------------------------------------------------------------------

        [ContextMenu("Rebuild")]
        public void Rebuild()
        {
            if (logRebuild)
            {
                Debug.Log(string.Format("[MapPanelView] Rebuild 进入：mapArea={0} graph={1} 点位={2}",
                    mapArea != null ? mapArea.name : "NULL",
                    graph != null ? graph.name : "NULL",
                    graph != null ? graph.Nodes.Count : -1));
            }

            if (mapArea == null || graph == null)
            {
                Debug.LogWarning("[MapPanelView] mapArea 或 graph 为空，跳过");
                return;
            }

            EnsureLayers();

            if (logRebuild)
            {
                Debug.Log(string.Format("[MapPanelView] EnsureLayers 之后：pathLayer={0} nodeLayer={1}",
                    pathLayer != null ? pathLayer.name : "NULL",
                    nodeLayer != null ? nodeLayer.name : "NULL"));
            }

            ClearSpawned();

            DrawPaths();
            DrawNodes();
            UpdatePlayerMarker();

            if (logRebuild)
            {
                Debug.Log(string.Format("[MapPanelView] Rebuild 完成：nodeLayer 子物体={0} pathLayer 子物体={1}",
                    nodeLayer != null ? nodeLayer.childCount : -1,
                    pathLayer != null ? pathLayer.transform.childCount : -1));
            }
        }

        private void EnsureLayers()
        {
            // 注意：这两层**不能**登记进 spawned 列表（旧版就是登记后被 ClearSpawned 立刻删掉，
            // 表现为"层建出来了、但一个节点都没有"）。
            // 它们由编辑器脚本作为真正的场景对象建好；这里只做兜底。
            if (pathLayer == null)
            {
                Transform t = mapArea.Find("PathLayer");
                if (t != null) pathLayer = t.GetComponent<MapPathGraphic>();
            }
            if (pathLayer == null)
            {
                GameObject go = new GameObject("PathLayer", typeof(RectTransform));
                go.transform.SetParent(mapArea, false);
                StretchTo(go.GetComponent<RectTransform>());
                pathLayer = go.AddComponent<MapPathGraphic>();
                pathLayer.raycastTarget = false;
            }

            if (nodeLayer == null)
            {
                Transform t = mapArea.Find("NodeLayer");
                if (t != null) nodeLayer = t as RectTransform;
            }
            if (nodeLayer == null)
            {
                GameObject go = new GameObject("NodeLayer", typeof(RectTransform));
                go.transform.SetParent(mapArea, false);
                StretchTo(go.GetComponent<RectTransform>());
                nodeLayer = go.GetComponent<RectTransform>();
            }
        }

        private static void StretchTo(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
        }

        private void ClearSpawned()
        {
            // 只清节点标记和上一轮生成的路径子物体。
            // PathLayer / NodeLayer 本身**必须长期存在**，绝不能在这里删
            //（旧版把两层登记进 spawned 后立刻删掉，表现为"层建出来了一个节点都没有"）。
            if (nodeLayer != null)
            {
                for (int i = nodeLayer.childCount - 1; i >= 0; i--)
                {
                    Kill(nodeLayer.GetChild(i).gameObject);
                }
            }

            if (pathLayer != null)
            {
                for (int i = pathLayer.transform.childCount - 1; i >= 0; i--)
                {
                    Kill(pathLayer.transform.GetChild(i).gameObject);
                }
            }
        }

        /// <summary>
        /// 运行时只能用 Destroy（DestroyImmediate 在播放模式下会毁掉资源），
        /// 编辑器里必须是 DestroyImmediate（Destroy 在编辑模式不生效）。
        /// </summary>
        private static void Kill(GameObject go)
        {
            if (go == null) return;

#if UNITY_EDITOR
            if (!Application.isPlaying) { DestroyImmediate(go); return; }
#endif
            Destroy(go);
        }

        private void DrawPaths()
        {
            if (pathLayer == null) return;

            // 走过的实线
            List<Vector3> traveled = graph.BuildVisitedPolyline();
            if (traveled.Count >= 2)
            {
                MapPathGraphic line = SpawnPath("TraveledPath", traveledColor, false);
                line.SetPoints(WorldToLocal(traveled));
            }

            // 预测的虚线
            Vector3 from, to;
            if (graph.TryBuildPredictedSegment(out from, out to))
            {
                MapPathGraphic dash = SpawnPath("PredictedPath", predictedColor, true);
                dash.SetPoints(WorldToLocal(new List<Vector3> { from, to }));
            }
        }

        private MapPathGraphic SpawnPath(string name, Color c, bool dashed)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(pathLayer.transform, false);
            StretchTo(go.GetComponent<RectTransform>());

            MapPathGraphic g = go.AddComponent<MapPathGraphic>();
            g.color = c;
            g.thickness = pathThickness;
            g.dashed = dashed;
            g.raycastTarget = false;

            // 虚线短一点更接近参考图
            g.dashLength = 10f;
            g.gapLength = 8f;

            return g;
        }

        private void DrawNodes()
        {
            if (nodeLayer == null) return;

            List<MapGraph.Node> nodes = graph.Nodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                MapGraph.Node n = nodes[i];
                if (!n.discovered) continue;

                Vector2 local = WorldToLocal(n.worldPosition);

                GameObject go = new GameObject("Node_" + n.id, typeof(RectTransform));
                go.transform.SetParent(nodeLayer, false);

                RectTransform rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = local;
                rt.sizeDelta = new Vector2(nodeRadius * 3f, nodeRadius * 3f);

                MapNodeGraphic g = go.AddComponent<MapNodeGraphic>();
                g.raycastTarget = false;

                if (n.isTarget)
                {
                    g.shape = n.visited ? MapNodeGraphic.Shape.FilledDiamond : MapNodeGraphic.Shape.HollowDiamond;
                    g.radius = targetRadius;
                    g.color = n.visited ? nodeBlue : nodeGrey;
                }
                else
                {
                    g.shape = n.confirmed ? MapNodeGraphic.Shape.FilledCircle : MapNodeGraphic.Shape.HollowCircle;
                    g.radius = nodeRadius;
                    g.color = n.confirmed ? nodeBlue : nodeGrey;
                }

                n.marker = rt;
            }
        }

        private void UpdatePlayerMarker()
        {
            if (playerMarker == null || mapArea == null || playerTransform == null) return;

            Vector2 local = WorldToLocal(playerTransform.position);

            RectTransform rt = playerMarker.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = local;

            // 三角默认朝上，玩家朝向是绕 Y 的 yaw：地图上顺时针为正，所以取负
            rt.localRotation = Quaternion.Euler(0f, 0f, -playerTransform.eulerAngles.y);
        }

        // ------------------------------------------------------------------
        // 坐标映射
        // ------------------------------------------------------------------

        /// <summary>世界坐标（只用 XZ）→ 地图区局部坐标。X 向右、Z 向上，等比缩放。</summary>
        public Vector2 WorldToLocal(Vector3 world)
        {
            Vector2 area = mapArea.rect.size;
            float usableW = Mathf.Max(1f, area.x - padding * 2f);
            float usableH = Mathf.Max(1f, area.y - padding * 2f);

            float spanX = Mathf.Max(0.0001f, worldMax.x - worldMin.x);
            float spanZ = Mathf.Max(0.0001f, worldMax.y - worldMin.y);

            // 等比缩放：保证圆是圆、菱形是菱形
            float scale = Mathf.Min(usableW / spanX, usableH / spanZ);

            float nx = (world.x - worldMin.x) * scale - (spanX * scale) * 0.5f;
            float nz = (world.z - worldMin.y) * scale - (spanZ * scale) * 0.5f;

            return new Vector2(nx, nz);
        }

        /// <summary>把一组世界坐标转成局部坐标。</summary>
        public List<Vector2> WorldToLocal(List<Vector3> world)
        {
            List<Vector2> outList = new List<Vector2>(world.Count);
            for (int i = 0; i < world.Count; i++) outList.Add(WorldToLocal(world[i]));
            return outList;
        }

        /// <summary>按场景里所有渲染物体自动撑开世界范围。</summary>
        [ContextMenu("Fit World Range To Scene")]
        public void FitWorldRangeToScene()
        {
            Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            if (renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer r in renderers)
            {
                if (r != null) bounds.Encapsulate(r.bounds);
            }

            worldMin = new Vector2(bounds.min.x, bounds.min.z);
            worldMax = new Vector2(bounds.max.x, bounds.max.z);
            Rebuild();
        }
    }
}
