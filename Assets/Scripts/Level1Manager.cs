using UnityEngine;
using TMPro;

public class Level1Manager : MonoBehaviour
{
    public SanSystem sanSystem;
    public Transform playerTransform;
    public Transform cameraTransform;
    public ProgressMonitor progressMonitor;
    public GameObject[] tombTargets;
    public GameObject[] tableTargets;

    [Header("UI提示")]
    public CanvasGroup hintCanvasGroup;
    public TextMeshProUGUI hintText;

    [Header("San值参数")]
    public float sanDefault = 60f;
    public float sanLookTarget = 80f;

    private enum Phase { Tomb, Table }
    private Phase currentPhase = Phase.Tomb;

    private float fadeDuration = 1.5f;
    private float fadeTimer = 0f;
    private bool isFading = false;

    void Start()
    {
        if (sanSystem == null) sanSystem = FindObjectOfType<SanSystem>();
        if (playerTransform == null) playerTransform = transform;
        if (cameraTransform == null) cameraTransform = Camera.main?.transform;

        sanSystem.SetTargetSan(sanDefault, true);
        hintText.text = "Look at the Tomb";
        hintCanvasGroup.alpha = 0f;
        StartFadeIn();
        progressMonitor?.Reset();
    }

    void Update()
    {
        if (isFading)
        {
            fadeTimer += Time.deltaTime;
            hintCanvasGroup.alpha = Mathf.Clamp01(fadeTimer / fadeDuration);
            if (fadeTimer >= fadeDuration) isFading = false;
        }

        switch (currentPhase)
        {
            case Phase.Tomb:
                bool lookingAtTomb = CheckAnyTargetInSight(tombTargets);
                float targetSanTomb = lookingAtTomb ? sanLookTarget : sanDefault;
                sanSystem.SetTargetSan(targetSanTomb, true);

                if (progressMonitor != null && progressMonitor.IsComplete())
                {
                    currentPhase = Phase.Table;
                    hintText.text = "Look at the Table";
                    StartFadeIn();
                    progressMonitor.Reset();
                }
                break;

            case Phase.Table:
                bool lookingAtTable = CheckAnyTargetInSight(tableTargets);
                float targetSan = lookingAtTable ? sanLookTarget : sanDefault;
                sanSystem.SetTargetSan(targetSan, true);
                break;
        }
    }

    bool CheckAnyTargetInSight(GameObject[] targets)
    {
        if (targets == null || targets.Length == 0) return false;

        Vector3 camForward = cameraTransform.forward;
        camForward.y = 0;
        camForward.Normalize();

        foreach (GameObject target in targets)
        {
            if (target == null) continue;
            Vector3 dirToTarget = target.transform.position - playerTransform.position;
            dirToTarget.y = 0;
            dirToTarget.Normalize();
            float angle = Vector3.Angle(camForward, dirToTarget);
            if (angle <= 15f) return true;
        }
        return false;
    }

    void StartFadeIn()
    {
        fadeTimer = 0f;
        isFading = true;
        hintCanvasGroup.alpha = 0f;
    }

    public void ResetLevel()
    {
        currentPhase = Phase.Tomb;
        sanSystem.SetTargetSan(sanDefault, true);
        hintText.text = "Look at the Tomb";
        StartFadeIn();
        progressMonitor?.Reset();
    }
}