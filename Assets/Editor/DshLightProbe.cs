using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Isolate what makes the maze dark: fog / volume / ambient / lights.</summary>
public static class DshLightProbe
{
    [MenuItem("Tools/DSH/Polish/Light Probe")]
    public static void Probe()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Gaming.unity", OpenSceneMode.Single);

        GameObject camGo = GameObject.Find("Player/Main Camera");
        if (camGo == null) return;
        Transform camT = camGo.transform;
        Vector3 oldPos = camT.position;
        Quaternion oldRot = camT.rotation;
        camT.position = new Vector3(0f, 1.65f, 3f);
        camT.rotation = Quaternion.Euler(6f, 0f, 0f);

        var volume = GameObject.Find("Global Volume");
        var fogOn = RenderSettings.fog;
        var ambientMode = RenderSettings.ambientMode;
        var sky = RenderSettings.ambientSkyColor;

        Shot("probe-0-current");

        RenderSettings.fog = false;
        Shot("probe-1-nofog");

        if (volume != null) volume.SetActive(false);
        Shot("probe-2-novolume");

        RenderSettings.ambientMode = AmbientMode.Skybox;
        DynamicGI.UpdateEnvironment();
        Shot("probe-3-skybox");

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.6f, 0.6f, 0.65f);
        DynamicGI.UpdateEnvironment();
        Shot("probe-4-flat");

        // restore
        RenderSettings.fog = fogOn;
        RenderSettings.ambientMode = ambientMode;
        RenderSettings.ambientSkyColor = sky;
        if (volume != null) volume.SetActive(true);
        DynamicGI.UpdateEnvironment();

        camT.position = oldPos;
        camT.rotation = oldRot;
        Debug.Log("[LightProbe] done");
    }

    static void Shot(string name)
    {
        DshCapture.Capture("Player/Main Camera", "E:/DSH_Unity/shots/" + name + ".png");
    }
}
