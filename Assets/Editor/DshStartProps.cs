using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Start锛堝紑濮嬬晫闈級鐢佃鍛ㄥ洿鐨勭敓娲诲寲閬撳叿锛?///   鐢婚潰宸︿晶涔︽锛堢湡瀹炰功妗屾ā鍨嬶級+ 閿洏 / 姘存澂 / 杞洏 / 涔︽湰
///   鐢婚潰鍙充晶鎵舵墜妞咃紝鐢佃鍓嶅湴姣紝宸﹀悗钀藉湴鐏?/// 鐢佃鏈韩锛堜綅缃?/ 澶у皬 / 鏉愯川 / 鍏夌収锛変竴寰嬩笉鍔ㄣ€?/// 娉ㄦ剰锛欶BX 鏍硅妭鐐瑰甫瀵煎叆杞存棆杞紝缁濅笉鑳界洿鎺ュ啓 rotation锛涗竴寰嬬敤 pivot 鍋?yaw銆?/// 鑿滃崟鍙噸澶嶆墽琛岋細姣忔鍏堝垹鎺?Start_Props 鍐嶉噸寤恒€?/// </summary>
public static class DshStartProps
{
    const string ScenePath = "Assets/Scenes/Start.unity";
    const string RootName = "Start_Props";

    [MenuItem("Tools/DSH/Start/Place Props")]
    public static void Build()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var scene = EditorSceneManager.GetActiveScene();

        GameObject old = GameObject.Find(RootName);
        if (old != null) Object.DestroyImmediate(old);

        GameObject root = new GameObject(RootName);

        // ---------- 鍦版锛堢數瑙嗘鍓嶆柟锛岄暱杈规部 X锛?----------
        Material rugMat = MatOrCreate("Start_Rug", new Color(0.72f, 0.67f, 0.58f), 0.15f);
        GameObject carpetPivot = Pivot("Start_Carpet", root.transform, new Vector3(0f, 0.004f, 0.55f), 90f);
        GameObject carpet = Extract("Assets/Models/Interior/MazeInterior1026.fbx", "Carpet_A", carpetPivot.transform, 0.7f);
        if (carpet != null)
        {
            SetAllMaterials(carpet, rugMat);
            PutBottom(carpetPivot, new Vector3(0f, 0.004f, 0.55f));
        }

        // ---------- 涔︽锛堢敾闈㈠乏渚?= 涓栫晫 +X锛?----------
        GameObject deskPivot = Pivot("Start_Desk", root.transform, new Vector3(1.3f, 0f, 0.1f), 180f);
        GameObject desk = Attach("Assets/Models/Interior/TableDesk.fbx", deskPivot.transform, 0.72f);
        if (desk != null)
        {
            SetAllMaterials(desk, Mat("Assets/Materials/Interior/Furn/TableDesk.mat"));
            PutBottom(deskPivot, new Vector3(1.3f, 0f, 0.1f));
        }

        Bounds db = BoundsOf(deskPivot);
        float topY = db.max.y;
        Debug.Log($"[StartProps] desk center={db.center} size={db.size} topY={topY}");

        // ---------- 閿洏锛堟涓婇潬鍓嶏紝闀胯竟娌?X锛?----------
        GameObject kbPivot = Pivot("Start_Keyboard", root.transform, new Vector3(0f, 0f, 0f), 90f);
        GameObject kb = Attach("Assets/Models/Interior/Keyboard.fbx", kbPivot.transform, 20f);
        if (kb != null)
        {
            PutBottom(kbPivot, new Vector3(db.center.x, topY + 0.001f, db.max.z - 0.16f));
        }

        // ---------- 姘存澂锛堟涓婂乏鍓嶆部锛?----------
        GameObject cupPivot = Pivot("Start_Cup", root.transform, new Vector3(0f, 0f, 0f), 0f);
        GameObject cup = Attach("Assets/Models/Interior/Cup.fbx", cupPivot.transform, 1f);
        if (cup != null)
        {
            foreach (Transform child in cup.transform)
            {
                if (child.name != "ordinalwater") child.gameObject.SetActive(false);
            }
            PutBottom(cupPivot, new Vector3(db.min.x + 0.17f, topY + 0.001f, db.max.z - 0.15f));
        }

        // ---------- 杞洏锛堟涓婂彸鍓嶏紝骞虫斁锛?----------
        GameObject floppyPivot = Pivot("Start_Floppy", root.transform, new Vector3(0f, 0f, 0f), 20f);
        GameObject floppy = Attach("Assets/Models/Interior/Floppy.fbx", floppyPivot.transform, 0.5f);
        if (floppy != null)
        {
            DisableStage(floppy);
            SetAllMaterials(floppy, Mat("Assets/Materials/Interior/StartDesk/Desk_Floppy.mat"));
            PutBottom(floppyPivot, new Vector3(db.max.x - 0.22f, topY + 0.001f, db.max.z - 0.18f));
        }

        // ---------- 涔︽湰锛堟涓婇潬鍚庯級 ----------
        GameObject book = GameObject.CreatePrimitive(PrimitiveType.Cube);
        book.name = "Start_Book";
        book.transform.SetParent(root.transform, true);
        book.transform.position = new Vector3(db.center.x + 0.16f, topY + 0.018f, db.min.z + 0.2f);
        book.transform.localScale = new Vector3(0.24f, 0.035f, 0.32f);
        book.transform.rotation = Quaternion.Euler(0f, 18f, 0f);
        Object.DestroyImmediate(book.GetComponent<Collider>());
        book.GetComponent<MeshRenderer>().sharedMaterial = rugMat;

        // ---------- 鎵舵墜妞咃紙鐢婚潰鍙充晶 = 涓栫晫 -X锛岄潰鍚戠數瑙嗭級 ----------
        GameObject chairPivot = Pivot("Start_Armchair", root.transform, new Vector3(-1.38f, 0f, 0.4f), 165f);
        GameObject chair = Extract("Assets/Models/Interior/MazeInterior1026.fbx", "SM_MChair_001", chairPivot.transform, 1f);
        if (chair != null)
        {
            SetAllMaterials(chair, Mat("Assets/Materials/Interior/BlendFurn/FurnBlend_textile_001.mat"));
            PutBottom(chairPivot, new Vector3(-1.38f, 0f, 0.4f));

            GameObject pillow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillow.name = "Start_Pillow";
            pillow.transform.SetParent(chairPivot.transform, true);
            pillow.transform.position = new Vector3(-1.4f, 0.48f, 0.34f);
            pillow.transform.localScale = new Vector3(0.36f, 0.12f, 0.36f);
            pillow.transform.rotation = Quaternion.Euler(0f, 165f, 0f);
            Object.DestroyImmediate(pillow.GetComponent<Collider>());
            pillow.GetComponent<MeshRenderer>().sharedMaterial = rugMat;
        }

        // ---------- 钀藉湴鐏紙鐢婚潰宸﹀悗锛岀函瑁呴グ锛屼笉鍔犲厜锛?----------
        Material dark = Mat("Assets/Materials/Interior/StartDesk/Desk_Plastic.mat");
        Material shade = Mat("Assets/Materials/Interior/StartDesk/Desk_Cup.mat");
        GameObject lamp = new GameObject("Start_FloorLamp");
        lamp.transform.SetParent(root.transform, true);
        lamp.transform.position = new Vector3(1.7f, 0f, -0.5f);
        Cylinder(lamp.transform, "Base", new Vector3(0f, 0.015f, 0f), 0.16f, 0.03f, dark);
        Cylinder(lamp.transform, "Pole", new Vector3(0f, 0.72f, 0f), 0.022f, 1.4f, dark);
        Cylinder(lamp.transform, "Shade", new Vector3(0f, 1.44f, 0f), 0.17f, 0.26f, shade);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[StartProps] built");
    }

    [MenuItem("Tools/DSH/Start/Remove Props")]
    public static void Remove()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var scene = EditorSceneManager.GetActiveScene();
        GameObject old = GameObject.Find(RootName);
        if (old != null) Object.DestroyImmediate(old);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[StartProps] removed");
    }

    // ------------------------------------------------------------------
    static GameObject Pivot(string name, Transform parent, Vector3 pos, float yaw)
    {
        GameObject pivot = new GameObject(name);
        pivot.transform.SetParent(parent, true);
        pivot.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
        return pivot;
    }

    static GameObject Attach(string fbxPath, Transform pivot, float scaleFactor)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (prefab == null) { Debug.LogWarning($"[StartProps] missing {fbxPath}"); return null; }

        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Vector3 baseScale = prefab.transform.localScale;
        go.transform.SetParent(pivot, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = prefab.transform.localRotation;
        go.transform.localScale = baseScale * scaleFactor;
        Physics.SyncTransforms();
        return go;
    }

    static GameObject Extract(string fbxPath, string childName, Transform pivot, float scaleFactor)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (prefab == null) { Debug.LogWarning($"[StartProps] missing {fbxPath}"); return null; }

        Transform child = prefab.transform.Find(childName);
        if (child == null) { Debug.LogWarning($"[StartProps] {fbxPath} has no child {childName}"); return null; }

        GameObject go = Object.Instantiate(child.gameObject);
        Vector3 baseScale = child.localScale;
        go.transform.SetParent(pivot, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = child.localRotation;
        go.transform.localScale = baseScale * scaleFactor;
        Physics.SyncTransforms();
        return go;
    }

    static void DisableStage(GameObject go)
    {
        foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "Stage") t.gameObject.SetActive(false);
        }
    }

    static Bounds BoundsOf(GameObject go)
    {
        Renderer[] rs = go.GetComponentsInChildren<Renderer>(true);
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }

    /// <summary>鎶?pivot 涓嬫墍鏈夋覆鏌撲綋鐨勫寘鍥寸洅搴曢潰涓績鏀惧埌 surface 鐐广€?/summary>
    static void PutBottom(GameObject pivot, Vector3 surface)
    {
        Physics.SyncTransforms();
        Bounds b = BoundsOf(pivot);
        pivot.transform.position += new Vector3(surface.x - b.center.x, surface.y - b.min.y, surface.z - b.center.z);
        Physics.SyncTransforms();
    }

    static void SetAllMaterials(GameObject go, Material mat)
    {
        if (mat == null) return;
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
        {
            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            r.sharedMaterials = mats;
        }
    }

    static void Cylinder(Transform parent, string name, Vector3 localPos, float radius, float height, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent, true);
        go.transform.localPosition = localPos;
        go.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    static Material Mat(string path)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) Debug.LogWarning($"[StartProps] missing material {path}");
        return mat;
    }

    static Material MatOrCreate(string name, Color color, float smoothness)
    {
        string path = $"Assets/Materials/Interior/StartDesk/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;

        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        mat = new Material(lit) { name = name };
        mat.color = color;
        mat.SetFloat("_Smoothness", smoothness);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }
}

