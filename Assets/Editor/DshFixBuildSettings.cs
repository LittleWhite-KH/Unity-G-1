using UnityEditor;
using UnityEngine;

/// <summary>把 Start / Gaming / Ending 写进 Build Settings 并保存工程设置。</summary>
public static class DshFixBuildSettings
{
    [MenuItem("Tools/DSH/Setup Build Settings")]
    public static void Setup()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/Start.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Gaming.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Ending.unity", true),
        };

        AssetDatabase.SaveAssets();
        EditorApplication.ExecuteMenuItem("File/Save Project");
        Debug.Log("[Build] scenes = Start(0), Gaming(1), Ending(2)");
    }
}
