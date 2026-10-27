using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>
/// 自动化验收：按真实阶段顺序传送玩家、模拟 M 开图 / E 拾取，跑完整条流程。
/// 只用于开发验证；测试结束后从场景移除或把 runOnStart 关掉。
/// </summary>
public class LevelAutoTest : MonoBehaviour
{
    public bool runOnStart = true;
    public bool verbose = true;

    private LevelDirector director;
    private Transform player;
    private MapPanel map;
    private bool levelCompleted;

    private IEnumerator Start()
    {
        if (!runOnStart)
        {
            yield break;
        }

        director = FindFirstObjectByType<LevelDirector>();
        map = FindFirstObjectByType<MapPanel>();
        GameObject playerGo = GameObject.Find("Player");
        player = playerGo != null ? playerGo.transform : null;

        if (director == null || player == null)
        {
            Debug.LogError("[AutoTest] director or player missing");
            yield break;
        }

        director.LevelCompleted += OnLevelCompleted;
        Log("start, level=" + director.levelDefinition.levelId);

        yield return new WaitForSeconds(1.5f);

        bool ok = false;
        yield return WaitPhase("charge_vase", 20f, v => ok = v);
        if (!ok) yield break;

        Log("phase charge_vase, teleport to vase");
        Teleport(new Vector3(-3f, 0.15f, 7.6f), new Vector3(-3f, 1.1f, 11.2f));
        yield return new WaitForSeconds(0.6f);

        yield return WaitPhase("reveal_map", 15f, v => ok = v);
        if (!ok) yield break;

        Log("phase reveal_map, open/close map");
        yield return new WaitForSeconds(1.2f);
        map.Open();
        yield return new WaitForSeconds(1.0f);
        map.Close();
        yield return new WaitForSeconds(0.5f);

        yield return WaitPhase("charge_terminal", 15f, v => ok = v);
        if (!ok) yield break;

        Log("phase charge_terminal, teleport to terminal");
        Teleport(new Vector3(21f, 0.15f, -6.5f), new Vector3(21f, 0.9f, -11.3f));
        yield return new WaitForSeconds(0.6f);

        yield return WaitPhase("door_trigger", 15f, v => ok = v);
        if (!ok) yield break;

        Log("phase door_trigger, teleport to door");
        Teleport(new Vector3(26.4f, 0.15f, 8.75f), new Vector3(29.9f, 1.1f, 8.75f));
        yield return new WaitForSeconds(0.6f);

        yield return WaitPhase("find_keys", 12f, v => ok = v);
        if (!ok) yield break;

        Log("phase find_keys, collect key_a");
        Teleport(new Vector3(1.85f, 0.15f, 6.4f), new Vector3(1.85f, 1.55f, 9f));
        yield return new WaitForSeconds(0.5f);
        yield return PressE();
        yield return new WaitForSeconds(0.6f);

        Log("collect key_b, count=" + (director.State != null ? director.State.Count : -1));
        Teleport(new Vector3(-19.85f, 0.15f, 5.4f), new Vector3(-19.85f, 1.55f, 3f));
        yield return new WaitForSeconds(0.5f);
        yield return PressE();
        yield return new WaitForSeconds(0.8f);

        yield return WaitPhase("open_door", 12f, v => ok = v);
        if (!ok) yield break;

        Log("phase open_door, press E at door");
        Teleport(new Vector3(26.4f, 0.15f, 8.75f), new Vector3(29.9f, 1.1f, 8.75f));
        yield return new WaitForSeconds(0.5f);
        yield return PressE();
        yield return new WaitForSeconds(0.8f);

        yield return WaitPhase("ending", 12f, v => ok = v);
        if (!ok) yield break;

        Log("phase ending, teleport to mainframe");
        Teleport(new Vector3(33f, 0.15f, 7.3f), new Vector3(33f, 1.2f, 9.65f));

        float t = 0f;
        while (t < 8f && !levelCompleted)
        {
            t += Time.deltaTime;
            yield return null;
        }

        if (levelCompleted || !director.IsRunning)
        {
            Debug.Log("[AutoTest] ALL PHASES COMPLETE");
        }
        else
        {
            Debug.LogError("[AutoTest] FAILED: level did not complete, phase=" + PhaseId() + " running=" + director.IsRunning);
        }
    }

    private void OnLevelCompleted(LevelDefinition definition)
    {
        levelCompleted = true;
        Debug.Log("[AutoTest] LevelCompleted event received");
    }

    private IEnumerator WaitPhase(string phaseId, float timeout, System.Action<bool> result)
    {
        float elapsed = 0f;
        while (elapsed < timeout)
        {
            if (PhaseId() == phaseId)
            {
                Log("reached phase " + phaseId);
                result(true);
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Debug.LogError($"[AutoTest] TIMEOUT waiting {phaseId}; current={PhaseId()}; index={(director != null ? director.CurrentIndex : -1)}");
        result(false);
    }

    private IEnumerator PressE()
    {
        if (Keyboard.current == null)
        {
            Debug.LogError("[AutoTest] no keyboard");
            yield break;
        }

        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.E));
        yield return null;
        yield return null;
        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
        yield return null;
        yield return null;
        Log("pressed E");
    }

    private void Teleport(Vector3 position, Vector3 lookAt)
    {
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = false;
        }

        player.position = position;
        Vector3 dir = lookAt - position;
        if (dir.sqrMagnitude > 0.001f)
        {
            player.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        if (controller != null)
        {
            controller.enabled = true;
        }

        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.transform.localRotation = Quaternion.identity;
        }

        Log($"teleported to {position}, facing {lookAt}");
    }

    private string PhaseId()
    {
        if (director == null || director.levelDefinition == null || director.CurrentIndex < 0)
        {
            return "";
        }

        var phases = director.levelDefinition.phases;
        return director.CurrentIndex < phases.Count ? phases[director.CurrentIndex].id : "";
    }

    private void Log(string message)
    {
        if (verbose)
        {
            Debug.Log("[AutoTest] " + message);
        }
    }
}
