using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ObjectCuller : MonoBehaviour
{
    [Header("剔除设置")]
    public float cullDistance = 20f;           // 可见范围半径
    public float checkInterval = 0.2f;         // 检查间隔
    public bool cullColliders = false;         // 是否同时禁用碰撞器

    [Header("自动注册")]
    public bool autoFindObjects = true;        // 是否自动查找场景中所有带Renderer的物体
    public LayerMask includeLayers = ~0;       // 要管理的层（~0表示所有层）

    private Transform player;                  // 玩家Transform
    private List<Renderer> renderers = new List<Renderer>();
    private List<Collider> colliders = new List<Collider>();
    private float sqrCullDistance;

    void Start()
    {
        player = transform;
        sqrCullDistance = cullDistance * cullDistance;

        if (autoFindObjects)
        {
            Renderer[] allRenderers = FindObjectsOfType<Renderer>();
            foreach (var rend in allRenderers)
            {
                // 排除玩家自身以及不在指定层的物体
                if (((1 << rend.gameObject.layer) & includeLayers) != 0 && rend.transform != player)
                {
                    renderers.Add(rend);
                    if (cullColliders)
                    {
                        Collider col = rend.GetComponent<Collider>();
                        if (col != null) colliders.Add(col);
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("ObjectCuller: 自动查找未启用，请在Inspector手动添加Renderer列表。");
        }

        UpdateCulling();
        StartCoroutine(CullingLoop());
    }

    IEnumerator CullingLoop()
    {
        WaitForSeconds wait = new WaitForSeconds(checkInterval);
        while (true)
        {
            UpdateCulling();
            yield return wait;
        }
    }

    void UpdateCulling()
    {
        Vector3 playerPos = player.position;

        // 遍历所有渲染器，使用边界框（边缘）判定
        for (int i = 0; i < renderers.Count; i++)
        {
            Renderer rend = renderers[i];
            if (rend == null) continue;

            // 【核心改动】：计算玩家到物体边界框的最近距离（边缘距离）
            float sqrDist = rend.bounds.SqrDistance(playerPos);
            bool visible = sqrDist <= sqrCullDistance;

            if (rend.enabled != visible)
                rend.enabled = visible;
        }

        // 如果启用碰撞器剔除，同样使用碰撞器的边界框
        if (cullColliders)
        {
            for (int i = 0; i < colliders.Count; i++)
            {
                Collider col = colliders[i];
                if (col == null) continue;

                float sqrDist = col.bounds.SqrDistance(playerPos);
                bool visible = sqrDist <= sqrCullDistance;

                if (col.enabled != visible)
                    col.enabled = visible;
            }
        }
    }

    public void AddObject(GameObject obj)
    {
        Renderer rend = obj.GetComponent<Renderer>();
        if (rend != null && !renderers.Contains(rend))
        {
            renderers.Add(rend);
            if (cullColliders)
            {
                Collider col = obj.GetComponent<Collider>();
                if (col != null) colliders.Add(col);
            }
        }
    }

    public void RemoveObject(GameObject obj)
    {
        Renderer rend = obj.GetComponent<Renderer>();
        if (rend != null && renderers.Contains(rend))
        {
            renderers.Remove(rend);
            if (cullColliders)
            {
                Collider col = obj.GetComponent<Collider>();
                if (col != null) colliders.Remove(col);
            }
        }
    }

    public void SetCullDistance(float newDistance)
    {
        cullDistance = newDistance;
        sqrCullDistance = cullDistance * cullDistance;
    }
}