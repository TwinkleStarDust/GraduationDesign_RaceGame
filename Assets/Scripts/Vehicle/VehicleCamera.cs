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

    // 私有变量
    private Vector3 currentVelocity;
    private bool isFirstPersonView = false;
    private float currentRotationAngle = 0;
    private VehicleController vehicleController;

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
            isFirstPersonView = !isFirstPersonView;
        }

        // 更新相机位置
        UpdateCameraPosition(false);
    }

    /// <summary>
    /// 更新相机位置
    /// </summary>
    private void UpdateCameraPosition(bool immediate)
    {
        if (isFirstPersonView)
        {
            UpdateFirstPersonView(immediate);
        }
        else
        {
            UpdateThirdPersonView(immediate);
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
}