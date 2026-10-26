using UnityEditor;
using UnityEngine;

/// <summary>Close-up renders of the two pickup keys, used to verify their colors.</summary>
public static class DshKeyPreview
{
    [MenuItem("Tools/DSH/Keys/Preview A and B")]
    public static void Preview()
    {
        Capture("Key_A", "E:/DSH_Unity/shots/key-a.png");
        Capture("Key_B", "E:/DSH_Unity/shots/key-b.png");
    }

    static void Capture(string keyName, string outPath)
    {
        GameObject key = GameObject.Find(keyName);
        if (key == null)
        {
            Debug.LogError("[KeyPreview] missing " + keyName);
            return;
        }

        GameObject camGo = GameObject.Find("Player/Main Camera");
        if (camGo == null)
        {
            Debug.LogError("[KeyPreview] missing Player/Main Camera");
            return;
        }

        Camera cam = camGo.GetComponent<Camera>();
        Transform camT = camGo.transform;
        Vector3 oldPos = camT.position;
        Quaternion oldRot = camT.rotation;
        float oldFov = cam.fieldOfView;
        int oldMask = cam.cullingMask;
        float oldNear = cam.nearClipPlane;

        Vector3 focus = key.transform.position;
        Vector3 preferred = keyName == "Key_B"
            ? new Vector3(0.18f, 1.5f, 0.4f)
            : new Vector3(0.62f, 0.32f, 0.72f);
        PickUnblockedDirection(key, focus, preferred, out Vector3 pos);
        camT.position = pos;
        camT.rotation = Quaternion.LookRotation(focus - pos, Vector3.up);
        cam.fieldOfView = 40f;
        cam.nearClipPlane = 0.02f;
        cam.cullingMask = ~0;

        DshCapture.Capture("Player/Main Camera", outPath);

        camT.position = oldPos;
        camT.rotation = oldRot;
        cam.fieldOfView = oldFov;
        cam.nearClipPlane = oldNear;
        cam.cullingMask = oldMask;
        Debug.Log("[KeyPreview] " + keyName + " -> " + outPath + " from " + pos);
    }

    // Walk around the key and use the first direction that is not blocked by walls.
    static Vector3 PickUnblockedDirection(GameObject key, Vector3 focus, Vector3 preferred, out Vector3 cameraPosition)
    {
        Vector3 fallback = focus + preferred.normalized * 0.55f;
        cameraPosition = fallback;

        // Try the preferred angle first, then a horizontal sweep.
        for (int i = -1; i < 12; i++)
        {
            Vector3 direction = i < 0
                ? preferred.normalized
                : Quaternion.Euler(0f, i * 30f, 0f) * Vector3.forward;
            Vector3 candidate = focus + direction * 0.55f + (i < 0 ? Vector3.zero : Vector3.up * 0.18f);

            Vector3 delta = focus - candidate;
            RaycastHit[] hits = Physics.RaycastAll(candidate, delta.normalized, delta.magnitude);
            bool blocked = false;
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null) continue;
                if (hit.collider.transform.root == key.transform.root) continue;
                blocked = true;
                break;
            }

            if (!blocked)
            {
                cameraPosition = candidate;
                return direction;
            }
        }

        return (fallback - focus).normalized;
    }
}
