using UnityEngine;

/// <summary>
/// Start（标题/结束）场景：
///   · 保持你在场景里摆好的画面：电视机 + 装饰 + 屏幕文字；
///   · forceShow=true（默认）时装饰常显；把它关掉才会按通关标记隐藏装饰。
/// </summary>
[DisallowMultipleComponent]
public class StartSceneReveal : MonoBehaviour
{
    [Tooltip("电视周围的装饰根物体（Start_Props）")]
    public GameObject props;

    public string finishPrefKey = "outdoor.finished";

    [Tooltip("true = 装饰常显（保持当前 Start 画面）；false = 未通关时隐藏装饰")]
    public bool forceShow = true;

    private void Awake()
    {
        bool finished = forceShow || PlayerPrefs.GetInt(finishPrefKey, 0) == 1;

        if (props == null)
        {
            GameObject found = GameObject.Find("Start_Props");
            if (found != null) props = found;
        }

        if (props != null)
        {
            props.SetActive(finished);
        }

        Debug.Log($"[StartSceneReveal] forceShow={forceShow} finished={finished} props={(props != null ? props.name : "null")}");
    }
}
