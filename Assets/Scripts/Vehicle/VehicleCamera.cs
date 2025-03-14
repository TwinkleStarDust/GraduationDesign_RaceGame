using UnityEngine;

/// <summary>
/// 车辆相机控制器
/// 负责跟随车辆并提供平滑的相机运动
/// </summary>
public class VehicleCamera : MonoBehaviour
{
    [Tooltip("目标车辆")]
    [SerializeField] private Transform target;

    [Header("跟随设置")]
    [Tooltip("相机距离")]
    [SerializeField] private float distance = 6.0f;

    [Tooltip("相机高度")]
    [SerializeField] private float height = 2.0f;

    [Tooltip("相机平滑度")]
    [SerializeField] private float smoothness = 10.0f;

    [Tooltip("相机旋转速度")]
    [SerializeField] private float rotationSpeed = 5.0f;

    [Header("视角切换")]
    [Tooltip("是否启用视角切换")]
    [SerializeField] private bool enableViewSwitch = true;

    [Tooltip("视角切换按键")]
    [SerializeField] private KeyCode switchViewKey = KeyCode.V;

    [Tooltip("第一人称视角偏移")]
    [SerializeField] private Vector3 firstPersonOffset = new Vector3(0, 1.5f, 0.2f);

    [Header("鼠标控制设置")]
    [Tooltip("鼠标水平灵敏度")]
    [SerializeField] private float mouseSensitivityX = 3.0f;

    [Tooltip("鼠标垂直灵敏度")]
    [SerializeField] private float mouseSensitivityY = 2.0f;

    [Tooltip("垂直视角限制（最小值）")]
    [SerializeField] private float minVerticalAngle = -30.0f;

    [Tooltip("垂直视角限制（最大值）")]
    [SerializeField] private float maxVerticalAngle = 60.0f;

    [Tooltip("环绕视角相机距离")]
    [SerializeField] private float orbitDistance = 5.0f;

    [Tooltip("环绕视角相机高度偏移")]
    [SerializeField] private float orbitHeightOffset = 1.0f;

    [Tooltip("是否在鼠标控制模式下隐藏鼠标")]
    [SerializeField] private bool hideCursorInMouseMode = true;

    // 相机视角模式枚举
    public enum CameraViewMode
    {
        ThirdPerson,    // 第三人称视角
        FirstPerson,    // 第一人称视角
        OrbitControl    // 环绕视角（鼠标控制）
    }

    // 当前相机视角模式
    private CameraViewMode currentViewMode = CameraViewMode.ThirdPerson;

    // 私有变量
    private Vector3 currentVelocity;
    private float currentRotationAngle = 0;
    private VehicleController vehicleController;

    // 环绕视角相关变量
    private float orbitX = 0f;
    private float orbitY = 10f;
    private Vector3 orbitOffset;
    private CursorLockMode previousCursorLockState;
    private bool previousCursorVisible;

    /// <summary>
    /// 初始化组件
    /// </summary>
    private void Start()
    {
        // 如果没有指定目标，尝试查找场景中的车辆
        if (target == null)
        {
            var vehicle = FindObjectOfType<VehicleController>();
            if (vehicle != null)
            {
                target = vehicle.transform;
                vehicleController = vehicle;
            }
            else
            {
                Debug.LogError("未找到车辆目标！");
            }
        }
        else
        {
            vehicleController = target.GetComponent<VehicleController>();
        }

        // 初始化相机位置
        if (target != null)
        {
            UpdateCameraPosition(true);
        }

        // 初始化环绕视角
        orbitX = target.eulerAngles.y;
        orbitY = 10f; // 初始仰角
        orbitOffset = new Vector3(0, orbitHeightOffset, 0);

        // 保存初始鼠标状态
        previousCursorLockState = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
    }

    /// <summary>
    /// 更新相机位置
    /// </summary>
    private void LateUpdate()
    {
        if (target == null) return;

        // 检查视角切换
        if (enableViewSwitch && Input.GetKeyDown(switchViewKey))
        {
            // 循环切换三种视角模式
            switch (currentViewMode)
            {
                case CameraViewMode.ThirdPerson:
                    SetViewMode(CameraViewMode.FirstPerson);
                    break;
                case CameraViewMode.FirstPerson:
                    SetViewMode(CameraViewMode.OrbitControl);
                    break;
                case CameraViewMode.OrbitControl:
                    SetViewMode(CameraViewMode.ThirdPerson);
                    break;
            }
        }

        // 更新相机位置
        UpdateCameraPosition(false);
    }

    /// <summary>
    /// 更新相机位置
    /// </summary>
    private void UpdateCameraPosition(bool immediate)
    {
        switch (currentViewMode)
        {
            case CameraViewMode.FirstPerson:
                UpdateFirstPersonView(immediate);
                break;
            case CameraViewMode.ThirdPerson:
                UpdateThirdPersonView(immediate);
                break;
            case CameraViewMode.OrbitControl:
                UpdateOrbitView(immediate);
                break;
        }
    }

    /// <summary>
    /// 更新第一人称视角
    /// </summary>
    private void UpdateFirstPersonView(bool immediate)
    {
        // 计算目标位置
        Vector3 targetPosition = target.TransformPoint(firstPersonOffset);

        // 计算目标旋转
        Quaternion targetRotation = target.rotation;

        // 设置相机位置和旋转
        if (immediate)
        {
            transform.position = targetPosition;
            transform.rotation = targetRotation;
        }
        else
        {
            // 平滑过渡
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, 1.0f / smoothness);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothness);
        }
    }

    /// <summary>
    /// 更新第三人称视角
    /// </summary>
    private void UpdateThirdPersonView(bool immediate)
    {
        // 获取车辆速度
        float speed = 0;
        if (vehicleController != null)
        {
            speed = vehicleController.GetCurrentSpeed();
        }

        // 根据车辆速度调整相机旋转
        float speedFactor = Mathf.Clamp01(speed / 50.0f);
        float rotationFactor = Mathf.Lerp(0.5f, 1.0f, speedFactor);

        // 计算目标位置
        Vector3 targetPosition = target.position;
        targetPosition.y += height;

        // 计算相机旋转角度
        currentRotationAngle = Mathf.LerpAngle(currentRotationAngle, target.eulerAngles.y, Time.deltaTime * rotationSpeed * rotationFactor);

        // 计算相机位置
        Vector3 direction = Quaternion.Euler(0, currentRotationAngle, 0) * Vector3.back;
        Vector3 desiredPosition = targetPosition + direction * distance;

        // 设置相机位置和旋转
        if (immediate)
        {
            transform.position = desiredPosition;
            transform.LookAt(targetPosition);
        }
        else
        {
            // 平滑过渡
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, 1.0f / smoothness);

            // 始终看向目标
            Vector3 lookAtPosition = targetPosition;
            transform.LookAt(lookAtPosition);
        }
    }

    /// <summary>
    /// 更新环绕视角（鼠标控制）
    /// </summary>
    private void UpdateOrbitView(bool immediate)
    {
        // 获取鼠标输入
        float mouseXInput = Input.GetAxis("Mouse X") * mouseSensitivityX;
        float mouseYInput = Input.GetAxis("Mouse Y") * mouseSensitivityY;

        // 更新环绕角度
        orbitX += mouseXInput;
        orbitY -= mouseYInput; // 反转Y轴，向上移动鼠标使视角向上

        // 限制垂直视角范围
        orbitY = Mathf.Clamp(orbitY, minVerticalAngle, maxVerticalAngle);

        // 计算目标中心点（车辆位置加上高度偏移）
        Vector3 targetCenter = target.position + new Vector3(0, orbitHeightOffset, 0);

        // 计算相机旋转
        Quaternion rotation = Quaternion.Euler(orbitY, orbitX, 0);

        // 计算相机位置（从目标点向外偏移一定距离）
        Vector3 negDistance = new Vector3(0, 0, -orbitDistance);
        Vector3 desiredPosition = targetCenter + rotation * negDistance;

        // 设置相机位置和旋转
        if (immediate)
        {
            transform.position = desiredPosition;
            transform.rotation = rotation;
        }
        else
        {
            // 平滑过渡
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, 1.0f / smoothness);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * smoothness);
        }

        // 确保相机始终看向目标中心点
        transform.LookAt(targetCenter);
    }

    /// <summary>
    /// 获取当前视角模式
    /// </summary>
    public CameraViewMode GetCurrentViewMode()
    {
        return currentViewMode;
    }

    /// <summary>
    /// 设置视角模式
    /// </summary>
    public void SetViewMode(CameraViewMode mode)
    {
        // 如果从环绕模式切换出去，恢复鼠标状态
        if (currentViewMode == CameraViewMode.OrbitControl && mode != CameraViewMode.OrbitControl)
        {
            RestoreCursorState();
        }

        currentViewMode = mode;

        // 如果切换到环绕模式，初始化环绕角度并隐藏鼠标
        if (mode == CameraViewMode.OrbitControl)
        {
            orbitX = target.eulerAngles.y;
            orbitY = 10f;

            if (hideCursorInMouseMode)
            {
                // 保存当前鼠标状态
                previousCursorLockState = Cursor.lockState;
                previousCursorVisible = Cursor.visible;

                // 锁定并隐藏鼠标
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    /// <summary>
    /// 恢复鼠标状态
    /// </summary>
    private void RestoreCursorState()
    {
        if (hideCursorInMouseMode)
        {
            // 恢复之前的鼠标状态
            Cursor.lockState = previousCursorLockState;
            Cursor.visible = previousCursorVisible;
        }
    }

    /// <summary>
    /// 当脚本被禁用或销毁时调用
    /// </summary>
    private void OnDisable()
    {
        // 确保恢复鼠标状态
        RestoreCursorState();
    }
}