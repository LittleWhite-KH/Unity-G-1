using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 场景内所有 <see cref="GuidanceTarget"/> 的注册表。
/// 新物体只需要挂组件并填 id，这里自动收进来；关卡/地图/距离计只认 id。
/// </summary>
public static class GuidanceRegistry
{
    private static readonly List<GuidanceTarget> targets = new List<GuidanceTarget>();

    public static IReadOnlyList<GuidanceTarget> All => targets;

    /// <summary>注册表变化（目标增减/重置）时触发，地图/UI 可以据此重画。</summary>
    public static event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        targets.Clear();
        Changed = null;
    }

    public static void Register(GuidanceTarget target)
    {
        if (target == null || targets.Contains(target))
        {
            return;
        }

        targets.Add(target);
        Changed?.Invoke();
    }

    public static void Unregister(GuidanceTarget target)
    {
        if (target == null)
        {
            return;
        }

        if (targets.Remove(target))
        {
            Changed?.Invoke();
        }
    }

    public static GuidanceTarget Find(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        for (int i = 0; i < targets.Count; i++)
        {
            GuidanceTarget t = targets[i];
            if (t != null && t.id == id)
            {
                return t;
            }
        }

        return null;
    }

    public static List<GuidanceTarget> FindAll(IEnumerable<string> ids)
    {
        var result = new List<GuidanceTarget>();
        if (ids == null)
        {
            return result;
        }

        foreach (string id in ids)
        {
            GuidanceTarget t = Find(id);
            if (t != null)
            {
                result.Add(t);
            }
            else
            {
                Debug.LogWarning($"[Guidance] 找不到目标 id={id}");
            }
        }

        return result;
    }

    /// <summary>最近的、未拾取的目标（可按类别过滤）。</summary>
    public static GuidanceTarget NearestUncollected(Vector3 from, GuidanceKind? kind = null)
    {
        GuidanceTarget best = null;
        float bestSq = float.MaxValue;

        for (int i = 0; i < targets.Count; i++)
        {
            GuidanceTarget t = targets[i];
            if (t == null || !t.isActiveAndEnabled || t.collected)
            {
                continue;
            }

            if (kind.HasValue && t.kind != kind.Value)
            {
                continue;
            }

            float sq = (t.AimPosition - from).sqrMagnitude;
            if (sq < bestSq)
            {
                bestSq = sq;
                best = t;
            }
        }

        return best;
    }

    public static bool AnyUncollected(IEnumerable<GuidanceTarget> list, out int collected, out int total)
    {
        collected = 0;
        total = 0;
        if (list == null)
        {
            return false;
        }

        bool any = false;
        foreach (GuidanceTarget t in list)
        {
            if (t == null)
            {
                continue;
            }

            total++;
            if (t.collected)
            {
                collected++;
            }
            else
            {
                any = true;
            }
        }

        return any;
    }
}
