using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Materials for the new props (IBM PC, lily vase).</summary>
public static class DshPhase3Materials
{
    const string IbmDir = "Assets/Models/Furniture/IBM_PC";
    const string IbmMatPath = "Assets/Models/Furniture/IBM_PC/IBM_PC_Mat.mat";
    static readonly string Report = "E:/DSH_Unity/tmp-read/phase3-materials.txt";

    [MenuItem("Tools/DSH/Phase3/Build Prop Materials")]
    public static void Build()
    {
        var sb = new StringBuilder();

        Texture2D baseColor = ImportTexture($"{IbmDir}/textures/ibm_lp_None_BaseColor.png", 1024, false);
        Texture2D normal = ImportTexture($"{IbmDir}/textures/ibm_lp_None_Normal.png", 1024, true);
        Texture2D metallic = ImportTexture($"{IbmDir}/textures/ibm_lp_None_Metallic.png", 512, false);
        Texture2D emissive = ImportTexture($"{IbmDir}/textures/ibm_lp_None_Emissive.png", 512, false);

        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(IbmMatPath);
        if (mat == null)
        {
            mat = new Material(lit) { name = "IBM_PC_Mat" };
            AssetDatabase.CreateAsset(mat, IbmMatPath);
        }
        mat.SetTexture("_BaseMap", baseColor);
        mat.SetTexture("_BumpMap", normal);
        mat.SetTexture("_MetallicGlossMap", metallic);
        mat.SetTexture("_EmissionMap", emissive);
        mat.SetFloat("_Metallic", 0.3f);
        mat.SetFloat("_Smoothness", 0.45f);
        mat.SetFloat("_BumpScale", 1f);
        mat.EnableKeyword("_NORMALMAP");
        mat.EnableKeyword("_METALLICSPECGLOSSMAP");
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
        sb.AppendLine($"IBM material base={(baseColor != null)} normal={(normal != null)} metal={(metallic != null)}");

        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Gaming.unity", OpenSceneMode.Single);
        GameObject pc = GameObject.Find("IBM_PC_Terminal");
        if (pc != null)
        {
            foreach (Renderer renderer in pc.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterial = mat;
            }
            sb.AppendLine("IBM material applied");
        }
        else sb.AppendLine("IBM_PC_Terminal missing");

        // Vase: the OBJ material is embedded; give the scene instance its own material.
        Material vaseMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/Furniture/VaseLily/VaseLily_Mat.mat");
        if (vaseMat == null)
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            vaseMat = new Material(litShader) { name = "VaseLily_Mat" };
            AssetDatabase.CreateAsset(vaseMat, "Assets/Models/Furniture/VaseLily/VaseLily_Mat.mat");
        }
        Texture2D vaseTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/Furniture/VaseLily/model_material_0.jpg");
        if (vaseTex != null) vaseMat.SetTexture("_BaseMap", vaseTex);
        vaseMat.SetFloat("_Smoothness", 0.35f);
        EditorUtility.SetDirty(vaseMat);

        GameObject vaseGo = GameObject.Find("Vase_Lily");
        if (vaseGo != null)
        {
            foreach (Renderer renderer in vaseGo.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterial = vaseMat;
            }
            sb.AppendLine($"vase material applied tex={(vaseTex != null)}");
        }
        else sb.AppendLine("Vase_Lily missing");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        File.WriteAllText(Report, sb.ToString());
        Debug.Log("[Phase3Materials] done");
    }

    static Texture2D ImportTexture(string path, int maxSize, bool normal)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return null;

        bool dirty = false;
        if (importer.maxTextureSize > maxSize) { importer.maxTextureSize = maxSize; dirty = true; }
        if (normal && importer.textureType != TextureImporterType.NormalMap)
        {
            importer.textureType = TextureImporterType.NormalMap;
            dirty = true;
        }
        if (dirty) importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    [MenuItem("Tools/DSH/Phase3/Preview Props Again")]
    public static void PreviewAgain()
    {
        DshPhase3.PreviewProps();
    }
}
