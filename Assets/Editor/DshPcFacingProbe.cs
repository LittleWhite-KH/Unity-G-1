using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>探测两台 Apple II 的朝向：16 方向射线找墙/开口，输出尺寸与建议朝向。</summary>
public static class DshPcFacingProbe
{
    const string OutPath = "E:/DSH_Unity/tmp-read/pc-facing.txt";
    static readonly string[] Names = { "OldPC_1_AppleII", "OldPC_2_AppleII" };

    [MenuItem("Tools/DSH/Probe/PC Facing")]
    public static void Run()
    {
        var sb = new StringBuilder();
        var maze = GameObject.Find("MazeRoot").transform;
        var walls = FindDeep(maze, "Walls").GetComponent<Collider>();
        Physics.SyncTransforms();

        foreach (var n in Names)
        {
            var t = FindDeep(maze, n);
            if (t == null) { sb.AppendLine($"{n}: NOT FOUND"); continue; }

            var rends = t.GetComponentsInChildren<Renderer>();
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);

            sb.AppendLine($"=== {n} ===");
            sb.AppendLine($"  pos={t.position} rot={t.eulerAngles} scale={t.lossyScale}");
            sb.AppendLine($"  bounds center={b.center} size={b.size} (w×h×d)");
            sb.AppendLine($"  minY={b.min.y:F3} maxY={b.max.y:F3}");

            // 16 方向射线（水平，1m 高）
            var dirs = new List<(float ang, float dist)>();
            Vector3 origin = new Vector3(t.position.x, 1.0f, t.position.z);
            for (int i = 0; i < 16; i++)
            {
                float ang = i * 22.5f;
                Vector3 d = Quaternion.Euler(0, ang, 0) * Vector3.forward; // ang=0 => +Z(北)
                if (Physics.Raycast(origin, d, out var hit, 15f, ~0, QueryTriggerInteraction.Ignore))
                {
                    bool isWall = hit.collider == walls;
                    dirs.Add((ang, isWall ? hit.distance : -hit.distance));
                }
                else dirs.Add((ang, 99f));
            }
            foreach (var (ang, dist) in dirs)
            {
                string label = ang == 0 ? "N(+Z)" : ang == 90 ? "E(+X)" : ang == 180 ? "S(-Z)" : ang == 270 ? "W(-X)" : $"{ang:F0}deg";
                string note = dist < 0 ? $"其他物体 {-dist:F2}m" : dist >= 99 ? "无碰撞(>15m)" : $"墙 {dist:F2}m";
                sb.AppendLine($"  dir {label,-7} -> {note}");
            }

            // 建议：最开阔方向（墙体距离最大）
            float bestAng = 0; float bestDist = -1;
            foreach (var (ang, dist) in dirs)
            {
                if (dist == 99f) continue;
                float wallDist = dist < 0 ? 0f : dist;
                if (wallDist > bestDist) { bestDist = wallDist; bestAng = ang; }
            }
            sb.AppendLine($"  mostOpenDir={bestAng:F1}deg dist={bestDist:F2}m");
            sb.AppendLine();
        }

        Directory.CreateDirectory(Path.GetDirectoryName(OutPath));
        File.WriteAllText(OutPath, sb.ToString());
        Debug.Log("[PcFacing] " + sb);
    }

    static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform c in root)
        {
            var r = FindDeep(c, name);
            if (r != null) return r;
        }
        return null;
    }
}
