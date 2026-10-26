using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 修地板闪烁（z-fighting）：旧户外背景 Background/Plane 是 500×500 的平面，
/// 与 MazeArchitecture/Floor 顶面（y=0）完全共面且法线同向，深度冲突导致地面整片闪烁。
/// 处理：把它整体下移到地板盒体内部（地板厚 0.2m），并关闭阴影投射。
/// 可回滚：Tools/DSH/Fix/Floor Z-Fighting Restore 恢复原始 y。
/// </summary>
public static class DshFixFloorZfight
{
    const string OutPath = "E:/DSH_Unity/tmp-read/fix-floor.txt";
    const float DropBy = 0.06f;

    [MenuItem("Tools/DSH/Fix/Floor Z-Fighting")]
    public static void Fix()
    {
        var sb = new StringBuilder();
        var floorGo = GameObject.Find("MazeRoot/MazeArchitecture/Floor");
        if (floorGo == null) { Debug.LogError("[FloorFix] floor not found"); return; }
        float topY = floorGo.GetComponent<Renderer>().bounds.max.y;
        Bounds fb = floorGo.GetComponent<Renderer>().bounds;

        int fixedCount = 0;
        var rends = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var r in rends)
        {
            if (!r.gameObject.activeInHierarchy) continue;
            if (r == floorGo.GetComponent<Renderer>()) continue;

            Bounds b = r.bounds;
            bool flat = b.size.y < 0.01f;                       // 厚度≈0 的平面
            bool atFloorTop = Mathf.Abs(b.center.y - topY) < 0.02f;
            bool xzOverlap = b.max.x > fb.min.x && b.min.x < fb.max.x &&
                             b.max.z > fb.min.z && b.min.z < fb.max.z;
            if (!flat || !atFloorTop || !xzOverlap) continue;

            var go = r.gameObject;
            var pos = go.transform.position;
            Undo.RecordObject(go.transform, "FloorZfight move plane");
            go.transform.position = new Vector3(pos.x, topY - DropBy, pos.z);

            var mr = r as MeshRenderer;
            if (mr != null)
            {
                Undo.RecordObject(mr, "FloorZfight no shadow");
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            fixedCount++;
            sb.AppendLine($"moved plane: {FullPath(go.transform)} y {pos.y:F4} -> {topY - DropBy:F4} (size={b.size}) mat={MatName(r)}");
        }

        // 记录基线，供 Restore 使用
        var bg = GameObject.Find("Background");
        if (bg != null)
        {
            var plane = bg.transform.Find("Plane");
            if (plane != null)
                sb.AppendLine($"Background/Plane now y={plane.position.y:F4} (restore y=0)");
        }

        sb.AppendLine($"fixed {fixedCount} coplanar plane(s)");
        Directory.CreateDirectory(Path.GetDirectoryName(OutPath));
        File.WriteAllText(OutPath, sb.ToString());

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("[FloorFix] " + sb.ToString());
    }

    [MenuItem("Tools/DSH/Fix/Floor Z-Fighting Restore")]
    public static void Restore()
    {
        var bg = GameObject.Find("Background");
        if (bg == null) { Debug.LogWarning("[FloorFix] no Background"); return; }
        var plane = bg.transform.Find("Plane");
        if (plane == null) { Debug.LogWarning("[FloorFix] no Background/Plane"); return; }

        Undo.RecordObject(plane, "Restore background plane");
        plane.position = new Vector3(plane.position.x, 0f, plane.position.z);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("[FloorFix] restored Background/Plane y=0");
    }

    static string FullPath(Transform t)
    {
        var s = t.name;
        var p = t.parent;
        while (p != null) { s = p.name + "/" + s; p = p.parent; }
        return s;
    }

    static string MatName(Renderer r)
    {
        var m = r.sharedMaterial;
        return m == null ? "null" : m.name;
    }
}
