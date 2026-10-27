using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Preview the ending door from the white world with the door opened / closed.</summary>
public static class DshEndPreview
{
    [MenuItem("Tools/DSH/Polish/Preview Door Open")]
    public static void PreviewOpen()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Gaming.unity", OpenSceneMode.Single);

        GameObject hinge = GameObject.Find("EndDoorHinge");
        if (hinge == null) { Debug.LogError("[EndPreview] hinge missing"); return; }

        var door = hinge.GetComponent<EndDoor>();
        Transform hingeT = hinge.transform;
        Quaternion closed = hingeT.localRotation;
        float angle = door != null ? door.openAngle : -96f;
        hingeT.localRotation = closed * Quaternion.Euler(0f, angle, 0f);

        GameObject player = GameObject.Find("Player");
        Camera cam = GameObject.Find("Player/Main Camera")?.GetComponent<Camera>();
        if (player == null || cam == null) { Debug.LogError("[EndPreview] player/camera missing"); return; }

        Vector3 oldPos = player.transform.position;
        Quaternion oldRot = player.transform.rotation;
        Vector3 oldCamPos = cam.transform.localPosition;
        Quaternion oldCamRot = cam.transform.localRotation;
        int oldMask = cam.cullingMask;

        cam.cullingMask = 1 << 30;
        cam.transform.localPosition = new Vector3(0f, 1.36f, 0f);

        // 1) standing at the spawn point, door open
        player.transform.position = new Vector3(0f, 0.05f, -202.2f);
        player.transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
        cam.transform.localRotation = Quaternion.identity;
        DshCapture.Capture("Player/Main Camera", "E:/DSH_Unity/shots/end-door-open.png");

        // 2) right at the doorway, looking slightly down (checks the floor seam / z-fighting)
        player.transform.position = new Vector3(0f, 0.05f, -204.55f);
        cam.transform.localRotation = Quaternion.Euler(18f, 0f, 0f);
        DshCapture.Capture("Player/Main Camera", "E:/DSH_Unity/shots/end-door-close.png");

        // 3) inside the dark room looking forward
        player.transform.position = new Vector3(0f, 0.05f, -205.6f);
        cam.transform.localRotation = Quaternion.identity;
        DshCapture.Capture("Player/Main Camera", "E:/DSH_Unity/shots/end-door-inside.png");

        // 4) inside the dark room looking back at the doorway
        player.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        DshCapture.Capture("Player/Main Camera", "E:/DSH_Unity/shots/end-door-inside-back.png");

        player.transform.position = oldPos;
        player.transform.rotation = oldRot;
        cam.transform.localPosition = oldCamPos;
        cam.transform.localRotation = oldCamRot;
        cam.cullingMask = oldMask;
        hingeT.localRotation = closed;

        Debug.Log("[EndPreview] done");
    }
}
