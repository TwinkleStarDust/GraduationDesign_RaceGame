// 车辆驱动系统
// 处理车辆的驱动力、制动力、转向和氮气系统
// 作为车辆的主控制器，协调各个子系统
// 简化版本，适合街机风格赛车游戏

using UnityEngine;
using System.Collections;
using System;

namespace Vehicle
{
    public class VehicleDriveSystem : MonoBehaviour
    {
        #region 公共变量
        [Header("调试选项")]
        [SerializeField] private bool showDebugInfo = false;

        [Header("性能参数")]
        [Tooltip("最高速度 (km/h)")]
        [Range(5f, 300f)]
        [SerializeField] private float maxSpeed = 200f;

        [Tooltip("加速度")]
        [Range(10f, 100f)]
        [SerializeField] private float acceleration = 30f;

        [Tooltip("制动力 - 影响S键制动效果")]
        [Range(10f, 300f)]
        [SerializeField] private float brakeForce = 150f;

        [Tooltip("转向速度")]
        [Range(10f, 100f)]
        [SerializeField] private float steeringSpeed = 40f;

        [Header("氮气系统")]
        [Tooltip("氮气容量")]
        [SerializeField] private float nitroCapacity = 100f;

        [Tooltip("氮气加速系数")]
        [Range(1.1f, 2.0f)]
        [SerializeField] private float nitroBoostFactor = 1.5f;

        [Tooltip("氮气恢复速度")]
        [Range(1f, 50f)]
        [SerializeField] private float nitroRecoveryRate = 10f;

        [Tooltip("氮气消耗速度")]
        [Range(1f, 50f)]
        [SerializeField] private float nitroConsumptionRate = 25f;

        [Header("高级驾驶设置")]
        [Tooltip("最大转向角度")]
        [Range(10f, 60f)]
        [SerializeField] private float maxSteeringAngle = 30f;

        [Tooltip("转向响应曲线")]
        [SerializeField] private AnimationCurve steeringCurve;

        [Tooltip("最大倒车速度 (km/h)")]
        [Range(10f, 50f)]
        [SerializeField] private float maxReverseSpeed = 40f;

        [Tooltip("倒车速度系数")]
        [Range(0.5f, 1.5f)]
        [SerializeField] private float reverseSpeedFactor = 0.8f;

        [Header("手刹与漂移设置")]
        [Tooltip("手刹力度 - 影响空格键制动和漂移效果")]
        [Range(100f, 2000f)]
        [SerializeField] private float handbrakeTorque = 500f;

        [Tooltip("漂移时后轮横向刚度系数 - 值越低漂移越明显")]
        [Range(0.1f, 1.0f)]
        [SerializeField] private float driftFactor = 0.5f;

        [Tooltip("漂移恢复时间（秒）- 值越高漂移结束后恢复越平滑")]
        [Range(0.1f, 2.0f)]
        [SerializeField] private float driftRecoveryTime = 1.3f;

        [Tooltip("漂移最小速度 - 低于此速度不会触发漂移效果")]
        [SerializeField] private float minDriftSpeed = 25f;

        [Tooltip("启用漂移")]
        [SerializeField] private bool enableDrifting = true;

        [Tooltip("引擎扭矩曲线")]
        [SerializeField]
        private AnimationCurve engineTorqueCurve;

        // 引用其他组件

        // 将km/h转换为m/s的系数
        private const float KMH_TO_MS = 0.2778f;

        // 用于管理漂移恢复的协程
        private Coroutine driftRecoveryCoroutine;
        // 保存原始的轮胎摩擦力设置
        private WheelFrictionCurve[] originalSidewaysFriction;

        #endregion

        // 传送事件
        public event Action OnBeforeTeleport;
        public event Action OnAfterTeleport;

        #region 私有变量
        // 引用
        private VehiclePhysics vehiclePhysics;
        private Rigidbody vehicleRigidbody;

        // 驱动因子 - 固定为后轮驱动
        private float frontWheelDriveFactor = 0.0f;
        private float rearWheelDriveFactor = 1.0f;

        // 输入值
        private float throttleInput = 0f;
        private float brakeInput = 0f;
        private float steeringInput = 0f;
        private bool isHandbrakeActive = false;
        private bool isNitroActive = false;

        // 氮气状态
        private float currentNitroAmount;

        // 车辆状态
        private float currentSpeed = 0f;
        private float normalizedSpeed = 0f;
        private float motorTorque = 0f;
        private bool isInAir = false;
        private bool isFlipped = false;
        private bool isUpsideDown = false;
        private bool isDrifting = false;
        private float currentDriftFactor = 0f;
        private bool isDriftingRequested = false;

        // RPM范围
        private float minRPM = 800f;
        private float maxRPM = 7000f;
        private float currentRPM = 800f;

        // 转向平滑变量
        private float steeringVelocity = 0f;
        
        // 滑行状态过渡变量
        private float coastingTransitionTime = 0f;
        private const float FULL_COASTING_TIME = 1.0f; // 完全进入滑行状态需要的时间（秒）
        #endregion

        #region 初始化
        private void Awake()
        {
            // 获取组件引用
            vehiclePhysics = GetComponent<VehiclePhysics>();
            vehicleRigidbody = GetComponent<Rigidbody>();

            if (vehiclePhysics == null)
            {
                Debug.LogError("【车辆驱动系统】缺少VehiclePhysics组件！");
                enabled = false;
                return;
            }

            // 初始化引擎扭矩曲线
            if (engineTorqueCurve == null || engineTorqueCurve.keys.Length == 0)
            {
                engineTorqueCurve = new AnimationCurve(
                    new Keyframe(0f, 1.0f),    // 静止时100%扭矩
                    new Keyframe(0.2f, 1.2f),  // 低速时120%扭矩，增强起步体验
                    new Keyframe(0.5f, 1.0f),  // 中速时100%扭矩
                    new Keyframe(0.8f, 0.8f),  // 高速时80%扭矩
                    new Keyframe(1.0f, 0.7f)   // 最高速时70%扭矩
                );
            }

            // 初始化转向响应曲线
            if (steeringCurve == null || steeringCurve.keys.Length == 0)
            {
                steeringCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            }

            // 驱动因子已在声明时固定为后轮驱动，无需 SetupDriveTypeFactors()
            // frontWheelDriveFactor = 0.0f;
            // rearWheelDriveFactor = 1.0f;

            // 初始化轮胎摩擦力数组
            if (vehiclePhysics != null && vehiclePhysics.GetRearLeftWheel() != null && vehiclePhysics.GetRearRightWheel() != null)
            {
                originalSidewaysFriction = new WheelFrictionCurve[4];
                originalSidewaysFriction[0] = vehiclePhysics.GetFrontLeftWheel().sidewaysFriction;
                originalSidewaysFriction[1] = vehiclePhysics.GetFrontRightWheel().sidewaysFriction;
                originalSidewaysFriction[2] = vehiclePhysics.GetRearLeftWheel().sidewaysFriction;
                originalSidewaysFriction[3] = vehiclePhysics.GetRearRightWheel().sidewaysFriction;
            }

            currentNitroAmount = nitroCapacity;
        }

        private void Update()
        {
            // 更新车辆状态
            UpdateVehicleStatus();

            // 更新RPM值
            UpdateRPM();

            // 更新氮气状态
            UpdateNitroState();

            // 显示调试信息
            if (showDebugInfo)
            {
                DisplayDebugInfo();
            }
        }

        /// 显示调试信息
        private void DisplayDebugInfo()
        {
            Debug.Log($"车辆状态: 速度={currentSpeed:F1}km/h, 在空中={isInAir}, 侧翻={isFlipped}, 倒置={isUpsideDown}, 漂移={isDrifting}");
        }

        private void FixedUpdate()
        {
            // 应用驱动力
            ApplyDrive();

            // 应用转向
            ApplySteering();

            // 限制速度
            LimitSpeed();
        }

        /// 设置驱动类型因子
        // Removed SetupDriveTypeFactors() method
        #endregion

        #region 更新车辆状态
        private void UpdateVehicleStatus()
        {
            if (vehiclePhysics == null) return;

            // 计算当前速度
            currentSpeed = Mathf.Abs(vehiclePhysics.GetForwardSpeed()) * (1f / KMH_TO_MS);
            // 标准化速度（0-1范围）
            normalizedSpeed = Mathf.Clamp01(currentSpeed / maxSpeed);

            // 计算发动机扭矩
            motorTorque = acceleration * 120f;

            // 更新车辆状态
            isInAir = vehiclePhysics.IsVehicleInAir();
            isFlipped = vehiclePhysics.IsVehicleFlipped();
            isUpsideDown = vehiclePhysics.IsVehicleUpsideDown();
        }

        private void UpdateRPM()
        {
            // 根据速度和油门计算RPM
            float targetRPM;

            if (throttleInput > 0.1f)
            {
                // 加速或巡航时的RPM
                targetRPM = Mathf.Lerp(minRPM, maxRPM, normalizedSpeed);
                // 在低速区额外提高RPM以反映高扭矩
                if (normalizedSpeed < 0.1f)
                {
                    targetRPM = Mathf.Lerp(minRPM * 2f, targetRPM, normalizedSpeed * 10f);
                }
            }
            else if (brakeInput > 0.1f)
            {
                // 制动时的RPM
                targetRPM = Mathf.Lerp(maxRPM * 0.5f, minRPM, brakeInput);
            }
            else
            {
                // 滑行时的RPM
                targetRPM = Mathf.Lerp(minRPM, maxRPM * 0.7f, normalizedSpeed);
            }

            // 平滑RPM变化
            currentRPM = Mathf.Lerp(currentRPM, targetRPM, Time.deltaTime * 3f);
        }

        private void UpdateNitroState()
        {
            // 如果氮气激活且有氮气可用
            if (isNitroActive && currentNitroAmount > 0)
            {
                // 消耗氮气
                currentNitroAmount -= nitroConsumptionRate * Time.deltaTime;
                currentNitroAmount = Mathf.Max(0f, currentNitroAmount);

                // 如果氮气耗尽，自动关闭
                if (currentNitroAmount <= 0)
                {
                    isNitroActive = false;
                }
            }
            // 如果氮气未激活，恢复氮气
            else if (!isNitroActive && currentNitroAmount < nitroCapacity)
            {
                // 恢复氮气
                currentNitroAmount += nitroRecoveryRate * Time.deltaTime;
                currentNitroAmount = Mathf.Min(nitroCapacity, currentNitroAmount);
            }
        }
        #endregion

        #region 驱动控制
        /// <summary>
        /// 应用驱动力和制动力
        /// </summary>
        private void ApplyDrive()
        {
            if (vehiclePhysics == null || vehicleRigidbody == null) return;

            // 获取当前速度和归一化速度
            currentSpeed = vehicleRigidbody.linearVelocity.magnitude * 3.6f; // m/s to km/h
            normalizedSpeed = Mathf.Clamp01(currentSpeed / maxSpeed);
            float reverseNormalizedSpeed = Mathf.Clamp01(currentSpeed / maxReverseSpeed);

            // 判断车辆是否在倒车
            float dotProduct = Vector3.Dot(transform.forward, vehicleRigidbody.linearVelocity.normalized);
            bool isReversing = dotProduct < -0.1f && throttleInput <= 0f; // 在后退且没有踩油门
            bool isMovingForward = dotProduct > 0.1f;

            // 计算引擎扭矩
            float currentMaxTorque = acceleration * 50f; // 基础扭矩，50是一个经验系数
            float rpmBasedTorqueMultiplier = engineTorqueCurve.Evaluate(normalizedSpeed);
            motorTorque = throttleInput * currentMaxTorque * rpmBasedTorqueMultiplier;

            // 氮气加速
            if (isNitroActive)
            {
                motorTorque *= nitroBoostFactor;
            }

            // 限制倒车扭矩
            if (throttleInput < 0f)
            {
                motorTorque *= reverseSpeedFactor;
            }

            // 根据驱动类型分配扭矩
            float frontMotorTorque = motorTorque * frontWheelDriveFactor;
            float rearMotorTorque = motorTorque * rearWheelDriveFactor;

            // 应用电机扭矩
            // 只有当油门输入和车辆移动方向一致，或车辆静止时才施加扭矩
            if ((throttleInput > 0 && !isReversing) || (throttleInput < 0 && !isMovingForward) || currentSpeed < 1.0f)
            {
                vehiclePhysics.ApplyMotorTorque(frontMotorTorque, rearMotorTorque);
            }
            else
            {
                // 如果油门和移动方向相反，则不施加驱动扭矩（允许滑行或刹车）
                vehiclePhysics.ApplyMotorTorque(0f, 0f);
            }

            // 处理漂移和手刹逻辑
            HandleDrifting();

            // 处理普通刹车逻辑 (仅在手刹未激活时应用)
            if (!isHandbrakeActive)
            {
                ApplyBraking(isReversing);
            }
            else
            {
                // 如果手刹激活，则不应用普通刹车
                // 手刹的制动力在 HandleDrifting() 中已经通过 SetBrakeTorque 应用了
                // 为确保之前的普通刹车扭矩被清除，可以再次调用SetBrakeTorque(0, 0)，但这取决于HandleDrifting的具体实现。
                // 目前HandleDrifting会调用SetBrakeTorque(0, handbrakeTorque)，已经覆盖了之前的刹车值。
            }
        }

        /// <summary>
        /// 处理漂移逻辑
        /// </summary>
        private void HandleDrifting()
        {
            if (!enableDrifting || vehiclePhysics == null) return;

            // 检查是否满足漂移条件：激活手刹、不在空中、达到最小漂移速度
            bool canDrift = isHandbrakeActive && !isInAir && currentSpeed >= minDriftSpeed;

            if (canDrift && !isDrifting)
            {
                // 开始漂移
                StartDrift();
            }
            else if (!canDrift && isDrifting)
            {
                // 停止漂移
                StopDrift();
            }

            // 如果正在漂移或手刹激活（即使速度不够漂移），应用手刹制动力
            if (isHandbrakeActive)
            {
                // 手刹只作用于后轮
                vehiclePhysics.SetBrakeTorque(0f, handbrakeTorque);
            }
            else if (!isDrifting)
            {
                 // 如果手刹未激活且未在漂移恢复中，确保手刹制动力为0
                 // （普通刹车逻辑在 ApplyBraking 中处理）
                 // 注意：这里不再需要设置刹车为0，因为 ApplyBraking 会覆盖
                 // 如果 ApplyBraking 因为 brakeInput 为0 而不设置刹车，
                 // 则需要确保手刹释放后刹车扭矩被清零。
                 // 为安全起见，可以在 ApplyBraking 中处理 brakeInput 为 0 的情况。
            }
        }

        /// <summary>
        /// 开始漂移
        /// </summary>
        private void StartDrift()
        {
            isDrifting = true;
            vehiclePhysics.SetRearWheelStiffness(driftFactor);

            // 停止可能正在运行的恢复协程
            if (driftRecoveryCoroutine != null)
            {
                StopCoroutine(driftRecoveryCoroutine);
                driftRecoveryCoroutine = null;
            }
            // 应用手刹制动力 (已移至 HandleDrifting 的调用处)
            // vehiclePhysics.SetBrakeTorque(0f, handbrakeTorque, true);
        }

        /// <summary>
        /// 停止漂移并开始恢复
        /// </summary>
        private void StopDrift()
        {
            isDrifting = false;
            // 启动恢复协程
            if (driftRecoveryCoroutine == null)
            {
                driftRecoveryCoroutine = StartCoroutine(RecoverWheelStiffness());
            }
            // 移除手刹制动力 (相关逻辑现在由 ApplyBraking 处理)
            // vehiclePhysics.SetBrakeTorque(0f, 0f, false);
        }

        /// <summary>
        /// 应用普通制动力（S键或倒车时的自动制动）
        /// </summary>
        /// <param name="_isReversing">车辆是否正在倒车</param>
        private void ApplyBraking(bool _isReversing)
        {
            float baseBrakeForce = 0f;

            // 情况1: 按下刹车键 (S)
            if (brakeInput > 0.01f)
            {
                // 基于速度动态调整基础制动力
                float speedFactor = Mathf.Clamp01(currentSpeed / maxSpeed);
                baseBrakeForce = brakeInput * brakeForce * Mathf.Lerp(0.4f, 1.0f, speedFactor);
            }
            // 情况2: 倒车时的自动制动
            else if (_isReversing && throttleInput >= 0f)
            {
                // 倒车时使用较小的制动力
                baseBrakeForce = brakeForce * 0.2f;
            }

            // 计算最终制动力
            float finalBrakeForce = 0f;
            if (baseBrakeForce > 0.01f)
            {
                // 基于当前速度调整制动力
                float currentSpeedAbs = Mathf.Abs(currentSpeed);
                float highSpeedThreshold = maxSpeed * 0.7f;  // 高速阈值提高到70%
                float lowSpeedThreshold = maxSpeed * 0.15f;  // 低速阈值提高到15%

                // 计算速度系数
                float speedFactor;
                if (currentSpeedAbs > highSpeedThreshold)
                {
                    // 高速区域：保持较强制动力
                    speedFactor = Mathf.Lerp(1.0f, 0.8f, (currentSpeedAbs - highSpeedThreshold) / (maxSpeed - highSpeedThreshold));
                }
                else if (currentSpeedAbs > lowSpeedThreshold)
                {
                    // 中速区域：完全制动力
                    speedFactor = 1.0f;
                }
                else
                {
                    // 低速区域：渐进减弱制动力，实现平滑停车
                    speedFactor = Mathf.Lerp(0.3f, 1.0f, currentSpeedAbs / lowSpeedThreshold);
                }

                finalBrakeForce = baseBrakeForce * speedFactor;

                // 在坡道上时调整制动力
                if (vehiclePhysics != null && vehiclePhysics.IsOnSlope())
                {
                    float slopeAngle = vehiclePhysics.GetSlopeAngle();
                    float slopeFactor = Mathf.Lerp(0.7f, 1.2f, slopeAngle / 45f);
                    finalBrakeForce *= slopeFactor;
                }
            }

            // 在空中时减少制动力
            if (isInAir)
            {
                finalBrakeForce *= 0.2f;
            }

            // 应用制动力到车轮
            vehiclePhysics.SetBrakeTorque(finalBrakeForce, finalBrakeForce);

            // 在低速时增加阻力以帮助车辆平稳停止
            if (currentSpeed < 5f && brakeInput > 0.5f)
            {
                if (vehicleRigidbody != null)
                {
                    float stopDrag = Mathf.Lerp(0.1f, 2f, (5f - currentSpeed) / 5f);
                    vehicleRigidbody.linearDamping = stopDrag;
                }
            }
            else if (vehicleRigidbody != null)
            {
                vehicleRigidbody.linearDamping = 0.1f;
            }
        }

        /// <summary>
        /// 恢复车轮刚度的协程
        /// </summary>
        private IEnumerator RecoverWheelStiffness()
        {
            if (vehiclePhysics == null)
            {
                driftRecoveryCoroutine = null;
                yield break;
            }

            Debug.Log("【车辆驱动】开始恢复车轮刚度...");

            float elapsedTime = 0f;
            // 获取当前漂移时的刚度因子 (driftFactor) 和目标刚度因子 (1.0f)
            float startFactor = driftFactor; // 或者更精确地获取当前实际应用的因子，但driftFactor应该足够
            float endFactor = 1.0f;

            while (elapsedTime < driftRecoveryTime)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / driftRecoveryTime);
                // 使用平滑插值 (例如 SmoothStep)
                t = t * t * (3f - 2f * t);

                float currentFactor = Mathf.Lerp(startFactor, endFactor, t);
                vehiclePhysics.SetRearWheelStiffness(currentFactor);

                yield return null;
            }

            // 确保最终恢复到精确的原始值
            vehiclePhysics.RestoreOriginalRearWheelStiffness(); 
            // 或者 vehiclePhysics.SetRearWheelStiffness(1.0f); 也可以，取决于 Restore 方法的实现是否更可靠

            Debug.Log("【车辆驱动】车轮刚度恢复完成。");
            driftRecoveryCoroutine = null;
        }
        #endregion

        /// <summary>
        /// 应用转向 - 优化版本，提供更平滑的转向体验
        /// 避免轻微转向就导致车辆甩尾
        /// </summary>
        private void ApplySteering()
        {
            if (vehiclePhysics == null) return;

            // 应用死区，忽略极小的转向输入，避免意外漂移
            float adjustedSteeringInput = steeringInput;
            float deadzone = 0.05f; // 5%的死区
            if (Mathf.Abs(steeringInput) < deadzone)
            {
                adjustedSteeringInput = 0f;
            }
            else
            {
                // 重新映射输入范围，保持平滑过渡
                float sign = Mathf.Sign(steeringInput);
                adjustedSteeringInput = sign * (Mathf.Abs(steeringInput) - deadzone) / (1f - deadzone);
            }

            // 根据速度调整转向
            float currentSpeedKmh = currentSpeed * (1f / KMH_TO_MS); // 转换为 km/h 以便理解
            
            // 简化：高速时线性降低最大转向角度
            // 例如：速度 > 60km/h 时开始降低，到 150km/h 时降低到最大值的 40%
            float highSpeedSteeringLimitStart = 60f;
            float highSpeedSteeringLimitEnd = 150f;
            float minSteeringFactor = 0.4f; // 高速时最小转向系数

            float speedBasedSteeringFactor = 1.0f;
            if (currentSpeedKmh > highSpeedSteeringLimitStart)
            {
                speedBasedSteeringFactor = Mathf.Lerp(1.0f, minSteeringFactor, 
                    Mathf.Clamp01((currentSpeedKmh - highSpeedSteeringLimitStart) / (highSpeedSteeringLimitEnd - highSpeedSteeringLimitStart)));
            }

            // 计算最终转向角度
            float targetSteeringAngle = maxSteeringAngle * adjustedSteeringInput * speedBasedSteeringFactor;

            // 平滑转向输入，防止突然的转向造成失控
            float deltaTime = Time.fixedDeltaTime;
            float currentSteerAngle = 0f;
            if (vehiclePhysics.GetFrontLeftWheel() != null)
            {
                currentSteerAngle = vehiclePhysics.GetFrontLeftWheel().steerAngle;
            }

            // 转向变化速率基于速度和转向幅度进行调整
            // 1. 高速时转向更缓慢
            // 2. 大幅度转向时响应更缓慢，避免突然甩尾
            float steerSpeedBase = Mathf.Lerp(steeringSpeed, steeringSpeed * 0.6f, normalizedSpeed);

            // 根据转向幅度调整响应速度 - 大幅度转向时响应更缓慢
            float turnMagnitude = Mathf.Abs(targetSteeringAngle - currentSteerAngle) / maxSteeringAngle;
            float steerSpeed = steerSpeedBase * (1f - turnMagnitude * 0.3f);

            // 使用更平滑的插值
            float smoothFactor = deltaTime * steerSpeed;
            float smoothedSteeringAngle = Mathf.Lerp(currentSteerAngle, targetSteeringAngle, smoothFactor);

            // 应用转向
            vehiclePhysics.SetSteeringAngle(smoothedSteeringAngle, smoothedSteeringAngle);
        }

        /// <summary>
        /// 限制车辆速度 - 现在只用于确保低阻尼，不再施加限制力
        /// </summary>
        private void LimitSpeed()
        {
            // 确保线性阻尼保持在预期的低值，以依赖自定义空气阻力
            if (vehicleRigidbody != null && vehicleRigidbody.linearDamping > 0.001f) // 只在阻尼过高时设置
            {
                vehicleRigidbody.linearDamping = 0.0005f;
            }
            
            // 移除所有强制速度限制逻辑
            /*
            // --- 以下是旧的强制限速逻辑 --- 
            if (vehicleRigidbody == null) return;

            // 获取当前速度 (使用速度向量的模长)
            float currentMpsSpeed = vehicleRigidbody.linearVelocity.magnitude;
            float currentKmhSpeed = currentMpsSpeed * (1f / KMH_TO_MS); // 转换为 km/h

            // --- 限制前进速度 --- 
            float effectiveMaxSpeed = maxSpeed; // km/h
            bool limitForwardSpeed = true; // 标记是否需要限制前进速度

            // 如果氮气激活且有氮气可用，提高有效最大速度
            if (isNitroActive && currentNitroAmount > 0)
            {
                effectiveMaxSpeed = maxSpeed * nitroBoostFactor;
                // 如果氮气量较少，逐渐降低最大速度增强效果
                if (currentNitroAmount < nitroCapacity * 0.3f)
                {
                    // 氮气量低于30%时，逐渐降低最大速度增强
                    float nitroFactor = currentNitroAmount / (nitroCapacity * 0.3f);
                    // 平滑过渡回普通最大速度
                    effectiveMaxSpeed = Mathf.Lerp(maxSpeed, effectiveMaxSpeed, nitroFactor);
                }
            }

            // 检查是否在倒车，如果在倒车，则不限制前进速度
            float forwardDot = Vector3.Dot(vehicleRigidbody.linearVelocity.normalized, transform.forward);
            if (forwardDot < -0.1f && currentKmhSpeed > 1.0f) // 速度大于1才算有效倒车
            {
                 limitForwardSpeed = false; // 正在倒车，不应用前进速度限制
            }

            // 限制前进速度逻辑
            if (limitForwardSpeed && currentKmhSpeed > effectiveMaxSpeed)
            {
                // 计算超出有效最大速度的程度
                float overSpeedRatio = (currentKmhSpeed - effectiveMaxSpeed) / effectiveMaxSpeed;

                // 计算需要施加的减速力 - 稍微加强基础力度 (40f -> 50f)
                float brakingForce = vehicleRigidbody.mass * overSpeedRatio * overSpeedRatio * 50f; 
                // 稍微加强最大限制 (15f -> 18f)
                brakingForce = Mathf.Min(brakingForce, vehicleRigidbody.mass * 18f); 

                // 如果使用氮气，减小制动力，使加速感更强
                if (isNitroActive && currentNitroAmount > 0)
                {
                    // 氮气激活时减小制动力，使车辆能够更快地加速到氮气增强的最大速度
                    brakingForce *= 0.6f;
                }

                // 在坡道上时减小制动力，让重力影响车辆速度
                if (vehiclePhysics != null && vehiclePhysics.IsOnSlope())
                {
                    // 在坡道上减小制动力，让车辆能够自然加速/减速
                    brakingForce *= 0.4f;
                }

                // 应用与车辆当前速度方向相反的力
                Vector3 brakingDirection = -vehicleRigidbody.linearVelocity.normalized;
                // 确保速度方向有效
                if (brakingDirection != Vector3.zero)
                {
                    vehicleRigidbody.AddForce(brakingDirection * brakingForce, ForceMode.Force);
                }
                
                // 如果超速过多，增加临时阻力以帮助减速，但不要过度
                if (overSpeedRatio > 0.1f)
                {
                    // 临时增加线性阻力，但保持平滑过渡
                    // 使用较小的最大阻力值，避免车辆突然停止
                    float tempDrag = Mathf.Lerp(0.01f, 0.3f, overSpeedRatio * 0.8f);

                    // 在坡道上减小阻力，让车辆保持惯性
                    if (vehiclePhysics != null && vehiclePhysics.IsOnSlope())
                    {
                        tempDrag *= 0.5f;
                    }

                    // 设置临时阻力，但不要覆盖现有阻力
                    vehicleRigidbody.linearDamping = Mathf.Max(vehicleRigidbody.linearDamping, tempDrag);
                }
            }
            else if (!limitForwardSpeed) // 如果是因为倒车而没限制前进速度，也要恢复阻力
            {
                 vehicleRigidbody.linearDamping = 0.0005f; 
            }
            else // 速度正常时恢复低阻力
            {
                vehicleRigidbody.linearDamping = 0.0005f;
            }

            // --- 限制倒车速度 --- 
            // forwardDot 已经在前面计算过
            if (forwardDot < -0.1f && currentKmhSpeed > maxReverseSpeed) // 检查是否在倒车且超速
            {
                // 计算超出最大倒车速度的程度
                float overSpeedRatio = (currentKmhSpeed - maxReverseSpeed) / maxReverseSpeed;
                
                // 计算需要施加的减速力 (施加到前进方向以减慢倒车) - 稍微加强基础力度 (30f -> 40f)
                float brakingForce = vehicleRigidbody.mass * overSpeedRatio * overSpeedRatio * 40f; 
                // 稍微加强最大限制 (10f -> 12f)
                brakingForce = Mathf.Min(brakingForce, vehicleRigidbody.mass * 12f); 

                // 在坡道上时减小制动力，让重力影响车辆速度
                if (vehiclePhysics != null && vehiclePhysics.IsOnSlope())
                {
                    // 在坡道上减小制动力，让车辆能够自然加速/减速
                    brakingForce *= 0.3f;
                }

                // 应用与车辆倒车方向相反的力（即车辆前进方向）
                Vector3 brakingDirection = transform.forward; 
                vehicleRigidbody.AddForce(brakingDirection * brakingForce, ForceMode.Force);

                // 如果超速过多，增加临时阻力
                if (overSpeedRatio > 0.2f)
                {
                    float tempDrag = Mathf.Lerp(0.005f, 0.2f, overSpeedRatio * 0.7f);

                    // 在坡道上减小阻力，让车辆保持惯性
                    if (vehiclePhysics != null && vehiclePhysics.IsOnSlope())
                    {
                        tempDrag *= 0.4f;
                    }

                    vehicleRigidbody.linearDamping = Mathf.Max(vehicleRigidbody.linearDamping, tempDrag);
                }
            }
            */
        }

        #region 公共接口
        /// 设置油门输入值
        public void SetThrottleInput(float value)
        {
            throttleInput = Mathf.Clamp01(value);
        }

        /// 设置制动输入值
        public void SetBrakeInput(float value)
        {
            brakeInput = Mathf.Clamp01(value);
        }

        /// 设置转向输入值
        public void SetSteeringInput(float value)
        {
            steeringInput = Mathf.Clamp(value, -1f, 1f);
        }

        /// 设置手刹状态
        public void SetHandbrakeActive(bool active)
        {
            isHandbrakeActive = active;
        }

        /// 设置氮气状态
        public void SetNitroActive(bool active)
        {
            isNitroActive = active;
        }

        /// 填充氮气
        public void RefillNitro(float amount)
        {
            currentNitroAmount = Mathf.Min(currentNitroAmount + amount, nitroCapacity);
        }

        /// 获取当前速度（km/h）
        public float GetCurrentSpeed()
        {
            return currentSpeed;
        }

        /// 获取油门输入值
        public float GetThrottleInput()
        {
            return throttleInput;
        }

        /// 获取制动输入值
        public float GetBrakeInput()
        {
            return brakeInput;
        }

        /// 获取氮气状态（是否激活）
        public bool IsNitroActive()
        {
            return isNitroActive;
        }

        /// 获取氮气量（0-1）
        public float GetNitroAmount()
        {
            return currentNitroAmount / nitroCapacity;
        }

        /// 获取发动机转速
        public float GetEngineRPM()
        {
            return currentRPM;
        }

        /// 获取最大速度
        public float GetMaxSpeed()
        {
            return maxSpeed;
        }

        /// 获取转向输入值
        public float GetSteeringInput()
        {
            return steeringInput;
        }

        /// 获取手刹状态
        public bool IsHandbrakeActive()
        {
            return isHandbrakeActive;
        }

        /// 获取是否正在漂移
        public bool IsDrifting()
        {
            return isDrifting;
        }

        /// 获取当前漂移强度
        public float GetDriftFactor()
        {
            return currentDriftFactor;
        }

        /// 获取是否在空中
        public bool IsInAir()
        {
            return isInAir;
        }

        /// 获取是否侧翻
        public bool IsFlipped()
        {
            return isFlipped;
        }

        /// 获取是否倒置
        public bool IsUpsideDown()
        {
            return isUpsideDown;
        }

        /// 设置车辆状态
        public void SetVehicleState(bool inAir, bool flipped, bool upsideDown)
        {
            isInAir = inAir;
            isFlipped = flipped;
            isUpsideDown = upsideDown;
        }

        /// 设置漂移状态
        public void SetDriftState(bool drifting, float driftFactor)
        {
            isDrifting = drifting;
            currentDriftFactor = driftFactor;
        }

        /// 重置车辆
        public void ResetVehicle()
        {
            // 重置物理状态
            if (vehiclePhysics != null)
            {
                vehiclePhysics.ResetPhysics();
            }

            // 重置位置和旋转
            transform.position = new Vector3(transform.position.x, transform.position.y + 1.0f, transform.position.z);
            transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
        }

        /// 在传送前调用
        public void PrepareForTeleport()
        {
            // 触发传送前事件
            OnBeforeTeleport?.Invoke();

            // 重置车辆状态
            ResetVehicleState();
        }

        /// 在传送后调用
        public void FinishTeleport()
        {
            // 重置车轮状态
            if (vehiclePhysics != null)
            {
                vehiclePhysics.ResetPhysics();
            }

            // 触发传送后事件
            OnAfterTeleport?.Invoke();
        }

        /// 重置车辆状态
        private void ResetVehicleState()
        {
            // 重置物理状态
            if (vehicleRigidbody != null)
            {
                vehicleRigidbody.linearVelocity = Vector3.zero;
                vehicleRigidbody.angularVelocity = Vector3.zero;
            }
        }

        /// <summary>
        /// 设置车辆输入 - 统一处理所有输入
        /// 作为唯一的输入处理接口，简化架构
        /// </summary>
        /// <param name="throttleInput">油门输入值</param>
        /// <param name="brakeInput">制动输入值</param>
        /// <param name="steeringInput">转向输入值</param>
        /// <param name="handbrakeInput">手刹输入状态</param>
        /// <param name="nitroInput">氮气输入状态</param>
        /// <param name="isDriftingRequested">是否请求漂移</param>
        public void SetInput(float throttleInput, float brakeInput, float steeringInput, bool handbrakeInput, bool nitroInput, bool isDriftingRequested)
        {
            // 更新输入值
            this.throttleInput = throttleInput;
            this.brakeInput = brakeInput;
            this.steeringInput = steeringInput;
            this.isHandbrakeActive = handbrakeInput; // 保存原始手刹状态
            this.isNitroActive = nitroInput && currentNitroAmount > 0;
            this.isDriftingRequested = isDriftingRequested; // 保存漂移请求状态

            // Debug log for input states
            // Debug.Log($"Input - Throttle: {throttleInput:F2}, Brake: {brakeInput:F2}, Steering: {steeringInput:F2}, Handbrake: {handbrakeInput}, Nitro: {nitroInput}, DriftReq: {isDriftingRequested}");
        }

        /// <summary>
        /// 计算油门保留系数 - 基于车速动态调整 (已固定为原后驱逻辑)
        /// </summary>
        private float CalculateThrottleRetentionFactor(float speed) // Removed DriveType parameter
        {
            // 基础油门保留系数 - 基于车速
            float speedBasedFactor;
            if (speed < 30f) {
                // 低速：较高的油门保留，帮助启动漂移
                speedBasedFactor = Mathf.Lerp(0.8f, 0.7f, speed / 30f);
            } else if (speed < 80f) {
                // 中速：适中的油门保留，平衡漂移和加速
                speedBasedFactor = Mathf.Lerp(0.7f, 0.5f, (speed - 30f) / 50f);
            } else {
                // 高速：较低的油门保留，防止过度加速导致失控
                speedBasedFactor = Mathf.Lerp(0.5f, 0.3f, Mathf.Min(1f, (speed - 80f) / 60f));
            }

            // 始终使用原后轮驱动的逻辑
            return speedBasedFactor; // Removed switch statement
        }
        #endregion
    }
}