using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 可走性检查：在地面高度铺 0.5m 网格做 CheckSphere，从出生点 BFS，
/// 看墓碑/桌子/电视/两台电脑前面是否还能走到（检测家具/桌椅是否堵路）。
/// </summary>
public static class DshReachabilityCheck
{
    const string OutPath = "E:/DSH_Unity/tmp-read/reachability.txt";
    const float Step = 0.5f;
    const float Radius = 0.40f;   // 略小于玩家 CC 半径 0.5，允许贴边

    static readonly Vector3[] Targets =
    {
        new Vector3(-34.5f, 0f, 3f),    // 出生点
        new Vector3(-3f, 0f, 9.5f),     // 墓碑前
        new Vector3(21f, 0f, -7.5f),    // 桌子前
        new Vector3(27.5f, 0f, 9f),     // 电视前
        new Vector3(2.9f, 0f, 9f),      // 电脑1 前
        new Vector3(-20.9f, 0f, 3f),    // 电脑2 前
        new Vector3(33f, 0f, 9.65f),    // Mainframe/终点区
    };

    static readonly string[] Names = { "spawn", "vase", "terminal", "tv", "pc1", "pc2", "mainframe" };

    [MenuItem("Tools/DSH/Diag/Reachability")]
    public static void Run()
    {
        var sb = new StringBuilder();
        var player = GameObject.Find("Player");
        var cc = player != null ? player.GetComponent<CharacterController>() : null;
        bool ccWas = cc != null && cc.enabled;
        if (cc != null) cc.enabled = false;
        Physics.SyncTransforms();

        float x0 = -36.5f, x1 = 36.5f, z0 = -12.5f, z1 = 12.5f;
        int nx = Mathf.CeilToInt((x1 - x0) / Step) + 1;
        int nz = Mathf.CeilToInt((z1 - z0) / Step) + 1;

        var blocked = new bool[nx, nz];
        int blockedCount = 0;
        for (int i = 0; i < nx; i++)
        {
            for (int j = 0; j < nz; j++)
            {
                var p = new Vector3(x0 + i * Step, 0.9f, z0 + j * Step);
                bool hit = Physics.CheckSphere(p, Radius, ~0, QueryTriggerInteraction.Ignore);
                blocked[i, j] = hit;
                if (hit) blockedCount++;
            }
        }
        sb.AppendLine($"grid {nx}x{nz} step={Step} blocked={blockedCount} total={nx * nz}");

        // BFS from spawn cell
        int si = Mathf.RoundToInt((Targets[0].x - x0) / Step);
        int sj = Mathf.RoundToInt((Targets[0].z - z0) / Step);
        var reach = new bool[nx, nz];
        var q = new Queue<(int, int)>();
        if (si >= 0 && si < nx && sj >= 0 && sj < nz)
        {
            reach[si, sj] = true;
            q.Enqueue((si, sj));
        }
        int reachCount = 0;
        while (q.Count > 0)
        {
            var (i, j) = q.Dequeue();
            reachCount++;
            for (int d = 0; d < 4; d++)
            {
                int ni = i + (d == 0 ? 1 : d == 1 ? -1 : 0);
                int nj = j + (d == 2 ? 1 : d == 3 ? -1 : 0);
                if (ni < 0 || ni >= nx || nj < 0 || nj >= nz) continue;
                if (reach[ni, nj] || blocked[ni, nj]) continue;
                reach[ni, nj] = true;
                q.Enqueue((ni, nj));
            }
        }
        sb.AppendLine($"reachable cells={reachCount}");

        sb.AppendLine("=== targets ===");
        for (int k = 0; k < Targets.Length; k++)
        {
            int ti = Mathf.RoundToInt((Targets[k].x - x0) / Step);
            int tj = Mathf.RoundToInt((Targets[k].z - z0) / Step);
            bool ok = false;
            float best = 999f;
            // 目标格附近 1.5m 内有可达格即算可达
            for (int di = -3; di <= 3; di++)
            {
                for (int dj = -3; dj <= 3; dj++)
                {
                    int a = ti + di, b = tj + dj;
                    if (a < 0 || a >= nx || b < 0 || b >= nz) continue;
                    if (reach[a, b])
                    {
                        ok = true;
                        float dist = Mathf.Sqrt(di * di + dj * dj) * Step;
                        if (dist < best) best = dist;
                    }
                }
            }
            sb.AppendLine($"  {Names[k],-10} {Targets[k]} reachable={ok} nearestReach={(ok ? best.ToString("F2") : "-")}");
        }

        if (cc != null) cc.enabled = ccWas;
        Physics.SyncTransforms();
        File.WriteAllText(OutPath, sb.ToString());
        Debug.Log("[Reach] " + sb);
    }
}
