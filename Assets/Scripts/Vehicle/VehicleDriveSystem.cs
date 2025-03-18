using UnityEngine;

namespace Vehicle
{
    /// <summary>
    /// 车辆驱动系统
    /// 负责处理车辆的驱动力、制动力和转向
    /// </summary>
    public class VehicleDriveSystem : MonoBehaviour
    {
        /// <summary>
        /// 车辆驱动类型枚举
        /// </summary>
        public enum DriveType
        {
            FrontWheelDrive,  // 前轮驱动
            RearWheelDrive,   // 后轮驱动
            AllWheelDrive     // 四轮驱动
        }

        [Header("驱动系统设置")]
        [Tooltip("驱动类型")]
        [SerializeField] private DriveType driveType = DriveType.FrontWheelDrive;

        [Tooltip("前轮驱动力分配 (0-1)")]
        [Range(0, 1)]
        [SerializeField] private float frontWheelDriveFactor = 1.0f;

        [Tooltip("后轮驱动力分配 (0-1)")]
        [Range(0, 1)]
        [SerializeField] private float rearWheelDriveFactor = 0.0f;

        [Tooltip("是否启用Ackerman转向")]
        [SerializeField] private bool useAckermanSteering = true;

        [Tooltip("Ackerman转向系数 (0-1)")]
        [Range(0, 1)]
        [SerializeField] private float ackermanCoefficient = 0.08f;

        [Header("性能参数")]
        [Tooltip("最大前进速度 (km/h)")]
        [SerializeField] private float maxSpeed = 100.0f;

        [Tooltip("最大后退速度 (km/h)")]
        [SerializeField] private float maxReverseSpeed = 30.0f;

        [Tooltip("加速度")]
        [SerializeField] private float acceleration = 10.0f;

        [Tooltip("制动力")]
        [SerializeField] private float brakeForce = 15.0f;

        [Tooltip("转向速度")]
        [SerializeField] private float steeringSpeed = 70.0f;

        [Tooltip("最大转向角度")]
        [SerializeField] private float maxSteeringAngle = 40.0f;

        [Tooltip("引擎扭矩曲线")]
        [SerializeField]
        private AnimationCurve engineTorqueCurve = new AnimationCurve(
            new Keyframe(0f, 1.0f),    // 静止时100%扭矩，而不是80%
            new Keyframe(0.2f, 1.2f),  // 低速时120%扭矩，增强起步体验
            new Keyframe(0.5f, 1.0f),  // 中速时100%扭矩
            new Keyframe(0.8f, 0.8f),  // 高速时80%扭矩
            new Keyframe(1.0f, 0.7f)   // 最高速时70%扭矩
        );

        [Header("氮气系统")]
        [Tooltip("氮气容量")]
        [SerializeField] private float nitroCapacity = 100f;

        [Tooltip("氮气恢复速度")]
        [SerializeField] private float nitroRecoveryRate = 8f;

        [Tooltip("氮气消耗速度")]
        [SerializeField] private float nitroConsumptionRate = 20f;

        [Tooltip("氮气加速提升系数")]
        [SerializeField] private float nitroBoostFactor = 1.3f;

        [Tooltip("氮气加速平滑度")]
        [SerializeField] private float nitroSmoothness = 0.5f;

        [Header("惯性设置")]
        [Tooltip("发动机制动力")]
        [SerializeField] private float engineBrakingForce = 5.0f;

        [Tooltip("滑行阻力")]
        [SerializeField] private float coastingDrag = 2.0f;

        [Tooltip("最小速度阈值")]
        [SerializeField] private float minSpeedThreshold = 1.0f;

        // 引用其他组件
        private VehiclePhysics vehiclePhysics;
        private Rigidbody vehicleRigidbody;

        // 驱动状态
        private float throttleInput = 0f;
        private float brakeInput = 0f;
        private float steeringInput = 0f;
        private bool isHandbrakeActive = false;
        private bool isNitroActive = false;
        private float currentSteeringAngle = 0f;
        private float currentSpeed = 0f;
        private float normalizedSpeed = 0f; // 0-1之间的速度因子
        private float currentNitroAmount;

        // 将km/h转换为m/s的系数
        private const float KMH_TO_MS = 0.2778f;

        /// <summary>
        /// 初始化组件
        /// </summary>
        private void Awake()
        {
            // 获取组件引用
            vehiclePhysics = GetComponent<VehiclePhysics>();
            vehicleRigidbody = GetComponent<Rigidbody>();

            if (vehiclePhysics == null)
            {
                Debug.LogError("未找到VehiclePhysics组件！");
            }

            // 根据驱动类型设置驱动力分配
            SetupDriveTypeFactors();

            // 初始化氮气量
            currentNitroAmount = nitroCapacity;
        }

        /// <summary>
        /// 根据驱动类型设置驱动力分配
        /// </summary>
        private void SetupDriveTypeFactors()
        {
            switch (driveType)
            {
                case DriveType.FrontWheelDrive:
                    frontWheelDriveFactor = 1.0f;
                    rearWheelDriveFactor = 0.0f;
                    break;
                case DriveType.RearWheelDrive:
                    frontWheelDriveFactor = 0.0f;
                    rearWheelDriveFactor = 1.0f;
                    break;
                case DriveType.AllWheelDrive:
                    frontWheelDriveFactor = 0.5f;
                    rearWheelDriveFactor = 0.5f;
                    break;
            }
        }

        /// <summary>
        /// 更新车辆状态
        /// </summary>
        private void Update()
        {
            // 更新当前速度 (km/h)
            if (vehicleRigidbody != null)
            {
                currentSpeed = vehicleRigidbody.linearVelocity.magnitude / KMH_TO_MS;
            }
        }

        /// <summary>
        /// 物理更新
        /// </summary>
        private void FixedUpdate()
        {
            // 应用驱动力
            ApplyDrive();

            // 应用转向
            ApplySteering();

            // 限制最大速度
            LimitMaxSpeed();
        }

        /// <summary>
        /// 应用驱动力和制动力到车轮
        /// </summary>
        private void ApplyDrive()
        {
            if (vehiclePhysics == null) return;

            // 获取本地空间中的速度
            Vector3 localVelocity = transform.InverseTransformDirection(vehicleRigidbody.linearVelocity);
            float forwardSpeed = localVelocity.z;

            // 计算标准化速度 (0-1)
            normalizedSpeed = Mathf.Clamp01(currentSpeed / maxSpeed);

            // 手刹逻辑
            if (isHandbrakeActive)
            {
                float handBrakeTorque = 10000f;
                vehiclePhysics.ApplyBrakeTorque(0f, handBrakeTorque);
                return;
            }

            // 处理油门输入 (W键)
            if (throttleInput > 0.1f)
            {
                // 如果车辆正在向后移动，先刹车
                if (forwardSpeed < -0.5f)
                {
                    float brakeTorque = brakeForce * 200;
                    vehiclePhysics.ApplyBrakeTorque(brakeTorque, brakeTorque);
                    vehiclePhysics.ApplyMotorTorque(0f, 0f);
                }
                // 否则加速前进
                else
                {
                    // 改进的引擎扭矩曲线，提高低速扭矩
                    float torqueMultiplier = engineTorqueCurve.Evaluate(normalizedSpeed);

                    // 计算基础扭矩
                    float motorTorque = acceleration * 120 * throttleInput * torqueMultiplier;

                    // 更新氮气状态
                    UpdateNitroState();

                    // 如果氮气激活，平滑增加扭矩
                    if (isNitroActive && currentNitroAmount > 0)
                    {
                        // 使用平滑过渡的氮气加速
                        float currentBoostFactor = 1.0f;
                        if (normalizedSpeed < 0.8f) // 只在80%最大速度以下时提供全额氮气加速
                        {
                            currentBoostFactor = Mathf.Lerp(1.0f, nitroBoostFactor,
                                (1f - normalizedSpeed) * (currentNitroAmount / nitroCapacity));
                        }
                        else // 高速时降低氮气效果，防止失控
                        {
                            currentBoostFactor = Mathf.Lerp(1.0f, 1.1f,
                                (currentNitroAmount / nitroCapacity));
                        }

                        // 平滑应用氮气加速
                        motorTorque *= Mathf.Lerp(1.0f, currentBoostFactor, nitroSmoothness);

                        // 消耗氮气
                        float consumptionRate = nitroConsumptionRate * (1f + normalizedSpeed * 0.5f);
                        currentNitroAmount = Mathf.Max(0, currentNitroAmount - consumptionRate * Time.fixedDeltaTime);
                    }
                    else
                    {
                        // 恢复氮气
                        float recoveryMultiplier = (1f - normalizedSpeed * 0.5f); // 高速时降低恢复速度
                        currentNitroAmount = Mathf.Min(nitroCapacity,
                            currentNitroAmount + nitroRecoveryRate * recoveryMultiplier * Time.fixedDeltaTime);
                    }

                    // 应用驱动力，确保在高速时不会产生过大的力
                    float speedLimitFactor = Mathf.Lerp(1.0f, 0.7f, normalizedSpeed * normalizedSpeed);
                    motorTorque *= speedLimitFactor;

                    vehiclePhysics.ApplyMotorTorque(
                        motorTorque * frontWheelDriveFactor,
                        motorTorque * rearWheelDriveFactor
                    );
                    vehiclePhysics.ApplyBrakeTorque(0f, 0f);
                }
            }
            // 处理刹车/倒车输入 (S键)
            else if (brakeInput > 0.1f)
            {
                // 如果车辆正在向前移动，先刹车
                if (forwardSpeed > 0.5f)
                {
                    float brakeTorque = brakeForce * 200;
                    vehiclePhysics.ApplyBrakeTorque(brakeTorque, brakeTorque);
                    vehiclePhysics.ApplyMotorTorque(0f, 0f);
                }
                // 如果车辆几乎停止或向后移动，则倒车
                else
                {
                    // 应用倒车驱动力
                    float motorTorque = -acceleration * 100 * brakeInput;

                    // 应用驱动力
                    vehiclePhysics.ApplyMotorTorque(
                        motorTorque * frontWheelDriveFactor,
                        motorTorque * rearWheelDriveFactor
                    );
                    vehiclePhysics.ApplyBrakeTorque(0f, 0f);
                }
            }
            // 没有输入时（滑行状态）
            else
            {
                // 计算滑行阻力（基于速度的非线性阻力）
                float speedFactor = Mathf.Clamp01(currentSpeed / 60.0f);

                // 极低速时（几乎停止）
                if (Mathf.Abs(forwardSpeed) < 0.5f)
                {
                    // 应用极小的制动力防止溜车
                    float stopBrakeTorque = 0.5f; // 显著降低停车制动力
                    vehiclePhysics.ApplyBrakeTorque(stopBrakeTorque, stopBrakeTorque);
                    vehiclePhysics.ApplyMotorTorque(0f, 0f);
                }
                // 正常滑行
                else
                {
                    // 使用非线性曲线计算阻力，使其更接近真实物理
                    float dragFactor = speedFactor * speedFactor; // 二次方曲线使高速阻力更明显
                    float naturalDrag = dragFactor * 1.5f; // 显著降低基础阻力

                    // 应用极小的制动力模拟自然阻力
                    vehiclePhysics.ApplyBrakeTorque(naturalDrag, naturalDrag);

                    // 在高速滑行时提供小的补偿扭矩，使减速更自然
                    if (currentSpeed > 30f && !vehiclePhysics.GetIsInAir())
                    {
                        float compensationFactor = Mathf.Lerp(0f, 0.15f, 1f - dragFactor);
                        float compensationTorque = acceleration * compensationFactor;

                        vehiclePhysics.ApplyMotorTorque(
                            compensationTorque * frontWheelDriveFactor,
                            compensationTorque * rearWheelDriveFactor
                        );
                    }
                    else
                    {
                        vehiclePhysics.ApplyMotorTorque(0f, 0f);
                    }
                }
            }
        }

        /// <summary>
        /// 应用转向到前轮
        /// </summary>
        private void ApplySteering()
        {
            if (vehiclePhysics == null) return;

            // 计算速度相关因子
            float speedFactor = Mathf.Clamp01(currentSpeed / 70.0f);

            // 低速时允许更大的转向角度，但仍然保持一定的控制
            float lowSpeedFactor = Mathf.Clamp01(currentSpeed / 10.0f);
            float steeringFactor = Mathf.Lerp(1.2f, 0.3f, speedFactor * speedFactor);

            // 计算目标转向角度
            float targetSteeringAngle = steeringInput * maxSteeringAngle * steeringFactor;

            // 根据速度调整转向响应速度
            float turnSpeed = Mathf.Lerp(steeringSpeed * 1.2f, steeringSpeed * 0.6f, speedFactor);

            // 低速时保持一定的转向能力，但避免原地打转
            if (Mathf.Abs(currentSpeed) < 2.0f)
            {
                // 降低低速转向的响应度，但不完全禁用
                turnSpeed *= 0.5f;
                targetSteeringAngle *= 0.7f;
            }

            // 平滑转向角度变化
            currentSteeringAngle = Mathf.Lerp(
                currentSteeringAngle,
                targetSteeringAngle,
                Time.fixedDeltaTime * turnSpeed
            );

            // Ackerman转向逻辑
            if (useAckermanSteering && Mathf.Abs(currentSteeringAngle) > 3.0f)
            {
                float innerWheelAngle = currentSteeringAngle;
                float outerWheelAngle = currentSteeringAngle * (1f - ackermanCoefficient * Mathf.Abs(currentSteeringAngle) / maxSteeringAngle);

                // 应用转向角度
                if (currentSteeringAngle > 0)
                {
                    vehiclePhysics.SetSteeringAngle(innerWheelAngle, outerWheelAngle);
                }
                else
                {
                    vehiclePhysics.SetSteeringAngle(outerWheelAngle, innerWheelAngle);
                }
            }
            else
            {
                vehiclePhysics.SetSteeringAngle(currentSteeringAngle, currentSteeringAngle);
            }
        }

        /// <summary>
        /// 限制车辆最大速度
        /// </summary>
        private void LimitMaxSpeed()
        {
            if (vehicleRigidbody == null) return;

            // 获取当前速度（m/s）
            float currentSpeedMS = vehicleRigidbody.linearVelocity.magnitude;

            // 将最大速度从km/h转换为m/s，增加一点最大速度容差
            float maxSpeedMS = maxSpeed * KMH_TO_MS * 1.05f; // 增加5%速度容差
            float maxReverseSpeedMS = maxReverseSpeed * KMH_TO_MS;

            // 检查是否超过最大速度
            if (currentSpeedMS > maxSpeedMS)
            {
                // 获取速度方向
                Vector3 velocityDirection = vehicleRigidbody.linearVelocity.normalized;

                // 使用更平滑的限速方式
                float targetSpeed = Mathf.Lerp(currentSpeedMS, maxSpeedMS, Time.fixedDeltaTime * 2.0f);
                vehicleRigidbody.linearVelocity = velocityDirection * targetSpeed;
            }

            // 检查是否超过最大后退速度
            if (currentSpeedMS > maxReverseSpeedMS)
            {
                // 获取本地空间中的速度
                Vector3 localVelocity = transform.InverseTransformDirection(vehicleRigidbody.linearVelocity);

                // 如果是后退
                if (localVelocity.z < -0.5f)
                {
                    // 获取速度方向
                    Vector3 velocityDirection = vehicleRigidbody.linearVelocity.normalized;

                    // 设置为最大允许后退速度
                    vehicleRigidbody.linearVelocity = velocityDirection * maxReverseSpeedMS;
                }
            }
        }

        /// <summary>
        /// 设置氮气激活状态
        /// </summary>
        private void UpdateNitroState()
        {
            // 确保氮气量足够
            if (isNitroActive && currentNitroAmount <= 0)
            {
                isNitroActive = false;
            }
        }

        #region 公共接口

        /// <summary>
        /// 设置油门输入
        /// </summary>
        public void SetThrottleInput(float input)
        {
            throttleInput = Mathf.Clamp01(input);
        }

        /// <summary>
        /// 设置刹车输入
        /// </summary>
        public void SetBrakeInput(float input)
        {
            brakeInput = Mathf.Clamp01(input);
        }

        /// <summary>
        /// 设置转向输入
        /// </summary>
        public void SetSteeringInput(float input)
        {
            steeringInput = Mathf.Clamp(input, -1f, 1f);
        }

        /// <summary>
        /// 设置手刹状态
        /// </summary>
        public void SetHandbrakeActive(bool active)
        {
            isHandbrakeActive = active;
        }

        /// <summary>
        /// 设置氮气状态
        /// </summary>
        public void SetNitroActive(bool active)
        {
            isNitroActive = active && currentNitroAmount > 0;
        }

        /// <summary>
        /// 获取当前车速
        /// </summary>
        public float GetCurrentSpeed()
        {
            return currentSpeed;
        }

        /// <summary>
        /// 获取最大车速
        /// </summary>
        public float GetMaxSpeed()
        {
            return maxSpeed;
        }

        /// <summary>
        /// 获取当前驱动类型
        /// </summary>
        public DriveType GetDriveType()
        {
            return driveType;
        }

        /// <summary>
        /// 获取手刹状态
        /// </summary>
        public bool IsHandbrakeActive()
        {
            return isHandbrakeActive;
        }

        /// <summary>
        /// 获取当前氮气量（0-1）
        /// </summary>
        public float GetNitroAmount()
        {
            return currentNitroAmount / nitroCapacity;
        }

        /// <summary>
        /// 获取氮气是否激活
        /// </summary>
        public bool IsNitroActive()
        {
            return isNitroActive;
        }

        /// <summary>
        /// 获取当前油门输入值
        /// </summary>
        public float GetThrottleInput()
        {
            return throttleInput;
        }

        /// <summary>
        /// 获取当前刹车输入值
        /// </summary>
        public float GetBrakeInput()
        {
            return brakeInput;
        }

        /// <summary>
        /// 获取转向输入
        /// </summary>
        public float GetSteeringInput()
        {
            return steeringInput;
        }

        #endregion
    }
}