using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Dump a scene hierarchy (including inactive objects) to tmp-read for inspection.</summary>
public static class DshDumpScene
{
    static readonly string OutDir = "E:/DSH_Unity/tmp-read";

    [MenuItem("Tools/DSH/Audit/Dump Start Scene")]
    public static void DumpStart() => Dump("Assets/Scenes/Start.unity", "start-tree.txt");

    [MenuItem("Tools/DSH/Audit/Dump Gaming Scene")]
    public static void DumpGaming() => Dump("Assets/Scenes/Gaming.unity", "gaming-tree.txt");

    static void Dump(string scenePath, string fileName)
    {
        Directory.CreateDirectory(OutDir);
        var sb = new StringBuilder();
        var previous = EditorSceneManager.GetActiveScene().path;
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        sb.AppendLine("SCENE " + scene.name);
        foreach (var root in scene.GetRootGameObjects())
        {
            Walk(root.transform, 0, sb);
        }

        File.WriteAllText(Path.Combine(OutDir, fileName), sb.ToString(), Encoding.UTF8);
        Debug.Log("[Dump] " + scene.name + " -> " + fileName);

        if (!string.IsNullOrEmpty(previous)) EditorSceneManager.OpenScene(previous);
    }

    static void Walk(Transform t, int depth, StringBuilder sb)
    {
        sb.Append(' ', depth * 2);
        sb.Append(t.name).Append(" active=").Append(t.gameObject.activeSelf)
          .Append(" layer=").Append(t.gameObject.layer)
          .Append(" pos=").Append(F(t.position));
        var renderer = t.GetComponent<Renderer>();
        if (renderer != null)
        {
            sb.Append(" mat=").Append(renderer.sharedMaterial != null ? renderer.sharedMaterial.name : "-");
            sb.Append(" bounds=").Append(F(renderer.bounds.center)).Append(" size=").Append(F(renderer.bounds.size));
        }
        var light = t.GetComponent<Light>();
        if (light != null)
        {
            sb.Append(" LIGHT type=").Append(light.type).Append(" range=").Append(light.range).Append(" intensity=").Append(light.intensity)
              .Append(" size=").Append(light.areaSize).Append(" bake=").Append(light.lightmapBakeType)
              .Append(" color=").Append(light.color).Append(" shadows=").Append(light.shadows);
        }
        var camera = t.GetComponent<Camera>();
        if (camera != null)
        {
            sb.Append(" CAM fov=").Append(camera.fieldOfView).Append(" depth=").Append(camera.depth);
        }
        var canvas = t.GetComponent<Canvas>();
        if (canvas != null)
        {
            sb.Append(" CANVAS mode=").Append(canvas.renderMode);
        }
        var behaviours = t.GetComponents<MonoBehaviour>();
        if (behaviours.Length > 0)
        {
            sb.Append(" scripts=");
            foreach (var b in behaviours)
            {
                sb.Append(b == null ? "MISSING" : b.GetType().Name).Append(',');
            }
        }
        sb.AppendLine();

        for (int i = 0; i < t.childCount; i++) Walk(t.GetChild(i), depth + 1, sb);
    }

    static string F(Vector3 v) => $"({v.x:0.###},{v.y:0.###},{v.z:0.###})";
}
