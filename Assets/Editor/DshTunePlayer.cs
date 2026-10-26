using UnityEditor;
using UnityEngine;

/// <summary>把手感参数落到场景：相机 1.45m、胶囊 1.8m、玩家 y=0。</summary>
public static class DshTunePlayer
{
    [MenuItem("Tools/DSH/Player/Tune Height And Speed")]
    public static void Tune()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null) { Debug.LogError("[Tune] Player not found"); return; }

        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.height = 1.8f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.skinWidth = 0.08f;
            cc.stepOffset = 0.3f;
            Debug.Log($"[Tune] CC height={cc.height} center={cc.center}");
        }

        Transform cam = player.transform.Find("Main Camera");
        if (cam != null)
        {
            cam.localPosition = new Vector3(0f, 1.45f, 0f);
            Debug.Log($"[Tune] camera local={cam.localPosition}");
        }

        PlayerMovement move = player.GetComponent<PlayerMovement>();
        if (move != null)
        {
            move.moveSpeed = 3.4f;
            move.boostMultiplier = 1.35f;
            EditorUtility.SetDirty(move);
            Debug.Log($"[Tune] moveSpeed={move.moveSpeed} boost={move.boostMultiplier}");
        }

        Vector3 p = player.transform.position;
        if (Mathf.Abs(p.y) > 0.001f)
        {
            player.transform.position = new Vector3(p.x, 0f, p.z);
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(player.scene);
        Debug.Log("[Tune] done, player euler=" + player.transform.eulerAngles);
    }
}
