using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Ending 走廊补光：提高主灯强度，并在门前加一盏暖白点光。</summary>
public static class DshTuneEndingLight
{
    [MenuItem("Tools/DSH/Ending/Tune Corridor Light")]
    public static void Tune()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Ending.unity", OpenSceneMode.Single);

        GameObject main = GameObject.Find("Ending_CorridorLight");
        if (main != null)
        {
            Light l = main.GetComponent<Light>();
            if (l != null)
            {
                l.intensity = 16f;
                l.range = 12f;
                Debug.Log("[EndingLight] corridor main light -> 16");
            }
        }

        GameObject old = GameObject.Find("Ending_DoorLight");
        if (old != null) Object.DestroyImmediate(old);

        GameObject doorLight = new GameObject("Ending_DoorLight");
        Light dl = doorLight.AddComponent<Light>();
        dl.type = LightType.Point;
        dl.range = 6f;
        dl.intensity = 9f;
        dl.color = new Color(1f, 0.93f, 0.82f);
        doorLight.transform.position = new Vector3(0f, 2.1f, -81.6f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[EndingLight] door light added");
    }
}
