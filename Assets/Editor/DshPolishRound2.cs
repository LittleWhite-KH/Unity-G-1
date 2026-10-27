using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Round 2 requests: grey ending door + a U-key switch that hides every UI layer for clean screenshots.
/// </summary>
public static class DshPolishRound2
{
    const string ReportPath = "E:/DSH_Unity/tmp-read/round2-polish.txt";
    const string DoorMatPath = "Assets/Models/Furniture/DoorEnd/EndDoor_Wood.mat";

    [MenuItem("Tools/DSH/Polish/Grey End Door")]
    public static void GreyDoor()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Gaming.unity", OpenSceneMode.Single);
        var sb = new StringBuilder();

        var mat = AssetDatabase.LoadAssetAtPath<Material>(DoorMatPath);
        if (mat == null)
        {
            Debug.LogError("[Round2] EndDoor_Wood.mat missing");
            return;
        }

        // Keep the door texture + surface detail, just repaint it a neutral grey.
        mat.SetColor("_BaseColor", new Color(0.40f, 0.41f, 0.43f, 1f));
        mat.SetFloat("_Smoothness", 0.38f);
        mat.SetFloat("_Metallic", 0.12f);
        mat.SetFloat("_DetailStrength", 0.24f);
        mat.SetFloat("_GrungeStrength", 0.18f);
        mat.SetFloat("_Triplanar", 0f);
        mat.SetFloat("_Pattern", 0f);
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();

        sb.AppendLine("EndDoor_Wood -> neutral grey (0.40, 0.41, 0.43)");
        File.WriteAllText(ReportPath, sb.ToString(), Encoding.UTF8);
        Debug.Log("[Round2] grey door applied");
    }

    [MenuItem("Tools/DSH/Polish/Setup UI Toggle")]
    public static void SetupToggle()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Gaming.unity", OpenSceneMode.Single);
        var sb = new StringBuilder();

        GameObject hud = GameObject.Find("HUD Canvas");
        if (hud == null)
        {
            Debug.LogError("[Round2] HUD Canvas missing");
            return;
        }

        var toggle = hud.GetComponent<HudVisibilityToggle>();
        if (toggle == null) toggle = hud.AddComponent<HudVisibilityToggle>();
        toggle.toggleKey = UnityEngine.InputSystem.Key.U;
        toggle.duration = 0.22f;
        toggle.slideDistance = 16f;
        toggle.includeWorldSpace = true;

        EditorUtility.SetDirty(toggle);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        sb.AppendLine("HudVisibilityToggle added to HUD Canvas (U key, 0.22s fade)");
        File.WriteAllText(ReportPath, sb.ToString(), Encoding.UTF8);
        Debug.Log("[Round2] UI toggle wired");
    }

    [MenuItem("Tools/DSH/Polish/Preview UI Toggle")]
    public static void PreviewToggle()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Gaming.unity", OpenSceneMode.Single);

        var toggle = Object.FindAnyObjectByType<HudVisibilityToggle>();
        if (toggle == null)
        {
            Debug.LogError("[Round2] HudVisibilityToggle missing");
            return;
        }

        GameObject camGo = GameObject.Find("Player/Main Camera");
        if (camGo == null) return;
        Transform camT = camGo.transform;
        Vector3 oldPos = camT.position;
        Quaternion oldRot = camT.rotation;

        camT.position = new Vector3(0f, 1.65f, 3f);
        camT.rotation = Quaternion.Euler(6f, 0f, 0f);

        DshCapture.Capture("Player/Main Camera", "E:/DSH_Unity/shots/ui-before.png");

        toggle.EditorPreview(true);
        DshCapture.Capture("Player/Main Camera", "E:/DSH_Unity/shots/ui-hidden.png");

        toggle.EditorPreview(false);
        DshCapture.Capture("Player/Main Camera", "E:/DSH_Unity/shots/ui-restored.png");

        camT.position = oldPos;
        camT.rotation = oldRot;

        Debug.Log("[Round2] UI toggle preview done");
    }
}
