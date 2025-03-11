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

    [Header("调试信息")]
    [Tooltip("是否显示调试信息")]
    [SerializeField] private bool showDebugInfo = true;

    [Tooltip("调试文本")]
    [SerializeField] private TextMeshProUGUI debugText;

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

        if (showDebugInfo && debugText == null)
        {
            Debug.LogWarning("启用了调试信息但未指定调试文本！");
        }
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

        // 更新调试信息
        if (showDebugInfo && debugText != null)
        {
            UpdateDebugInfo();
        }
    }

    /// <summary>
    /// 更新调试信息
    /// </summary>
    private void UpdateDebugInfo()
    {
        // 获取车辆信息
        string debugInfo = "";

        // 添加速度信息
        debugInfo += "速度: " + Mathf.Round(vehicleController.GetCurrentSpeed()).ToString() + " km/h\n";

        // 添加物理信息
        Rigidbody rb = vehicleController.GetComponent<Rigidbody>();
        if (rb != null)
        {
            debugInfo += "质量: " + rb.mass.ToString("F1") + " kg\n";
            debugInfo += "速度向量: " + rb.linearVelocity.ToString("F1") + "\n";
            debugInfo += "角速度: " + rb.angularVelocity.ToString("F1") + "\n";
        }

        // 添加控制信息
        debugInfo += "\n控制说明:\n";
        debugInfo += "W/S - 前进/后退\n";
        debugInfo += "A/D - 左转/右转\n";
        debugInfo += "空格 - 手刹\n";
        debugInfo += "R - 重置车辆\n";
        debugInfo += "V - 切换视角\n";

        // 设置调试文本
        debugText.text = debugInfo;
    }
}