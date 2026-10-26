using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>清掉自动测试留下的 Play 起始场景与通关标记，恢复正常的开始界面。</summary>
public static class DshResetPlayStart
{
    [MenuItem("Tools/DSH/Test/Reset Play Start Scene")]
    public static void ResetPlayStart()
    {
        EditorSceneManager.playModeStartScene = null;
        Debug.Log("[Reset] playModeStartScene cleared");
    }

    [MenuItem("Tools/DSH/Test/Clear Finish Flag")]
    public static void ClearFinishFlag()
    {
        PlayerPrefs.DeleteKey("outdoor.finished");
        PlayerPrefs.Save();
        Debug.Log("[Reset] outdoor.finished cleared");
    }

    [MenuItem("Tools/DSH/Test/Set Finish Flag")]
    public static void SetFinishFlag()
    {
        PlayerPrefs.SetInt("outdoor.finished", 1);
        PlayerPrefs.Save();
        Debug.Log("[Reset] outdoor.finished = 1");
    }
}
