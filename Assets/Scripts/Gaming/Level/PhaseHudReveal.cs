using System.Collections;
using UnityEngine;

/// <summary>
/// UI 揭示阶段（策划案 Part1 第 4~6 步）：
///   1) 播放 HUD 揭示动画（右侧栏从右滑入、上方 UI 从上滑入…）；
///   2) **不主动打开地图**，而是亮出"按 M 打开地图"的提示；
///   3) 等玩家自己按 M 打开、再关掉地图；
///   4) SAN 回到平稳值，阶段完成。
/// </summary>
public class PhaseHudReveal : LevelPhaseBase
{
    [Header("HUD 揭示")]
    public bool revealHud = true;

    [Tooltip("揭示动画播完后、开始等待玩家操作之前等待的秒数")]
    public float holdAfterReveal = 1.0f;

    [Header("地图提示")]
    [Tooltip("要显示的\"按 M 打开地图\"提示对象（通常是侧栏 MAP 旁边的小标记）")]
    public GameObject mapHintRoot;

    [Tooltip("玩家打开地图后是否收起提示")]
    public bool hideHintAfterOpen = true;

    [Header("地图")]
    public MapPanel mapPanel;

    [Tooltip("是否等玩家完成一次\"打开再关闭地图\"")]
    public bool waitForMapCycle = true;

    [Tooltip("等待超时（秒），0 = 一直等")]
    public float mapCycleTimeout = 0f;

    [Tooltip("地图打开时顺便设置的下一个点位（留空则不动）")]
    public Transform nextWaypoint;

    [Header("收尾")]
    [Tooltip("地图关闭后把 SAN 目标设为该值（回到平稳）")]
    public float sanAfterStable = 40f;

    public bool setSanAfterStable = true;

    private Coroutine routine;

    public override void OnEnter()
    {
        if (revealHud && Hud != null)
        {
            Hud.Play();
            Log("开始播放 HUD 揭示动画");
        }

        if (mapHintRoot != null)
        {
            mapHintRoot.SetActive(true);
        }

        if (nextWaypoint != null && mapPanel != null)
        {
            mapPanel.SetWaypoint(nextWaypoint);
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
    }

    private IEnumerator Run()
    {
        if (holdAfterReveal > 0f)
        {
            yield return new WaitForSeconds(holdAfterReveal);
        }

        if (waitForMapCycle && mapPanel != null)
        {
            float elapsed = 0f;
            bool timeoutEnabled = mapCycleTimeout > 0f;

            // 1) 等玩家主动打开地图
            while (!mapPanel.IsOpen)
            {
                elapsed += Time.deltaTime;
                if (timeoutEnabled && elapsed >= mapCycleTimeout)
                {
                    Log("等待打开地图超时，跳过");
                    break;
                }

                yield return null;
            }

            if (mapPanel.IsOpen && hideHintAfterOpen && mapHintRoot != null)
            {
                mapHintRoot.SetActive(false);
            }

            // 2) 等玩家关闭地图
            while (mapPanel.IsOpen)
            {
                yield return null;
            }

            Log("玩家已完成一次地图开关");
        }

        if (setSanAfterStable && San != null)
        {
            San.SetTarget(sanAfterStable);
        }

        routine = null;
        Complete();
    }
}
