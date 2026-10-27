using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// The last door in the white world: press E, the door swings open, the player walks through
/// and the TV portal plays its ending (fade + load Start). No auto-trigger.
/// </summary>
[DisallowMultipleComponent]
public class EndDoor : MonoBehaviour
{
    [Header("Refs")]
    public Transform player;
    public Transform hinge;
    public Transform doorLeaf;
    public Transform doorOpening;
    public TvPortal portal;

    [Header("Interaction")]
    public float interactDistance = 5.5f;
    public float promptDistance = 9f;
    public float openAngle = -96f;
    public float openDuration = 1.15f;
    public float pushDuration = 0.9f;
    public float pushDistance = 3.4f;
    public string promptText = "[E]   OPEN THE DOOR";

    [Header("Dark room")]
    [Tooltip("打开并走进黑房后，把门板换成纯黑材质，避免门外灯光把门板照亮而穿帮")]
    public Material darkLeafMaterial;

    private CanvasGroup promptGroup;
    private TextMeshProUGUI promptLabel;
    private bool busy;
    private bool finished;
    private Quaternion closedRotation;

    private void Start()
    {
        if (hinge != null) closedRotation = hinge.localRotation;
        if (portal == null) portal = FindAnyObjectByType<TvPortal>();
        if (player == null)
        {
            GameObject found = GameObject.Find("Player");
            if (found != null) player = found.transform;
        }

        BuildPrompt();
    }

    private void Update()
    {
        if (finished || busy || player == null || doorOpening == null) return;

        float distance = HorizontalDistance();
        bool showPrompt = distance <= promptDistance;
        bool canInteract = distance <= interactDistance;
        SetPrompt(showPrompt);

        if (canInteract && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            StartCoroutine(OpenRoutine());
        }
    }

    private float HorizontalDistance()
    {
        Vector3 a = player.position;
        Vector3 b = doorOpening.position;
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private IEnumerator OpenRoutine()
    {
        busy = true;
        SetPrompt(false);

        MouseLook look = player.GetComponent<MouseLook>();
        PlayerMovement move = player.GetComponent<PlayerMovement>();
        if (look != null) look.canControl = false;
        if (move != null) move.canMove = false;

        Quaternion openRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);
        float t = 0f;
        while (t < openDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / openDuration));
            hinge.localRotation = Quaternion.Slerp(closedRotation, openRotation, k);
            yield return null;
        }
        hinge.localRotation = openRotation;

        // Step through the doorway.
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;

        Vector3 start = player.position;
        Vector3 end = start + player.forward * pushDistance;
        t = 0f;
        while (t < pushDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / pushDuration));
            player.position = Vector3.Lerp(start, end, k);
            yield return null;
        }
        player.position = end;

        if (controller != null) controller.enabled = true;
        Physics.SyncTransforms();

        // We are inside the dark room now: make the leaf pitch black so the warm light
        // from the white world cannot outline it behind the player.
        if (darkLeafMaterial != null && doorLeaf != null)
        {
            foreach (var r in doorLeaf.GetComponentsInChildren<Renderer>(true))
            {
                if (r != null) r.sharedMaterial = darkLeafMaterial;
            }
        }

        finished = true;

        if (portal != null)
        {
            portal.TriggerEnding();
        }
        else
        {
            Debug.LogError("[EndDoor] TvPortal missing, cannot finish");
        }
    }

    // ------------------------------------------------------------------
    private void BuildPrompt()
    {
        GameObject go = new GameObject("TV_EndDoorPrompt");
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1500;
        go.AddComponent<CanvasScaler>();

        promptGroup = go.AddComponent<CanvasGroup>();
        promptGroup.alpha = 0f;
        promptGroup.blocksRaycasts = false;
        promptGroup.interactable = false;

        GameObject labelGo = new GameObject("Label");
        labelGo.transform.SetParent(go.transform, false);
        promptLabel = labelGo.AddComponent<TextMeshProUGUI>();
        promptLabel.text = promptText;
        promptLabel.fontSize = 52f;
        promptLabel.alignment = TextAlignmentOptions.Center;
        promptLabel.color = new Color(0.08f, 0.09f, 0.11f, 1f);
        promptLabel.raycastTarget = false;

        RectTransform rect = labelGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 180f);
        rect.sizeDelta = new Vector2(1400f, 110f);
    }

    private void SetPrompt(bool visible)
    {
        if (promptGroup == null) return;
        float target = visible ? 1f : 0f;
        promptGroup.alpha = Mathf.MoveTowards(promptGroup.alpha, target, Time.deltaTime * 4f);
    }
}
