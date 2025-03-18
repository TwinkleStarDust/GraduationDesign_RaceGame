using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace Vehicle
{
    /// <summary>
    /// 车辆UI控制器
    /// 负责显示车辆的速度和其他信息
    /// </summary>
    public class VehicleUI : MonoBehaviour
    {
        [Tooltip("车辆控制器引用")]
        [SerializeField] private VehicleController vehicleController;

        [Tooltip("相机控制器引用")]
        [SerializeField] private VehicleCamera vehicleCamera;

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

        [Tooltip("相机模式文本")]
        [SerializeField] private TextMeshProUGUI cameraViewModeText;

        [Tooltip("氮气量填充条")]
        [SerializeField] private Image nitroFill;

        [Tooltip("氮气激活指示器")]
        [SerializeField] private Image nitroActiveIndicator;

        [Tooltip("氮气激活颜色")]
        [SerializeField] private Color nitroActiveColor = new Color(0, 0.8f, 1f);

        [Header("状态提示")]
        [Tooltip("翻转提示面板")]
        [SerializeField] private GameObject flipPromptPanel;

        [Tooltip("翻转提示文本")]
        [SerializeField] private TextMeshProUGUI flipPromptText;

        [Tooltip("空中控制提示面板")]
        [SerializeField] private GameObject airControlPromptPanel;

        [Tooltip("空中控制提示文本")]
        [SerializeField] private TextMeshProUGUI airControlPromptText;

        // 私有变量
        private Color normalColor;

        /// <summary>
        /// 初始化组件
        /// </summary>
        private void Start()
        {
            StartCoroutine(InitializeComponents());
        }

        /// <summary>
        /// 延迟初始化组件
        /// </summary>
        private IEnumerator InitializeComponents()
        {
            // 等待一帧，确保其他组件都已初始化
            yield return null;

            // 如果没有指定车辆控制器，尝试查找
            if (vehicleController == null)
            {
                vehicleController = FindObjectOfType<VehicleController>();

                if (vehicleController == null)
                {
                    Debug.LogError("未找到车辆控制器！请确保场景中存在带有VehicleController组件的车辆。");
                    yield break;
                }
            }

            // 如果没有指定相机控制器，尝试查找
            if (vehicleCamera == null)
            {
                vehicleCamera = FindObjectOfType<VehicleCamera>();

                if (vehicleCamera == null)
                {
                    Debug.LogWarning("未找到相机控制器！");
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

            // 设置提示文本
            if (flipPromptText != null)
            {
                flipPromptText.text = "车辆侧翻！按 A/D 键翻转车辆";
            }

            if (airControlPromptText != null)
            {
                airControlPromptText.text = "空中控制：W/S - 前后翻转，A/D - 左右翻滚";
            }

            // 初始隐藏提示面板
            if (flipPromptPanel != null)
            {
                flipPromptPanel.SetActive(false);
            }

            if (airControlPromptPanel != null)
            {
                airControlPromptPanel.SetActive(false);
            }

            // 更新驱动类型文本
            UpdateDriveTypeText();

            // 更新相机模式文本
            UpdateCameraViewModeText();
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

            // 更新氮气UI
            UpdateNitroUI();

            // 更新相机模式文本
            UpdateCameraViewModeText();

            // 更新状态提示
            UpdateStatusPrompts();
        }

        /// <summary>
        /// 更新状态提示
        /// </summary>
        private void UpdateStatusPrompts()
        {
            // 更新翻转提示
            if (flipPromptPanel != null && vehicleController != null)
            {
                bool shouldShow = vehicleController.IsFlipped();
                flipPromptPanel.SetActive(shouldShow);

                // 如果显示翻转提示，添加闪烁效果
                if (shouldShow && flipPromptText != null)
                {
                    float alpha = 0.5f + Mathf.PingPong(Time.time * 2f, 0.5f);
                    Color textColor = flipPromptText.color;
                    textColor.a = alpha;
                    flipPromptText.color = textColor;
                }
            }

            // 更新空中控制提示
            if (airControlPromptPanel != null && vehicleController != null)
            {
                bool shouldShow = vehicleController.IsInAir();
                airControlPromptPanel.SetActive(shouldShow);

                // 如果显示空中控制提示，添加闪烁效果
                if (shouldShow && airControlPromptText != null)
                {
                    float alpha = 0.5f + Mathf.PingPong(Time.time * 2f, 0.5f);
                    Color textColor = airControlPromptText.color;
                    textColor.a = alpha;
                    airControlPromptText.color = textColor;
                }
            }
        }

        /// <summary>
        /// 更新驱动类型文本
        /// </summary>
        private void UpdateDriveTypeText()
        {
            if (driveTypeText == null || vehicleController == null) return;

            // 获取驱动类型
            VehicleDriveSystem.DriveType driveType = vehicleController.GetDriveType();

            // 设置文本
            switch (driveType)
            {
                case VehicleDriveSystem.DriveType.FrontWheelDrive:
                    driveTypeText.text = "前轮驱动 (FWD)";
                    break;
                case VehicleDriveSystem.DriveType.RearWheelDrive:
                    driveTypeText.text = "后轮驱动 (RWD)";
                    break;
                case VehicleDriveSystem.DriveType.AllWheelDrive:
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

        /// <summary>
        /// 更新相机模式文本
        /// </summary>
        private void UpdateCameraViewModeText()
        {
            if (cameraViewModeText == null || vehicleCamera == null) return;

            // 获取当前相机模式
            VehicleCamera.CameraViewMode viewMode = vehicleCamera.GetCurrentViewMode();

            // 设置文本
            switch (viewMode)
            {
                case VehicleCamera.CameraViewMode.ThirdPerson:
                    cameraViewModeText.text = "视角: 第三人称";
                    break;
                case VehicleCamera.CameraViewMode.FirstPerson:
                    cameraViewModeText.text = "视角: 第一人称";
                    break;
                case VehicleCamera.CameraViewMode.OrbitControl:
                    cameraViewModeText.text = "视角: 环绕视角";
                    break;
            }
        }

        /// <summary>
        /// 更新氮气UI
        /// </summary>
        private void UpdateNitroUI()
        {
            if (vehicleController == null) return;

            // 更新氮气填充条
            if (nitroFill != null)
            {
                float nitroAmount = vehicleController.GetNitroAmount();
                nitroFill.fillAmount = nitroAmount;

                // 当氮气量低时给予视觉提示
                if (nitroAmount < 0.2f)
                {
                    nitroFill.color = Color.Lerp(Color.red, Color.yellow, nitroAmount * 5f);
                }
                else
                {
                    nitroFill.color = Color.white;
                }
            }

            // 更新氮气激活指示器
            if (nitroActiveIndicator != null)
            {
                bool isNitroActive = vehicleController.IsNitroActive();

                if (isNitroActive)
                {
                    // 氮气激活时的视觉效果
                    nitroActiveIndicator.enabled = true;
                    nitroActiveIndicator.color = nitroActiveColor;

                    // 添加脉动效果
                    float pulseFactor = 0.8f + Mathf.PingPong(Time.time * 5f, 0.4f);
                    nitroActiveIndicator.transform.localScale = Vector3.one * pulseFactor;
                }
                else
                {
                    // 氮气未激活时的视觉效果
                    nitroActiveIndicator.enabled = false;
                    nitroActiveIndicator.transform.localScale = Vector3.one;
                }
            }
        }
    }
}