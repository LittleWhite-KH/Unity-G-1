using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>检查导入的模型：尺寸、朝向、材质槽。用于替换旧电脑前确认摆放。</summary>
public static class DshCheckModel
{
    [MenuItem("Tools/DSH/Check Model/AppleII")]
    public static void CheckApple() => Check("Assets/Models/Interior/AppleII.fbx");

    [MenuItem("Tools/DSH/Check Model/Cup")]
    public static void CheckCup() => Check("Assets/Models/Interior/Cup.fbx");

    [MenuItem("Tools/DSH/Check Model/Keyboard")]
    public static void CheckKeyboard() => Check("Assets/Models/Interior/Keyboard.fbx");

    public static void Check(string path)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogError($"[Model] not found: {path}");
            return;
        }

        GameObject go = (GameObject)Object.Instantiate(prefab);
        go.transform.position = Vector3.zero;
        go.transform.rotation = Quaternion.identity;

        Bounds b = new Bounds(Vector3.zero, Vector3.zero);
        bool first = true;
        var sb = new StringBuilder();
        sb.AppendLine($"[Model] === {path} ===");

        foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
        {
            if (first) { b = r.bounds; first = false; }
            else b.Encapsulate(r.bounds);

            string mats = "";
            foreach (Material m in r.sharedMaterials)
            {
                mats += (m != null ? m.name : "null") + ",";
            }
            sb.AppendLine($"[Model]   {r.name}  bounds={r.bounds.size}  mats={mats}");
        }

        sb.AppendLine($"[Model] TOTAL center={b.center} size={b.size} min={b.min} max={b.max}");
        Debug.Log(sb.ToString());

        Object.DestroyImmediate(go);
    }
}
