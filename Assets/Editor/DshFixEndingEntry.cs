using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 修 Ending 起始机位：电视 ×4.5 后前表面在 z=+1.53，
/// 玩家必须站在它外面（z=2.69），穿屏触发点改到 1.88。
/// </summary>
public static class DshFixEndingEntry
{
    [MenuItem("Tools/DSH/Ending/Fix Entry Position")]
    public static void Fix()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Ending.unity", OpenSceneMode.Single);

        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            player.transform.position = new Vector3(0f, 0f, 2.69f);
            player.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            Debug.Log($"[EndingFix] player -> {player.transform.position}");
        }

        EndingDirector dir = Object.FindFirstObjectByType<EndingDirector>();
        if (dir != null)
        {
            dir.screenCenter = new Vector3(0f, 1.45f, 1.53f);
            dir.screenTriggerZ = 1.88f;
            EditorUtility.SetDirty(dir);
            Debug.Log($"[EndingFix] director triggerZ={dir.screenTriggerZ} center={dir.screenCenter}");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[EndingFix] done");
    }
}
