using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>强制重新导入 TableDesk.fbx 并打印其包围盒（验证导出轴对不对）。</summary>
public static class DshReloadDeskFbx
{
    const string Path = "Assets/Models/Interior/TableDesk.fbx";
    const string Out = "E:/DSH_Unity/tmp-read/desk-fbx.txt";

    [MenuItem("Tools/DSH/Tune/Reload Desk FBX")]
    public static void Run()
    {
        AssetDatabase.ImportAsset(Path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        var go = AssetDatabase.LoadAssetAtPath<GameObject>(Path);
        if (go == null) { Debug.LogError("[DeskReload] load failed"); return; }

        var rs = go.GetComponentsInChildren<Renderer>();
        var sb = new StringBuilder();
        sb.AppendLine($"root={go.name} childCount={go.transform.childCount} renderers={rs.Length}");
        sb.AppendLine($"root localPos={go.transform.localPosition} localRot={go.transform.localEulerAngles} localScale={go.transform.localScale}");
        if (rs.Length > 0)
        {
            var b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            sb.AppendLine($"bounds center={b.center} size={b.size}");
            foreach (Transform t in go.transform)
                sb.AppendLine($"  child {t.name} localPos={t.localPosition} rot={t.localEulerAngles} scale={t.localScale}");
        }
        File.WriteAllText(Out, sb.ToString());
        Debug.Log("[DeskReload] " + sb);
    }
}
