using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>输出当前场景的结构报告：根对象、Renderer 世界包围盒、Collider、相机。</summary>
public static class DshSceneAudit
{
    [MenuItem("Tools/DSH/Audit/Scene Report")]
    public static void Report()
    {
        Scene scene = SceneManager.GetActiveScene();
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"SCENE {scene.name} path={scene.path} dirty={scene.isDirty}");

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform t = root.transform;
            sb.AppendLine($"ROOT {root.name} active={root.activeSelf} pos={F(t.position)} rot={F(t.eulerAngles)} scale={F(t.localScale)}");

            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            {
                Bounds b = r.bounds;
                string mat = r.sharedMaterial != null ? r.sharedMaterial.name : "-";
                sb.AppendLine($"  R {GetPath(r.transform)} active={r.gameObject.activeInHierarchy} enabled={r.enabled} mat={mat} center={F(b.center)} size={F(b.size)}");
            }

            foreach (Collider c in root.GetComponentsInChildren<Collider>(true))
            {
                Bounds b = c.bounds;
                sb.AppendLine($"  C {GetPath(c.transform)} type={c.GetType().Name} enabled={c.enabled} trigger={c.isTrigger} center={F(b.center)} size={F(b.size)}");
            }

            foreach (Camera cam in root.GetComponentsInChildren<Camera>(true))
            {
                sb.AppendLine($"  CAM {GetPath(cam.transform)} pos={F(cam.transform.position)} rot={F(cam.transform.eulerAngles)} fov={cam.fieldOfView} depth={cam.depth}");
            }

            foreach (Light l in root.GetComponentsInChildren<Light>(true))
            {
                sb.AppendLine($"  L {GetPath(l.transform)} type={l.type} pos={F(l.transform.position)} range={l.range} intensity={l.intensity} color={l.color}");
            }
        }

        Directory.CreateDirectory("E:/DSH_Unity/tmp-read");
        File.WriteAllText("E:/DSH_Unity/tmp-read/scene-audit.txt", sb.ToString());
        Debug.Log($"[Audit] {scene.name} roots={scene.rootCount} -> tmp-read/scene-audit.txt");
    }

    static string F(Vector3 v) => $"({v.x:0.###},{v.y:0.###},{v.z:0.###})";

    static string GetPath(Transform t)
    {
        string p = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            p = t.name + "/" + p;
        }
        return p;
    }
}
