using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 在墙上批量放英文 TMP 指引。
/// 关键：文字面与墙面法线方向留 0.025m，避免和墙体共面导致 z-fighting 闪烁。
/// </summary>
public static class DshPlaceWallGuides
{
    const string FontPath = "Assets/Fonts/Menlo/Menlo Regular SDF.asset";
    const float WallGap = 0.025f;

    struct Spec
    {
        public string name;
        public string text;
        public Vector3 anchor;     // 墙面上的点
        public Vector3 normal;     // 从墙指向房间的方向
        public float fontSize;
        public float width;
        public Color color;
        public float radius;
    }

    [MenuItem("Tools/DSH/Guides/Create Demo Guide")]
    public static void CreateDemo()
    {
        Create(new Spec
        {
            name = "Guide_FollowSignal",
            text = "FOLLOW THE SIGNAL",
            anchor = new Vector3(-34.5f, 1.72f, 0.13f),
            normal = Vector3.forward,
            fontSize = 30f,
            width = 6f,
            color = new Color(0.75f, 0.86f, 1f, 1f),
            radius = 5f,
        }, true);
    }

    [MenuItem("Tools/DSH/Guides/Create Level Guides")]
    public static void CreateLevelGuides()
    {
        Create(new Spec
        {
            name = "Guide_TV",
            text = "INSERT FLOPPY + DISC",
            anchor = new Vector3(29.86f, 2.10f, 9f),
            normal = new Vector3(-1f, 0f, 0f),
            fontSize = 30f,
            width = 5f,
            color = new Color(0.95f, 0.85f, 0.65f, 1f),
            radius = 6f,
        }, false);

        Create(new Spec
        {
            name = "Guide_Vase",
            text = "THE VASE",
            anchor = new Vector3(-3f, 2.05f, 12.85f),
            normal = new Vector3(0f, 0f, -1f),
            fontSize = 30f,
            width = 5f,
            color = new Color(0.75f, 0.86f, 1f, 1f),
            radius = 6f,
        }, false);

        Debug.Log("[Guide] level guides created");
    }
    [MenuItem("Tools/DSH/Guides/Create More Guides")]
    public static void CreateMoreGuides()
    {
        Create(new Spec
        {
            name = "Guide_Floppy",
            text = "TAKE THE FLOPPY",
            anchor = new Vector3(1.85f, 1.95f, 12.84f),
            normal = new Vector3(0f, 0f, -1f),
            fontSize = 26f,
            width = 5f,
            color = new Color(0.70f, 0.88f, 1f, 1f),
            radius = 6f,
        }, false);

        Create(new Spec
        {
            name = "Guide_Terminal",
            text = "THE TERMINAL",
            anchor = new Vector3(21f, 1.95f, -12.84f),
            normal = new Vector3(0f, 0f, 1f),
            fontSize = 28f,
            width = 5f,
            color = new Color(0.75f, 0.86f, 1f, 1f),
            radius = 6f,
        }, false);

        Debug.Log("[Guide] more guides created");
    }
    [MenuItem("Tools/DSH/Guides/Auto Guides For Apple II")]
    public static void AutoGuidesForApple()
    {
        AutoPlace("Guide_AppleI_1", "FLOPPY + DISC", new Vector3(1.85f, 0f, 9f), 0.70f, 0.88f, 1f);
        AutoPlace("Guide_AppleI_2", "FLOPPY + DISC", new Vector3(-19.85f, 0f, 3f), 0.70f, 0.88f, 1f);
    }

    static void AutoPlace(string name, string text, Vector3 origin, float r, float g, float b)
    {
        Vector3[] dirs =
        {
            Vector3.forward, Vector3.back, Vector3.right, Vector3.left,
        };

        foreach (Vector3 dir in dirs)
        {
            if (!Physics.Raycast(origin + Vector3.up * 1.5f, dir, out RaycastHit hit, 9f)) continue;
            if (!hit.collider.name.Contains("Walls")) continue;

            Vector3 normal = -dir;
            Vector3 anchor = hit.point + normal * 0.03f;
            anchor.y = 1.9f;

            Create(new Spec
            {
                name = name,
                text = text,
                anchor = anchor,
                normal = normal,
                fontSize = 26f,
                width = 4.5f,
                color = new Color(r, g, b, 1f),
                radius = 4f,
            }, false);

            Debug.Log($"[Guide] auto {name} wall={hit.collider.name} point={hit.point} normal={normal}");
            return;
        }

        Debug.LogWarning($"[Guide] no wall found near {origin}");
    }
    static void Create(Spec spec, bool showOnStart)
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null) { Debug.LogError($"[Guide] font not found: {FontPath}"); return; }

        GameObject existing = GameObject.Find(spec.name);
        if (existing != null) Object.DestroyImmediate(existing);

        GameObject go = new GameObject(spec.name);
        go.transform.position = spec.anchor + spec.normal.normalized * WallGap;
        // TMP 3D 文本的正面朝 -Z，所以让 -Z 指向房间
        go.transform.rotation = Quaternion.LookRotation(-spec.normal.normalized, Vector3.up);

        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.font = font;
        tmp.fontSize = spec.fontSize;
        tmp.text = spec.text;
        tmp.color = spec.color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.rectTransform.sizeDelta = new Vector2(spec.width, 2f);
        tmp.rectTransform.pivot = new Vector2(0.5f, 0.5f);

        // 世界尺寸：TMP 3D 的字号是“字体单位”，用缩放换算成米
        go.transform.localScale = Vector3.one * 0.045f;

        WallGuide guide = go.AddComponent<WallGuide>();
        guide.triggerRadius = spec.radius;
        guide.showOnStart = showOnStart;
        guide.riseDistance = 0.03f;
        guide.fadeIn = 0.28f;
        guide.hold = 4.5f;
        guide.fadeOut = 0.45f;

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log($"[Guide] created {spec.name} at {go.transform.position} normal={spec.normal} gap={WallGap}");
    }
}
