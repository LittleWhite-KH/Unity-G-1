using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Key pickup colors: soft-disk key is blue, disc key is orange.
/// The two TV cabinet slots use the same language (floppy slot blue, disc slot orange),
/// so the player can match key color to slot color.
/// </summary>
public static class DshKeyColors
{
    const string Dir = "Assets/Materials/Interior/TV";
    const string OrangePath = Dir + "/Key_Glow_Orange.mat";
    const string BluePath = Dir + "/Key_Glow_Blue.mat";
    static readonly string Report = "E:/DSH_Unity/tmp-read/key-colors.txt";

    [MenuItem("Tools/DSH/Keys/Apply Blue Floppy And Orange Disc")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        Material orange = Load(OrangePath);
        Material blue = Load(BluePath);
        if (orange == null) { Debug.LogError("[Keys] orange material missing: " + OrangePath); return; }
        if (blue == null)
        {
            if (!AssetDatabase.CopyAsset(OrangePath, BluePath))
            {
                Debug.LogError("[Keys] failed to copy " + OrangePath + " -> " + BluePath);
                return;
            }
            AssetDatabase.Refresh();
            blue = Load(BluePath);
        }
        if (blue == null) { Debug.LogError("[Keys] blue material missing after copy"); return; }

        blue.SetColor("_BaseColor", new Color(0.16f, 0.55f, 1f, 1f));
        blue.SetColor("_Color", new Color(0.16f, 0.55f, 1f, 1f));
        blue.EnableKeyword("_EMISSION");
        blue.SetColor("_EmissionColor", new Color(0.22f, 0.85f, 1.6f, 1f));
        blue.SetFloat("_Smoothness", 0.4f);
        EditorUtility.SetDirty(blue);

        orange.SetColor("_BaseColor", new Color(1f, 0.52f, 0.12f, 1f));
        orange.SetColor("_Color", new Color(1f, 0.52f, 0.12f, 1f));
        orange.EnableKeyword("_EMISSION");
        orange.SetColor("_EmissionColor", new Color(2.2f, 1f, 0.3f, 1f));
        EditorUtility.SetDirty(orange);

        var report = new StringBuilder();
        report.AppendLine("Key color pass");

        PaintKey("Key_A", blue, new Color(0.35f, 0.7f, 1f), report);
        PaintKey("Key_B", orange, new Color(1f, 0.58f, 0.22f), report);

        AssetDatabase.SaveAssets();

        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        DumpPortalRoom(report);

        Directory.CreateDirectory(Path.GetDirectoryName(Report));
        File.WriteAllText(Report, report.ToString(), Encoding.UTF8);
        Debug.Log("[Keys] color pass done -> " + Report);
    }

    static Material Load(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Material>(path);
    }

    static void PaintKey(string keyName, Material material, Color lightColor, StringBuilder report)
    {
        GameObject key = GameObject.Find(keyName);
        if (key == null)
        {
            report.AppendLine(keyName + ": NOT FOUND");
            return;
        }

        report.AppendLine($"{keyName}: pos={key.transform.position} active={key.activeInHierarchy}");
        foreach (var renderer in key.GetComponentsInChildren<Renderer>(true))
        {
            if (!renderer.gameObject.activeInHierarchy) continue;
            var mats = renderer.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = material;
            renderer.sharedMaterials = mats;
            EditorUtility.SetDirty(renderer);
            report.AppendLine($"  renderer={renderer.name} layer={renderer.gameObject.layer} mat={material.name} bounds={renderer.bounds}");
        }

        foreach (var light in key.GetComponentsInChildren<Light>(true))
        {
            light.color = lightColor;
            light.intensity = 1.6f;
            EditorUtility.SetDirty(light);
            report.AppendLine($"  light={light.name} color={light.color} intensity={light.intensity} range={light.range} enabled={light.enabled} type={light.type}");
        }
    }

    static void DumpPortalRoom(StringBuilder report)
    {
        report.AppendLine();
        report.AppendLine("White world renderers");
        GameObject room = GameObject.Find("TV_ExitRoom");
        if (room == null)
        {
            report.AppendLine("TV_ExitRoom: NOT FOUND");
        }
        else
        {
            report.AppendLine($"TV_ExitRoom layer={room.layer} active={room.activeInHierarchy}");
            foreach (var renderer in room.GetComponentsInChildren<Renderer>(true))
            {
                string path = renderer.name;
                Transform parent = renderer.transform.parent;
                while (parent != null && parent != room.transform) { path = parent.name + "/" + path; parent = parent.parent; }
                report.AppendLine($"  {path} layer={renderer.gameObject.layer} enabled={renderer.enabled} active={renderer.gameObject.activeInHierarchy} bounds={renderer.bounds} mat={(renderer.sharedMaterial != null ? renderer.sharedMaterial.name : "null")}");
            }
        }

        report.AppendLine();
        report.AppendLine("Cullers / cameras");
        foreach (var culler in Object.FindObjectsByType<ObjectCuller>(FindObjectsSortMode.None))
        {
            report.AppendLine($"  ObjectCuller on {culler.name} includeLayers={culler.includeLayers.value} cullDistance={culler.cullDistance} enabled={culler.enabled}");
        }
        foreach (var culler in Object.FindObjectsByType<LightCuller>(FindObjectsSortMode.None))
        {
            report.AppendLine($"  LightCuller on {culler.name} enabled={culler.enabled}");
        }
        TvPortal portal = Object.FindAnyObjectByType<TvPortal>();
        if (portal != null)
        {
            report.AppendLine($"  TvPortal player={(portal.player != null ? portal.player.name : "null")} camera={(portal.playerCamera != null ? portal.playerCamera.name : "null")} roomLayer={portal.exitRoomLayer}");
            if (portal.playerCamera != null)
            {
                report.AppendLine($"  Main camera cullingMask=0x{portal.playerCamera.cullingMask:X} clearFlags={portal.playerCamera.clearFlags}");
            }
            if (portal.portalCamera != null)
            {
                report.AppendLine($"  Portal camera cullingMask=0x{portal.portalCamera.cullingMask:X} enabled={portal.portalCamera.enabled} depth={portal.portalCamera.depth}");
            }
        }
    }

    // ------------------------------------------------------------------
    [MenuItem("Tools/DSH/Fix/Cullers Ignore White Room")]
    public static void FixCullers()
    {
        int roomMask = 1 << 30;
        int count = 0;

        foreach (var culler in Object.FindObjectsByType<ObjectCuller>(FindObjectsSortMode.None))
        {
            culler.includeLayers = culler.includeLayers.value & ~roomMask;
            EditorUtility.SetDirty(culler);
            count++;
        }
        foreach (var culler in Object.FindObjectsByType<LightCuller>(FindObjectsSortMode.None))
        {
            culler.includeLayers = culler.includeLayers.value & ~roomMask;
            EditorUtility.SetDirty(culler);
            count++;
        }

        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Fix] cullers ignoring layer 30: " + count);
    }

    // ------------------------------------------------------------------
    [MenuItem("Tools/DSH/Keys/Report")]
    public static void ReportOnly()
    {
        var report = new StringBuilder();
        DumpPortalRoom(report);
        foreach (string name in new[] { "Key_A", "Key_B" })
        {
            GameObject key = GameObject.Find(name);
            if (key == null) { report.AppendLine(name + ": NOT FOUND"); continue; }
            report.AppendLine($"{name}: pos={key.transform.position}");
            foreach (var renderer in key.GetComponentsInChildren<Renderer>(true))
            {
                report.AppendLine($"  renderer={renderer.name} layer={renderer.gameObject.layer} mat={(renderer.sharedMaterial != null ? renderer.sharedMaterial.name : "null")}");
            }
            foreach (var light in key.GetComponentsInChildren<Light>(true))
            {
                report.AppendLine($"  light={light.name} color={light.color} intensity={light.intensity}");
            }
        }
        Directory.CreateDirectory(Path.GetDirectoryName(Report));
        File.WriteAllText(Report, report.ToString(), Encoding.UTF8);
        Debug.Log("[Keys] report -> " + Report);
    }
}
