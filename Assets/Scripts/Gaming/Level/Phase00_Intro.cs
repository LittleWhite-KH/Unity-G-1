using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 开场阶段（策划案 Part1 第 1 步）：
///   1) 先锁住玩家操控（默认关掉 MouseLook / PlayerMovement）；
///   2) 让 SAN 波形从"平的"过渡到"平稳阶段"（SanDisplay.BeginStabilize）；
///   3) 动画结束后解锁操控，阶段完成。
/// </summary>
public class Phase00_Intro : LevelPhaseBase
{
    [Header("开场")]
    [Tooltip("进入阶段后先等待的秒数")]
    public float startDelay = 0.5f;

    [Tooltip("锁住玩家操控（关掉下面列出的组件），动画结束后解锁")]
    public bool lockPlayerControl = true;

    [Tooltip("平稳动画结束后再多等一会儿再解锁")]
    public float extraWaitAfterStabilize = 0.5f;

    [Tooltip("要临时关掉的玩家组件名（按类型名匹配）")]
    public string[] controlComponents = { "MouseLook", "PlayerMovement" };

    [Tooltip("等待平稳动画的超时保护（秒）")]
    public float stabilizeTimeout = 8f;

    private readonly List<Behaviour> lockedComponents = new List<Behaviour>();
    private Coroutine routine;

    public override void OnEnter()
    {
        SetPlayerControl(false);

        if (SanDisplay != null)
        {
            SanDisplay.BeginStabilize();
        }
        else
        {
            Debug.LogWarning($"[{phaseName}] 没有 SanDisplay 引用，无法播放\"平的 → 平稳\"动画。");
        }

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Run());
    }

    public override void OnExit()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        // 保险：无论怎么退出都解锁操控
        SetPlayerControl(true);
    }

    private IEnumerator Run()
    {
        if (startDelay > 0f)
        {
            yield return new WaitForSeconds(startDelay);
        }

        // 等"平的 → 平稳"动画结束
        float elapsed = 0f;
        while (SanDisplay != null && !SanDisplay.IsStabilized && elapsed < stabilizeTimeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (extraWaitAfterStabilize > 0f)
        {
            yield return new WaitForSeconds(extraWaitAfterStabilize);
        }

        SetPlayerControl(true);
        Log($"开场完成（SAN {San?.Value:0.#}），已解锁操控");

        routine = null;
        Complete();
    }

    private void SetPlayerControl(bool enable)
    {
        if (!lockPlayerControl || Player == null)
        {
            return;
        }

        if (!enable)
        {
            lockedComponents.Clear();

            foreach (Behaviour behaviour in Player.GetComponentsInChildren<Behaviour>(true))
            {
                if (behaviour == null || !behaviour.enabled)
                {
                    continue;
                }

                if (Matches(behaviour.GetType().Name))
                {
                    behaviour.enabled = false;
                    lockedComponents.Add(behaviour);
                }
            }

            Log($"已锁定操控：{lockedComponents.Count} 个组件");
        }
        else
        {
            foreach (Behaviour behaviour in lockedComponents)
            {
                if (behaviour != null)
                {
                    behaviour.enabled = true;
                }
            }

            if (lockedComponents.Count > 0)
            {
                Log($"已解锁操控：{lockedComponents.Count} 个组件");
            }

            lockedComponents.Clear();
        }
    }

    private bool Matches(string typeName)
    {
        if (controlComponents == null)
        {
            return false;
        }

        foreach (string name in controlComponents)
        {
            if (!string.IsNullOrEmpty(name) && typeName == name)
            {
                return true;
            }
        }

        return false;
    }
}
