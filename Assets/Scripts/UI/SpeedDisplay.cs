using UnityEngine;
using TMPro;

namespace UI
{
    /// <summary>
    /// 速度显示器
    /// 显示当前车速并根据相机FOV和速度百分比进行缩放
    /// </summary>
    public class SpeedDisplay : MonoBehaviour
    {
        [Header("速度显示设置")]
        [Tooltip("速度文本")]
        [SerializeField] private TextMeshProUGUI speedText;

        [Tooltip("速度单位 (km/h)")]
        [SerializeField] private TextMeshProUGUI unitText;

        [Tooltip("默认字体大小")]
        [SerializeField] private float defaultFontSize = 72f;

        [Header("速度平滑设置")]
        [Tooltip("速度显示平滑时间（秒）")]
        [SerializeField] private float speedSmoothTime = 0.2f;

        [Tooltip("速度显示更新阈值（差值大于此值才更新显示）")]
        [SerializeField] private float speedUpdateThreshold = 1f;

        [Header("缩放设置")]
        [Tooltip("基础缩放系数 (基于FOV变化)")]
        [SerializeField] private float fovScaleFactor = 1.2f;

        [Tooltip("速度缩放系数 (基于速度百分比)")]
        [SerializeField] private float speedScaleFactor = 1.5f;

        [Tooltip("速度缩放曲线")]
        [SerializeField] private AnimationCurve speedScaleCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 1.5f);

        [Header("引用")]
        // [Tooltip("车辆控制器脚本")] // 移除或注释掉这个SerializeField，因为我们将自动查找
        // [SerializeField] private CarController m_CarController;
        private CarController m_CarController; // 改为私有，不再通过Inspector赋值

        [Tooltip("相机FOV控制器")]
        [SerializeField] private CameraFOVController fovController;

        // 缓存
        private Camera mainCamera;
        private float defaultFOV;
        private float maxFOV;

        // 速度平滑
        private float currentDisplaySpeed;
        private float speedSmoothVelocity;
        private int lastDisplayedSpeed = -1;

        private void Awake()
        {
            // 自动获取带有 "Player" 标签的车辆的 CarController
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                m_CarController = playerObject.GetComponent<CarController>();
                if (m_CarController == null)
                {
                    Debug.LogError("SpeedDisplay: 在带有 'Player' 标签的对象上未找到 CarController 组件!", this);
                    enabled = false;
                    return;
                }
            }
            else
            {
                Debug.LogError("SpeedDisplay: 未在场景中找到带有 'Player' 标签的游戏对象!", this);
                enabled = false;
                return;
            }

            mainCamera = Camera.main;

            if (fovController != null)
            {
                // 从FOV控制器获取默认值
                defaultFOV = fovController.DefaultFOV;
                maxFOV = fovController.NitroFOV;
            }
            else
            {
                // 使用默认值
                defaultFOV = 60f;
                maxFOV = 70f;
            }

            // 设置初始字体大小
            if (speedText != null)
            {
                speedText.fontSize = defaultFontSize;
            }
            if (unitText != null)
            {
                unitText.fontSize = defaultFontSize * 0.5f; // 单位文本默认为速度文本的一半大小
            }

            // 设置默认缩放曲线（如果没有自定义的话）
            if (speedScaleCurve.length == 0)
            {
                speedScaleCurve = new AnimationCurve(
                    new Keyframe(0f, 1f, 0f, 0f),
                    new Keyframe(0.7f, 1.1f, 1f, 1f),
                    new Keyframe(1f, speedScaleFactor, 2f, 0f)
                );
            }
        }

        private void Update()
        {
            if (m_CarController == null || speedText == null) return;

            // 从CarController获取前进速度 (m/s)
            float forwardSpeedMS = m_CarController.GetCurrentForwardSpeedMS();
            float currentSpeedKPH = Mathf.Abs(forwardSpeedMS * 3.6f); // 转换为km/h并取绝对值

            // 从CarController获取目标引擎最大速度 (km/h)
            float targetEngineSpeedKPH_forDisplay = m_CarController.TargetEngineSpeedKPH; // 使用新的属性 TargetEngineSpeedKPH

            // 平滑处理速度显示 (km/h)
            currentDisplaySpeed = Mathf.SmoothDamp(
                currentDisplaySpeed, 
                currentSpeedKPH, 
                ref speedSmoothVelocity, 
                speedSmoothTime
            );

            // 只有当速度变化超过阈值或首次更新时才更新显示
            int roundedSpeedKPH = Mathf.RoundToInt(currentDisplaySpeed);
            if (Mathf.Abs(roundedSpeedKPH - lastDisplayedSpeed) >= speedUpdateThreshold || lastDisplayedSpeed == -1)
            {
                speedText.text = roundedSpeedKPH.ToString();
                lastDisplayedSpeed = roundedSpeedKPH;
            }

            // 计算速度百分比 (使用km/h进行比较, 基于目标引擎速度)
            float speedPercentage = (targetEngineSpeedKPH_forDisplay > 0.01f) ? Mathf.Clamp01(currentDisplaySpeed / targetEngineSpeedKPH_forDisplay) : 0f;
            
            // 计算FOV缩放
            float fovScale = 1f;
            if (mainCamera != null)
            {
                float currentFOV = mainCamera.fieldOfView;
                float fovDelta = currentFOV - defaultFOV;
                float maxFovDelta = maxFOV - defaultFOV;

                if (fovDelta > 0 && maxFovDelta > 0)
                {
                    float t = fovDelta / maxFovDelta;
                    fovScale = Mathf.Lerp(1f, fovScaleFactor, t);
                }
            }

            // 计算速度缩放（使用动画曲线）
            float speedScale = speedScaleCurve.Evaluate(speedPercentage);

            // 合并两种缩放效果
            float finalScale = fovScale * speedScale;

            // 应用缩放
            speedText.fontSize = defaultFontSize * finalScale;
            if (unitText != null)
            {
                unitText.fontSize = defaultFontSize * 0.5f * finalScale;
            }
        }
    }
} 