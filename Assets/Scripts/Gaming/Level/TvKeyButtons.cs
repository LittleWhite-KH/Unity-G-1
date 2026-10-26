using UnityEngine;

/// <summary>
/// 电视柜上的两个槽（Slot_Floppy 蓝 / Slot_Disc 橙）：
/// 默认是暗的，捡到软盘（key_a）亮蓝槽，捡到光盘（key_b）亮橙槽。
/// 不再使用额外按钮，直接用电视柜上原有的两个槽。
/// </summary>
[DisallowMultipleComponent]
public class TvKeyButtons : MonoBehaviour
{
    public LevelDirector director;

    public string keyA = "key_a";
    public string keyB = "key_b";

    private Renderer floppySlot;
    private Renderer discSlot;
    private Material floppyOn;
    private Material floppyOff;
    private Material discOn;
    private Material discOff;

    private bool lastA;
    private bool lastB;
    private bool initialized;

    private void Start()
    {
        GameObject f = GameObject.Find("Slot_Floppy");
        GameObject d = GameObject.Find("Slot_Disc");
        if (f != null) floppySlot = f.GetComponent<Renderer>();
        if (d != null) discSlot = d.GetComponent<Renderer>();

        floppyOff = MakeLit(new Color(0.05f, 0.07f, 0.10f), Color.black);
        floppyOn = MakeLit(new Color(0.35f, 0.75f, 1f), new Color(0.56f, 1.2f, 1.6f));
        discOff = MakeLit(new Color(0.10f, 0.06f, 0.03f), Color.black);
        discOn = MakeLit(new Color(1f, 0.55f, 0.25f), new Color(1.6f, 0.88f, 0.4f));

        Apply(false, false, true);
        initialized = true;
    }

    private void Update()
    {
        if (director == null)
        {
            director = FindAnyObjectByType<LevelDirector>();
            if (director == null) return;
        }

        LevelState state = director.State;
        bool a = state != null && state.Has(keyA);
        bool b = state != null && state.Has(keyB);

        if (!initialized || a != lastA || b != lastB)
        {
            Apply(a, b, false);
            initialized = true;
        }
    }

    private void Apply(bool a, bool b, bool force)
    {
        bool changed = force || a != lastA || b != lastB;
        lastA = a;
        lastB = b;
        if (!changed) return;

        if (floppySlot != null) floppySlot.sharedMaterial = a ? floppyOn : floppyOff;
        if (discSlot != null) discSlot.sharedMaterial = b ? discOn : discOff;

        Debug.Log($"[TvKeyButtons] floppy={(a ? "ON" : "off")} disc={(b ? "ON" : "off")}");
    }

    private void OnDestroy()
    {
        if (floppyOn != null) Destroy(floppyOn);
        if (floppyOff != null) Destroy(floppyOff);
        if (discOn != null) Destroy(discOn);
        if (discOff != null) Destroy(discOff);
    }

    private static Material MakeLit(Color baseColor, Color emission)
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        Material mat = new Material(lit) { name = "TvSlot_Runtime" };
        mat.SetColor("_BaseColor", baseColor);
        mat.color = baseColor;
        mat.SetFloat("_Smoothness", 0.6f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", emission);
        return mat;
    }
}
