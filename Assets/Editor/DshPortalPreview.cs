using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Editor preview of the TV portal: place the player in front of the TV and render Player/Main Camera.</summary>
public static class DshPortalPreview
{
    [MenuItem("Tools/DSH/Portal/Preview Exit Room")]
    public static void PreviewExitRoom()
    {
        TvPortal portal = Object.FindAnyObjectByType<TvPortal>();
        if (portal == null || portal.player == null)
        {
            Debug.LogError("[PortalPreview] missing portal/player");
            return;
        }

        Transform pt = portal.player;
        Camera cam = portal.playerCamera;
        Vector3 oldPos = pt.position;
        Quaternion oldRot = pt.rotation;
        int oldMask = cam.cullingMask;
        Vector3 oldCamPos = cam.transform.localPosition;
        Quaternion oldCamRot = cam.transform.localRotation;

        pt.position = portal.exitAnchor.position;
        Vector3 dir = portal.exitLookAt.position - pt.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f) pt.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        cam.transform.localPosition = new Vector3(0f, 1.36f, 0f);
        cam.transform.localRotation = Quaternion.identity;
        cam.cullingMask = 1 << portal.exitRoomLayer;

        DshCapture.Capture("Player/Main Camera", "E:/DSH_Unity/shots/exit-room.png");

        pt.position = oldPos;
        pt.rotation = oldRot;
        cam.cullingMask = oldMask;
        cam.transform.localPosition = oldCamPos;
        cam.transform.localRotation = oldCamRot;
    }

    [MenuItem("Tools/DSH/Portal/Preview From TV Front")]
    public static void Preview()
    {
        TvPortal portal = Object.FindAnyObjectByType<TvPortal>();
        if (portal == null || portal.player == null || portal.playerCamera == null || portal.sourcePortal == null)
        {
            Debug.LogError("[PortalPreview] missing portal references");
            return;
        }

        Camera cam = portal.playerCamera;
        Transform src = portal.sourcePortal;
        Transform dst = portal.destinationPortal;
        Camera pc = portal.portalCamera;
        Renderer surface = portal.portalSurface;

        RenderTexture rt = new RenderTexture(1024, 576, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        Shader shader = Shader.Find("DSH/TvPortal");
        if (shader == null)
        {
            Debug.LogError("[PortalPreview] shader not found");
            return;
        }

        Material mat = new Material(shader);
        mat.SetTexture("_MainTex", rt);
        Material oldMat = surface.sharedMaterial;
        bool oldEnabled = surface.enabled;
        surface.sharedMaterial = mat;
        surface.enabled = true;
        Renderer glass = portal.glassSurface;
        bool glassOld = glass != null && glass.enabled;
        if (glass != null) glass.enabled = true;

        Transform pt = portal.player;
        Vector3 oldPos = pt.position;
        Quaternion oldRot = pt.rotation;
        Vector3 oldCamPos = cam.transform.localPosition;
        Quaternion oldCamRot = cam.transform.localRotation;

        Vector3 foot = src.position + src.forward * 2.2f;
        foot.y = 0f;
        pt.position = foot;
        Quaternion lookRot = Quaternion.LookRotation(-src.forward, Vector3.up);
        pt.rotation = lookRot;
        cam.transform.localPosition = new Vector3(0f, 1.36f, 0f);
        cam.transform.localRotation = Quaternion.identity;

        Vector3 lp = src.InverseTransformPoint(cam.transform.position);
        lp.z = -lp.z;
        lp.z = Mathf.Clamp(lp.z, -5.6f, -0.15f);
        pc.transform.position = dst.TransformPoint(lp);
        pc.transform.rotation = dst.rotation * Quaternion.Inverse(src.rotation) * cam.transform.rotation;
        pc.projectionMatrix = cam.projectionMatrix;

        rt.Create();
        pc.targetTexture = rt;
        pc.enabled = true;
        // URP ignores Camera.Render for non-main cameras; submit a request so the RT actually gets painted.
        if (GraphicsSettings.currentRenderPipeline == null)
        {
            pc.Render();
        }
        else
        {
            var request = new UniversalRenderPipeline.SingleCameraRequest { destination = rt };
            RenderPipeline.SubmitRenderRequest(pc, request);
        }
        pc.enabled = false;

        // Save what the portal camera sees so the doorway can be checked separately.
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        File.WriteAllBytes("E:/DSH_Unity/shots/portal-rt.png", tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        DshCapture.Capture("Player/Main Camera", "E:/DSH_Unity/shots/portal-preview.png");
        Debug.Log($"[PortalPreview] camPos={cam.transform.position} portalCam={pc.transform.position} portalCamRot={pc.transform.eulerAngles}");

        if (glass != null) glass.enabled = glassOld;
        surface.sharedMaterial = oldMat;
        surface.enabled = oldEnabled;
        pc.targetTexture = null;
        rt.Release();
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(mat);
        pt.position = oldPos;
        pt.rotation = oldRot;
        cam.transform.localPosition = oldCamPos;
        cam.transform.localRotation = oldCamRot;
    }
}

