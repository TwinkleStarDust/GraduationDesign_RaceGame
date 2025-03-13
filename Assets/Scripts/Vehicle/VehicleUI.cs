using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 车辆UI控制器
/// 负责显示车辆的速度和其他信息
/// </summary>
public class VehicleUI : MonoBehaviour
{
    [Tooltip("车辆控制器引用")]
    [SerializeField] private VehicleController vehicleController;

    [Header("UI元素")]
    [Tooltip("速度文本")]
    [SerializeField] private TextMeshProUGUI speedText;

    [Tooltip("速度表")]
    [SerializeField] private Image speedometerFill;

    [Tooltip("最大速度 (用于速度表)")]
    [SerializeField] private float maxSpeedForGauge = 200.0f;

    [Tooltip("驱动类型文本")]
    [SerializeField] private TextMeshProUGUI driveTypeText;

    [Tooltip("漂移指示器")]
    [SerializeField] private Image driftIndicator;

    [Tooltip("漂移指示器颜色")]
    [SerializeField] private Color driftColor = Color.red;

    // 私有变量
    private Color normalColor;

    /// <summary>
    /// 初始化组件
    /// </summary>
    private void Start()
    {
        // 如果没有指定车辆控制器，尝试查找
        if (vehicleController == null)
        {
            vehicleController = FindObjectOfType<VehicleController>();

            if (vehicleController == null)
            {
                Debug.LogError("未找到车辆控制器！");
            }
        }

        // 检查UI元素
        if (speedText == null)
        {
            Debug.LogWarning("未指定速度文本！");
        }

        if (speedometerFill == null)
        {
            Debug.LogWarning("未指定速度表！");
        }

        // 保存漂移指示器的正常颜色
        if (driftIndicator != null)
        {
            normalColor = driftIndicator.color;
        }

        // 更新驱动类型文本
        UpdateDriveTypeText();
    }

    /// <summary>
    /// 更新UI
    /// </summary>
    private void Update()
    {
        if (vehicleController == null) return;

        // 获取当前速度
        float currentSpeed = vehicleController.GetCurrentSpeed();

        // 更新速度文本
        if (speedText != null)
        {
            speedText.text = Mathf.Round(currentSpeed).ToString() + " km/h";
        }

        // 更新速度表
        if (speedometerFill != null)
        {
            speedometerFill.fillAmount = Mathf.Clamp01(currentSpeed / maxSpeedForGauge);
        }

        // 更新漂移指示器
        UpdateDriftIndicator();
    }

    /// <summary>
    /// 更新驱动类型文本
    /// </summary>
    private void UpdateDriveTypeText()
    {
        if (driveTypeText == null || vehicleController == null) return;

        // 获取驱动类型
        VehicleController.DriveType driveType = vehicleController.GetDriveType();

        // 设置文本
        switch (driveType)
        {
            case VehicleController.DriveType.FrontWheelDrive:
                driveTypeText.text = "前轮驱动 (FWD)";
                break;
            case VehicleController.DriveType.RearWheelDrive:
                driveTypeText.text = "后轮驱动 (RWD)";
                break;
            case VehicleController.DriveType.AllWheelDrive:
                driveTypeText.text = "四轮驱动 (AWD)";
                break;
        }
    }

    /// <summary>
    /// 更新漂移指示器
    /// </summary>
    private void UpdateDriftIndicator()
    {
        if (driftIndicator == null || vehicleController == null) return;

        // 获取漂移状态
        bool isDrifting = vehicleController.IsDrifting();
        float driftFactor = vehicleController.GetDriftFactor();

        // 更新指示器颜色
        if (isDrifting)
        {
            // 根据漂移强度插值颜色
            driftIndicator.color = Color.Lerp(normalColor, driftColor, driftFactor);

            // 可以添加一些动画效果，如缩放或闪烁
            float pulseFactor = 0.8f + Mathf.PingPong(Time.time * 2f, 0.4f);
            driftIndicator.transform.localScale = Vector3.one * pulseFactor;
        }
        else
        {
            // 恢复正常颜色和大小
            driftIndicator.color = normalColor;
            driftIndicator.transform.localScale = Vector3.one;
        }
    }
}