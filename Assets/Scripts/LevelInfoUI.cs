using UnityEngine;
using TMPro;

public class LevelInfoUI : MonoBehaviour
{
    [Header("UI引用")]
    public TextMeshProUGUI levelNameText;
    public TextMeshProUGUI progressText;

    [Header("数据源")]
    public Level1Manager levelManager;      // 拖拽赋值
    public ProgressMonitor progressMonitor; // 可选，用于显示进度

    private void Start()
    {
        if (levelManager == null)
            levelManager = FindObjectOfType<Level1Manager>();
        if (progressMonitor == null)
            progressMonitor = FindObjectOfType<ProgressMonitor>();

        UpdateUI();
    }

    private void Update()
    {
        // 每帧更新（或通过事件触发优化，此处简单每帧更新）
        UpdateUI();
    }

    void UpdateUI()
    {
        if (levelManager == null) return;

        // 获取当前阶段名称（可通过枚举映射或直接取字符串）
        string phaseName = levelManager.GetCurrentPhaseName(); // 需在Level1Manager添加该方法
        levelNameText.text = $"Level 1 - {phaseName}";

        // 获取额外信息（如进度或提示）
        string extra = "";
        if (progressMonitor != null)
        {
            float progress = progressMonitor.GetProgress(); // 需添加GetProgress方法
            extra = $"Progress: {progress * 100:F0}%";
        }
        else
        {
            // 或直接显示当前提示（如"Look at the Tomb"）
            extra = levelManager.GetCurrentHint(); // 需添加GetCurrentHint方法
        }
        progressText.text = extra;
    }
}