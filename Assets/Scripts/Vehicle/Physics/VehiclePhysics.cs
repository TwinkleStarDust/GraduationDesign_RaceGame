using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Vehicle
{
    /// <summary>
    /// 车辆物理系统
    /// 处理车辆的物理行为，如悬挂、摩擦力、稳定性和碰撞
    /// 简化版本，适合街机风格赛车游戏
    /// </summary>
    public class VehiclePhysics : MonoBehaviour
    {
        [Header("物理参数")]
        [Tooltip("重心高度")]
        [SerializeField] private float centerOfMassHeight = -0.3f;

        [Tooltip("下压力系数")]
        [Range(0.5f, 5.0f)]
        [SerializeField] private float downforceCoefficient = 2.0f;

        [Header("车轮碰撞器")]
        [Tooltip("前左轮")]
        [SerializeField] private WheelCollider frontLeftWheel;

        [Tooltip("前右轮")]
        [SerializeField] private WheelCollider frontRightWheel;

        [Tooltip("后左轮")]
        [SerializeField] private WheelCollider rearLeftWheel;

        [Tooltip("后右轮")]
        [SerializeField] private WheelCollider rearRightWheel;

        [Header("车轮网格")]
        [Tooltip("前左轮网格")]
        [SerializeField] private Transform frontLeftWheelModel;

        [Tooltip("前右轮网格")]
        [SerializeField] private Transform frontRightWheelModel;

        [Tooltip("后左轮网格")]
        [SerializeField] private Transform rearLeftWheelModel;

        [Tooltip("后右轮网格")]
        [SerializeField] private Transform rearRightWheelModel;

        [Header("稳定性设置")]
        [Tooltip("防翻滚力矩")]
        [Range(2000f, 10000f)]
        [SerializeField] private float antiRollForce = 5000f;

        [Header("摩擦力设置")]
        [Tooltip("前轮前向摩擦力")]
        [Range(0.5f, 3.0f)]
        [SerializeField] private float frontWheelForwardFriction = 1.8f;

        [Tooltip("前轮侧向摩擦力")]
        [Range(0.5f, 3.0f)]
        [SerializeField] private float frontWheelSidewaysFriction = 1.3f;

        [Tooltip("后轮前向摩擦力")]
        [Range(0.5f, 3.0f)]
        [SerializeField] private float rearWheelForwardFriction = 1.9f;

        [Tooltip("后轮侧向摩擦力")]
        [Range(0.5f, 3.0f)]
        [SerializeField] private float rearWheelSidewaysFriction = 1.2f;

        [Header("悬挂设置")]
        [Tooltip("使用Unity自带的悬挂系统")]
        [SerializeField] private bool useDefaultSuspension = true;

        [Tooltip("悬挂弹簧强度")]
        [Range(10000f, 40000f)]
        [SerializeField] private float suspensionSpring = 25000f;

        [Tooltip("悬挂阻尼")]
        [Range(1000f, 5000f)]
        [SerializeField] private float suspensionDamper = 2500f;

        [Header("音频系统")]
        [Tooltip("引擎音频源")]
        [SerializeField] private AudioSource engineAudioSource;

        [Tooltip("轮胎打滑音频源")]
        [SerializeField] private AudioSource tireSkidAudioSource;

        [Tooltip("碰撞音频源")]
        [SerializeField] private AudioSource crashAudioSource;

        [Tooltip("氮气音频源")]
        [SerializeField] private AudioSource nitroAudioSource;

        // 引用其他组件
        private Rigidbody vehicleRigidbody;
        private VehicleController vehicleController;
        private VehicleDriveSystem vehicleDriveSystem;

        // 车辆状态
        private bool isInAir = false;
        private bool isFlipped = false;
        private bool isUpsideDown = false;
        private float airTime = 0f;

        // 缓存原始摩擦力设置
        private WheelFrictionCurve originalFrontWheelForwardFriction;
        private WheelFrictionCurve originalFrontWheelSidewaysFriction;
        private WheelFrictionCurve originalRearWheelForwardFriction;
        private WheelFrictionCurve originalRearWheelSidewaysFriction;

        // 速度相关
        private float currentSpeed = 0f;

        // 音频状态变量
        private bool isPlayingSkidSound = false;
        private float currentSkidVolume = 0f;
        private bool isNitroActive = false;
        private Dictionary<WheelCollider, float> wheelSlipAmount = new Dictionary<WheelCollider, float>();
        private float enginePitch = 0.5f;

        // 声音过渡时间
        private const float SOUND_TRANSITION_SPEED = 5.0f;

        // 保存原始后轮刚度值
        private float originalRearWheelStiffness = 0f;

        /// 初始化物理系统
        private void Awake()
        {
            // 获取组件引用
            vehicleRigidbody = GetComponent<Rigidbody>();
            vehicleController = GetComponent<VehicleController>();
            vehicleDriveSystem = GetComponent<VehicleDriveSystem>();

            // 配置刚体
            SetupRigidbody();

            // 配置车轮
            SetupWheels();

            // 初始化音频系统
            InitializeAudioSources();
        }

        /// 启动时进行额外的优化和准备
        private void Start()
        {
            // 验证所有必要组件
            ValidateComponents();

            // 初始化车轮位置
            UpdateWheelModels();

            // 保存原始摩擦力设置
            SaveOriginalFrictionSettings();
        }

        /// 验证所有必要组件是否已正确设置
        private void ValidateComponents()
        {
            if (frontLeftWheel == null || frontRightWheel == null ||
                rearLeftWheel == null || rearRightWheel == null)
            {
                Debug.LogError("【车辆物理】缺少车轮碰撞器！请检查Inspector中的车轮碰撞器设置。");
            }

            if (frontLeftWheelModel == null || frontRightWheelModel == null ||
                rearLeftWheelModel == null || rearRightWheelModel == null)
            {
                Debug.LogError("【车辆物理】缺少车轮模型！请检查Inspector中的车轮模型设置。");
            }

            if (vehicleRigidbody == null)
            {
                Debug.LogError("【车辆物理】缺少Rigidbody组件！请添加Rigidbody组件到车辆。");
            }
        }

        /// 保存原始摩擦力设置
        private void SaveOriginalFrictionSettings()
        {
            originalFrontWheelForwardFriction = frontLeftWheel.forwardFriction;
            originalFrontWheelSidewaysFriction = frontLeftWheel.sidewaysFriction;
            originalRearWheelForwardFriction = rearLeftWheel.forwardFriction;
            originalRearWheelSidewaysFriction = rearLeftWheel.sidewaysFriction;
        }

        /// 配置刚体
        private void SetupRigidbody()
        {
            if (vehicleRigidbody != null)
            {
                // 设置重心位置
                vehicleRigidbody.centerOfMass = new Vector3(0, centerOfMassHeight, 0);

                // 设置适合街机风格的拖拽和角阻力
                // 使用很小的默认阻力，让速度限制由 AddForce 来控制
                vehicleRigidbody.linearDamping = 0.01f;
                vehicleRigidbody.angularDamping = 0.5f;

                // 增加插值设置减少抖动
                vehicleRigidbody.interpolation = RigidbodyInterpolation.Interpolate;

                // 提高解算器迭代次数以减少抖动
                vehicleRigidbody.solverIterations = 10;
                vehicleRigidbody.solverVelocityIterations = 10;
            }
        }

        /// 初始化音频源
        private void InitializeAudioSources()
        {
            // 确保所有音频源都有适当的设置
            if (engineAudioSource != null)
            {
                engineAudioSource.loop = true;
                engineAudioSource.playOnAwake = true;
                engineAudioSource.Play();
            }

            if (tireSkidAudioSource != null)
            {
                tireSkidAudioSource.loop = true;
                tireSkidAudioSource.playOnAwake = false;
                tireSkidAudioSource.volume = 0;
            }

            if (nitroAudioSource != null)
            {
                nitroAudioSource.loop = true;
                nitroAudioSource.playOnAwake = false;
                nitroAudioSource.volume = 0;
            }
        }

        /// 配置车轮
        private void SetupWheels()
        {
            // 设置车轮摩擦力
            SetupWheelFriction();

            // 如果不使用默认悬挂系统，则应用自定义设置
            if (!useDefaultSuspension)
            {
                SetupWheelSuspension();
            }
        }

        /// 设置车轮摩擦力
        private void SetupWheelFriction()
        {
            // 前轮摩擦力
            SetWheelFriction(frontLeftWheel, frontWheelForwardFriction, frontWheelSidewaysFriction);
            SetWheelFriction(frontRightWheel, frontWheelForwardFriction, frontWheelSidewaysFriction);

            // 后轮摩擦力
            SetWheelFriction(rearLeftWheel, rearWheelForwardFriction, rearWheelSidewaysFriction);
            SetWheelFriction(rearRightWheel, rearWheelForwardFriction, rearWheelSidewaysFriction);
        }

        /// 设置单个车轮的摩擦力
        private void SetWheelFriction(WheelCollider wheel, float forwardStiffness, float sidewaysStiffness)
        {
            // 获取当前曲线但仅修改刚度
            WheelFrictionCurve forwardFriction = wheel.forwardFriction;

            // 保留曲线形状，只调整刚度
            forwardFriction.stiffness = forwardStiffness;

            // 不强制设置其他参数，使用Unity编辑器中配置的值
            // 如果需要调整，可以在Unity编辑器中直接修改WheelCollider组件

            wheel.forwardFriction = forwardFriction;

            WheelFrictionCurve sidewaysFriction = wheel.sidewaysFriction;
            sidewaysFriction.stiffness = sidewaysStiffness;

            // 同上，不强制更改曲线形状

            wheel.sidewaysFriction = sidewaysFriction;
        }

        /// 设置车轮悬挂
        private void SetupWheelSuspension()
        {
            // 设置所有车轮的悬挂参数
            ConfigureWheelSuspension(frontLeftWheel);
            ConfigureWheelSuspension(frontRightWheel);
            ConfigureWheelSuspension(rearLeftWheel);
            ConfigureWheelSuspension(rearRightWheel);
        }

        /// 配置单个车轮的悬挂
        private void ConfigureWheelSuspension(WheelCollider wheel)
        {
            JointSpring spring = wheel.suspensionSpring;
            spring.spring = suspensionSpring;
            spring.damper = suspensionDamper;
            spring.targetPosition = 0.5f;
            wheel.suspensionSpring = spring;
        }

        /// 物理更新
        private void FixedUpdate()
        {
            try
        {
            // 更新车辆状态
            UpdateVehicleState();

                // 更新车轮模型位置
            UpdateWheelModels();

                // 应用稳定性控制
            StabilizeVehicle();

            // 应用下压力
            ApplyDownforce();

                // 更新音频
            UpdateVehicleAudio();
        }
            catch (System.Exception e)
            {
                // 记录异常并防止游戏崩溃
                Debug.LogError("VehiclePhysics发生错误: " + e.Message + "\n" + e.StackTrace);

                // 发生错误时重置物理状态防止连锁反应
                if (vehicleRigidbody != null)
                {
                    vehicleRigidbody.linearVelocity = Vector3.zero;
                }
            }
        }

        /// 更新车辆状态
        private void UpdateVehicleState()
        {
            // 检查是否在空中
            isInAir = !IsAnyWheelGrounded();

            // 如果在空中，增加空中时间
            if (isInAir)
            {
                airTime += Time.fixedDeltaTime;
            }
            else
            {
                airTime = 0f;
            }

            // 检查车辆是否侧翻或倒置
            Vector3 upDirection = transform.up;
            isFlipped = Vector3.Dot(upDirection, Vector3.up) < 0.5f;
            isUpsideDown = Vector3.Dot(upDirection, Vector3.up) < 0.1f;

            // 更新车辆控制器中的状态
            if (vehicleController != null)
            {
                vehicleController.SetVehicleState(isInAir, isFlipped, isUpsideDown);
            }

            // 更新当前速度
            if (vehicleDriveSystem != null)
            {
                currentSpeed = vehicleDriveSystem.GetCurrentSpeed();
            }
        }

        /// 检查是否有任何车轮在地面上
        private bool IsAnyWheelGrounded()
        {
            return IsWheelGrounded(frontLeftWheel) ||
                   IsWheelGrounded(frontRightWheel) ||
                   IsWheelGrounded(rearLeftWheel) ||
                   IsWheelGrounded(rearRightWheel);
        }

        /// 检查单个车轮是否在地面上
        private bool IsWheelGrounded(WheelCollider wheel)
        {
            if (wheel == null) return false;

            WheelHit hit;
            return wheel.GetGroundHit(out hit);
        }

        /// 更新车轮模型位置和旋转
        private void UpdateWheelModels()
        {
            UpdateWheelModel(frontLeftWheel, frontLeftWheelModel);
            UpdateWheelModel(frontRightWheel, frontRightWheelModel);
            UpdateWheelModel(rearLeftWheel, rearLeftWheelModel);
            UpdateWheelModel(rearRightWheel, rearRightWheelModel);
        }

        /// 更新单个车轮模型
        private void UpdateWheelModel(WheelCollider wheelCollider, Transform wheelModel)
        {
            if (wheelCollider == null || wheelModel == null) return;

            Vector3 position;
            Quaternion rotation;

            // 获取车轮位置和旋转
            wheelCollider.GetWorldPose(out position, out rotation);

            // 应用到车轮模型
            wheelModel.position = position;
            wheelModel.rotation = rotation;
        }

        /// 稳定车辆
        private void StabilizeVehicle()
        {
            if (vehicleRigidbody == null) return;

            // 如果在地面上，应用防侧翻力矩
            if (!isInAir)
            {
                ApplyAntiRollForce();
            }
            // 在空中时应用简单的姿态控制
            else if (airTime > 0.5f)
            {
                // 简单的空中姿态控制 - 尝试保持车辆水平
                Vector3 torqueDir = Vector3.Cross(transform.up, Vector3.up);
                float torqueMag = 1.0f - Vector3.Dot(transform.up, Vector3.up);

                vehicleRigidbody.AddTorque(torqueDir.normalized * torqueMag * 5f, ForceMode.Acceleration);
            }
        }

        /// 应用防侧翻力矩
        private void ApplyAntiRollForce()
        {
            // 前轮防侧翻
            ApplyAntiRollToAxle(frontLeftWheel, frontRightWheel);

            // 后轮防侧翻
            ApplyAntiRollToAxle(rearLeftWheel, rearRightWheel);
        }

        /// 对一个车轴应用防侧翻力
        private void ApplyAntiRollToAxle(WheelCollider leftWheel, WheelCollider rightWheel)
        {
            if (leftWheel == null || rightWheel == null) return;

            WheelHit leftWheelHit, rightWheelHit;
            bool leftGrounded = leftWheel.GetGroundHit(out leftWheelHit);
            bool rightGrounded = rightWheel.GetGroundHit(out rightWheelHit);

            float leftTravel = 1.0f;
            float rightTravel = 1.0f;

            if (leftGrounded)
                leftTravel = (-leftWheel.transform.InverseTransformPoint(leftWheelHit.point).y - leftWheel.radius) / leftWheel.suspensionDistance;

            if (rightGrounded)
                rightTravel = (-rightWheel.transform.InverseTransformPoint(rightWheelHit.point).y - rightWheel.radius) / rightWheel.suspensionDistance;

            float antiRollForceAmount = (leftTravel - rightTravel) * antiRollForce;

            // 平滑应用防侧翻力，减少抖动
            float deltaTime = Time.fixedDeltaTime;
            float smoothFactor = Mathf.Clamp01(deltaTime * 10f);
            antiRollForceAmount = Mathf.Lerp(0, antiRollForceAmount, smoothFactor);

            if (leftGrounded)
                vehicleRigidbody.AddForceAtPosition(leftWheel.transform.up * -antiRollForceAmount, leftWheel.transform.position);

            if (rightGrounded)
                vehicleRigidbody.AddForceAtPosition(rightWheel.transform.up * antiRollForceAmount, rightWheel.transform.position);
        }

        /// 应用下压力，增加高速稳定性
        private void ApplyDownforce()
        {
            if (vehicleRigidbody == null || isInAir) return;

            // 获取车辆最大速度
            float maxSpeed = 200f; // 默认值
            bool isNitroActive = false;
            float nitroBoostFactor = 1.5f; // 默认氮气加速系数

            if (vehicleDriveSystem != null)
            {
                maxSpeed = vehicleDriveSystem.GetMaxSpeed();
                isNitroActive = vehicleDriveSystem.IsNitroActive();

                // 如果氮气激活，考虑氮气增强的最大速度
                // 这样下压力计算也会考虑氮气加速的情况
                if (isNitroActive && vehicleDriveSystem.GetNitroAmount() > 0)
                {
                    // 使用氮气加速系数来计算有效最大速度
                    // 这里我们使用稍微低一点的系数，以避免下压力过大
                    float effectiveNitroFactor = Mathf.Lerp(1.0f, nitroBoostFactor, 0.8f);
                    maxSpeed *= effectiveNitroFactor;
                }
            }

            // 使用实际最大速度的百分比计算下压力
            // 这样无论最大速度设置为多少，下压力都会平滑增加
            float speedPercentage = Mathf.Clamp01(currentSpeed / maxSpeed);

            // 使用平滑的曲线而不是二次方关系，避免在最大速度时突然增加
            // 使用三次曲线模拟平滑增加，在接近最大速度时增长速度减缓
            float smoothCurve = speedPercentage * speedPercentage * (3f - 2f * speedPercentage);

            // 计算下压力，使用平滑曲线
            float downforce = smoothCurve * downforceCoefficient * vehicleRigidbody.mass * 10f;

            // 如果氮气激活，稍微减少下压力，使车辆更容易加速
            if (isNitroActive)
            {
                downforce *= 0.85f; // 减少15%的下压力
            }

            // 在接近最大速度时稍微减少下压力，避免突然下沉
            if (speedPercentage > 0.9f)
            {
                // 在接近最大速度时稍微减少下压力
                float reductionFactor = (speedPercentage - 0.9f) * 10f; // 0-1范围
                downforce *= (1f - reductionFactor * 0.2f); // 最多减少20%
            }

            // 平滑应用下压力，减少抖动
            float deltaTime = Time.fixedDeltaTime;
            float smoothFactor = Mathf.Clamp01(deltaTime * 5f);
            downforce = Mathf.Lerp(0, downforce, smoothFactor);

            // 向下施加力
            vehicleRigidbody.AddForce(-transform.up * downforce, ForceMode.Force);

            // 在高速转弯时增加角阻尼减少过度摆动
            if (speedPercentage > 0.3f) // 使用百分比而不是固定速度
            {
                float turningFactor = 0f;
                if (frontLeftWheel != null && frontRightWheel != null)
                {
                    // 检测是否在转弯
                    float steerAngle = Mathf.Abs(frontLeftWheel.steerAngle);
                    turningFactor = Mathf.Clamp01(steerAngle / 20f);
                }

                if (turningFactor > 0.1f)
                {
                    // 增加角阻尼以稳定高速转弯
                    float angularDamping = turningFactor * speedPercentage * 2.0f;
                    vehicleRigidbody.angularDamping = Mathf.Lerp(0.5f, 2.0f, angularDamping);
                }
                else
                {
                    // 不转弯时恢复默认值
                    vehicleRigidbody.angularDamping = 0.5f;
                }
            }
        }

        /// 更新车辆音频
        private void UpdateVehicleAudio()
        {
            // 更新引擎声音
            UpdateEngineSound();

            // 更新轮胎打滑声音
            UpdateTireSkidSound();

            // 更新氮气声音
            UpdateNitroSound();
        }

        /// 更新引擎声音
        private void UpdateEngineSound()
        {
            if (engineAudioSource == null || vehicleDriveSystem == null) return;

            // 获取引擎转速
            float rpm = vehicleDriveSystem.GetEngineRPM();

            // 计算引擎音调
            float targetPitch = Mathf.Lerp(0.5f, 1.5f, Mathf.InverseLerp(800f, 7000f, rpm));

            // 根据油门输入调整音量
            float throttle = vehicleDriveSystem.GetThrottleInput();
            float targetVolume = Mathf.Lerp(0.4f, 0.8f, throttle * 0.8f);

            // 平滑过渡
            enginePitch = Mathf.Lerp(enginePitch, targetPitch, Time.deltaTime * 3f);
            engineAudioSource.pitch = enginePitch;
            engineAudioSource.volume = targetVolume;
        }

        /// 更新轮胎打滑声音
        private void UpdateTireSkidSound()
        {
            if (tireSkidAudioSource == null) return;

            // 检测任何车轮是否打滑
            float slipSum = 0f;
            int wheelCount = 0;

            if (!isInAir)
            {
                CheckWheelSlip(frontLeftWheel, ref slipSum, ref wheelCount);
                CheckWheelSlip(frontRightWheel, ref slipSum, ref wheelCount);
                CheckWheelSlip(rearLeftWheel, ref slipSum, ref wheelCount);
                CheckWheelSlip(rearRightWheel, ref slipSum, ref wheelCount);
            }

            // 计算平均打滑值
            float averageSlip = wheelCount > 0 ? slipSum / wheelCount : 0f;

            // 当打滑值超过阈值时播放声音
            float targetVolume = 0f;
            if (averageSlip > 0.2f && !isInAir)
            {
                targetVolume = Mathf.Lerp(0.1f, 0.8f, Mathf.InverseLerp(0.2f, 0.8f, averageSlip));
            }

            // 平滑过渡音量
            currentSkidVolume = Mathf.Lerp(currentSkidVolume, targetVolume, Time.deltaTime * 5f);
            tireSkidAudioSource.volume = currentSkidVolume;

            // 管理声音播放状态
            if (currentSkidVolume > 0.01f && !tireSkidAudioSource.isPlaying)
            {
                tireSkidAudioSource.Play();
                isPlayingSkidSound = true;
            }
            else if (currentSkidVolume < 0.01f && isPlayingSkidSound)
            {
                tireSkidAudioSource.Stop();
                isPlayingSkidSound = false;
            }
        }

        /// 检查单个车轮的打滑情况
        private void CheckWheelSlip(WheelCollider wheel, ref float slipSum, ref int wheelCount)
        {
            if (wheel == null) return;

            WheelHit hit;
            if (wheel.GetGroundHit(out hit))
            {
                float slipValue = Mathf.Abs(hit.sidewaysSlip) + Mathf.Abs(hit.forwardSlip) * 0.5f;
                slipSum += slipValue;
                wheelCount++;
            }
        }

        /// 更新氮气声音
        private void UpdateNitroSound()
        {
            if (nitroAudioSource == null || vehicleDriveSystem == null) return;

            // 检查氮气状态
            bool nitroActive = vehicleDriveSystem.IsNitroActive();

            // 如果氮气状态改变
            if (nitroActive != isNitroActive)
            {
                isNitroActive = nitroActive;

                // 根据状态调整声音
                if (isNitroActive && !nitroAudioSource.isPlaying)
                {
                    nitroAudioSource.Play();
                    nitroAudioSource.volume = 0.6f;
                }
                else if (!isNitroActive && nitroAudioSource.isPlaying)
                {
                    // 平滑淡出
                    StartCoroutine(FadeOutAudio(nitroAudioSource, 0.3f));
                }
            }
        }

        /// 平滑淡出音频
        private IEnumerator FadeOutAudio(AudioSource audioSource, float duration)
        {
            float startVolume = audioSource.volume;
            float time = 0;

            while (time < duration)
            {
                time += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(startVolume, 0, time / duration);
                yield return null;
            }

            audioSource.Stop();
            audioSource.volume = 0;
        }

        /// 碰撞处理
        private void OnCollisionEnter(Collision collision)
        {
            if (crashAudioSource == null) return;

            // 计算碰撞力度
            float collisionForce = collision.relativeVelocity.magnitude;

            // 播放碰撞声音
            if (collisionForce > 3f)
            {
                // 播放碰撞声音
                crashAudioSource.volume = Mathf.Lerp(0.3f, 1.0f, Mathf.InverseLerp(3f, 15f, collisionForce));
                crashAudioSource.pitch = Random.Range(0.9f, 1.1f);
                crashAudioSource.Play();
            }
        }

        #region 公共接口

        /// 调整后轮摩擦力
        public void AdjustRearWheelFriction(float forwardStiffness, float sidewaysStiffness)
        {
            SetWheelFriction(rearLeftWheel, forwardStiffness, sidewaysStiffness);
            SetWheelFriction(rearRightWheel, forwardStiffness, sidewaysStiffness);
        }

        /// 重置车轮摩擦力到默认值
        public void ResetWheelFriction()
        {
            // 恢复原始摩擦力设置
            if (originalFrontWheelForwardFriction.stiffness > 0)
            {
                frontLeftWheel.forwardFriction = originalFrontWheelForwardFriction;
                frontLeftWheel.sidewaysFriction = originalFrontWheelSidewaysFriction;
                frontRightWheel.forwardFriction = originalFrontWheelForwardFriction;
                frontRightWheel.sidewaysFriction = originalFrontWheelSidewaysFriction;

                rearLeftWheel.forwardFriction = originalRearWheelForwardFriction;
                rearLeftWheel.sidewaysFriction = originalRearWheelSidewaysFriction;
                rearRightWheel.forwardFriction = originalRearWheelForwardFriction;
                rearRightWheel.sidewaysFriction = originalRearWheelSidewaysFriction;
            }
            else
            {
                // 如果原始设置不可用，使用默认值
            SetupWheelFriction();
            }
        }

        /// 设置转向角度
        public void SetSteeringAngle(float leftAngle, float rightAngle)
        {
            if (frontLeftWheel != null) frontLeftWheel.steerAngle = leftAngle;
            if (frontRightWheel != null) frontRightWheel.steerAngle = rightAngle;
        }

        /// 应用马达扭矩
        public void ApplyMotorTorque(float frontTorque, float rearTorque)
        {
            if (frontLeftWheel != null) frontLeftWheel.motorTorque = frontTorque;
            if (frontRightWheel != null) frontRightWheel.motorTorque = frontTorque;
            if (rearLeftWheel != null) rearLeftWheel.motorTorque = rearTorque;
            if (rearRightWheel != null) rearRightWheel.motorTorque = rearTorque;
        }

        /// 应用制动扭矩
        public void ApplyBrakeTorque(float frontBrakeTorque, float rearBrakeTorque)
        {
            // 调用统一的制动方法
            SetBrakeTorque(frontBrakeTorque, rearBrakeTorque);
        }

        /// 设置车轮制动扭矩
        public void SetBrakeTorque(float frontTorque, float rearTorque)
        {
            // 确保制动力足够高
            if (frontTorque > 0) frontTorque = Mathf.Max(frontTorque, 100f);
            if (rearTorque > 0) rearTorque = Mathf.Max(rearTorque, 100f);

            // 应用制动力
            if (frontLeftWheel != null)
                frontLeftWheel.brakeTorque = frontTorque;

            if (frontRightWheel != null)
                frontRightWheel.brakeTorque = frontTorque;

            if (rearLeftWheel != null)
                rearLeftWheel.brakeTorque = rearTorque;

            if (rearRightWheel != null)
                rearRightWheel.brakeTorque = rearTorque;
        }

        /// 设置手刹扭矩（主要影响后轮）
        public void SetHandbrakeTorque(float frontTorque, float rearTorque)
        {
            // 手刹同时调整前后轮，确保制动效果明显
            if (frontLeftWheel != null)
                frontLeftWheel.brakeTorque = frontTorque + (rearTorque * 0.3f); // 给前轮一部分制动力

            if (frontRightWheel != null)
                frontRightWheel.brakeTorque = frontTorque + (rearTorque * 0.3f);

            // 后轮保持强大的制动力
            if (rearLeftWheel != null)
                rearLeftWheel.brakeTorque = rearTorque;

            if (rearRightWheel != null)
                rearRightWheel.brakeTorque = rearTorque;
        }

        /// 重置物理状态
        public void ResetPhysics()
        {
            if (vehicleRigidbody != null)
            {
                // 重置速度和角速度
                vehicleRigidbody.linearVelocity = Vector3.zero;
                vehicleRigidbody.angularVelocity = Vector3.zero;
            }

            // 重置车轮状态
            ResetWheels();
        }

        /// 重置车轮状态
        private void ResetWheels()
        {
            // 重置车轮旋转和力
            if (frontLeftWheel != null) frontLeftWheel.motorTorque = 0;
            if (frontRightWheel != null) frontRightWheel.motorTorque = 0;
            if (rearLeftWheel != null) rearLeftWheel.motorTorque = 0;
            if (rearRightWheel != null) rearRightWheel.motorTorque = 0;

            if (frontLeftWheel != null) frontLeftWheel.brakeTorque = 0;
            if (frontRightWheel != null) frontRightWheel.brakeTorque = 0;
            if (rearLeftWheel != null) rearLeftWheel.brakeTorque = 0;
            if (rearRightWheel != null) rearRightWheel.brakeTorque = 0;
        }

        /// 获取是否在空中
        public bool GetIsInAir()
        {
            return isInAir;
        }

        /// 获取是否侧翻
        public bool GetIsFlipped()
        {
            return isFlipped;
        }

        /// 获取是否倒置
        public bool GetIsUpsideDown()
        {
            return isUpsideDown;
        }

        /// 获取前左轮
        public WheelCollider GetFrontLeftWheel()
        {
            return frontLeftWheel;
        }

        /// 获取前右轮
        public WheelCollider GetFrontRightWheel()
        {
            return frontRightWheel;
        }

        /// 获取后左轮
        public WheelCollider GetRearLeftWheel()
        {
            return rearLeftWheel;
        }

        /// 获取后右轮
        public WheelCollider GetRearRightWheel()
        {
            return rearRightWheel;
        }

        /// 对车辆施加力
        public void AddForce(Vector3 force)
        {
            if (vehicleRigidbody != null)
            {
                vehicleRigidbody.AddForce(force, ForceMode.Force);
            }
        }

        /// 检查车辆是否有刚体组件
        public bool HasRigidbody()
        {
            return vehicleRigidbody != null;
        }

        /// 获取车辆前进速度（米/秒）
        public float GetForwardSpeed()
        {
            if (vehicleRigidbody != null)
            {
                // 计算沿着车辆前方向的速度
                return Vector3.Dot(vehicleRigidbody.linearVelocity, transform.forward);
            }
            return 0f;
        }

        /// 设置前轮转向角度
        public void SetSteeringAngle(float angle)
        {
            if (frontLeftWheel != null)
                frontLeftWheel.steerAngle = angle;

            if (frontRightWheel != null)
                frontRightWheel.steerAngle = angle;
        }

        /// 设置车轮驱动扭矩
        public void SetMotorTorque(float frontTorque, float rearTorque)
        {
            if (frontLeftWheel != null)
                frontLeftWheel.motorTorque = frontTorque;

            if (frontRightWheel != null)
                frontRightWheel.motorTorque = frontTorque;

            if (rearLeftWheel != null)
                rearLeftWheel.motorTorque = rearTorque;

            if (rearRightWheel != null)
                rearRightWheel.motorTorque = rearTorque;
        }

        /// 设置后轮横向刚度（用于漂移）
        public void SetRearWheelStiffness(float stiffnessFactor)
        {
            if (rearLeftWheel == null || rearRightWheel == null)
                return;

            WheelFrictionCurve leftFriction = rearLeftWheel.sidewaysFriction;
            WheelFrictionCurve rightFriction = rearRightWheel.sidewaysFriction;

            // 确保原始刚度值已保存
            if (originalRearWheelStiffness <= 0)
            {
                SaveOriginalRearWheelStiffness();
            }

            // 应用新的刚度值，但确保漂移不会过度
            float minStiffness = originalRearWheelStiffness * 0.5f; // 提高最小摩擦力，防止高速打滑
            float newStiffness = Mathf.Max(minStiffness, originalRearWheelStiffness * Mathf.Clamp01(stiffnessFactor));

            leftFriction.stiffness = newStiffness;
            rightFriction.stiffness = newStiffness;

            // 不修改摩擦曲线形状，保留Unity编辑器中的设置

            rearLeftWheel.sidewaysFriction = leftFriction;
            rearRightWheel.sidewaysFriction = rightFriction;
        }

        /// 获取与地面接触的车轮数量
        public int GetGroundedWheelCount()
        {
            int groundedCount = 0;

            // 检查所有车轮的接地状态
            if (frontLeftWheel != null && frontLeftWheel.isGrounded) groundedCount++;
            if (frontRightWheel != null && frontRightWheel.isGrounded) groundedCount++;
            if (rearLeftWheel != null && rearLeftWheel.isGrounded) groundedCount++;
            if (rearRightWheel != null && rearRightWheel.isGrounded) groundedCount++;

            return groundedCount;
        }

        /// <summary>
        /// 保存原始后轮刚度值（用于漂移恢复）
        /// </summary>
        public void SaveOriginalRearWheelStiffness()
        {
            if (rearLeftWheel != null)
            {
                originalRearWheelStiffness = rearLeftWheel.sidewaysFriction.stiffness;
            }
        }

        /// <summary>
        /// 恢复原始后轮刚度值
        /// </summary>
        public void RestoreOriginalRearWheelStiffness()
        {
            if (originalRearWheelStiffness > 0 && rearLeftWheel != null && rearRightWheel != null)
            {
                SetRearWheelStiffness(1.0f);
            }
        }

        /// <summary>
        /// 立即停止车轮旋转 - 用于手刹立即锁死车轮
        /// </summary>
        public void StopWheelRotation()
        {
            // 对所有车轮设置极高的制动力，立即停止旋转
            float emergencyBrakeTorque = 10000f; // 非常高的制动扭矩值

            // 应用紧急制动扭矩到所有车轮
            if (rearLeftWheel != null)
            {
                // 强制设置角速度为零，立即停止旋转
                rearLeftWheel.brakeTorque = emergencyBrakeTorque;
                // 如果车轮有角速度属性，直接设置为零
                rearLeftWheel.motorTorque = 0f;
            }

            if (rearRightWheel != null)
            {
                rearRightWheel.brakeTorque = emergencyBrakeTorque;
                rearRightWheel.motorTorque = 0f;
            }

            // 前轮也进行适当制动，但力度较小
            if (frontLeftWheel != null)
            {
                frontLeftWheel.brakeTorque = emergencyBrakeTorque * 0.5f;
                frontLeftWheel.motorTorque = 0f;
            }

            if (frontRightWheel != null)
            {
                frontRightWheel.brakeTorque = emergencyBrakeTorque * 0.5f;
                frontRightWheel.motorTorque = 0f;
            }
        }

        #endregion
    }
}