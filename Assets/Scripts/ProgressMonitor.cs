using UnityEngine;
using UnityEngine.UI;

public class ProgressMonitor : MonoBehaviour
{
    [Header("San 目标")]
    public SanSystem sanSystem;
    public float requiredSan = 80f;

    [Header("计时参数")]
    public float holdDuration = 5f;      // 需要维持达标的时间（秒）
    public float fillDuration = 2f;      // 进度条填充动画时长（秒）

    [Header("UI 绑定")]
    public Image progressImage;          // 进度条图像（Image 类型为 Filled）

    private enum State { Waiting, Holding, Filling, Complete }
    private State currentState = State.Waiting;
    private float holdTimer = 0f;
    private float fillTimer = 0f;
    private bool isComplete = false;

    void Update()
    {
        if (isComplete) return;

        float currentSan = sanSystem.GetCurrentSan();

        switch (currentState)
        {
            case State.Waiting:
                if (currentSan >= requiredSan)
                {
                    currentState = State.Holding;
                    holdTimer = 0f;
                }
                else
                {
                    progressImage.fillAmount = 0f;
                }
                break;

            case State.Holding:
                if (currentSan >= requiredSan)
                {
                    holdTimer += Time.deltaTime;
                    if (holdTimer >= holdDuration)
                    {
                        currentState = State.Filling;
                        fillTimer = 0f;
                        progressImage.fillAmount = 0f;
                    }
                }
                else
                {
                    currentState = State.Waiting;
                    holdTimer = 0f;
                    progressImage.fillAmount = 0f;
                }
                break;

            case State.Filling:
                fillTimer += Time.deltaTime;
                float progress = Mathf.Clamp01(fillTimer / fillDuration);
                progressImage.fillAmount = progress;
                if (progress >= 1f)
                {
                    currentState = State.Complete;
                    isComplete = true;
                    Debug.Log("进度条完成！");
                    OnProgressComplete();
                }
                break;

            case State.Complete:
                // 已完成，保持进度条满
                break;
        }
    }

    // 重置方法（供外部调用）
    public void Reset()
    {
        currentState = State.Waiting;
        holdTimer = 0f;
        fillTimer = 0f;
        isComplete = false;
        if (progressImage != null) progressImage.fillAmount = 0f;
    }

    public bool IsComplete()
    {
        return isComplete;
    }

    private void OnProgressComplete()
    {
        // 可在此触发后续事件，或通过委托/事件广播
    }
}