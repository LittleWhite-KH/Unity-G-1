using Outdoor.UI;
using UnityEngine;

/// <summary>
/// 运行时共享引用：所有 Runner 都从这里拿系统，不再各自 FindObjectOfType。
/// 由 <see cref="LevelDirector"/> 初始化一次。
/// </summary>
public class LevelContext
{
    public LevelDirector Director;
    public SanSystem San;
    public SanDisplay SanDisplay;
    public ThreatSystem Threat;
    public HudReveal Hud;
    public Transform Player;
    public Camera PlayerCamera;
    public ChargeSystem Charge;
    public MapPanel Map;
    public MapGraph Graph;
    public LevelState State;

    /// <summary>"OPEN MAP" 那个侧栏提示。</summary>
    public GameObject MapHintRoot;

    /// <summary>整屏黑幕（可空）。</summary>
    public CanvasGroup FadeOverlay;

    public bool IsReady => Player != null && PlayerCamera != null;
}
