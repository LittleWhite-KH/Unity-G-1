using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Turns the maze ceiling lights into wide, downward area-like lights.</summary>
public static class DshLampPolish
{
    static readonly string Report = "E:/DSH_Unity/tmp-read/lamp-polish.txt";

    [MenuItem("Tools/DSH/Phase2/Lamps To Area Light")]
    public static void ConvertLamps()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        GameObject group = GameObject.Find("MazeLights");
        var sb = new StringBuilder();
        if (group == null)
        {
            Debug.LogError("[Lamps] MazeLights not found");
            return;
        }

        int count = 0;
        foreach (Transform child in group.transform)
        {
            Light light = child.GetComponent<Light>();
            if (light == null) continue;

            light.type = LightType.Spot;
            light.spotAngle = 150f;
            light.innerSpotAngle = 105f;
            light.range = 24f;
            light.intensity = 4f;
            light.shadows = LightShadows.None;
            light.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            Vector3 p = light.transform.position;
            light.transform.position = new Vector3(p.x, 2.42f, p.z);

            EditorUtility.SetDirty(light);
            sb.AppendLine($"{child.name} pos={F(light.transform.position)} type={light.type} angle={light.spotAngle}/{light.innerSpotAngle} range={light.range} intensity={light.intensity}");
            count++;
        }

        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Directory.CreateDirectory(Path.GetDirectoryName(Report));
        File.WriteAllText(Report, sb.ToString());
        Debug.Log($"[Lamps] converted {count} lights -> wide spot");
    }

    [MenuItem("Tools/DSH/Phase2/Preview Maze Lights")]
    public static void PreviewMaze()
    {
        GameObject camGo = GameObject.Find("Player/Main Camera");
        if (camGo == null) { Debug.LogError("[Lamps] camera missing"); return; }

        Transform camT = camGo.transform;
        Vector3 oldPos = camT.position;
        Quaternion oldRot = camT.rotation;

        camT.position = new Vector3(0f, 1.65f, 3f);
        camT.rotation = Quaternion.Euler(6f, 0f, 0f);

        DshCapture.Capture("Player/Main Camera", "E:/DSH_Unity/shots/maze-lights.png");

        camT.position = oldPos;
        camT.rotation = oldRot;
    }

    static string F(Vector3 v) => $"({v.x:0.##},{v.y:0.##},{v.z:0.##})";
}
