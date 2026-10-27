using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Wires the minimal Start scene: only the TV and its TMP text before finishing.</summary>
public static class DshStartMinimal
{
    [MenuItem("Tools/DSH/Phase2/Configure Start Minimal")]
    public static void Configure()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Start.unity", OpenSceneMode.Single);
        StartSceneReveal reveal = Object.FindAnyObjectByType<StartSceneReveal>();
        if (reveal == null)
        {
            Debug.LogError("[StartMinimal] StartSceneReveal missing");
            return;
        }

        GameObject props = GameObject.Find("Start_Props");
        GameObject background = GameObject.Find("3Dobj");

        reveal.props = props;
        reveal.hideWhenNotFinished = background != null ? new[] { background } : new GameObject[0];
        reveal.forceShow = false;
        EditorUtility.SetDirty(reveal);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[StartMinimal] props={(props != null)} background={(background != null)} configured");
    }

    [MenuItem("Tools/DSH/Phase2/Preview Start Minimal")]
    public static void Preview()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Start.unity", OpenSceneMode.Single);
        StartSceneReveal reveal = Object.FindAnyObjectByType<StartSceneReveal>();
        if (reveal == null) return;

        GameObject props = GameObject.Find("Start_Props");
        GameObject background = GameObject.Find("3Dobj");
        if (props != null) props.SetActive(false);
        if (background != null) background.SetActive(false);

        DshCapture.Capture("Main Camera", "E:/DSH_Unity/shots/start-minimal.png");

        if (props != null) props.SetActive(true);
        if (background != null) background.SetActive(true);
        EditorSceneManager.MarkSceneDirty(scene);
    }

    [MenuItem("Tools/DSH/Phase2/Preview Start Full")]
    public static void PreviewFull()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Start.unity", OpenSceneMode.Single);
        DshCapture.Capture("Main Camera", "E:/DSH_Unity/shots/start-full.png");
    }
}
