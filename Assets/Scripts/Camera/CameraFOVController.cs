using UnityEngine;
using Vehicle;

/// <summary>
/// 相机FOV控制器
/// 用于在氮气加速时增大FOV，增强加速感
/// </summary>
public class CameraFOVController : MonoBehaviour
{
    [Header("FOV设置")]
    [Tooltip("默认FOV值")]
    [SerializeField] private float defaultFOV = 60f;

    [Tooltip("氮气加速时的最大FOV值")]
    [SerializeField] private float nitroFOV = 70f;

    [Tooltip("FOV变化速度")]
    [SerializeField] private float fovChangeSpeed = 3f;

    [Tooltip("FOV平滑度")]
    [SerializeField] private float fovSmoothness = 0.2f;

    [Header("引用")]
    [Tooltip("车辆控制器")]
    [SerializeField] private VehicleController vehicleController;

    // 相机组件
    private Camera cameraComponent;

    // 目标FOV值
    private float targetFOV;
    private float currentTargetFOV;
    private float velocityFOV = 0f;

    private void Awake()
    {
        // 获取相机组件
        cameraComponent = GetComponent<Camera>();
        if (cameraComponent == null)
        {
            Debug.LogError("CameraFOVController需要挂载在带有Camera组件的游戏对象上！");
            enabled = false;
            return;
        }

        // 设置初始FOV
        cameraComponent.fieldOfView = defaultFOV;
        targetFOV = defaultFOV;
        currentTargetFOV = defaultFOV;

        // 如果没有指定车辆控制器，尝试在场景中查找
        if (vehicleController == null)
        {
            vehicleController = FindObjectOfType<VehicleController>();
            if (vehicleController == null)
            {
                Debug.LogWarning("未找到VehicleController，FOV效果将不可用");
            }
        }
    }

    private void Update()
    {
        if (vehicleController == null) return;

        // 检查氮气状态和速度
        bool isNitroActive = vehicleController.IsNitroActive();
        float currentSpeed = vehicleController.GetCurrentSpeed();
        float maxSpeed = vehicleController.GetMaxSpeed();
        float speedFactor = Mathf.Clamp01(currentSpeed / maxSpeed);

        // 根据氮气状态和速度设置目标FOV
        if (isNitroActive)
        {
            // 根据速度因子计算FOV增量，速度越快FOV增加越多
            float speedFactorCurved = speedFactor * speedFactor;
            float fovIncrease = Mathf.Lerp(0, nitroFOV - defaultFOV, speedFactorCurved);
            targetFOV = defaultFOV + fovIncrease;
        }
        else
        {
            // 当速度超过最大速度的70%时，也稍微增加FOV
            if (speedFactor > 0.7f)
            {
                float highSpeedFactor = (speedFactor - 0.7f) / 0.3f;
                float highSpeedFactorCurved = highSpeedFactor * highSpeedFactor;
                float highSpeedFovIncrease = Mathf.Lerp(0, (nitroFOV - defaultFOV) * 0.4f, highSpeedFactorCurved);
                targetFOV = defaultFOV + highSpeedFovIncrease;
            }
            else
            {
                targetFOV = defaultFOV;
            }
        }

        // 双重平滑处理
        currentTargetFOV = Mathf.SmoothDamp(currentTargetFOV, targetFOV, ref velocityFOV, fovSmoothness);

        cameraComponent.fieldOfView = Mathf.Lerp(cameraComponent.fieldOfView, currentTargetFOV, Time.deltaTime * fovChangeSpeed);
    }
}