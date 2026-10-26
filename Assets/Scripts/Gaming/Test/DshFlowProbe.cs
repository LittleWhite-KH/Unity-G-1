#if UNITY_EDITOR
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

/// <summary>
/// 编辑器流程探针：Play 后自动跳到 tv_unlock、发钥匙、传送到电视前、模拟 E，
/// 用来验证「插盘 → 进电视 → Ending」整条链。
/// 通过 PlayerPrefs 键 dsh.flowprobe 控制是否启用。
/// </summary>
public class DshFlowProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (PlayerPrefs.GetInt("dsh.flowprobe", 0) != 1) return;
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName != "Gaming" && sceneName != "Ending") return;
        if (FindFirstObjectByType<DshFlowProbe>() != null) return;

        GameObject go = new GameObject("DshFlowProbe");
        go.AddComponent<DshFlowProbe>();
    }

    private IEnumerator Start()
    {
        if (SceneManager.GetActiveScene().name == "Ending")
        {
            yield return EndingRun();
            yield break;
        }

        yield return new WaitForSeconds(2.5f);

        LevelDirector dir = FindFirstObjectByType<LevelDirector>();
        GameObject player = GameObject.Find("Player");
        if (dir == null || player == null)
        {
            Debug.LogError("[Probe] director/player missing");
            yield break;
        }

        Debug.Log("[Probe] jump to tv_unlock");
        dir.JumpTo("tv_unlock");
        yield return new WaitForSeconds(1.2f);

        dir.State.Add("key_a");
        dir.State.Add("key_b");
        Debug.Log($"[Probe] state keys={dir.State.Count}");

        // 传送到电视前
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        player.transform.position = new Vector3(27.2f, 0f, 9f);
        player.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        if (cc != null) cc.enabled = true;

        MouseLook look = player.GetComponent<MouseLook>();
        if (look != null) look.canControl = true;
        PlayerMovement move = player.GetComponent<PlayerMovement>();
        if (move != null) move.canMove = true;

        yield return new WaitForSeconds(0.6f);
        Debug.Log($"[Probe] before E, phase={dir.CurrentIndex}");

        yield return PressE();
        yield return new WaitForSeconds(1.2f);
        Debug.Log($"[Probe] after submit E, phase={dir.CurrentIndex}");

        yield return PressE();
        yield return new WaitForSeconds(3.5f);
        Debug.Log($"[Probe] after enter E, scene={SceneManager.GetActiveScene().name}");
    }

    private IEnumerator EndingRun()
    {
        yield return new WaitForSeconds(2f);
        GameObject player = GameObject.Find("Player");
        if (player == null) { Debug.LogError("[Probe] ending player missing"); yield break; }

        Debug.Log($"[Probe] ending start pos={player.transform.position}");
        player.transform.position = new Vector3(0f, 0f, 0.2f);
        yield return new WaitForSeconds(0.8f);
        Debug.Log($"[Probe] after screen pos={player.transform.position}");

        DoorController door = GameObject.Find("Ending_Door") != null
            ? GameObject.Find("Ending_Door").GetComponent<DoorController>()
            : null;

        if (door != null)
        {
            door.Open();
            Debug.Log("[Probe] forced door.Open()");
        }
        else
        {
            Debug.LogError("[Probe] Ending_Door missing");
        }

        yield return new WaitForSeconds(2.5f);
        Debug.Log($"[Probe] ending done scene={SceneManager.GetActiveScene().name}");
    }
    private static IEnumerator PressE()
    {
        if (Keyboard.current == null) yield break;

        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.E));
        yield return null;
        yield return null;
        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
        yield return null;
    }
}
#endif
