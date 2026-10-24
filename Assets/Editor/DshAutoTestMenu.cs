using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>测试用：给 LevelDirector 挂/摘 LevelAutoTest。</summary>
internal static class DshAutoTestMenu
{
    [MenuItem("Tools/DSH/Auto Test/Add")]
    private static void Add()
    {
        LevelDirector director = Object.FindFirstObjectByType<LevelDirector>();
        if (director == null)
        {
            Debug.LogError("[DSH] LevelDirector not found");
            return;
        }

        LevelAutoTest test = director.GetComponent<LevelAutoTest>();
        if (test == null)
        {
            test = director.gameObject.AddComponent<LevelAutoTest>();
        }

        test.runOnStart = true;
        test.verbose = true;
        EditorUtility.SetDirty(test);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("[DSH] LevelAutoTest added");
    }

    [MenuItem("Tools/DSH/Auto Test/Remove")]
    private static void Remove()
    {
        LevelDirector director = Object.FindFirstObjectByType<LevelDirector>();
        if (director == null)
        {
            return;
        }

        LevelAutoTest test = director.GetComponent<LevelAutoTest>();
        if (test != null)
        {
            Object.DestroyImmediate(test);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Debug.Log("[DSH] LevelAutoTest removed");
        }
    }
}
