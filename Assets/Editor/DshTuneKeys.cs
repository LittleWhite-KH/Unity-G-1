using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 让桌上的密钥在暗场景里一眼看见：换成 URP Unlit 高亮材质（A 青 / B 橙），
/// 并打印每件 key 模型的世界包围盒用于确认位置。
/// </summary>
public static class DshTuneKeys
{
    const string OutPath = "E:/DSH_Unity/tmp-read/tune-keys.txt";
    const string MatDir = "Assets/Materials/Interior/Keys";

    [MenuItem("Tools/DSH/Tune/Glow Keys")]
    public static void Apply()
    {
        var sb = new StringBuilder();
        var keys = GameObject.Find("Keys").transform;

        ApplyOne(sb, keys, "Key_A", new Color(0.35f, 0.85f, 1f));
        ApplyOne(sb, keys, "Key_B", new Color(1f, 0.65f, 0.25f));

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        File.WriteAllText(OutPath, sb.ToString());
        Debug.Log("[Keys] " + sb);
    }

    static void ApplyOne(StringBuilder sb, Transform keys, string keyName, Color color)
    {
        var key = keys.Find(keyName);
        if (key == null) { sb.AppendLine(keyName + " missing"); return; }

        // 抬到桌面（0.844）以上
        var p = key.position;
        key.position = new Vector3(p.x, 0.844f + 0.16f, p.z);

        // 软盘/光盘本体只有 1cm（之前的自动缩放没保住），按实测包围盒放大到 20cm；Stage 底座抵消掉
        var model = key.Find("Model_Floppy") != null ? key.Find("Model_Floppy") : key.Find("Model_Disc");
        if (model != null)
        {
            // Stage 是模型自带的投影面，会飘在旁边，每次执行都强制关掉
            var stageAlways = model.Find("Stage");
            if (stageAlways != null) stageAlways.gameObject.SetActive(false);

            Renderer bodyR = null;
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                if (r.name == "Floppy_Body" || r.name == "Vinyl_Disc" || r.name == "Floppy_Label" || r.name == "Vinyl_Label")
                { bodyR = r; break; }
            }
            if (bodyR != null)
            {
                float size = Mathf.Max(bodyR.bounds.size.x, bodyR.bounds.size.z);
                if (size < 0.05f)
                {
                    float k = 0.20f / Mathf.Max(0.0005f, size);
                    model.localScale *= k;
                    var stage = model.Find("Stage");
                    if (stage != null) { stage.localScale /= k; stage.gameObject.SetActive(false); }
                    sb.AppendLine($"  scaled {model.name} x{k:F1} (was {size:F4}m) -> localScale {model.localScale}");
                }
            }
        }

        var mat = GetOrCreate(keyName + "_Glow", color);
        sb.AppendLine($"{keyName} pos={key.position}");

        foreach (var r in key.GetComponentsInChildren<Renderer>(true))
        {
            if (!r.gameObject.activeInHierarchy) continue;
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            r.sharedMaterials = mats;
            var b = r.bounds;
            sb.AppendLine($"  {r.name} enabled={r.enabled} boundsC={b.center} boundsS={b.size}");
        }
    }

    static Material GetOrCreate(string name, Color color)
    {
        string path = $"{MatDir}/{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = name };
            AssetDatabase.CreateAsset(m, path);
        }
        m.SetColor("_BaseColor", color);
        m.SetColor("_Color", color);
        m.SetFloat("_Surface", 0f);
        m.SetInt("_ZWrite", 1);
        return m;
    }
}
