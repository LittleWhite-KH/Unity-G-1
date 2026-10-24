using UnityEngine;
using UnityEngine.UI;

public class ProgressMonitor : MonoBehaviour
{
    [Header("San 目标")]
    public ThreeLayerNativeSlider threeLayerSlider;   // 改为引用 ThreeLayerNativeSlider
    public float requiredSan = 80f;

    [Header("计时参数")]
    public float holdDuration = 5f;
    public float fillDuration = 2f;

    [Header("UI 绑定")]
    public Image progressImage;

    private enum State { Waiting, Holding, Filling, Complete }
    private State currentState = State.Waiting;
    private float holdTimer = 0f;
    private float fillTimer = 0f;
    private bool isComplete = false;

    void Update()
    {
        if (isComplete) return;
        if (threeLayerSlider == null) return;

        float currentSan = threeLayerSlider.GetCurrentSan();

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
                break;
        }
    }

    public void Reset()
    {
        currentState = State.Waiting;
        holdTimer = 0f;
        fillTimer = 0f;
        isComplete = false;
        if (progressImage != null) progressImage.fillAmount = 0f;
    }

    public bool IsComplete() => isComplete;

    private void OnProgressComplete()
    {
        // 可触发事件
    }
}