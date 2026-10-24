using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using System.Collections;
using System.Collections.Generic;

public enum CameraControlMode
{
    Orbit,          // 围绕目标+偏移轴心旋转，看向目标
    Fixed,          // 相机位置固定，视角旋转，看向轴心
    WorldOrbit      // 围绕世界坐标轴心旋转，看向轴心
}

public class OrbitCameraWithMouseHover : MonoBehaviour
{
    [Header("控制模式")]
    public CameraControlMode mode = CameraControlMode.WorldOrbit;

    [Header("默认目标与轴心（Orbit / Fixed 模式使用）")]
    public Transform defaultTarget;
    public Vector3 localPivotOffset = Vector3.zero;

    [Header("世界轴心（WorldOrbit 模式使用）")]
    public Vector3 worldPivotPosition = new Vector3(0, 0.4f, 0);

    [Header("默认距离（Orbit / WorldOrbit 模式有效）")]
    public float defaultDistance = 2f;

    [Header("固定位置（Fixed 模式有效）")]
    public Vector3 fixedPosition = Vector3.zero;

    [Header("角度限制（度）")]
    public float maxHorizontalAngle = 15f;
    public float maxVerticalAngle = 15f;

    [Header("灵敏度")]
    public float sensitivity = 1f;

    [Header("缓动速度（角度）")]
    public float smoothSpeed = 8f;

    [System.Serializable]
    public class TriggerGroup
    {
        public GameObject object1;
        public GameObject object2;

        [Header("悬停放大参数")]
        public float hoverDistance = 1f;          // 悬停时的距离（替代原 hoverDistance）
        public float transitionDuration = 1.0f;  // 过渡时长（放大和恢复共用）

        [Header("放大位置偏移（X / Y 轴，世界坐标）")]
        [Tooltip("放大后相机轴心相对 Object1 位置的 X 轴偏移")]
        public float zoomOffsetX = 0f;

        [Tooltip("放大后相机轴心相对 Object1 位置的 Y 轴偏移")]
        public float zoomOffsetY = 0f;
    }

    [Header("悬停分组触发")]
    public List<TriggerGroup> triggerGroups = new List<TriggerGroup>();

    // 内部状态
    private float currentH, currentV;
    private float targetH, targetV;
    private float baseH, baseV;
    private Vector2 mouseDelta;

    // 当前使用的轴心和距离
    private Vector3 currentPivot;
    private float currentDistance;

    // 默认轴心（用于恢复）
    private Vector3 defaultPivot;
    private float defaultDist;

    // 轨道控制是否启用
    private bool orbitEnabled = true;

    // ---------- 悬停组状态 ----------
    private int activeGroupIndex = -1;      // -1 表示无激活组
    private bool isZooming = false;         // 是否处于放大状态（含过渡中）
    private bool isMoving = false;          // 是否正在过渡动画
    private Coroutine currentTransition = null;

    // 保存过渡前的轨道参数（用于恢复时的起始值）
    private Vector3 startPivot;
    private float startDistance;

    // 目标值（由触发组决定）
    private Vector3 targetPivot;
    private float targetDistance;

    // ---------- 生命周期 ----------
    private void Start()
    {
        InitializeOrbit();
    }

    private void Update()
    {
        // 处理悬停组触发
        HandleHoverGroups();

        // 如果轨道控制启用且未处于放大状态，则正常更新轨道（鼠标旋转）
        if (orbitEnabled && !isZooming)
        {
            ReadMouseInput();
            ApplyAngleSmoothing();
            UpdateCamera();
        }
        // 否则不处理鼠标输入（轨道暂停）
    }

    // ---------- 轨道控制初始化 ----------
    private void InitializeOrbit()
    {
        if (mode != CameraControlMode.WorldOrbit && defaultTarget == null)
        {
            Debug.LogError("OrbitCameraWithMouseHover: Orbit/Fixed 模式需要指定 defaultTarget！");
            enabled = false;
            return;
        }

        defaultPivot = GetPivotPosition();
        defaultDist = defaultDistance;
        currentPivot = defaultPivot;
        currentDistance = defaultDist;

        Vector3 dir;
        if (mode == CameraControlMode.Orbit || mode == CameraControlMode.WorldOrbit)
            dir = (transform.position - currentPivot).normalized;
        else
            dir = (currentPivot - transform.position).normalized;
        if (dir.magnitude < 0.001f) dir = Vector3.forward;

        baseH = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        baseV = Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg;
        currentH = targetH = baseH;
        currentV = targetV = baseV;

        if (mode == CameraControlMode.Fixed)
            transform.position = fixedPosition;

        UpdateCamera();
    }

    // ---------- 鼠标输入（新旧 Input System 兼容） ----------
    private void ReadMouseInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            Vector2 delta = Mouse.current.delta.ReadValue() * 0.01f;
            mouseDelta = delta;
        }
        else
        {
            mouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        }
        float mx = mouseDelta.x * sensitivity;
        float my = mouseDelta.y * sensitivity;
#else
        float mx = Input.GetAxis("Mouse X") * sensitivity;
        float my = Input.GetAxis("Mouse Y") * sensitivity;
#endif
        targetH += mx;
        targetV -= my;

        float minH = baseH - maxHorizontalAngle;
        float maxH = baseH + maxHorizontalAngle;
        float minV = baseV - maxVerticalAngle;
        float maxV = baseV + maxVerticalAngle;
        targetH = Mathf.Clamp(targetH, minH, maxH);
        targetV = Mathf.Clamp(targetV, minV, maxV);
    }

    private void ApplyAngleSmoothing()
    {
        currentH = Mathf.Lerp(currentH, targetH, smoothSpeed * Time.deltaTime);
        currentV = Mathf.Lerp(currentV, targetV, smoothSpeed * Time.deltaTime);
    }

    // ---------- 核心：悬停组处理 ----------
    private void HandleHoverGroups()
    {
        // 获取鼠标下所有碰撞体
        List<GameObject> hitObjects = GetHitObjects();

        // 查找第一个满足条件的组（两组物体同时命中）
        int triggeredGroup = -1;
        for (int i = 0; i < triggerGroups.Count; i++)
        {
            var group = triggerGroups[i];
            if (group.object1 != null && group.object2 != null &&
                hitObjects.Contains(group.object1) && hitObjects.Contains(group.object2))
            {
                triggeredGroup = i;
                break;
            }
        }

        // ---------- 状态机 ----------
        if (activeGroupIndex == -1)  // 无激活组
        {
            if (triggeredGroup != -1)
            {
                // 触发放大
                StartZoom(triggeredGroup);
            }
        }
        else  // 已有激活组
        {
            if (isMoving)
            {
                // 过渡中，忽略一切
                return;
            }
            else  // 过渡完成，处于放大保持状态
            {
                var group = triggerGroups[activeGroupIndex];
                // 只检测第二个物体是否仍被悬停
                bool stillHoverSecond = hitObjects.Contains(group.object2);
                if (!stillHoverSecond)
                {
                    // 移出第二个物体 → 开始恢复
                    StartRestore();
                }
                // 否则保持放大
            }
        }
    }

    private List<GameObject> GetHitObjects()
    {
        List<GameObject> result = new List<GameObject>();
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null) return result;
        Vector2 mousePos = Mouse.current.position.ReadValue();
#else
        Vector3 mousePos = Input.mousePosition;
#endif
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        RaycastHit[] hits = Physics.RaycastAll(ray);
        foreach (var hit in hits)
        {
            result.Add(hit.collider.gameObject);
        }
        return result;
    }

    // ---------- 开始放大 ----------
    private void StartZoom(int groupIndex)
    {
        // 保存当前轨道状态作为起始值
        startPivot = currentPivot;
        startDistance = currentDistance;

        activeGroupIndex = groupIndex;
        isZooming = true;
        orbitEnabled = false;      // 禁用轨道控制

        var group = triggerGroups[groupIndex];
        if (group.object1 == null)
        {
            Debug.LogWarning($"组 {groupIndex} 的 Object1 为空，取消放大");
            RestoreImmediate();
            return;
        }

        // 目标轴心：Object1 的位置 + 每组自定义的 X / Y 轴偏移
        targetPivot = group.object1.transform.position + new Vector3(group.zoomOffsetX, group.zoomOffsetY, 0f);
        // 目标距离：使用组内设定的 hoverDistance
        targetDistance = group.hoverDistance;

        // 启动协程平滑过渡
        if (currentTransition != null) StopCoroutine(currentTransition);
        currentTransition = StartCoroutine(SmoothTransition(startPivot, startDistance, targetPivot, targetDistance, group.transitionDuration, () =>
        {
            isMoving = false;
        }));
        isMoving = true;
    }

    // ---------- 开始恢复 ----------
    private void StartRestore()
    {
        // 保存当前状态作为起始值
        startPivot = currentPivot;
        startDistance = currentDistance;

        // 目标恢复为默认轴心和距离
        targetPivot = defaultPivot;
        targetDistance = defaultDist;

        // 获取组过渡时长（可共用）
        float duration = triggerGroups[activeGroupIndex].transitionDuration;

        if (currentTransition != null) StopCoroutine(currentTransition);
        currentTransition = StartCoroutine(SmoothTransition(startPivot, startDistance, targetPivot, targetDistance, duration, () =>
        {
            // 恢复轨道控制
            orbitEnabled = true;
            isZooming = false;
            activeGroupIndex = -1;
            // 同步角度以避免跳变
            SyncAnglesFromTransform();
            // 更新当前轴心和距离（已由过渡协程设置）
            isMoving = false;
        }));
        isMoving = true;
    }

    private void RestoreImmediate()
    {
        orbitEnabled = true;
        isZooming = false;
        activeGroupIndex = -1;
        isMoving = false;
        if (currentTransition != null)
        {
            StopCoroutine(currentTransition);
            currentTransition = null;
        }
        // 直接跳回默认
        currentPivot = defaultPivot;
        currentDistance = defaultDist;
        SyncAnglesFromTransform();
        UpdateCamera();
    }

    // ---------- 平滑过渡协程（轴心和距离） ----------
    private IEnumerator SmoothTransition(Vector3 fromPivot, float fromDist, Vector3 toPivot, float toDist, float duration, System.Action onComplete)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            // 使用 SmoothStep 使运动更自然
            float smoothT = t * t * (3f - 2f * t);
            currentPivot = Vector3.Lerp(fromPivot, toPivot, smoothT);
            currentDistance = Mathf.Lerp(fromDist, toDist, smoothT);
            UpdateCamera();  // 每帧更新摄像机位置
            elapsed += Time.deltaTime;
            yield return null;
        }
        // 确保最终值准确
        currentPivot = toPivot;
        currentDistance = toDist;
        UpdateCamera();
        onComplete?.Invoke();
    }

    // ---------- 工具方法 ----------
    private void SyncAnglesFromTransform()
    {
        Vector3 pivot = GetPivotPosition();
        Vector3 dir = (transform.position - pivot).normalized;
        if (dir.magnitude < 0.001f) dir = Vector3.forward;
        float h = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        float v = Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg;
        currentH = targetH = h;
        currentV = targetV = v;
        baseH = h;
        baseV = v;
    }

    // ---------- 更新摄像机（轨道模式） ----------
    private void UpdateCamera()
    {
        Vector3 pivot = currentPivot;
        float dist = currentDistance;

        if (mode == CameraControlMode.Orbit || mode == CameraControlMode.WorldOrbit)
        {
            float hRad = currentH * Mathf.Deg2Rad;
            float vRad = currentV * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(
                Mathf.Sin(hRad) * Mathf.Cos(vRad),
                Mathf.Sin(vRad),
                Mathf.Cos(hRad) * Mathf.Cos(vRad)
            );
            transform.position = pivot + dir * dist;
            transform.LookAt(pivot);
        }
        else // Fixed
        {
            transform.position = fixedPosition;
            Vector3 toTarget = pivot - transform.position;
            if (toTarget.magnitude < 0.001f) toTarget = Vector3.forward;

            Vector3 baseDir = (pivot - transform.position).normalized;
            if (baseDir.magnitude < 0.001f) baseDir = Vector3.forward;

            Vector3 targetDir = baseDir;
            targetDir = Quaternion.AngleAxis(currentH - baseH, Vector3.up) * targetDir;
            Vector3 right = Vector3.Cross(Vector3.up, targetDir).normalized;
            if (right.magnitude < 0.001f) right = Vector3.right;
            targetDir = Quaternion.AngleAxis(currentV - baseV, right) * targetDir;

            transform.rotation = Quaternion.LookRotation(targetDir, Vector3.up);
        }
    }

    private Vector3 GetPivotPosition()
    {
        if (mode == CameraControlMode.WorldOrbit)
            return worldPivotPosition;
        else if (defaultTarget != null)
            return defaultTarget.position + defaultTarget.rotation * localPivotOffset;
        else
            return Vector3.zero;
    }

    // ---------- 调试可视化 ----------
    private void OnDrawGizmosSelected()
    {
        Vector3 pivot = GetPivotPosition();
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(pivot, 0.2f);
        if (defaultTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(defaultTarget.position, 0.15f);
        }
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, pivot);
    }
}