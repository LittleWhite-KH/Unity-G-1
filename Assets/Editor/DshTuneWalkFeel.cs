using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 走路手感调整：
/// - 降低人物整体高度（CC 1.80 -> 1.72，相机 1.45 -> 1.36）
/// - 禁止踩台阶（stepOffset = 0），配 PlayerMovement 的重力，人不会再被踩到的东西顶起来
/// - 给 Main Camera 挂 HeadBob（眼高锁定 + 走路摇晃）
/// </summary>
public static class DshTuneWalkFeel
{
    public const float EyeHeight = 1.36f;
    public const float CcHeight = 1.72f;
    /// <summary>CharacterController 不支持 0，会报 "Step Offset must be positive"；用 1cm 近似"不能踩"。</summary>
    public const float StepOffset = 0.01f;

    [MenuItem("Tools/DSH/Tune/Walk Feel")]
    public static void Apply()
    {
        var player = GameObject.Find("Player");
        if (player == null) { Debug.LogError("[WalkFeel] Player missing"); return; }

        var cc = player.GetComponent<CharacterController>();
        if (cc == null) { Debug.LogError("[WalkFeel] CharacterController missing"); return; }
        cc.height = CcHeight;
        cc.center = new Vector3(0f, CcHeight * 0.5f, 0f);
        cc.stepOffset = StepOffset;
        cc.slopeLimit = 30f;

        var camT = player.transform.Find("Main Camera");
        if (camT == null) { Debug.LogError("[WalkFeel] Player/Main Camera missing"); return; }
        camT.localPosition = new Vector3(0f, EyeHeight, 0f);

        var bob = camT.GetComponent<HeadBob>();
        if (bob == null) bob = camT.gameObject.AddComponent<HeadBob>();
        bob.movement = player.GetComponent<PlayerMovement>();
        bob.lockEyeHeight = true;
        bob.eyeHeight = EyeHeight;
        bob.groundY = player.transform.position.y;

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log($"[WalkFeel] cc h={cc.height} center={cc.center} step={cc.stepOffset} slope={cc.slopeLimit}; cam localY={camT.localPosition.y}; HeadBob eye={bob.eyeHeight} ground={bob.groundY}");
    }
}
