using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Phase 3 model swaps: tomb -> table + lily vase, table -> IBM PC terminal.</summary>
public static class DshPhase3Models
{
    const string IbmPath = "Assets/Models/Furniture/IBM_PC/ibmpc_mesh.fbx";
    const string VasePath = "Assets/Models/Furniture/VaseLily/model.obj";
    static readonly string Report = "E:/DSH_Unity/tmp-read/phase3-models.txt";

    [MenuItem("Tools/DSH/Phase3/Inspect New Models")]
    public static void Inspect()
    {
        var sb = new StringBuilder();
        Describe(IbmPath, sb);
        Describe(VasePath, sb);
        File.WriteAllText(Report, sb.ToString());
        Debug.Log("[Phase3Models] inspect -> " + Report);
    }

    static void Describe(string path, StringBuilder sb)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            sb.AppendLine(path + ": MISSING");
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.position = Vector3.zero;
        sb.AppendLine("MODEL " + path);
        sb.AppendLine("  root scale=" + instance.transform.lossyScale + " rot=" + instance.transform.eulerAngles);

        bool has = false;
        Bounds total = new Bounds(Vector3.zero, Vector3.zero);
        foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
        {
            if (!has) { total = renderer.bounds; has = true; }
            else total.Encapsulate(renderer.bounds);
            sb.AppendLine($"  renderer {renderer.name} mat={(renderer.sharedMaterial != null ? renderer.sharedMaterial.name : "-")} bounds={renderer.bounds.center} size={renderer.bounds.size}");
        }
        if (has) sb.AppendLine($"  TOTAL center={total.center} size={total.size}");

        foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
        {
            sb.AppendLine($"  collider {collider.name} {collider.GetType().Name}");
        }

        Object.DestroyImmediate(instance);
    }

    [MenuItem("Tools/DSH/Phase3/Swap Tomb To Table")]
    public static void SwapTomb()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Gaming.unity", OpenSceneMode.Single);
        var sb = new StringBuilder();

        GameObject gtTomb = GameObject.Find("GT_Tomb");
        if (gtTomb == null) { Debug.LogError("[Phase3Models] GT_Tomb missing"); return; }

        Transform oldTomb = gtTomb.transform.Find("墓碑_简单");
        if (oldTomb != null)
        {
            Object.DestroyImmediate(oldTomb.gameObject);
            sb.AppendLine("tomb removed");
        }

        GameObject sourceTable = GameObject.Find("SM_MTable_001");
        if (sourceTable == null) { Debug.LogError("[Phase3Models] table source missing"); return; }

        GameObject table = Object.Instantiate(sourceTable, gtTomb.transform);
        table.name = "Table_Under_Vase";
        table.transform.rotation = gtTomb.transform.rotation;
        AlignBottom(table, gtTomb.transform.position);
        EnsureBoxCollider(table);
        sb.AppendLine($"table bounds={BoundsOf(table).center} size={BoundsOf(table).size}");

        GameObject vasePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VasePath);
        if (vasePrefab != null)
        {
            GameObject vase = (GameObject)PrefabUtility.InstantiatePrefab(vasePrefab);
            PrefabUtility.UnpackPrefabInstance(vase, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            vase.name = "Vase_Lily";
            vase.transform.SetParent(gtTomb.transform, true);
            ScaleToHeight(vase, 0.8f);
            vase.transform.rotation = Quaternion.Euler(0f, 35f, 0f);
            Bounds tableBounds = BoundsOf(table);
            Vector3 vasePos = new Vector3(gtTomb.transform.position.x, tableBounds.max.y, gtTomb.transform.position.z);
            AlignBottom(vase, vasePos);
            EnsureBoxCollider(vase);
            sb.AppendLine($"vase height=0.8 bounds={BoundsOf(vase).center} size={BoundsOf(vase).size}");
        }
        else sb.AppendLine("vase prefab missing");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        File.WriteAllText(Report, sb.ToString());
        Debug.Log("[Phase3Models] tomb swapped");
    }

    [MenuItem("Tools/DSH/Phase3/Swap Table To IBM PC")]
    public static void SwapTable()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Gaming.unity", OpenSceneMode.Single);
        var sb = new StringBuilder();

        GameObject gtTable = GameObject.Find("GT_Table");
        if (gtTable == null) { Debug.LogError("[Phase3Models] GT_Table missing"); return; }

        // Remove previously added terminal (idempotent).
        Transform old = gtTable.transform.Find("IBM_PC_Terminal");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        GameObject ibmPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(IbmPath);
        if (ibmPrefab == null) { Debug.LogError("[Phase3Models] IBM fbx missing"); return; }

        GameObject ibm = (GameObject)PrefabUtility.InstantiatePrefab(ibmPrefab);
        PrefabUtility.UnpackPrefabInstance(ibm, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        ibm.name = "IBM_PC_Terminal";
        ibm.transform.SetParent(gtTable.transform, true);
        ScaleToHeight(ibm, 0.62f);

        // Desk top of the existing table.
        Transform desk = gtTable.transform.Find("桌台_简单");
        float topY = desk != null ? BoundsOf(desk.gameObject).max.y : 1.2f;
        Vector3 pos = new Vector3(21.008f, topY, -11.478f);
        ibm.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        AlignBottom(ibm, pos);
        EnsureBoxCollider(ibm);
        sb.AppendLine($"ibm bounds={BoundsOf(ibm).center} size={BoundsOf(ibm).size} topY={topY}");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        File.WriteAllText(Report, sb.ToString());
        Debug.Log("[Phase3Models] table swapped");
    }

    // ------------------------------------------------------------------
    static Bounds BoundsOf(GameObject go)
    {
        bool has = false;
        Bounds bounds = new Bounds(go.transform.position, Vector3.zero);
        foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>(true))
        {
            if (!renderer.gameObject.activeInHierarchy) continue;
            if (!has) { bounds = renderer.bounds; has = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        return bounds;
    }

    static void AlignBottom(GameObject go, Vector3 groundCenter)
    {
        Physics.SyncTransforms();
        Bounds bounds = BoundsOf(go);
        Vector3 delta = new Vector3(
            groundCenter.x - bounds.center.x,
            groundCenter.y - bounds.min.y,
            groundCenter.z - bounds.center.z);
        go.transform.position += delta;
        Physics.SyncTransforms();
    }

    static void ScaleToHeight(GameObject go, float targetHeight)
    {
        Physics.SyncTransforms();
        Bounds bounds = BoundsOf(go);
        if (bounds.size.y <= 0.0001f) return;
        float factor = targetHeight / bounds.size.y;
        go.transform.localScale *= factor;
        Physics.SyncTransforms();
    }

    static void EnsureBoxCollider(GameObject go)
    {
        if (go.GetComponent<Collider>() != null) return;
        Bounds bounds = BoundsOf(go);
        BoxCollider box = go.AddComponent<BoxCollider>();
        Vector3 scale = go.transform.lossyScale;
        box.size = new Vector3(
            bounds.size.x / Mathf.Max(0.0001f, scale.x),
            bounds.size.y / Mathf.Max(0.0001f, scale.y),
            bounds.size.z / Mathf.Max(0.0001f, scale.z));
        box.center = go.transform.InverseTransformPoint(bounds.center);
    }
}
