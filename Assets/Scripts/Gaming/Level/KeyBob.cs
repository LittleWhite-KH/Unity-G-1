using UnityEngine;

/// <summary>钥匙的小动画：上下浮动 + 自转，便于在暗处找到。</summary>
public class KeyBob : MonoBehaviour
{
    public float bobAmplitude = 0.09f;
    public float bobSpeed = 1.6f;
    public float spinSpeed = 45f;

    private Vector3 baseLocalPosition;

    private void Start()
    {
        baseLocalPosition = transform.localPosition;
    }

    private void Update()
    {
        float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        transform.localPosition = baseLocalPosition + Vector3.up * bob;
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.Self);
    }
}
