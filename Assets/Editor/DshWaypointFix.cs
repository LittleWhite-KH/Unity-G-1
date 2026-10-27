using System.Collections.Generic;
using System.IO;
using System.Text;
using Outdoor.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Task 1: clean up the two maze waypoints.
///   tomb  -> lily vase  (GT_Vase / id "vase")
///   table -> IBM terminal (GT_Terminal / id "terminal")
/// Removes the leftover grave/table collision boxes, rebuilds the terminal stand,
/// and rewrites every player-facing reference (objective text, wall guides, map nodes, level asset).
/// </summary>
public static class DshWaypointFix
{
    const string ScenePath = "Assets/Scenes/Gaming.unity";
    const string ReportPath = "E:/DSH_Unity/tmp-read/waypoint-fix.txt";
    const string IbmPrefab = "Assets/Models/Furniture/IBM_PC/ibmpc_mesh.fbx";
    const string LevelPath = "Assets/Config/Level_01.asset";

    [MenuItem("Tools/DSH/Polish/Fix Waypoints")]
    public static void Fix()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var sb = new StringBuilder();

        FixVase(sb);
        FixTerminal(sb);
        FixTexts(sb);
        FixMapGraph(sb);
        FixLegacyComponents(sb);
        RemoveLeftovers(sb);
        FixLevelAsset(sb);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        File.WriteAllText(ReportPath, sb.ToString(), Encoding.UTF8);
        Debug.Log("[WaypointFix] -> " + ReportPath);
    }

    // ------------------------------------------------------------------
    // Vase waypoint (was GT_Tomb)
    // ------------------------------------------------------------------
    static void FixVase(StringBuilder sb)
    {
        GameObject gt = FindByPath("LevelProps/GT_Tomb") ?? FindByPath("LevelProps/GT_Vase");
        if (gt == null)
        {
            sb.AppendLine("GT_Vase: missing");
            return;
        }

        gt.name = "GT_Vase";

        // The old grave collider (1.59 x 2.29 x 2.25) was still sitting on the root.
        int removed = 0;
        foreach (var c in gt.GetComponents<Collider>())
        {
            Object.DestroyImmediate(c);
            removed++;
        }

        var guide = gt.GetComponent<GuidanceTarget>();
        if (guide != null)
        {
            guide.id = "vase";
            guide.label = "THE VASE";
        }

        Transform table = gt.transform.Find("Table_Under_Vase");
        if (table != null) RebuildBox(table.gameObject, sb);

        Transform vase = gt.transform.Find("Vase_Lily");
        if (vase != null) RebuildBox(vase.gameObject, sb);

        sb.AppendLine($"GT_Vase: removed {removed} legacy root collider(s); table+vase colliders rebuilt");
    }

    // ------------------------------------------------------------------
    // Terminal waypoint (was GT_Table)
    // ------------------------------------------------------------------
    static void FixTerminal(StringBuilder sb)
    {
        GameObject gt = FindByPath("LevelProps/GT_Table") ?? FindByPath("LevelProps/GT_Terminal");
        if (gt == null)
        {
            sb.AppendLine("GT_Terminal: missing");
            return;
        }

        gt.name = "GT_Terminal";

        // The old table left its collision box behind at z=-9 while the props had moved to -11.3.
        int removed = 0;
        foreach (var c in gt.GetComponents<Collider>())
        {
            Object.DestroyImmediate(c);
            removed++;
        }

        var guide = gt.GetComponent<GuidanceTarget>();
        if (guide != null)
        {
            guide.id = "terminal";
            guide.label = "THE TERMINAL";
        }

        Vector3 basePos = new Vector3(21f, 0f, -11.3f);
        gt.transform.position = basePos;

        Transform oldDesk = gt.transform.Find("桌台_简单");
        if (oldDesk != null)
        {
            Object.DestroyImmediate(oldDesk.gameObject);
            sb.AppendLine("GT_Terminal: removed old desk 桌台_简单 (legacy table model)");
        }

        Transform oldPc = gt.transform.Find("IBM_PC_Terminal");
        if (oldPc != null)
        {
            Object.DestroyImmediate(oldPc.gameObject);
            sb.AppendLine("GT_Terminal: removed old terminal instance");
        }

        float deskTop = 0.94f;
        GameObject stand = null;
        GameObject template = FindByPath("MazeRoot/Interior_FromBlend/SM_MTable_001");
        if (template != null)
        {
            stand = Object.Instantiate(template);
            stand.name = "Terminal_Stand";
            stand.transform.SetParent(gt.transform, true);
            stand.transform.localScale = new Vector3(0.601f, 1f, 1f);
            stand.transform.rotation = template.transform.rotation;
            AlignBottom(stand, basePos);
            RebuildBox(stand, sb);
            deskTop = BoundsOf(stand).max.y;
            sb.AppendLine($"GT_Terminal: stand rebuilt, top={deskTop:0.###}");
        }
        else
        {
            sb.AppendLine("GT_Terminal: SM_MTable_001 template missing, using floor height");
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(IbmPrefab);
        if (prefab == null)
        {
            sb.AppendLine("GT_Terminal: IBM prefab missing");
            return;
        }

        GameObject pc = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        PrefabUtility.UnpackPrefabInstance(pc, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        pc.name = "IBM_PC_Terminal";
        pc.transform.SetParent(gt.transform, true);
        ScaleToHeight(pc, 0.82f);
        pc.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        AlignBottom(pc, new Vector3(basePos.x, deskTop, basePos.z));

        Material ibmMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/Furniture/IBM_PC/IBM_PC_Mat.mat");
        if (ibmMat == null)
        {
            foreach (string guid in AssetDatabase.FindAssets("IBM_PC_Mat t:Material"))
            {
                ibmMat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                break;
            }
        }
        foreach (var r in pc.GetComponentsInChildren<Renderer>(true))
        {
            if (ibmMat != null) r.sharedMaterial = ibmMat;
        }

        RebuildBox(pc, sb);
        sb.AppendLine($"GT_Terminal: removed {removed} legacy root collider(s); terminal size={BoundsOf(pc).size} center={BoundsOf(pc).center}");
    }

    // ------------------------------------------------------------------
    // Player-facing text
    // ------------------------------------------------------------------
    static void FixTexts(StringBuilder sb)
    {
        var map = new Dictionary<string, string>
        {
            { "THE TOMB", "THE VASE" },
            { "FIND THE TOMB", "FIND THE VASE" },
            { "CLICK THE TOMB", "CLICK THE VASE" },
            { "CROSS THE TOMB", "CROSS THE HALL" },
            { "THE TABLE", "THE TERMINAL" },
            { "FIND THE TABLE", "FIND THE TERMINAL" },
            { "CLICK THE TABLE", "CLICK THE TERMINAL" },
        };

        foreach (var tmp in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (tmp == null) continue;
            string text = tmp.text ?? string.Empty;
            if (map.TryGetValue(text, out string replacement))
            {
                sb.AppendLine($"text {PathOf(tmp.transform)}: \"{text}\" -> \"{replacement}\"");
                tmp.text = replacement;
                EditorUtility.SetDirty(tmp);
            }
        }
    }

    // ------------------------------------------------------------------
    // Map graph nodes
    // ------------------------------------------------------------------
    static void FixMapGraph(StringBuilder sb)
    {
        MapGraph graph = Object.FindFirstObjectByType<MapGraph>();
        if (graph == null)
        {
            sb.AppendLine("map graph missing");
            return;
        }

        var keep = new List<MapGraph.Node>();
        foreach (var node in graph.Nodes)
        {
            if (node.id == "tomb")
            {
                node.id = "vase";
                node.label = "THE VASE";
                node.worldPosition = new Vector3(-3f, 0f, 11.2f);
            }
            else if (node.id == "table")
            {
                node.id = "terminal";
                node.label = "THE TERMINAL";
                node.worldPosition = new Vector3(21f, 0f, -11.3f);
            }
            else if (node.id == "door" || node.id == "ending")
            {
                sb.AppendLine("map node dropped: " + node.id + " (legacy TV-interior flow)");
                continue;
            }

            keep.Add(node);
        }

        graph.ConfigureNodes(keep);
        EditorUtility.SetDirty(graph);
        sb.AppendLine("map graph nodes: " + keep.Count);
    }

    // ------------------------------------------------------------------
    // Legacy phase components
    // ------------------------------------------------------------------
    static void FixLegacyComponents(StringBuilder sb)
    {
        GameObject terminal = FindByPath("LevelProps/GT_Terminal");

        foreach (var phase in Object.FindObjectsByType<PhaseChargeTarget>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (phase == null) continue;

            if (phase.phaseName == "Level01_ChargeAtTomb") phase.phaseName = "Level01_ChargeAtVase";
            if (phase.phaseName == "Level02_ChargeAtTable") phase.phaseName = "Level02_ChargeAtTerminal";

            if (phase.objectiveText == "FIND THE TOMB") phase.objectiveText = "FIND THE VASE";
            if (phase.objectiveText == "FIND THE TABLE") phase.objectiveText = "FIND THE TERMINAL";

            if (phase.targetOverride != null && phase.targetOverride.name == "桌台_简单")
            {
                phase.targetOverride = terminal != null ? terminal.transform : null;
                sb.AppendLine($"legacy {phase.phaseName}: targetOverride retargeted");
            }

            EditorUtility.SetDirty(phase);
        }
    }

    // ------------------------------------------------------------------
    // Leftovers
    // ------------------------------------------------------------------
    static void RemoveLeftovers(StringBuilder sb)
    {
        string[] paths =
        {
            "Object Sketch/1C1/CanvasGrave",
            "LevelDirector/Phase01_LookAtTomb",
            "LevelProps/GT_Door",
            "LevelProps/GT_Ending",
            "HUD Canvas/MapPanel/MapContent/MapCard/MapArea/NodeLayer/Node_tomb",
            "HUD Canvas/MapPanel/MapContent/MapCard/MapArea/NodeLayer/Node_table",
            "HUD Canvas/MapPanel/MapContent/MapCard/MapArea/NodeLayer/Node_door",
            "HUD Canvas/MapPanel/MapContent/MapCard/MapArea/NodeLayer/Node_ending",
        };

        foreach (string path in paths)
        {
            GameObject go = FindByPath(path);
            if (go == null) continue;
            sb.AppendLine("removed leftover: " + path);
            Object.DestroyImmediate(go);
        }

        Rename("Guide_Tomb", "Guide_Vase", sb);
        Rename("Guide_Table", "Guide_Terminal", sb);
    }

    static void Rename(string from, string to, StringBuilder sb)
    {
        GameObject go = FindByPath(from);
        if (go == null) return;
        go.name = to;
        sb.AppendLine($"renamed {from} -> {to}");
    }

    // ------------------------------------------------------------------
    // Level asset
    // ------------------------------------------------------------------
    static void FixLevelAsset(StringBuilder sb)
    {
        var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(LevelPath);
        if (level == null)
        {
            sb.AppendLine("level asset missing");
            return;
        }

        foreach (var phase in level.phases)
        {
            if (phase == null) continue;

            if (phase.id == "charge_tomb")
            {
                phase.id = "charge_vase";
                phase.objective = "FIND THE VASE";
                phase.targetIds = new[] { "vase" };
                if (phase.waypointId == "tomb") phase.waypointId = "vase";
                if (phase.mapNodeId == "tomb") phase.mapNodeId = "vase";
                sb.AppendLine("level: charge_tomb -> charge_vase");
            }
            else if (phase.id == "charge_table")
            {
                phase.id = "charge_terminal";
                phase.objective = "FIND THE TERMINAL";
                phase.targetIds = new[] { "terminal" };
                if (phase.waypointId == "table") phase.waypointId = "terminal";
                if (phase.mapNodeId == "table") phase.mapNodeId = "terminal";
                sb.AppendLine("level: charge_table -> charge_terminal");
            }
            else
            {
                if (phase.waypointId == "table") phase.waypointId = "terminal";
                if (phase.waypointId == "tomb") phase.waypointId = "vase";
                if (phase.mapNodeId == "table") phase.mapNodeId = "terminal";
                if (phase.mapNodeId == "tomb") phase.mapNodeId = "vase";
                foreach (string id in phase.targetIds)
                {
                    if (id == "tomb" || id == "table") sb.AppendLine("level WARNING target id still " + id + " in " + phase.id);
                }
            }
        }

        EditorUtility.SetDirty(level);
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------
    static GameObject FindByPath(string path)
    {
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t == null) continue;
            if (PathOf(t) == path) return t.gameObject;
        }
        return null;
    }

    static string PathOf(Transform t)
    {
        if (t == null) return "null";
        var sb = new StringBuilder(t.name);
        Transform p = t.parent;
        while (p != null) { sb.Insert(0, p.name + "/"); p = p.parent; }
        return sb.ToString();
    }

    static Bounds BoundsOf(GameObject go)
    {
        bool has = false;
        Bounds bounds = new Bounds(go.transform.position, Vector3.zero);
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            if (!r.gameObject.activeInHierarchy || !r.enabled) continue;
            if (!has) { bounds = r.bounds; has = true; }
            else bounds.Encapsulate(r.bounds);
        }
        return bounds;
    }

    static void AlignBottom(GameObject go, Vector3 groundCenter)
    {
        Physics.SyncTransforms();
        Bounds b = BoundsOf(go);
        Vector3 delta = new Vector3(groundCenter.x - b.center.x, groundCenter.y - b.min.y, groundCenter.z - b.center.z);
        go.transform.position += delta;
        Physics.SyncTransforms();
    }

    static void ScaleToHeight(GameObject go, float targetHeight)
    {
        Physics.SyncTransforms();
        Bounds b = BoundsOf(go);
        if (b.size.y <= 0.0001f) return;
        go.transform.localScale *= targetHeight / b.size.y;
        Physics.SyncTransforms();
    }

    static void RebuildBox(GameObject go, StringBuilder sb)
    {
        foreach (var c in go.GetComponents<Collider>()) Object.DestroyImmediate(c);

        Physics.SyncTransforms();
        Bounds b = BoundsOf(go);
        if (b.size.sqrMagnitude < 0.000001f) return;

        Vector3 scale = go.transform.lossyScale;
        var box = go.AddComponent<BoxCollider>();
        box.size = new Vector3(
            b.size.x / Mathf.Max(0.0001f, Mathf.Abs(scale.x)),
            b.size.y / Mathf.Max(0.0001f, Mathf.Abs(scale.y)),
            b.size.z / Mathf.Max(0.0001f, Mathf.Abs(scale.z)));
        box.center = go.transform.InverseTransformPoint(b.center);
        sb.AppendLine($"  collider rebuilt {go.name}: size={b.size} center={b.center}");
    }
}
