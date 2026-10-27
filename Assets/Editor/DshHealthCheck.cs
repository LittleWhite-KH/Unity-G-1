using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>场景体检：缺失脚本、空材质、关键对象、碰撞、钥匙/玩家配置、Console 计数。</summary>
public static class DshHealthCheck
{
    const string OutPath = "E:/DSH_Unity/tmp-read/health-check.txt";

    [MenuItem("Tools/DSH/Diag/Health Check")]
    public static void Run()
    {
        var sb = new StringBuilder();
        var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        // ---- 1. Missing scripts ----
        int missing = 0;
        foreach (var t in all)
        {
            foreach (var c in t.GetComponents<Component>())
            {
                if (c == null)
                {
                    missing++;
                    if (missing <= 20) sb.AppendLine($"MISSING SCRIPT on {Path(t)}");
                }
            }
        }
        sb.AppendLine($"=== missing scripts: {missing} ===");

        // ---- 2. 关键对象 ----
        sb.AppendLine("=== key objects ===");
        foreach (var n in new[] { "Player", "Main Camera", "MazeRoot", "Keys", "LevelDirector", "LevelFade",
            "TV_Ending", "DoorQuad", "GT_Vase", "GT_Terminal", "Slot_Floppy", "Slot_Disc" })
        {
            var go = FindByName(all, n);
            if (go == null) { sb.AppendLine($"  {n}: NOT FOUND"); continue; }
            sb.AppendLine($"  {n}: active={go.gameObject.activeInHierarchy} pos={go.position}");
        }

        // ---- 3. 空材质 / null shader ----
        int badMat = 0;
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!r.gameObject.activeInHierarchy) continue;
            foreach (var m in r.sharedMaterials)
            {
                if (m == null || m.shader == null)
                {
                    badMat++;
                    if (badMat <= 20) sb.AppendLine($"BAD MATERIAL on {Path(r.transform)} ({r.name})");
                }
                else if (m.shader.name == "Hidden/InternalErrorShader")
                {
                    badMat++;
                    if (badMat <= 20) sb.AppendLine($"ERROR SHADER on {Path(r.transform)}");
                }
            }
        }
        sb.AppendLine($"=== bad materials: {badMat} ===");

        // ---- 4. 玩家参数 ----
        sb.AppendLine("=== player ===");
        var player = FindByName(all, "Player");
        if (player != null)
        {
            var cc = player.GetComponent<CharacterController>();
            var pm = player.GetComponent<PlayerMovement>();
            var cam = player.transform.Find("Main Camera");
            var hb = cam != null ? cam.GetComponent<HeadBob>() : null;
            sb.AppendLine($"  CC h={cc.height} c={cc.center} step={cc.stepOffset} slope={cc.slopeLimit} enabled={cc.enabled}");
            sb.AppendLine($"  PlayerMovement enabled={pm.enabled} canMove={pm.canMove} speed={pm.moveSpeed} gravity={pm.gravity}");
            sb.AppendLine($"  camera localY={(cam != null ? cam.localPosition.y : -1)} HeadBob={(hb != null ? $"eye={hb.eyeHeight} ground={hb.groundY} lock={hb.lockEyeHeight}" : "MISSING")}");
        }

        // ---- 5. 钥匙 ----
        sb.AppendLine("=== keys ===");
        foreach (var kn in new[] { "Key_A", "Key_B" })
        {
            var k = FindByName(all, kn);
            if (k == null) { sb.AppendLine($"  {kn}: MISSING"); continue; }
            var gt = k.GetComponent<GuidanceTarget>();
            var col = k.GetComponent<Collider>();
            sb.AppendLine($"  {kn}: active={k.gameObject.activeInHierarchy} pos={k.position} collider={(col != null)} " +
                          $"guidance={(gt != null ? $"id={gt.id} aim={gt.AimPosition}" : "MISSING")}");
        }

        // ---- 6. 家具/桌子 ----
        sb.AppendLine("=== furniture ===");
        var furn = FindByName(all, "Interior_Furniture");
        var blend = FindByName(all, "Interior_FromBlend");
        sb.AppendLine($"  Interior_Furniture active={(furn != null && furn.gameObject.activeSelf)} children={(furn != null ? furn.childCount : -1)}");
        sb.AppendLine($"  Interior_FromBlend active={(blend != null && blend.gameObject.activeSelf)} children={(blend != null ? blend.childCount : -1)}");
        foreach (var tn in new[] { "Table_PC1", "Table_PC2" })
        {
            var t = FindByName(all, tn);
            sb.AppendLine($"  {tn}: {(t == null ? "MISSING" : $"pos={t.position} scale={t.lossyScale}")}");
        }

        // ---- 7. Console 计数 ----
        var logEntries = System.Type.GetType("UnityEditor.LogEntries, UnityEditor");
        if (logEntries != null)
        {
            var getCount = logEntries.GetMethod("GetCount", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (getCount != null) sb.AppendLine($"=== console entries: {getCount.Invoke(null, null)} ===");
        }

        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(OutPath));
        File.WriteAllText(OutPath, sb.ToString());
        Debug.Log("[HealthCheck]\n" + sb);
    }

    static Transform FindByName(Transform[] all, string name)
    {
        foreach (var t in all) if (t.name == name) return t;
        return null;
    }

    static string Path(Transform t)
    {
        var s = t.name;
        var p = t.parent;
        while (p != null) { s = p.name + "/" + s; p = p.parent; }
        return s;
    }
}
