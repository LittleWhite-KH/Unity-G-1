using UnityEditor;
using UnityEngine;

/// <summary>流程探针的开关（Play 前设置，Play 后读日志）。</summary>
public static class DshFlowTestMenu
{
    [MenuItem("Tools/DSH/Test/Flow Probe ON")]
    public static void On()
    {
        PlayerPrefs.SetInt("dsh.flowprobe", 1);
        PlayerPrefs.Save();
        Debug.Log("[Probe] enabled");
    }

    [MenuItem("Tools/DSH/Test/Flow Probe OFF")]
    public static void Off()
    {
        PlayerPrefs.SetInt("dsh.flowprobe", 0);
        PlayerPrefs.Save();
        Debug.Log("[Probe] disabled");
    }
}
