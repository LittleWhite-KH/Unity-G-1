using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Assigns the curved portal material and reports shader availability.</summary>
public static class DshPhase3Portal
{
    const string MatPath = "Assets/Materials/Interior/TV/TV_PortalCurved.mat";
    static readonly string Report = "E:/DSH_Unity/tmp-read/phase3-portal.txt";

    [MenuItem("Tools/DSH/Phase3/Assign Portal Material")]
    public static void Assign()
    {
        var sb = new StringBuilder();
        Shader curved = Shader.Find("DSH/TvPortalCurved");
        Shader legacy = Shader.Find("DSH/TvPortal");
        Shader screenShader = Shader.Find("DSH/TvPortalScreen");
        sb.AppendLine($"curved={(curved != null)} legacy={(legacy != null)} screen={(screenShader != null)}");

        // Planar portal is a simple screen quad: the clear mapping is the quad's own UV.
        Shader use = screenShader != null ? screenShader : legacy;
        if (use != null)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
            if (mat == null)
            {
                mat = new Material(use) { name = "TV_PortalCurved" };
                AssetDatabase.CreateAsset(mat, MatPath);
                sb.AppendLine("material created");
            }
            else
            {
                mat.shader = use;
                sb.AppendLine("material reused");
            }
            mat.SetColor("_Tint", Color.white);
            mat.SetFloat("_Glow", 0.02f);
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();

            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Gaming.unity", OpenSceneMode.Single);
            TvPortal portal = Object.FindAnyObjectByType<TvPortal>();
            if (portal != null)
            {
                portal.portalMaterial = mat;
                EditorUtility.SetDirty(portal);
                if (portal.portalSurface != null)
                {
                    MeshFilter filter = portal.portalSurface.GetComponent<MeshFilter>();
                    Mesh mesh = filter != null ? filter.sharedMesh : null;
                    sb.AppendLine($"portal surface mesh={(mesh != null ? mesh.name : "null")} verts={(mesh != null ? mesh.vertexCount : 0)}");
                }
                sb.AppendLine("material assigned to TvPortal");
            }
            else sb.AppendLine("TvPortal missing");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        else
        {
            // Fallback: keep the legacy planar shader.
            sb.AppendLine("curved shader missing, legacy will be used");
        }

        File.WriteAllText(Report, sb.ToString());
        Debug.Log("[Phase3Portal] assign done");
    }

    [MenuItem("Tools/DSH/Phase3/Check Portal Setup")]
    public static void Check()
    {
        var sb = new StringBuilder();
        TvPortal portal = Object.FindAnyObjectByType<TvPortal>();
        if (portal == null) { sb.AppendLine("TvPortal missing"); File.WriteAllText(Report, sb.ToString()); return; }

        sb.AppendLine($"portalMaterial={(portal.portalMaterial != null ? portal.portalMaterial.name + "/" + portal.portalMaterial.shader.name : "null")}");
        sb.AppendLine($"portalShader={(portal.portalShader != null ? portal.portalShader.name : "null")}");
        sb.AppendLine($"surface={(portal.portalSurface != null ? portal.portalSurface.name : "null")}");
        if (portal.portalSurface != null)
        {
            MeshFilter filter = portal.portalSurface.GetComponent<MeshFilter>();
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            sb.AppendLine($"surface mesh={(mesh != null ? mesh.name : "null")} verts={(mesh != null ? mesh.vertexCount : 0)} bounds={(mesh != null ? mesh.bounds.size : Vector3.zero)}");
            sb.AppendLine($"surface material={(portal.portalSurface.sharedMaterial != null ? portal.portalSurface.sharedMaterial.name : "null")}");
        }
        if (portal.glassSurface != null)
        {
            MeshFilter filter = portal.glassSurface.GetComponent<MeshFilter>();
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            sb.AppendLine($"glass mesh={(mesh != null ? mesh.name : "null")} verts={(mesh != null ? mesh.vertexCount : 0)}");
        }

        File.WriteAllText(Report, sb.ToString());
        Debug.Log("[Phase3Portal] check -> " + Report);
    }
}
