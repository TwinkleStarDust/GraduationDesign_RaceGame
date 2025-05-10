using UnityEngine;
// Removed: using Vehicle; // No longer needed if CarController is in global namespace or its own

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

    [Tooltip("FOV达到目标值所需的平滑时间（秒）")] // Changed Tooltip for clarity with SmoothDamp
    [SerializeField] private float fovSmoothTime = 0.3f; // Renamed from fovChangeSpeed & fovSmoothness

    // Removed fovSmoothness as we'll primarily use fovSmoothTime with SmoothDamp

    [Header("引用")]
    // [Tooltip("车辆控制器脚本实例")] // 移除或注释掉这个SerializeField
    // [SerializeField] private CarController targetCarController;
    private CarController targetCarController; // 改为私有

    // 公共属性
    public float DefaultFOV => defaultFOV;
    public float NitroFOV => nitroFOV;
    public float CurrentFOV => cameraComponent ? cameraComponent.fieldOfView : defaultFOV;

    // 相机组件
    private Camera cameraComponent;

    // 目标FOV值
    private float targetFOVForSmoothing; // Renamed from targetFOV to avoid confusion with the local targetFOV in Update
    private float velocityFOV = 0f;
    // Removed currentTargetFOV as SmoothDamp handles the current value internally and updates it via ref velocity

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
        targetFOVForSmoothing = defaultFOV;

        // 自动获取带有 "Player" 标签的车辆的 CarController
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            targetCarController = playerObject.GetComponent<CarController>();
            if (targetCarController == null)
            {
                Debug.LogWarning("CameraFOVController: 在带有 'Player' 标签的对象上未找到 CarController 组件! FOV效果可能受影响。", this);
            }
        }
        else
        {
            Debug.LogWarning("CameraFOVController: 未在场景中找到带有 'Player' 标签的游戏对象! FOV效果可能受影响。", this);
        }
    }

    private void Update()
    {
        if (cameraComponent == null) return; // Should not happen if Awake succeeded

        float calculatedTargetFOV = defaultFOV;

        if (targetCarController != null && targetCarController.IsNitroSystemEnabled) // 使用新的属性 IsNitroSystemEnabled
        {
            bool isNitroActive = targetCarController.IsNitroActiveAndEnabled; // 使用新的属性 IsNitroActiveAndEnabled
            float currentSpeedMS = targetCarController.GetCurrentForwardSpeedMS();
            float maxSpeedKPH = targetCarController.TargetEngineSpeedKPH; // 使用新的属性 TargetEngineSpeedKPH
            float maxSpeedMS = maxSpeedKPH / 3.6f; // Convert to M/S for consistent comparison

            float speedFactor = 0f;
            if (maxSpeedMS > 0.01f) // Avoid division by zero
            {
                speedFactor = Mathf.Clamp01(currentSpeedMS / maxSpeedMS);
            }

            // 根据氮气状态和速度设置目标FOV
            if (isNitroActive)
            {
                // 根据速度因子计算FOV增量，速度越快FOV增加越多
                float speedFactorCurved = speedFactor * speedFactor; // Simple squaring for non-linear response
                float fovIncrease = Mathf.Lerp(0, nitroFOV - defaultFOV, speedFactorCurved);
                calculatedTargetFOV = defaultFOV + fovIncrease;
            }
            else
            {
                // 当速度超过最大速度的70%时，也稍微增加FOV
                if (speedFactor > 0.7f)
                {
                    float highSpeedFactor = (speedFactor - 0.7f) / 0.3f; // Normalize 0.7-1.0 range to 0-1
                    float highSpeedFactorCurved = highSpeedFactor * highSpeedFactor;
                    float highSpeedFovIncrease = Mathf.Lerp(0, (nitroFOV - defaultFOV) * 0.4f, highSpeedFactorCurved); // Max 40% of nitro FOV increase
                    calculatedTargetFOV = defaultFOV + highSpeedFovIncrease;
                }
                else
                {
                    calculatedTargetFOV = defaultFOV;
                }
            }
        }
        else // If no CarController or nitro system disabled, revert to default FOV
        {
            calculatedTargetFOV = defaultFOV;
        }
        
        // 使用SmoothDamp平滑FOV变化
        cameraComponent.fieldOfView = Mathf.SmoothDamp(cameraComponent.fieldOfView, calculatedTargetFOV, ref velocityFOV, fovSmoothTime);
    }
}