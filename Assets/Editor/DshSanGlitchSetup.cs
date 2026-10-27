using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Sets up the SAN glitch fullscreen pass and previews it.</summary>
public static class DshSanGlitchSetup
{
    const string RendererPath = "Assets/Settings/PC_Renderer.asset";
    const string ShaderPath = "Assets/Shaders/SanGlitch.shader";
    const string MatPath = "Assets/Settings/SanGlitch.mat";
    static readonly string Report = "E:/DSH_Unity/tmp-read/san-glitch.txt";

    [MenuItem("Tools/DSH/San/Setup Glitch Feature")]
    public static void Setup()
    {
        var sb = new StringBuilder();

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
        if (mat == null)
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (shader == null) shader = Shader.Find("DSH/SanGlitch");
            if (shader == null)
            {
                Debug.LogError("[SanGlitch] shader missing");
                return;
            }
            mat = new Material(shader) { name = "SanGlitch" };
            AssetDatabase.CreateAsset(mat, MatPath);
            sb.AppendLine("material created");
        }
        else sb.AppendLine("material present");

        UniversalRendererData data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
        if (data == null)
        {
            Debug.LogError("[SanGlitch] renderer data missing");
            return;
        }

        // Drop the custom feature if it was registered by an earlier build.
        for (int i = data.rendererFeatures.Count - 1; i >= 0; i--)
        {
            if (data.rendererFeatures[i] is SanGlitchFeature)
            {
                Object.DestroyImmediate(data.rendererFeatures[i], true);
                data.rendererFeatures.RemoveAt(i);
                sb.AppendLine("custom feature removed");
            }
        }

        FullScreenPassRendererFeature fullScreen = null;
        foreach (ScriptableRendererFeature feature in data.rendererFeatures)
        {
            if (feature is FullScreenPassRendererFeature found) { fullScreen = found; break; }
        }

        if (fullScreen == null)
        {
            fullScreen = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
            fullScreen.name = "SanGlitchFullScreen";
            AssetDatabase.AddObjectToAsset(fullScreen, data);
            data.rendererFeatures.Add(fullScreen);
            sb.AppendLine("fullscreen feature created");
        }
        else sb.AppendLine("fullscreen feature present");

        fullScreen.passMaterial = mat;
        fullScreen.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.AfterRenderingPostProcessing;
        fullScreen.passIndex = 0;
        fullScreen.requirements = ScriptableRenderPassInput.None;
        EditorUtility.SetDirty(fullScreen);
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();

        // Add the controller to the player in the Gaming scene.
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Gaming.unity", OpenSceneMode.Single);
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            if (player.GetComponent<SanGlitchController>() == null)
            {
                player.AddComponent<SanGlitchController>();
                sb.AppendLine("controller added to Player");
            }
            else sb.AppendLine("controller already on Player");
        }
        else sb.AppendLine("Player missing");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        sb.AppendLine("PC_Renderer features:");
        foreach (ScriptableRendererFeature feature in data.rendererFeatures)
        {
            sb.AppendLine("  " + (feature != null ? feature.name : "null"));
        }

        File.WriteAllText(Report, sb.ToString());
        Debug.Log("[SanGlitch] setup done");
    }

    [MenuItem("Tools/DSH/San/Preview Glitch 0")]
    public static void Preview0() => Preview(0f, "san-glitch-0.png");

    [MenuItem("Tools/DSH/San/Preview Glitch 035")]
    public static void Preview035() => Preview(0.35f, "san-glitch-035.png");

    [MenuItem("Tools/DSH/San/Preview Glitch 065")]
    public static void Preview065() => Preview(0.65f, "san-glitch-065.png");

    [MenuItem("Tools/DSH/San/Preview Glitch 085")]
    public static void Preview085() => Preview(0.85f, "san-glitch-085.png");

    static void Preview(float value, string fileName)
    {
        Shader.SetGlobalFloat("_SanGlitch", value);
        Shader.SetGlobalFloat("_SanGlitchTime", (float)EditorApplication.timeSinceStartup);

        // Put the player camera somewhere with readable geometry.
        GameObject camGo = GameObject.Find("Player/Main Camera");
        if (camGo == null) { Debug.LogError("[SanGlitch] camera missing"); return; }

        Transform camT = camGo.transform;
        Vector3 oldPos = camT.position;
        Quaternion oldRot = camT.rotation;
        camT.position = new Vector3(0f, 1.65f, 3f);
        camT.rotation = Quaternion.Euler(6f, 0f, 0f);

        DshCapture.Capture("Player/Main Camera", "E:/DSH_Unity/shots/" + fileName);

        camT.position = oldPos;
        camT.rotation = oldRot;
        Shader.SetGlobalFloat("_SanGlitch", 0f);
        Debug.Log("[SanGlitch] preview " + value + " -> " + fileName);
    }
}
