using UnityEngine;

/// <summary>
/// Feeds the global "_SanGlitch" value from the player's SAN value.
/// SAN 100 -> 0 (clean picture), SAN 0 -> strong glitch (the reference image).
/// </summary>
[DisallowMultipleComponent]
public class SanGlitchController : MonoBehaviour
{
    [Tooltip("Lower SAN becomes even more broken (power curve).")]
    public float lowSanPow = 1.35f;

    [Tooltip("How fast the effect follows the SAN value.")]
    public float blendSpeed = 1.6f;

    [Tooltip("Maximum glitch amount.")]
    [Range(0f, 1f)] public float maxGlitch = 0.9f;

    [Tooltip("Small breathing wobble so even a steady SAN feels unstable.")]
    [Range(0f, 0.2f)] public float idleWobble = 0.03f;

    private SanSystem san;
    private float current;

    private void Awake()
    {
        san = GetComponent<SanSystem>();
        if (san == null) san = FindAnyObjectByType<SanSystem>();
    }

    private void Update()
    {
        float target = san != null
            ? Mathf.Pow(Mathf.Clamp01(1f - san.Value01), lowSanPow) * maxGlitch
            : 0f;

        if (target > 0.01f)
        {
            target += Mathf.Sin(Time.time * 2.1f) * idleWobble * target;
        }

        current = Mathf.MoveTowards(current, target, blendSpeed * Time.deltaTime);
        Shader.SetGlobalFloat("_SanGlitch", current);
        Shader.SetGlobalFloat("_SanGlitchTime", Time.unscaledTime);
    }

    private void OnDisable()
    {
        Shader.SetGlobalFloat("_SanGlitch", 0f);
    }

    private void OnDestroy()
    {
        Shader.SetGlobalFloat("_SanGlitch", 0f);
    }
}
