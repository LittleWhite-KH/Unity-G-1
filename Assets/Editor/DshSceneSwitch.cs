using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DshSceneSwitch
{
    [MenuItem("Tools/DSH/Open Start Scene")]
    public static void OpenStart()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[Dsh] stop play mode first");
            return;
        }

        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Start.unity", OpenSceneMode.Single);
        Debug.Log($"[Dsh] opened {scene.path}");
    }


    [MenuItem("Tools/DSH/Open Ending Scene")]
    public static void OpenEnding()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[Dsh] stop play mode first");
            return;
        }

        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Ending.unity", OpenSceneMode.Single);
        Debug.Log($"[Dsh] opened {scene.path}");
    }
    [MenuItem("Tools/DSH/Open Gaming Scene")]
    public static void OpenGaming()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[Dsh] stop play mode first");
            return;
        }

        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Gaming.unity", OpenSceneMode.Single);
        Debug.Log($"[Dsh] opened {scene.path}");
    }
}
