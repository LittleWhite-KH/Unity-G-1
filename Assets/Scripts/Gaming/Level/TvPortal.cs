using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// TvPortal: TV screen portal + white ending world.
/// Teleport is synchronous (position/camera/input are applied immediately) so it never
/// depends on coroutines or timeScale; the fade is only cosmetic.
/// </summary>
[DefaultExecutionOrder(1000)]
[DisallowMultipleComponent]
public class TvPortal : MonoBehaviour
{
    [Header("TV Screen")]
    public Transform player;
    public Camera playerCamera;
    public Transform sourcePortal;
    public Renderer portalSurface;
    public Renderer glassSurface;
    public Camera portalCamera;
    public Shader portalShader;
    public Transform destinationPortal;

    [Header("Params")]
    public float activeDistance = 16f;
    public float eyeHeight = 1.36f;
    public int exitRoomLayer = 30;

    [Header("White world door")]
    public Transform exitAnchor;
    public Transform exitLookAt;
    public Transform doorOpening;
    public float doorTriggerDistance = 1.6f;

    [Header("Ending")]
    public string endSceneName = "Start";
    public string finishPrefKey = "outdoor.finished";

    private RenderTexture rt;
    private Material portalMat;
    private MouseLook look;
    private PlayerMovement move;
    private CanvasGroup flash;
    private bool screenOn;
    private bool entered;
    private bool finished;

    public bool HasEntered { get { return entered; } }

    private void Awake()
    {
        Resolve();
        CreateFlash();
        // The remote room and screen have their own visibility lifecycle.
        int roomMask = 1 << exitRoomLayer;
        if (playerCamera != null) playerCamera.cullingMask &= ~roomMask;
        if (player != null)
        {
            ObjectCuller objects = player.GetComponent<ObjectCuller>();
            if (objects != null) objects.includeLayers &= ~roomMask;
            LightCuller lights = player.GetComponent<LightCuller>();
            if (lights != null) lights.includeLayers &= ~roomMask;
        }
        InitPortal();
        SetScreenOn(false);
    }

    private void LateUpdate()
    {
        if (player == null) return;

        if (!entered)
        {
            RenderPortal();
            return;
        }

        if (!finished)
        {
            UpdateWhiteRoom();
        }
    }

    private void OnDestroy()
    {
        if (portalCamera != null) portalCamera.targetTexture = null;
        if (rt != null) { rt.Release(); Destroy(rt); rt = null; }
        if (portalMat != null) Destroy(portalMat);
    }

    // ------------------------------------------------------------------
    public void SetScreenOn(bool on)
    {
        screenOn = on;
        if (!on)
        {
            if (portalSurface != null) portalSurface.enabled = false;
            if (glassSurface != null) glassSurface.enabled = false;
        }
    }

    private void RenderPortal()
    {
        if (!screenOn || portalSurface == null || portalCamera == null || playerCamera == null
            || sourcePortal == null || destinationPortal == null)
        {
            return;
        }

        float distance = Vector3.Distance(player.position, sourcePortal.position);
        bool near = distance <= activeDistance;
        if (portalSurface.enabled != near) portalSurface.enabled = near;
        if (glassSurface != null && glassSurface.enabled != near) glassSurface.enabled = near;
        if (!near) return;

        // Map the camera rigidly between matching outward-facing portal frames.
        // TransformPoint would also apply the TV model's 1.4 scale to the view.
        Vector3 local = Quaternion.Inverse(sourcePortal.rotation)
                        * (playerCamera.transform.position - sourcePortal.position);
        portalCamera.transform.position = destinationPortal.position + destinationPortal.rotation * local;
        portalCamera.transform.rotation = destinationPortal.rotation
                                           * Quaternion.Inverse(sourcePortal.rotation)
                                           * playerCamera.transform.rotation;
        EnsureRenderTexture();
        portalCamera.projectionMatrix = playerCamera.projectionMatrix;

        if (GraphicsSettings.currentRenderPipeline == null) portalCamera.Render();
        else
        {
            var request = new UniversalRenderPipeline.SingleCameraRequest { destination = rt };
            RenderPipeline.SubmitRenderRequest(portalCamera, request);
        }
    }

    // ------------------------------------------------------------------
    /// <summary>Take the screen off and teleport immediately (no coroutine dependency).</summary>
    public void RemoveScreen()
    {
        if (entered) return;
        Enter();
        if (!entered) return;

        if (portalSurface != null)
        {
            portalSurface.enabled = false;
            portalSurface.transform.position += Vector3.down * 1.05f;
        }
        if (glassSurface != null)
        {
            glassSurface.enabled = false;
            glassSurface.transform.position += Vector3.down * 1.05f;
        }

    }

    public void Enter()
    {
        if (entered) return;
        Resolve();
        if (player == null || playerCamera == null || exitAnchor == null)
        {
            Debug.LogError("[TvPortal] Cannot enter: player, camera or exit anchor is missing.", this);
            return;
        }
        entered = true;
        TeleportNow();
    }

    private void TeleportNow()
    {
        Resolve();
        MapPanel map = Object.FindAnyObjectByType<MapPanel>();
        if (map != null)
        {
            map.CloseImmediate();
            map.inputEnabled = false;
        }
        SettingsPanel settings = Object.FindAnyObjectByType<SettingsPanel>();
        if (settings != null)
        {
            settings.SetOpen(false);
            settings.enabled = false;
        }

        if (player != null && exitAnchor != null)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.position = exitAnchor.position + Vector3.up * 0.05f;
            if (cc != null) cc.enabled = true;
            Physics.SyncTransforms();

            Vector3 dir = exitLookAt != null ? exitLookAt.position - exitAnchor.position : exitAnchor.forward;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
            {
                player.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            }

            if (playerCamera != null)
            {
                playerCamera.transform.localPosition = new Vector3(0f, eyeHeight, 0f);
                playerCamera.transform.localRotation = Quaternion.identity;
                playerCamera.cullingMask = 1 << exitRoomLayer;
                playerCamera.clearFlags = CameraClearFlags.SolidColor;
                playerCamera.backgroundColor = new Color(0.96f, 0.96f, 0.95f);
            }

            Debug.Log($"[TvPortal] teleported to {player.position}");
        }
        else
        {
            Debug.LogWarning($"[TvPortal] teleport skipped player={(player != null)} anchor={(exitAnchor != null)}");
        }

        if (portalSurface != null) portalSurface.enabled = false;
        if (glassSurface != null) glassSurface.enabled = false;
        if (portalCamera != null) portalCamera.enabled = false;

        HideHud();

        foreach (ObjectCuller culler in Object.FindObjectsByType<ObjectCuller>(FindObjectsSortMode.None)) culler.enabled = false;
        foreach (LightCuller culler in Object.FindObjectsByType<LightCuller>(FindObjectsSortMode.None)) culler.enabled = false;

        if (look != null)
        {
            look.ResetLook();
            look.enabled = true;
            look.canControl = true;
        }
        if (move != null)
        {
            move.ResetMotion();
            move.enabled = true;
            move.canMove = true;
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (flash != null)
        {
            StopAllCoroutines();
            flash.alpha = 1f;
            StartCoroutine(FadeOutFlash());
        }
    }

    private IEnumerator FadeOutFlash()
    {
        yield return null;
        yield return FlashTo(0f, 0.35f);
    }

    // ------------------------------------------------------------------
    private void HideHud()
    {
        foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (canvas == null) continue;
            if (flash != null && canvas.transform == flash.transform) continue;
            canvas.gameObject.SetActive(false);
        }
    }

    private void UpdateWhiteRoom()
    {
        if (doorOpening == null) return;

        float dx = player.position.x - doorOpening.position.x;
        float dz = player.position.z - doorOpening.position.z;
        float d = Mathf.Sqrt(dx * dx + dz * dz);
        if (d <= doorTriggerDistance)
        {
            finished = true;
            StartCoroutine(EndRoutine());
        }
    }

    private IEnumerator EndRoutine()
    {
        if (look != null) look.canControl = false;
        if (move != null) move.canMove = false;

        yield return new WaitForSecondsRealtime(0.1f);
        yield return FlashTo(1f, 0.6f);
        yield return new WaitForSecondsRealtime(0.3f);

        PlayerPrefs.SetInt(finishPrefKey, 1);
        PlayerPrefs.Save();

        Debug.Log("[TvPortal] finished, loading " + endSceneName);
        SceneManager.LoadScene(endSceneName);
    }

    // ------------------------------------------------------------------
    private void Resolve()
    {
        if (player == null)
        {
            GameObject p = GameObject.Find("Player");
            if (p != null) player = p.transform;
        }
        if (playerCamera == null && player != null) playerCamera = player.GetComponentInChildren<Camera>();
        if (look == null && player != null) look = player.GetComponent<MouseLook>();
        if (move == null && player != null) move = player.GetComponent<PlayerMovement>();
    }

    private void InitPortal()
    {
        if (portalSurface == null || portalCamera == null) return;

        Shader shader = portalShader != null ? portalShader : Shader.Find("DSH/TvPortal");
        if (shader != null)
        {
            portalMat = new Material(shader);
            portalMat.name = "TV_PortalMat";
            portalMat.SetTexture("_MainTex", rt);
            portalSurface.sharedMaterial = portalMat;
        }

        UniversalAdditionalCameraData data = portalCamera.GetUniversalAdditionalCameraData();
        data.renderPostProcessing = false;
        data.volumeLayerMask = 0;
        data.renderShadows = false;
        EnsureRenderTexture();
        portalCamera.enabled = false;
        portalSurface.enabled = false;
        if (glassSurface != null) glassSurface.enabled = false;
    }

    private void EnsureRenderTexture()
    {
        int sourceWidth = Mathf.Max(1, playerCamera != null ? playerCamera.pixelWidth : Screen.width);
        int sourceHeight = Mathf.Max(1, playerCamera != null ? playerCamera.pixelHeight : Screen.height);
        float scale = Mathf.Min(1f, 1536f / Mathf.Max(sourceWidth, sourceHeight));
        int width = Mathf.Max(1, Mathf.RoundToInt(sourceWidth * scale));
        int height = Mathf.Max(1, Mathf.RoundToInt(sourceHeight * scale));
        if (rt != null && rt.width == width && rt.height == height) return;
        if (rt != null) { rt.Release(); Destroy(rt); }
        rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        rt.name = "TV_PortalRT";
        rt.Create();
        portalCamera.targetTexture = rt;
        if (portalMat != null) portalMat.SetTexture("_MainTex", rt);
    }

    private void CreateFlash()
    {
        if (flash != null) return;

        GameObject go = new GameObject("TV_PortalFlash");
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2000;
        go.AddComponent<CanvasScaler>();

        CanvasGroup group = go.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;

        GameObject imgGo = new GameObject("Black");
        imgGo.transform.SetParent(go.transform, false);
        Image img = imgGo.AddComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = false;

        RectTransform rect = img.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        flash = group;
    }

    private IEnumerator FlashTo(float target, float duration)
    {
        if (flash == null) yield break;

        float from = flash.alpha;
        float t = 0f;
        duration = Mathf.Max(0.01f, duration);

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            flash.alpha = Mathf.Lerp(from, target, Mathf.Clamp01(t / duration));
            yield return null;
        }

        flash.alpha = target;
    }
}
