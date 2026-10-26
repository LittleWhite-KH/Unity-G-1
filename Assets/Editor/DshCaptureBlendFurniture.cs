using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>渲染 blend 家具导入后的整体效果（俯视 + 玩家视角）。</summary>
public static class DshCaptureBlendFurniture
{
    [MenuItem("Tools/DSH/Blend/Capture Plan")]
    public static void Plan()
    {
        var hidden = new[] { "Ceiling", "CeilingGrid", "LightPanels" };
        var states = new List<(GameObject go, bool active)>();
        var maze = GameObject.Find("MazeRoot").transform;
        foreach (var n in hidden)
        {
            var t = FindDeep(maze, n);
            if (t != null) { states.Add((t.gameObject, t.gameObject.activeSelf)); t.gameObject.SetActive(false); }
        }

        bool fog = RenderSettings.fog;
        var fogColor = RenderSettings.fogColor;
        float fogStart = RenderSettings.fogStartDistance, fogEnd = RenderSettings.fogEndDistance;
        Color ambient = RenderSettings.ambientLight;

        // temporary light for a readable plan view
        var lightGo = new GameObject("DshTempPlanLight");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 2.2f;
        light.color = new Color(1f, 0.97f, 0.92f);
        lightGo.transform.rotation = Quaternion.Euler(60f, -30f, 0f);
        light.shadows = LightShadows.None;

        RenderSettings.fog = false;
        RenderSettings.ambientLight = new Color(0.45f, 0.45f, 0.48f);

        Shot(new Vector3(0f, 40f, 0f), new Vector3(0f, 0f, 0f), 21f, true, "E:/DSH_Unity/shots/furn-plan.png");


        Object.DestroyImmediate(lightGo);
        RenderSettings.fog = fog;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogStartDistance = fogStart;
        RenderSettings.fogEndDistance = fogEnd;
        RenderSettings.ambientLight = ambient;

        foreach (var s in states) s.go.SetActive(s.active);
    }

    [MenuItem("Tools/DSH/Blend/Capture Rooms")]
    public static void Rooms()
    {
        Shot(new Vector3(-22f, 1.65f, -10f), new Vector3(-28f, 1.0f, 2f), 60f, false, "E:/DSH_Unity/shots/furn-west.png");
        Shot(new Vector3(4f, 1.65f, -9f), new Vector3(8f, 0.9f, 0f), 60f, false, "E:/DSH_Unity/shots/furn-bed.png");
        Shot(new Vector3(14f, 1.65f, 10f), new Vector3(6f, 0.9f, -2f), 60f, false, "E:/DSH_Unity/shots/furn-east.png");
        Shot(new Vector3(-8f, 1.65f, 12f), new Vector3(-8f, 0.9f, 2f), 60f, false, "E:/DSH_Unity/shots/furn-center.png");
    }

    /// <summary>家具穿墙/越界检查（OverlapBox 精确查询，只认墙体 collider）。</summary>
    [MenuItem("Tools/DSH/Blend/Check Furniture vs Walls")]
    public static void CheckWalls()
    {
        var sb = new System.Text.StringBuilder();
        var maze = GameObject.Find("MazeRoot").transform;
        var root = FindDeep(maze, "Interior_FromBlend");
        if (root == null) { Debug.LogError("[FurnCheck] Interior_FromBlend missing"); return; }

        var wallGo = FindDeep(maze, "Walls");
        var wallCol = wallGo.GetComponent<Collider>();
        var furnColliders = new HashSet<Collider>(root.GetComponentsInChildren<Collider>(true));
        Physics.SyncTransforms();

        int bad = 0, total = 0, outsideCount = 0;
        foreach (var bc in root.GetComponentsInChildren<BoxCollider>(true))
        {
            total++;
            var b = bc.bounds;
            if (b.min.x < -36.2f || b.max.x > 36.2f || b.min.z < -12.2f || b.max.z > 12.2f)
            {
                outsideCount++;
                sb.AppendLine($"OUTSIDE {bc.name} c={b.center} s={b.size}");
            }
            var hits = Physics.OverlapBox(b.center, b.extents * 0.92f, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            foreach (var h in hits)
            {
                if (h == null || h == bc || furnColliders.Contains(h)) continue;
                if (h == wallCol)
                {
                    bad++;
                    sb.AppendLine($"INTO WALL {bc.name} c={b.center} s={b.size}");
                    break;
                }
            }
        }
        sb.AppendLine($"checked={total} intoWall={bad} outside={outsideCount}");
        File.WriteAllText("E:/DSH_Unity/tmp-read/furn-wall-check.txt", sb.ToString());
        Debug.Log("[FurnCheck] " + sb);
    }

    [MenuItem("Tools/DSH/Blend/Capture Suspects")]
    public static void Suspects()
    {
        Shot(new Vector3(-9f, 1.7f, -7.5f), new Vector3(-14.7f, 1.0f, -3.4f), 55f, false, "E:/DSH_Unity/shots/furn-suspect-03.png");
        Shot(new Vector3(12f, 1.7f, 6.5f), new Vector3(16.4f, 1.0f, 10.1f), 55f, false, "E:/DSH_Unity/shots/furn-suspect-02_1.png");
        Shot(new Vector3(1.17f, 1.7f, 2.2f), new Vector3(1.17f, 1.55f, 5.6f), 55f, false, "E:/DSH_Unity/shots/furn-suspect-cupboard.png");
    }

    static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform c in root)
        {
            var r = FindDeep(c, name);
            if (r != null) return r;
        }
        return null;
    }

    static void Shot(Vector3 eye, Vector3 look, float sizeOrFov, bool ortho, string outPath)
    {
        var go = new GameObject("DshTempFurnCam");
        var cam = go.AddComponent<Camera>();
        cam.fieldOfView = sizeOrFov;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 500f;
        if (ortho)
        {
            cam.orthographic = true;
            cam.orthographicSize = sizeOrFov;
            go.transform.position = eye;
            // 正上方俯视：LookAt 在正下方会退化，显式朝下
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
        else
        {
            go.transform.position = eye;
            go.transform.LookAt(look);
        }

        const int W = 1600, H = 900;
        var rt = RenderTexture.GetTemporary(W, H, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        tex.Apply();

        RenderTexture.active = null;
        cam.targetTexture = null;
        RenderTexture.ReleaseTemporary(rt);
        Object.DestroyImmediate(go);

        Directory.CreateDirectory(Path.GetDirectoryName(outPath));
        File.WriteAllBytes(outPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        Debug.Log($"[CaptureFurn] {outPath}");
    }
}
