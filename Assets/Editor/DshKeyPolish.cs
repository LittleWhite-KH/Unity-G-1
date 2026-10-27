using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Phase 2 polish: key lights overlap the keys, keys move off the computers.</summary>
public static class DshKeyPolish
{
    static readonly string Report = "E:/DSH_Unity/tmp-read/key-polish.txt";

    [MenuItem("Tools/DSH/Phase2/Polish Keys")]
    public static void Polish()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        var sb = new StringBuilder();
        sb.AppendLine("Key polish");

        Key_A(sb, "Key_A");
        Key_A(sb, "Key_B");

        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Directory.CreateDirectory(Path.GetDirectoryName(Report));
        File.WriteAllText(Report, sb.ToString());
        Debug.Log("[KeyPolish] done -> " + Report);
    }

    static void Key_A(StringBuilder sb, string name)
    {
        GameObject key = GameObject.Find(name);
        if (key == null)
        {
            sb.AppendLine(name + ": NOT FOUND");
            return;
        }

        // Start each pass from the authored position so the search is deterministic.
        Vector3 authored = name == "Key_A"
            ? new Vector3(2.476f, 0.951f, 8.956f)
            : new Vector3(-20.179f, 0.932f, 3f);
        key.transform.position = authored;

        sb.AppendLine($"{name}: start={F(key.transform.position)}");

        Vector3 candidate = FindClearSpot(key, sb);
        if (candidate != Vector3.zero)
        {
            key.transform.position = candidate;
            EditorUtility.SetDirty(key);
            sb.AppendLine($"  moved to {F(candidate)}");
        }
        else
        {
            sb.AppendLine("  no clear spot found, key stays put");
        }

        // Key light sits exactly on the visible model centre.
        Vector3 centre = ModelCentre(key);
        foreach (Light light in key.GetComponentsInChildren<Light>(true))
        {
            light.transform.position = centre;
            EditorUtility.SetDirty(light);
            sb.AppendLine($"  light {light.name} -> {F(centre)} type={light.type} range={light.range} intensity={light.intensity}");
        }

        foreach (Renderer renderer in key.GetComponentsInChildren<Renderer>(true))
        {
            sb.AppendLine($"  renderer {renderer.name} bounds={F(renderer.bounds.center)} size={F(renderer.bounds.size)}");
        }
    }

    static Vector3 ModelCentre(GameObject key)
    {
        bool has = false;
        Bounds bounds = new Bounds(key.transform.position, Vector3.zero);
        foreach (Renderer renderer in key.GetComponentsInChildren<Renderer>(true))
        {
            if (!renderer.gameObject.activeInHierarchy) continue;
            if (!has) { bounds = renderer.bounds; has = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        return has ? bounds.center : key.transform.position;
    }

    static Vector3 FindClearSpot(GameObject key, StringBuilder sb)
    {
        Vector3 origin = key.transform.position;
        float[] radii = { 1.0f, 1.35f, 1.7f, 2.05f };
        Vector3[] directions =
        {
            new Vector3(1, 0, 0), new Vector3(-1, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 0, -1),
            new Vector3(0.707f, 0, 0.707f), new Vector3(-0.707f, 0, 0.707f),
            new Vector3(0.707f, 0, -0.707f), new Vector3(-0.707f, 0, -0.707f),
        };

        foreach (float radius in radii)
        {
            foreach (Vector3 direction in directions)
            {
                Vector3 candidate = origin + direction * radius;
                if (IsClear(key, candidate))
                {
                    return candidate;
                }
            }
        }

        sb.AppendLine("  every candidate blocked");
        return Vector3.zero;
    }

    static bool IsClear(GameObject key, Vector3 position)
    {
        // Keep the key above the floor and away from any collider that is not part of the key.
        Collider[] hits = Physics.OverlapSphere(position, 0.38f, ~0, QueryTriggerInteraction.Ignore);
        foreach (Collider hit in hits)
        {
            if (hit == null) continue;
            if (hit.transform.IsChildOf(key.transform)) continue;
            return false;
        }

        // Also avoid visual overlap with the computer / desk it used to sit on.
        const float clearance = 0.55f;
        foreach (Renderer renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (renderer == null) continue;
            if (renderer.transform.IsChildOf(key.transform)) continue;
            string n = renderer.name;
            if (n.IndexOf("OldPC", System.StringComparison.OrdinalIgnoreCase) < 0 &&
                n.IndexOf("Desk", System.StringComparison.OrdinalIgnoreCase) < 0 &&
                n.IndexOf("Table", System.StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }
            if (renderer.bounds.SqrDistance(position) < clearance * clearance) return false;
        }

        // Ground must exist below, otherwise the key floats over a void.
        if (!Physics.Raycast(position + Vector3.up * 0.1f, Vector3.down, 2.5f, ~0, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        return true;
    }

    static string F(Vector3 v) => $"({v.x:0.###},{v.y:0.###},{v.z:0.###})";
}
