using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DshFixOrbit
{
    [MenuItem("Tools/DSH/Fix/Clear Orbit Trigger Groups")]
    public static void ClearGroups()
    {
        GameObject cam = GameObject.Find("Main Camera");
        if (cam == null) { Debug.LogError("[Fix] Main Camera missing"); return; }

        OrbitCameraWithMouseHover orbit = cam.GetComponent<OrbitCameraWithMouseHover>();
        if (orbit == null) { Debug.LogError("[Fix] orbit component missing"); return; }

        int before = orbit.triggerGroups != null ? orbit.triggerGroups.Count : 0;
        orbit.triggerGroups.Clear();
        EditorUtility.SetDirty(orbit);
        EditorSceneManager.MarkSceneDirty(cam.scene);
        EditorSceneManager.SaveScene(cam.scene);
        Debug.Log($"[Fix] cleared {before} orbit trigger groups");
    }
}
