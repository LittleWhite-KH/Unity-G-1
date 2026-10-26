using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 把两把“钥匙”的视觉换成软盘 / 光盘：
///   Key_A -> Floppy.fbx（软盘）
///   Key_B -> Vinyl.fbx（光盘/唱片）
/// 保留 GuidanceTarget + KeyBob（浮动/自转）与 Key_X_Light。
/// </summary>
public static class DshReplaceKeys
{
    const string MatDir = "Assets/Materials/Interior/Keys";

    [MenuItem("Tools/DSH/Keys/Replace With Floppy And Disc")]
    public static void Replace()
    {
        ReplaceOne("Key_A", "Assets/Models/Interior/Floppy.fbx", "Floppy", new Color(0.10f, 0.10f, 0.11f), 0.20f);
        ReplaceOne("Key_B", "Assets/Models/Interior/Vinyl.fbx", "Disc", new Color(0.16f, 0.16f, 0.18f), 0.26f);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log("[Keys] done");
    }

    static void ReplaceOne(string keyName, string modelPath, string tag, Color color, float targetSize)
    {
        GameObject keyGo = GameObject.Find("Keys/" + keyName);
        if (keyGo == null) keyGo = GameObject.Find(keyName);
        if (keyGo == null) { Debug.LogError($"[Keys] {keyName} not found"); return; }

        Transform key = keyGo.transform;

        // 1) 关掉旧的发光卡片
        foreach (Transform child in key)
        {
            if (child.name.EndsWith("_Card"))
            {
                child.gameObject.SetActive(false);
                Debug.Log($"[Keys] {keyName}: disabled {child.name}");
            }
        }

        // 2) 清掉上次生成的模型
        Transform existing = key.Find("Model_" + tag);
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        // 3) 实例化新模型
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (prefab == null) { Debug.LogError($"[Keys] model not found {modelPath}"); return; }

        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, key);
        go.name = "Model_" + tag;
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;

        // 4) 自动缩放到目标尺寸
        Renderer[] rs = go.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) { Debug.LogError($"[Keys] no renderer in {modelPath}"); return; }

        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        float maxDim = Mathf.Max(b.size.x, b.size.y, b.size.z);
        float scale = maxDim > 0.0001f ? targetSize / maxDim : 1f;
        go.transform.localScale = Vector3.one * scale;

        // 5) 材质
        ApplyMaterial(go, tag, color);

        Debug.Log($"[Keys] {keyName}: {tag} size={b.size} maxDim={maxDim:0.###} scale={scale:0.###}");
    }

    static void ApplyMaterial(GameObject go, string tag, Color color)
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null) { Debug.LogError("[Keys] URP Lit not found"); return; }

        if (!AssetDatabase.IsValidFolder("Assets/Materials/Interior")) AssetDatabase.CreateFolder("Assets/Materials", "Interior");
        if (!AssetDatabase.IsValidFolder(MatDir)) AssetDatabase.CreateFolder("Assets/Materials/Interior", "Keys");

        string path = $"{MatDir}/Key_{tag}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(lit) { name = "Key_" + tag };
            mat.color = color;
            mat.SetFloat("_Smoothness", 0.55f);
            mat.SetFloat("_Metallic", tag == "Disc" ? 0.6f : 0.1f);
            AssetDatabase.CreateAsset(mat, path);
        }

        foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
        {
            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            r.sharedMaterials = mats;
        }

        AssetDatabase.SaveAssets();
    }
}
