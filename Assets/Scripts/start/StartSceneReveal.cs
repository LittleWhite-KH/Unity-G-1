using UnityEngine;

/// <summary>
/// Start（标题/结束）场景的显隐：
///   · 未通关：只保留电视机、屏幕上的 TMP 文字和必要的光/相机，其余全部隐藏（极简开场）；
///   · 通关后（PlayerPrefs 里有 finishPrefKey）：装饰与背景全部显现。
/// forceShow=true 时强制全显，方便在编辑器里布置场景。
/// </summary>
[DisallowMultipleComponent]
public class StartSceneReveal : MonoBehaviour
{
    [Tooltip("电视周围的装饰根物体（Start_Props），未通关时隐藏")]
    public GameObject props;

    [Tooltip("除装饰外还需要在未通关时隐藏的对象（例如背景 3Dobj：地板/背板）")]
    public GameObject[] hideWhenNotFinished = new GameObject[0];

    public string finishPrefKey = "outdoor.finished";

    [Tooltip("true = 强制全部显示（编辑/调试用）；false = 按通关标记切换")]
    public bool forceShow = false;

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

        if (hideWhenNotFinished != null)
        {
            for (int i = 0; i < hideWhenNotFinished.Length; i++)
            {
                if (hideWhenNotFinished[i] != null) hideWhenNotFinished[i].SetActive(finished);
            }
        }

        Debug.Log($"[StartSceneReveal] forceShow={forceShow} finished={finished} props={(props != null ? props.name : "null")}");
    }
}
