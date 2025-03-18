// 车辆驱动系统
// 处理车辆的驱动力、制动力、转向和氮气系统
// 简化版本，适合街机风格赛车游戏

using UnityEngine;
using System.Collections;

namespace Vehicle
{
    public class VehicleDriveSystem : MonoBehaviour
    {
        /// <summary>
        /// 驱动类型枚举
        /// </summary>
        public enum DriveType
        {
            FrontWheelDrive,  // 前轮驱动
            RearWheelDrive,   // 后轮驱动
            AllWheelDrive     // 四轮驱动
        }

        #region 公共变量
        [Header("驱动类型")]
        [Tooltip("驱动类型")]
        [SerializeField] private DriveType driveType = DriveType.RearWheelDrive;

        [Header("性能参数")]
        [Tooltip("最高速度 (km/h)")]
        [Range(50f, 400f)]
        [SerializeField] private float maxSpeed = 200f;

        [Tooltip("加速度")]
        [Range(10f, 100f)]
        [SerializeField] private float acceleration = 30f;

        [Tooltip("制动力")]
        [Range(10f, 300f)]
        [SerializeField] private float brakeForce = 200f;

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

        [Header("手刹设置")]
        [Tooltip("手刹力度")]
        [Range(100f, 2000f)]
        [SerializeField] private float handbrakeTorque = 1200f;

        [Tooltip("漂移时后轮横向刚度系数")]
        [Range(0.1f, 1.0f)]
        [SerializeField] private float driftFactor = 0.9f;

        [Tooltip("漂移恢复时间（秒）")]
        [Range(0.1f, 2.0f)]
        [SerializeField] private float driftRecoveryTime = 0.8f;

        [Tooltip("漂移最小速度")]
        [SerializeField] private float minDriftSpeed = 20f;

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

        #region 私有变量
        // 引用
        private VehicleController vehicleController;
        private VehiclePhysics vehiclePhysics;
        private Rigidbody vehicleRigidbody;

        // 驱动因子
        private float frontWheelDriveFactor = 0.0f;
        private float rearWheelDriveFactor = 1.0f;

        // 输入值
        private float throttleInput = 0f;
        private float brakeInput = 0f;
        private float steeringInput = 0f;
        private bool isHandbrakeActive = false;
        private bool isNitroActive = false;
        private bool isBrakingWithThrottle = false; // 新增：带油门的制动状态标志

        // 氮气状态
        private float currentNitroAmount;

        // 车辆状态
        private float currentSpeed = 0f;
        private float normalizedSpeed = 0f;
        private float motorTorque = 0f;

        // RPM范围
        private float minRPM = 800f;
        private float maxRPM = 7000f;
        private float currentRPM = 800f;
        #endregion

        #region 初始化
        private void Awake()
        {
            // 获取组件引用
            vehicleController = GetComponent<VehicleController>();
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

            // 初始化驱动类型系数
            SetupDriveTypeFactors();

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

            // 更新车辆控制器中的速度
            if (vehicleController != null)
            {
                vehicleController.SetVehicleSpeed(currentSpeed);
            }
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

            // 更新车辆控制器中的氮气状态
            if (vehicleController != null)
            {
                vehicleController.SetNitroStatus(isNitroActive, currentNitroAmount / nitroCapacity);
            }
        }
        #endregion

        #region 驱动控制
        private void ApplyDrive()
        {
            if (vehiclePhysics == null) return;

            // 获取本地空间中的速度
            Vector3 localVelocity = transform.InverseTransformDirection(vehicleRigidbody.linearVelocity);
            float forwardSpeed = localVelocity.z;

            // 计算实际驱动力（考虑氮气加成）
            float throttleForce = throttleInput * motorTorque;
            if (isNitroActive && currentNitroAmount > 0)
            {
                throttleForce *= nitroBoostFactor;
            }

            // 计算制动力 - 提高刹车效果，使用绝对值确保S键始终有效
            float finalBrakeForce = brakeInput * brakeForce * 3.0f;

            // 应用手刹 - 增强油门+手刹的处理
            if (isHandbrakeActive)
            {
                // 获取碰撞器引用
                WheelCollider rearLeft = vehiclePhysics.GetRearLeftWheel();
                WheelCollider rearRight = vehiclePhysics.GetRearRightWheel();

                // W+空格（油门+手刹）共存的情况下，降低后轮驱动力，增加制动力
                if (isBrakingWithThrottle && throttleInput > 0.1f)
                {
                    // 仍然应用手刹，但减弱漂移效果，增强制动
                    float handbrakeFactor = Mathf.Lerp(0.5f, 0.8f, currentSpeed / 100f);
                    float adjustedHandbrakeTorque = handbrakeTorque * handbrakeFactor;

                    // 在油门+手刹情况下，给所有车轮施加一定的制动力
                    vehiclePhysics.SetHandbrakeTorque(adjustedHandbrakeTorque * 0.3f, adjustedHandbrakeTorque);

                    // 同时减小马达扭矩，但不要完全消除（与W+S的区别）
                    if (throttleForce > 0)
                    {
                        // 快速行驶时降低驱动力，低速时保留更多驱动力
                        float speedFactor = Mathf.Clamp01(currentSpeed / 80f);
                        float reducedThrottleFactor = Mathf.Lerp(0.6f, 0.2f, speedFactor);

                        // 应用降低的驱动力
                        vehiclePhysics.SetMotorTorque(
                            throttleForce * frontWheelDriveFactor * reducedThrottleFactor,
                            throttleForce * rearWheelDriveFactor * reducedThrottleFactor
                        );
                    }

                    // 根据速度应用漂移效果
                    if (enableDrifting && currentSpeed > minDriftSpeed && !vehiclePhysics.GetIsInAir())
                    {
                        // 为后轮应用漂移，但漂移效果应比纯手刹弱
                        ApplyLimitedDriftEffect(rearLeft, rearRight, 0.6f);
                    }
                }
                else
                {
                    // 原有的手刹逻辑 - 纯手刹模式
                    // 只有当速度足够高且在地面上时才应用漂移效果
                    if (enableDrifting && currentSpeed > minDriftSpeed && !vehiclePhysics.GetIsInAir())
                    {
                        // 应用漂移效果 - 高速下提高摩擦力以防止过度打滑
                        ApplyFullDriftEffect(rearLeft, rearRight);
                    }

                    // 应用手刹扭矩 - 确保立即制动效果
                    float adjustedHandbrakeTorque = handbrakeTorque * 1.5f; // 增强手刹力度

                    // 不再根据速度降低手刹力度，确保任何速度下都能立即锁死车轮
                    // 应用强力制动
                    vehiclePhysics.SetHandbrakeTorque(0f, adjustedHandbrakeTorque);

                    // 立即停止车轮旋转 - 设置车轮角速度为零
                    vehiclePhysics.StopWheelRotation();

                    // 同时减小马达扭矩以配合制动
                    vehiclePhysics.SetMotorTorque(0f, 0f);
                }
            }
            else
            {
                // 如果手刹不再激活，恢复轮胎正常摩擦力
                if (enableDrifting && driftRecoveryCoroutine == null)
                {
                    driftRecoveryCoroutine = StartCoroutine(RecoverWheelStiffness());
                }

                // 通知车辆控制器停止漂移
                if (vehicleController != null)
                {
                    vehicleController.SetDriftState(false, 0f);
                }

                // 应用正常制动
                vehiclePhysics.SetBrakeTorque(finalBrakeForce, finalBrakeForce);
            }

            // 处理油门输入 (W键) - 不在手刹状态下
            if (throttleInput > 0.1f && brakeInput < 0.1f && !isHandbrakeActive)
            {
                // 根据速度调整动力，高速时减少动力防止打滑
                float speedAdjustedThrottle = throttleInput;
                if (currentSpeed > 100f) {
                    // 高速时逐渐降低加速力，避免轮胎打滑
                    speedAdjustedThrottle *= Mathf.Lerp(1.0f, 0.6f, (currentSpeed - 100f) / 60f);
                }

                // 应用驱动力
                float adjustedThrottleForce = throttleForce * speedAdjustedThrottle;
                vehiclePhysics.SetMotorTorque(
                    adjustedThrottleForce * frontWheelDriveFactor,
                    adjustedThrottleForce * rearWheelDriveFactor
                );

                // 确保制动力为0
                vehiclePhysics.SetBrakeTorque(0, 0);
            }
            // 处理刹车/倒车输入 (S键) - 不在手刹状态下
            else if (brakeInput > 0.1f && !isHandbrakeActive)
            {
                // 如果车辆正在向前移动，应用制动力
                if (forwardSpeed > 1.0f)
                {
                    // 应用强力制动以迅速减速
                    float speedBasedBrakeForce = finalBrakeForce * 2.0f;
                    vehiclePhysics.SetBrakeTorque(speedBasedBrakeForce, speedBasedBrakeForce);
                    vehiclePhysics.SetMotorTorque(0f, 0f);
                }
                // 如果车辆几乎停止，应用倒车力
                else if (Mathf.Abs(forwardSpeed) < 1.0f)
                {
                    // 先清除制动力
                    vehiclePhysics.SetBrakeTorque(0f, 0f);

                    // 应用强力倒车
                    float reverseMotorTorque = -motorTorque * brakeInput * reverseSpeedFactor * 2.0f;
                    vehiclePhysics.SetMotorTorque(
                        reverseMotorTorque * frontWheelDriveFactor,
                        reverseMotorTorque * rearWheelDriveFactor
                    );
                }
                // 已经在倒车
                else if (forwardSpeed < -0.5f)
                {
                    // 继续倒车
                    float reverseMotorTorque = -motorTorque * brakeInput * reverseSpeedFactor * 2.0f;
                    vehiclePhysics.SetMotorTorque(
                        reverseMotorTorque * frontWheelDriveFactor,
                        reverseMotorTorque * rearWheelDriveFactor
                    );
                    vehiclePhysics.SetBrakeTorque(0f, 0f);
                }
            }
            // W+S同时按下的情况，油门和刹车同时存在（isBrakingWithThrottle类似，但手刹未激活）
            else if (throttleInput > 0.01f && brakeInput > 0.01f && !isHandbrakeActive)
            {
                // 仍然保留一些驱动力，但同时施加制动力
                float reducedThrottleFactor = 0.3f; // 保留30%油门效果
                float adjustedThrottleForce = throttleForce * reducedThrottleFactor;

                // 应用降低的驱动力
                vehiclePhysics.SetMotorTorque(
                    adjustedThrottleForce * frontWheelDriveFactor,
                    adjustedThrottleForce * rearWheelDriveFactor
                );

                // 同时应用制动
                float enhancedBrakeForce = finalBrakeForce * 1.5f;
                vehiclePhysics.SetBrakeTorque(enhancedBrakeForce, enhancedBrakeForce);
            }
            // 没有输入时（滑行状态）
            else if (!isHandbrakeActive)
            {
                vehiclePhysics.SetMotorTorque(0f, 0f);
                // 应用轻微的制动力模拟发动机制动
                vehiclePhysics.SetBrakeTorque(brakeForce * 0.2f, brakeForce * 0.2f);
            }
        }

        /// <summary>
        /// 恢复轮胎摩擦力的协程
        /// </summary>
        private IEnumerator RecoverWheelStiffness()
        {
            // 获取碰撞器引用
            WheelCollider rearLeft = vehiclePhysics.GetRearLeftWheel();
            WheelCollider rearRight = vehiclePhysics.GetRearRightWheel();

            if (rearLeft == null || rearRight == null)
            {
                yield break;
            }

            float elapsedTime = 0;

            // 获取当前的摩擦力设置
            WheelFrictionCurve currentLeftFriction = rearLeft.sidewaysFriction;
            WheelFrictionCurve currentRightFriction = rearRight.sidewaysFriction;

            // 确保当前值有效
            if (currentLeftFriction.stiffness <= 0 || currentRightFriction.stiffness <= 0)
            {
                rearLeft.sidewaysFriction = originalSidewaysFriction[2];
                rearRight.sidewaysFriction = originalSidewaysFriction[3];
                driftRecoveryCoroutine = null;
                yield break;
            }

            // 根据速度调整恢复时间，高速时需要更长的恢复时间
            float speedFactor = Mathf.Clamp01(currentSpeed / maxSpeed);
            float adjustedRecoveryTime = driftRecoveryTime * (1.0f + speedFactor * 0.5f);

            while (elapsedTime < adjustedRecoveryTime)
            {
                elapsedTime += Time.deltaTime;
                // 使用平方插值让恢复更加自然
                float t = (elapsedTime / adjustedRecoveryTime);
                t = t * t;

                // 平滑恢复摩擦力
                WheelFrictionCurve leftFriction = rearLeft.sidewaysFriction;
                WheelFrictionCurve rightFriction = rearRight.sidewaysFriction;

                leftFriction.stiffness = Mathf.Lerp(
                    currentLeftFriction.stiffness,
                    originalSidewaysFriction[2].stiffness,
                    t
                );

                rightFriction.stiffness = Mathf.Lerp(
                    currentRightFriction.stiffness,
                    originalSidewaysFriction[3].stiffness,
                    t
                );

                rearLeft.sidewaysFriction = leftFriction;
                rearRight.sidewaysFriction = rightFriction;

                yield return null;
            }

            // 确保最终值是原始值
            rearLeft.sidewaysFriction = originalSidewaysFriction[2];
            rearRight.sidewaysFriction = originalSidewaysFriction[3];

            driftRecoveryCoroutine = null;
        }

        /// <summary>
        /// 应用完全漂移效果
        /// </summary>
        private void ApplyFullDriftEffect(WheelCollider rearLeft, WheelCollider rearRight)
        {
            WheelFrictionCurve leftFriction = rearLeft.sidewaysFriction;
            WheelFrictionCurve rightFriction = rearRight.sidewaysFriction;

            // 速度越高，保持越高的摩擦力以维持稳定性
            // 超过100km/h时增加最小摩擦力系数
            float speedBasedDriftFactor = Mathf.Lerp(driftFactor,
                                                   Mathf.Min(1.0f, driftFactor + 0.5f),
                                                   Mathf.Clamp01((currentSpeed - 80) / 60f));

            // 提高漂移时的横向摩擦力，避免完全失控
            float adjustedDriftFactor = Mathf.Lerp(0.8f, speedBasedDriftFactor, Mathf.Min(1.0f, currentSpeed / 100f));

            // 确保高速不会过度打滑
            if (currentSpeed > 120f) {
                adjustedDriftFactor = Mathf.Lerp(adjustedDriftFactor, 1.0f, (currentSpeed - 120f) / 40f);
            }

            leftFriction.stiffness = originalSidewaysFriction[2].stiffness * adjustedDriftFactor;
            rightFriction.stiffness = originalSidewaysFriction[3].stiffness * adjustedDriftFactor;

            rearLeft.sidewaysFriction = leftFriction;
            rearRight.sidewaysFriction = rightFriction;

            // 如果已经有恢复协程在运行，停止它
            if (driftRecoveryCoroutine != null)
            {
                StopCoroutine(driftRecoveryCoroutine);
                driftRecoveryCoroutine = null;
            }

            // 通知车辆控制器正在漂移
            if (vehicleController != null)
            {
                vehicleController.SetDriftState(true, 1.0f - adjustedDriftFactor);
            }
        }

        /// <summary>
        /// 应用有限的漂移效果（油门+手刹情况）
        /// </summary>
        private void ApplyLimitedDriftEffect(WheelCollider rearLeft, WheelCollider rearRight, float intensityFactor)
        {
            WheelFrictionCurve leftFriction = rearLeft.sidewaysFriction;
            WheelFrictionCurve rightFriction = rearRight.sidewaysFriction;

            // 对于油门+手刹情况，增加摩擦力，减弱漂移效果
            float baseFactor = driftFactor + (1.0f - driftFactor) * (1.0f - intensityFactor);
            float speedBasedDriftFactor = Mathf.Lerp(baseFactor,
                                                  Mathf.Min(1.0f, baseFactor + 0.3f),
                                                  Mathf.Clamp01((currentSpeed - 80) / 60f));

            // 提高漂移时的横向摩擦力，减少打滑
            float adjustedDriftFactor = Mathf.Lerp(0.85f, speedBasedDriftFactor, Mathf.Min(1.0f, currentSpeed / 100f));

            // 高速时更快回归正常状态
            if (currentSpeed > 100f) {
                adjustedDriftFactor = Mathf.Lerp(adjustedDriftFactor, 1.0f, (currentSpeed - 100f) / 40f);
            }

            leftFriction.stiffness = originalSidewaysFriction[2].stiffness * adjustedDriftFactor;
            rightFriction.stiffness = originalSidewaysFriction[3].stiffness * adjustedDriftFactor;

            rearLeft.sidewaysFriction = leftFriction;
            rearRight.sidewaysFriction = rightFriction;

            // 如果已经有恢复协程在运行，停止它
            if (driftRecoveryCoroutine != null)
            {
                StopCoroutine(driftRecoveryCoroutine);
                driftRecoveryCoroutine = null;
            }

            // 通知车辆控制器正在漂移，但强度更低
            if (vehicleController != null)
            {
                vehicleController.SetDriftState(true, (1.0f - adjustedDriftFactor) * intensityFactor);
            }
        }
        #endregion

        private void ApplySteering()
        {
            if (vehiclePhysics == null) return;

            // 根据速度调整转向
            float speedFactor = Mathf.Clamp01(currentSpeed / 100f);

            // 高速下大幅减小转向角度，防止过度转向
            float highSpeedReduction = Mathf.Lerp(1f, 0.3f, Mathf.Clamp01((currentSpeed - 80f) / 40f));

            // 使用转向曲线获取响应系数
            float steeringResponse = steeringCurve.Evaluate(speedFactor);

            // 计算转向角度 - 高速下更进一步减小转向角度
            float targetSteeringAngle = maxSteeringAngle * steeringInput * (1f - speedFactor * 0.7f) * highSpeedReduction;

            // 平滑转向输入，防止突然的转向造成失控
            float deltaTime = Time.fixedDeltaTime;
            float currentSteerAngle = 0f;
            if (vehiclePhysics.GetFrontLeftWheel() != null)
            {
                currentSteerAngle = vehiclePhysics.GetFrontLeftWheel().steerAngle;
            }

            // 转向变化速率基于速度进行调整，高速时转向更缓慢
            float steerSpeed = Mathf.Lerp(steeringSpeed, steeringSpeed * 0.5f, speedFactor);
            float smoothedSteeringAngle = Mathf.Lerp(currentSteerAngle, targetSteeringAngle, deltaTime * steerSpeed);

            // 应用转向
            vehiclePhysics.SetSteeringAngle(smoothedSteeringAngle, smoothedSteeringAngle);
        }

        private void LimitSpeed()
        {
            if (vehicleRigidbody == null) return;

            // 获取本地空间中的速度
            Vector3 localVelocity = transform.InverseTransformDirection(vehicleRigidbody.linearVelocity);
            float forwardSpeed = localVelocity.z;
            float currentKmhSpeed = forwardSpeed * (1f / KMH_TO_MS);

            // 计算氮气增强的最大速度
            float effectiveMaxSpeed = maxSpeed;

            // 如果氮气激活且有氮气可用，允许车辆超过普通最大速度
            if (isNitroActive && currentNitroAmount > 0)
            {
                // 氮气增强的最大速度 = 普通最大速度 * 氮气加速系数
                // 使用氮气加速系数作为最大速度的增强系数，保持一致性
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

            // 限制前进速度 - 使用 AddForce 而不是直接修改 linearVelocity
            if (currentKmhSpeed > effectiveMaxSpeed)
            {
                // 计算超出有效最大速度的程度
                float overSpeedRatio = (currentKmhSpeed - effectiveMaxSpeed) / effectiveMaxSpeed;

                // 计算需要施加的减速力
                // 超速越多，减速力越大，但保持平滑过渡
                float brakingForce = vehicleRigidbody.mass * overSpeedRatio * 50f;

                // 限制最大减速力，避免过度反应
                brakingForce = Mathf.Min(brakingForce, vehicleRigidbody.mass * 20f);

                // 如果使用氮气，减小制动力，使加速感更强
                if (isNitroActive && currentNitroAmount > 0)
                {
                    // 氮气激活时减小制动力，使车辆能够更快地加速到氮气增强的最大速度
                    brakingForce *= 0.7f;
                }

                // 应用与车辆前进方向相反的力
                vehicleRigidbody.AddForce(-transform.forward * brakingForce, ForceMode.Force);

                // 如果超速过多，增加临时阻力以帮助减速
                if (overSpeedRatio > 0.1f)
                {
                    // 临时增加线性阻力，但保持平滑过渡
                    float tempDrag = Mathf.Lerp(0.01f, 0.5f, overSpeedRatio);
                    vehicleRigidbody.linearDamping = Mathf.Max(vehicleRigidbody.linearDamping, tempDrag);
                }
            }
            else
            {
                // 当速度正常时，恢复默认阻力
                vehicleRigidbody.linearDamping = 0.01f;
            }

            // 限制倒车速度 - 同样使用 AddForce
            if (currentKmhSpeed < -maxReverseSpeed)
            {
                // 计算超出最大倒车速度的程度
                float overSpeedRatio = (-currentKmhSpeed - maxReverseSpeed) / maxReverseSpeed;

                // 计算需要施加的减速力
                float brakingForce = vehicleRigidbody.mass * overSpeedRatio * 50f;
                brakingForce = Mathf.Min(brakingForce, vehicleRigidbody.mass * 20f);

                // 应用与车辆后退方向相反的力
                vehicleRigidbody.AddForce(transform.forward * brakingForce, ForceMode.Force);

                // 如果超速过多，增加临时阻力
                if (overSpeedRatio > 0.1f)
                {
                    float tempDrag = Mathf.Lerp(0.01f, 0.5f, overSpeedRatio);
                    vehicleRigidbody.linearDamping = Mathf.Max(vehicleRigidbody.linearDamping, tempDrag);
                }
            }
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

        /// 获取驱动类型
        public DriveType GetDriveType()
        {
            return driveType;
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

        /// <summary>
        /// 设置是否同时使用油门和制动
        /// </summary>
        public void SetBrakingWithThrottle(bool active)
        {
            isBrakingWithThrottle = active;
        }
        #endregion
    }
}