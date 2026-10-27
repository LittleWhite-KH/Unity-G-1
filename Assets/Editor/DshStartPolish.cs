using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Start scene polish: tame the over-exposed white room and give the TV real materials.</summary>
public static class DshStartPolish
{
    const string ReportPath = "E:/DSH_Unity/tmp-read/start-polish.txt";

    [MenuItem("Tools/DSH/Polish/Polish Start Scene")]
    public static void Polish()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Start.unity", OpenSceneMode.Single);
        var sb = new StringBuilder();
        Shader surface = Shader.Find("DSH/Surface");

        // ---- exposure override -------------------------------------------
        string profilePath = "Assets/Settings/StartProfile.asset";
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, profilePath);
        }

        var color = profile.TryGet<ColorAdjustments>(out var existing) ? existing : profile.Add<ColorAdjustments>(true);
        color.active = true;
        color.postExposure.value = -0.55f;
        color.contrast.value = 8f;
        color.saturation.value = 4f;

        var tonemap = profile.TryGet<Tonemapping>(out var tmExisting) ? tmExisting : profile.Add<Tonemapping>(true);
        tonemap.active = true;
        tonemap.mode.value = TonemappingMode.Neutral;

        var bloom = profile.TryGet<Bloom>(out var bloomExisting) ? bloomExisting : profile.Add<Bloom>(true);
        bloom.active = true;
        bloom.threshold.value = 1.05f;
        bloom.intensity.value = 0.35f;
        bloom.scatter.value = 0.6f;

        EditorUtility.SetDirty(profile);

        GameObject old = GameObject.Find("Start Exposure Volume");
        if (old != null) Object.DestroyImmediate(old);

        GameObject go = new GameObject("Start Exposure Volume");
        var volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 20f;
        volume.weight = 6f;
        volume.profile = profile;

        sb.AppendLine("start exposure volume: postExposure -0.55, neutral tonemap, soft bloom");

        // ---- TV materials ------------------------------------------------
        if (surface != null)
        {
            UpgradeTv(surface, "Assets/Materials/Interior/TV/TV_Body.mat", new Color(0.86f, 0.85f, 0.82f), 0.30f, 0.22f, 0.16f, sb);
            UpgradeTv(surface, "Assets/Materials/Interior/TV/TV_Cabinet.mat", new Color(0.16f, 0.15f, 0.14f), 0.42f, 0.28f, 0.22f, sb);
            UpgradeTv(surface, "Assets/Materials/Start/White.mat", new Color(0.90f, 0.89f, 0.86f), 0.18f, 0.10f, 0.06f, sb);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        File.WriteAllText(ReportPath, sb.ToString(), Encoding.UTF8);
        Debug.Log("[StartPolish] -> " + ReportPath);
    }

    static void UpgradeTv(Shader shader, string path, Color tint, float smoothness, float detail, float grunge, StringBuilder sb)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            sb.AppendLine("missing " + path);
            return;
        }

        Texture baseMap = mat.HasProperty("_BaseMap") ? mat.GetTexture("_BaseMap") : null;

        mat.shader = shader;
        mat.SetTexture("_BaseMap", baseMap);
        mat.SetColor("_BaseColor", tint);
        mat.SetFloat("_Metallic", 0.05f);
        mat.SetFloat("_Smoothness", smoothness);
        mat.SetFloat("_DetailStrength", detail);
        mat.SetFloat("_DetailScale", 14f);
        mat.SetFloat("_GrungeStrength", grunge);
        mat.SetFloat("_GrungeScale", 0.8f);
        mat.SetFloat("_Pattern", 0f);
        mat.SetFloat("_PatternStrength", 0f);
        mat.SetFloat("_Triplanar", 1f);
        mat.SetFloat("_WorldTiling", 1.4f);
        mat.SetFloat("_PanelScale", 0f);
        mat.SetFloat("_WearStrength", 0.2f);
        mat.SetColor("_EmissionColor", Color.black);
        EditorUtility.SetDirty(mat);
        sb.AppendLine("upgraded " + mat.name);
    }
}
