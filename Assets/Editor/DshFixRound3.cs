using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Small robustness fixes found during the final project review.</summary>
public static class DshFixRound3
{
    static readonly string Report = "E:/DSH_Unity/tmp-read/fix-round3.txt";

    [MenuItem("Tools/DSH/Fix/Round 3 Cleanup")]
    public static void Fix()
    {
        var sb = new StringBuilder();

        // 1) TV model meshes must be readable for QuickOutline (the knobs were imported without it).
        var importer = AssetImporter.GetAtPath("Assets/3D/Start/tv.fbx") as ModelImporter;
        if (importer != null && !importer.isReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
            sb.AppendLine("tv.fbx readable enabled");
        }
        else sb.AppendLine("tv.fbx readable already / missing");

        // 2) EndDoor distance fields were serialized before the defaults changed.
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Gaming.unity", OpenSceneMode.Single);
        foreach (EndDoor door in Object.FindObjectsByType<EndDoor>(FindObjectsSortMode.None))
        {
            door.interactDistance = 5.5f;
            door.promptDistance = 9f;
            EditorUtility.SetDirty(door);
            sb.AppendLine($"EndDoor on {door.gameObject.name}: interact={door.interactDistance} prompt={door.promptDistance}");
        }
        foreach (TvPortal portal in Object.FindObjectsByType<TvPortal>(FindObjectsSortMode.None))
        {
            portal.waitForDoorInteraction = true;
            EditorUtility.SetDirty(portal);
            sb.AppendLine("TvPortal waitForDoorInteraction=true");
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        // 3) Ending scene is deprecated; keep it out of builds.
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        for (int i = 0; i < scenes.Length; i++)
        {
            if (scenes[i].path.EndsWith("Ending.unity") && scenes[i].enabled)
            {
                scenes[i] = new EditorBuildSettingsScene(scenes[i].path, false);
                sb.AppendLine("Ending scene disabled in build settings");
            }
        }
        EditorBuildSettings.scenes = scenes;

        File.WriteAllText(Report, sb.ToString());
        Debug.Log("[Fix] round 3 cleanup done");
    }
}
