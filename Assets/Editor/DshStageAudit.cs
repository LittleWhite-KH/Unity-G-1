using System.Collections.Generic;
using System.IO;
using System.Text;
using Outdoor.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>Read-only audit of the Gaming scene: waypoints, guidance wiring, end door, dark room, white world.</summary>
public static class DshStageAudit
{
    const string OutDir = "E:/DSH_Unity/tmp-read";

    [MenuItem("Tools/DSH/Audit/Stage Audit")]
    public static void Run()
    {
        Directory.CreateDirectory(OutDir);
        EditorSceneManager.OpenScene("Assets/Scenes/Gaming.unity", OpenSceneMode.Single);
        var sb = new StringBuilder();

        sb.AppendLine("=== GUIDANCE TARGETS ===");
        foreach (var gt in All<GuidanceTarget>())
        {
            sb.AppendLine($"{PathOf(gt.transform)} id={gt.id} label={gt.label} kind={gt.kind} pos={F(gt.transform.position)} " +
                          $"hOff={gt.heightOffset} discovered={gt.discovered} map={gt.showOnMap} collected={gt.collected} active={gt.gameObject.activeInHierarchy}");
        }

        sb.AppendLine();
        sb.AppendLine("=== CHARGE SYSTEMS ===");
        foreach (var cs in All<ChargeSystem>())
        {
            sb.AppendLine($"{PathOf(cs.transform)} enabled={cs.enabled} target={(cs.target != null ? PathOf(cs.target) : "null")} " +
                          $"targetPos={(cs.target != null ? F(cs.target.position) : "-")} chargeDist={cs.chargeDistance} angle={cs.angleThreshold} " +
                          $"duration={cs.chargeDuration} guidanceOnly={cs.guidanceOnly}");
        }

        sb.AppendLine();
        sb.AppendLine("=== PHASE CHARGE TARGETS (legacy components) ===");
        foreach (var p in All<PhaseChargeTarget>())
        {
            sb.AppendLine($"{PathOf(p.transform)} phase={p.phaseName} active={p.gameObject.activeInHierarchy} targetOverride={(p.targetOverride != null ? PathOf(p.targetOverride) : "null")}");
            sb.AppendLine($"    uiHintRoot={(p.uiHintRoot != null ? PathOf(p.uiHintRoot.transform) : "null")} worldHint={(p.worldHint != null ? PathOf(p.worldHint.transform) : "null")} clickTarget={(p.clickTarget != null ? PathOf(p.clickTarget.transform) : "null")} requireClick={p.requireClickToComplete}");
            if (p.worldHint != null) sb.AppendLine("    worldHint.text=" + TmpText(p.worldHint.gameObject));
            if (p.clickTarget != null) sb.AppendLine("    click.target=" + (p.clickTarget.target != null ? PathOf(p.clickTarget.target) : "null"));
        }

        sb.AppendLine();
        sb.AppendLine("=== WORLD HINTS ===");
        foreach (var w in All<WorldHint>())
        {
            sb.AppendLine($"{PathOf(w.transform)} active={w.gameObject.activeInHierarchy} text={TmpText(w.gameObject)}");
        }

        sb.AppendLine();
        sb.AppendLine("=== WORLD CLICK TARGETS ===");
        foreach (var c in All<WorldClickTarget>())
        {
            sb.AppendLine($"{PathOf(c.transform)} active={c.gameObject.activeInHierarchy} target={(c.target != null ? PathOf(c.target) : "null")} maxDist={c.maxDistance}");
        }

        sb.AppendLine();
        sb.AppendLine("=== MAP GRAPH NODES ===");
        foreach (var g in All<MapGraph>())
        {
            sb.AppendLine(PathOf(g.transform) + " nodes=" + g.Nodes.Count);
            foreach (var n in g.Nodes)
            {
                sb.AppendLine($"    node id={n.id} label={n.label} pos={F(n.worldPosition)} target={n.isTarget} visited={n.visited} confirmed={n.confirmed} discovered={n.discovered}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("=== WAYPOINT PROPS DETAIL ===");
        DumpTree(GameObject.Find("GT_Tomb"), sb);
        DumpTree(GameObject.Find("GT_Table"), sb);

        sb.AppendLine();
        sb.AppendLine("=== NAME MATCHES (tomb / grave / table / vase / ibm / terminal) ===");
        foreach (var t in All<Transform>())
        {
            string n = t.name.ToLowerInvariant();
            if (n.Contains("tomb") || n.Contains("grave") || n.Contains("table") || n.Contains("vase") ||
                n.Contains("ibm") || n.Contains("terminal"))
            {
                sb.AppendLine($"  {PathOf(t)} active={t.gameObject.activeInHierarchy} layer={t.gameObject.layer}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("=== MATERIAL ASSETS CONTAINING tomb/grave ===");
        foreach (string guid in AssetDatabase.FindAssets("t:Material"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string n = FileName(path).ToLowerInvariant();
            if (n.Contains("tomb") || n.Contains("grave")) sb.AppendLine("  " + path);
        }

        sb.AppendLine();
        sb.AppendLine("=== END DOOR / WHITE WORLD / DARK ROOM TREES ===");
        DumpTree(GameObject.Find("TV_ExitRoom"), sb);
        DumpTree(GameObject.Find("TV_DarkRoom"), sb);
        DumpTree(GameObject.Find("EndDoorRoot"), sb);
        DumpTree(GameObject.Find("EndDoorHinge"), sb);

        sb.AppendLine();
        sb.AppendLine("=== COLLIDERS NEAR WAYPOINTS ===");
        DumpCollidersNear(new Vector3(-3f, 0f, 11.2f), 6f, sb, "GT_Tomb");
        DumpCollidersNear(new Vector3(21f, 0f, -11f), 6f, sb, "GT_Table");

        sb.AppendLine();
        sb.AppendLine("=== LIGHTS ===");
        foreach (var l in All<Light>())
        {
            sb.AppendLine($"{PathOf(l.transform)} type={l.type} range={l.range} intensity={l.intensity} color={l.color} bake={l.lightmapBakeType} shadows={l.shadows} active={l.gameObject.activeInHierarchy}");
        }

        sb.AppendLine();
        sb.AppendLine("=== RENDERERS WITHOUT MATERIAL / DEFAULT LIT ===");
        foreach (var r in All<Renderer>())
        {
            if (!r.gameObject.activeInHierarchy) continue;
            var mats = r.sharedMaterials;
            if (mats.Length == 0 || mats[0] == null)
            {
                sb.AppendLine("  NULL " + PathOf(r.transform));
            }
        }

        sb.AppendLine();
        sb.AppendLine("=== ALL TMP TEXTS ===");
        foreach (var t in All<TMP_Text>())
        {
            sb.AppendLine($"{PathOf(t.transform)} active={t.gameObject.activeInHierarchy} text=\"{t.text.Replace("\n", "\\n")}\"");
        }

        sb.AppendLine();
        sb.AppendLine("=== VOLUMES ===");
        foreach (var v in All<Volume>())
        {
            sb.AppendLine($"{PathOf(v.transform)} profile={(v.profile != null ? v.profile.name : "null")} weight={v.weight} isGlobal={v.isGlobal}");
            if (v.profile != null && v.profile.components != null)
            {
                foreach (var c in v.profile.components)
                {
                    if (c == null) continue;
                    sb.AppendLine($"    {c.GetType().Name} active={c.active}");
                }
            }
        }

        sb.AppendLine();
        sb.AppendLine("=== MATERIAL USAGE ===");
        var matCount = new Dictionary<string, int>();
        foreach (var r in All<Renderer>())
        {
            if (!r.gameObject.activeInHierarchy) continue;
            foreach (var m in r.sharedMaterials)
            {
                if (m == null) continue;
                string key = m.name + " [" + (m.shader != null ? m.shader.name : "null") + "]";
                matCount[key] = matCount.TryGetValue(key, out int c) ? c + 1 : 1;
            }
        }
        foreach (var kv in matCount) sb.AppendLine($"  {kv.Value,4} x {kv.Key}");

        File.WriteAllText(Path.Combine(OutDir, "stage-audit.txt"), sb.ToString(), Encoding.UTF8);
        Debug.Log("[StageAudit] -> " + OutDir + "/stage-audit.txt");
    }

    static void DumpTree(GameObject root, StringBuilder sb)
    {
        if (root == null)
        {
            sb.AppendLine("TREE (missing)");
            return;
        }
        sb.AppendLine("TREE " + PathOf(root.transform));
        Walk(root.transform, 1, sb);
    }

    static void Walk(Transform t, int depth, StringBuilder sb)
    {
        sb.Append(' ', depth * 2).Append("- ").Append(t.name).Append(" active=").Append(t.gameObject.activeSelf)
          .Append(" layer=").Append(t.gameObject.layer).Append(" localPos=").Append(F(t.localPosition))
          .Append(" worldPos=").Append(F(t.position)).Append(" localScale=").Append(F(t.localScale));
        var r = t.GetComponent<Renderer>();
        if (r != null)
        {
            sb.Append(" renderer=").Append(r.GetType().Name).Append(" enabled=").Append(r.enabled);
            sb.Append(" mats=");
            foreach (var m in r.sharedMaterials) sb.Append(m != null ? m.name + " " : "NULL ");
            sb.Append(" bounds=").Append(F(r.bounds.center)).Append("/").Append(F(r.bounds.size));
        }
        sb.AppendLine();
        foreach (var c in t.GetComponents<Collider>())
        {
            sb.Append(' ', depth * 2 + 2).Append("collider ").Append(c.GetType().Name).Append(" enabled=").Append(c.enabled)
              .Append(" isTrigger=").Append(c.isTrigger);
            if (c is BoxCollider b) sb.Append(" size=").Append(F(b.size)).Append(" center=").Append(F(b.center));
            sb.Append(" worldBounds=").Append(F(c.bounds.center)).Append("/").Append(F(c.bounds.size)).AppendLine();
        }
        for (int i = 0; i < t.childCount; i++) Walk(t.GetChild(i), depth + 1, sb);
    }

    static void DumpCollidersNear(Vector3 center, float radius, StringBuilder sb, string label)
    {
        sb.AppendLine($"-- near {label} {F(center)} r={radius}");
        foreach (var c in All<Collider>())
        {
            if (c == null) continue;
            float d = Vector3.Distance(c.bounds.center, center);
            if (d > radius) continue;
            sb.AppendLine($"  {PathOf(c.transform)} {c.GetType().Name} enabled={c.enabled} trigger={c.isTrigger} d={d:0.##} bounds={F(c.bounds.center)}/{F(c.bounds.size)}");
        }
    }

    static string TmpText(GameObject root)
    {
        var tmp = root.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null) return "\"" + tmp.text.Replace("\n", "\\n") + "\"";
        var ui = root.GetComponentInChildren<Text>(true);
        if (ui != null) return "\"" + ui.text.Replace("\n", "\\n") + "\"";
        return "(none)";
    }

    static List<T> All<T>() where T : Component
    {
        var list = new List<T>();
        foreach (var o in Resources.FindObjectsOfTypeAll<T>())
        {
            if (o == null) continue;
            var go = o.gameObject;
            if (!go.scene.IsValid() || EditorUtility.IsPersistent(go)) continue;
            list.Add(o);
        }
        return list;
    }

    static string PathOf(Transform t)
    {
        if (t == null) return "null";
        var sb = new StringBuilder(t.name);
        Transform p = t.parent;
        while (p != null) { sb.Insert(0, p.name + "/"); p = p.parent; }
        return sb.ToString();
    }

    static string FileName(string assetPath) => System.IO.Path.GetFileNameWithoutExtension(assetPath);
    static string F(Vector3 v) => $"({v.x:0.###},{v.y:0.###},{v.z:0.###})";
}
