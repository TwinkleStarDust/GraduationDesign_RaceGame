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
            // 没有输入时，逐渐减速而不是突然制动
            else
            {
                // 只在几乎停止时应用很小的制动力防止溜车
                if (Mathf.Abs(forwardSpeed) < 0.5f)
                {
                    float brakeTorque = 15f;  // 进一步降低停车时的制动力，从20f降到15f
                    vehiclePhysics.ApplyBrakeTorque(brakeTorque, brakeTorque);
                    vehiclePhysics.ApplyMotorTorque(0f, 0f);
                }
                // 高速滑行时应用极小的制动力模拟发动机阻力和空气阻力
                else
                {
                    // 最小化滑行时的制动力，让车辆保持速度更久
                    float speedFactor = Mathf.Clamp01(currentSpeed / 60.0f);
                    float brakeTorque = Mathf.Lerp(2f, 10f, speedFactor); // 进一步降低制动力
                    vehiclePhysics.ApplyBrakeTorque(brakeTorque, brakeTorque);

                    // 在高速时添加一个小的正向扭矩，抵消部分阻力
                    if (currentSpeed > 30f && vehiclePhysics != null && !vehiclePhysics.GetIsInAir())
                    {
                        float compensationTorque = Mathf.Lerp(0f, 30f, speedFactor - 0.5f);
                        if (compensationTorque > 0)
                        {
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

            // 确保在车辆几乎停止时不应用转向
            if (Mathf.Abs(currentSpeed) < 2.0f)
            {
                // 逐渐将转向角度归零
                currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, 0f, Time.fixedDeltaTime * steeringSpeed * 2.0f);

                // 应用转向角度到前轮
                vehiclePhysics.SetSteeringAngle(currentSteeringAngle, currentSteeringAngle);
                return;
            }

            // 改进的速度因子计算，使高速转向更加平滑
            float speedFactor = Mathf.Clamp01(currentSpeed / 70.0f); // 提高参考速度从50km/h到70km/h

            // 更加平滑的转向因子曲线，高速时转向更温和
            float steeringFactor = Mathf.Lerp(1.0f, 0.3f, speedFactor * speedFactor); // 使用平方函数让高速时转向更加柔和

            // 计算目标转向角度，添加平滑过渡
            float targetSteeringAngle = steeringInput * maxSteeringAngle * steeringFactor;

            // 高速时转向响应更缓慢，防止突然转向导致侧翻
            float turnSpeed = Mathf.Lerp(steeringSpeed, steeringSpeed * 0.6f, speedFactor);

            // 平滑转向，高速时响应更慢
            currentSteeringAngle = Mathf.Lerp(
                currentSteeringAngle,
                targetSteeringAngle,
                Time.fixedDeltaTime * turnSpeed
            );

            // 如果启用了Ackerman转向，则计算内外轮转向角度差异
            if (useAckermanSteering && Mathf.Abs(currentSteeringAngle) > 3.0f)
            {
                // 计算Ackerman效应下的内外轮转向角度
                float innerWheelAngle = currentSteeringAngle;

                // 调整外轮角度计算公式，使转向更平滑
                float outerWheelAngle = currentSteeringAngle * (1f - ackermanCoefficient * Mathf.Abs(currentSteeringAngle) / maxSteeringAngle);

                // 根据转向方向确定哪个是内轮，哪个是外轮
                if (currentSteeringAngle > 0) // 向右转
                {
                    vehiclePhysics.SetSteeringAngle(innerWheelAngle, outerWheelAngle);
                }
                else // 向左转
                {
                    vehiclePhysics.SetSteeringAngle(outerWheelAngle, innerWheelAngle);
                }
            }
            else
            {
                // 不使用Ackerman转向时，两轮角度相同
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