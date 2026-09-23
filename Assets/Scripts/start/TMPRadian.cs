using UnityEngine;
using TMPro;

[ExecuteAlways]
[RequireComponent(typeof(TMP_Text))]
public class TMPRadian : MonoBehaviour
{
    [Header("Arc Settings")]

    [Tooltip("整体弯曲角度")]
    [Range(-180f, 180f)]
    public float arcAngle = 60f;

    [Tooltip("圆弧半径，越大越平缓")]
    [Min(1f)]
    public float radius = 500f;

    [Tooltip("字符是否跟随 XZ 圆弧旋转")]
    public bool rotateCharacter = true;

    [Header("Offset")]

    [Tooltip("X / Z 方向偏移")]
    public Vector2 offset = Vector2.zero;

    private TMP_Text tmpText;

    private void OnEnable()
    {
        tmpText = GetComponent<TMP_Text>();

        tmpText.OnPreRenderText += BendText;

        Refresh();
    }

    private void OnDisable()
    {
        if (tmpText != null)
        {
            tmpText.OnPreRenderText -= BendText;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (tmpText == null)
            tmpText = GetComponent<TMP_Text>();

        Refresh();
    }
#endif

    public void Refresh()
    {
        if (tmpText == null)
            return;

        tmpText.ForceMeshUpdate();
    }

    private void BendText(TMP_TextInfo textInfo)
    {
        if (textInfo == null || textInfo.characterCount == 0)
            return;

        float minX = float.MaxValue;
        float maxX = float.MinValue;

        bool foundVisibleCharacter = false;

        // ---------------------------------
        // 获取整段文字的 X 范围
        // ---------------------------------
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];

            if (!charInfo.isVisible)
                continue;

            foundVisibleCharacter = true;

            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;

            Vector3[] vertices =
                textInfo.meshInfo[materialIndex].vertices;

            float centerX =
                (vertices[vertexIndex].x +
                 vertices[vertexIndex + 2].x) * 0.5f;

            minX = Mathf.Min(minX, centerX);
            maxX = Mathf.Max(maxX, centerX);
        }

        if (!foundVisibleCharacter)
            return;

        float textWidth = maxX - minX;

        if (textWidth <= 0.001f)
            return;

        float originalCenterX =
            (minX + maxX) * 0.5f;

        // ---------------------------------
        // 逐字符修改
        // ---------------------------------
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo =
                textInfo.characterInfo[i];

            if (!charInfo.isVisible)
                continue;

            int materialIndex =
                charInfo.materialReferenceIndex;

            int vertexIndex =
                charInfo.vertexIndex;

            Vector3[] vertices =
                textInfo.meshInfo[materialIndex].vertices;

            // 当前字符中心
            Vector3 charCenter =
                (vertices[vertexIndex] +
                 vertices[vertexIndex + 2]) * 0.5f;

            // 0 ~ 1
            float normalizedPosition =
                Mathf.InverseLerp(
                    minX,
                    maxX,
                    charCenter.x
                );

            // -0.5 ~ 0.5
            float centeredPosition =
                normalizedPosition - 0.5f;

            // 当前字符所处圆弧角度
            float angle =
                centeredPosition * arcAngle;

            float rad =
                angle * Mathf.Deg2Rad;

            // =================================
            // X-Z 圆弧
            // =================================

            float arcX =
                Mathf.Sin(rad) * radius;

            float arcZ =
                radius -
                Mathf.Cos(rad) * radius;

            // Y 保持原值
            Vector3 targetCenter =
                new Vector3(
                    originalCenterX + arcX + offset.x,
                    charCenter.y,
                    charCenter.z + arcZ + offset.y
                );

            // ---------------------------------
            // 保存字符自己的顶点形状
            // ---------------------------------
            Vector3 bottomLeft =
                vertices[vertexIndex] -
                charCenter;

            Vector3 topLeft =
                vertices[vertexIndex + 1] -
                charCenter;

            Vector3 topRight =
                vertices[vertexIndex + 2] -
                charCenter;

            Vector3 bottomRight =
                vertices[vertexIndex + 3] -
                charCenter;

            Quaternion rotation =
                Quaternion.identity;

            // ---------------------------------
            // XZ 平面的切线旋转
            // 因此绕 Y 轴旋转
            // ---------------------------------
            if (rotateCharacter)
            {
                rotation =
                    Quaternion.Euler(
                        0f,
                        -angle,
                        0f
                    );
            }

            vertices[vertexIndex] =
                targetCenter +
                rotation * bottomLeft;

            vertices[vertexIndex + 1] =
                targetCenter +
                rotation * topLeft;

            vertices[vertexIndex + 2] =
                targetCenter +
                rotation * topRight;

            vertices[vertexIndex + 3] =
                targetCenter +
                rotation * bottomRight;
        }
    }
}