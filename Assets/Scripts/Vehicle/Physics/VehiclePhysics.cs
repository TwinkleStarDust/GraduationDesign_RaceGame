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
        [Tooltip("前轮前向摩擦力 - 影响加速和制动")]
        [Range(0.5f, 3.0f)]
        [SerializeField] private float frontWheelForwardFriction = 1.7f;

        [Tooltip("前轮侧向摩擦力 - 影响转向稳定性")]
        [Range(0.5f, 3.0f)]
        [SerializeField] private float frontWheelSidewaysFriction = 1.4f;

        [Tooltip("后轮前向摩擦力 - 影响加速和制动")]
        [Range(0.5f, 3.0f)]
        [SerializeField] private float rearWheelForwardFriction = 1.8f;

        [Tooltip("后轮侧向摩擦力 - 影响漂移难易程度")]
        [Range(0.5f, 3.0f)]
        [SerializeField] private float rearWheelSidewaysFriction = 1.1f;

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

        // 数组形式的原始摩擦力设置，用于漂移恢复
        // [0]=前左, [1]=前右, [2]=后左, [3]=后右
        private WheelFrictionCurve[] originalForwardFriction = new WheelFrictionCurve[4];
        private WheelFrictionCurve[] originalSidewaysFriction = new WheelFrictionCurve[4];

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

        [Header("高级物理设置")]
        [Tooltip("空气阻力系数，影响高速时的自然减速和最高速度")]
        [SerializeField] private float dragFactor = 0.01f;

        /// 初始化物理系统
        private void Awake()
        {
            // 获取组件引用
            vehicleRigidbody = GetComponent<Rigidbody>();
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
            // 保存单个变量形式的原始摩擦力设置
            originalFrontWheelForwardFriction = frontLeftWheel.forwardFriction;
            originalFrontWheelSidewaysFriction = frontLeftWheel.sidewaysFriction;
            originalRearWheelForwardFriction = rearLeftWheel.forwardFriction;
            originalRearWheelSidewaysFriction = rearLeftWheel.sidewaysFriction;

            // 保存数组形式的原始摩擦力设置
            if (frontLeftWheel != null) {
                originalForwardFriction[0] = frontLeftWheel.forwardFriction;
                originalSidewaysFriction[0] = frontLeftWheel.sidewaysFriction;
            }

            if (frontRightWheel != null) {
                originalForwardFriction[1] = frontRightWheel.forwardFriction;
                originalSidewaysFriction[1] = frontRightWheel.sidewaysFriction;
            }

            if (rearLeftWheel != null) {
                originalForwardFriction[2] = rearLeftWheel.forwardFriction;
                originalSidewaysFriction[2] = rearLeftWheel.sidewaysFriction;
            }

            if (rearRightWheel != null) {
                originalForwardFriction[3] = rearRightWheel.forwardFriction;
                originalSidewaysFriction[3] = rearRightWheel.sidewaysFriction;
            }
        }

        /// 配置刚体
        private void SetupRigidbody()
        {
            if (vehicleRigidbody != null)
            {
                // 将重心设定在车辆底部附近，提高稳定性
                vehicleRigidbody.centerOfMass = new Vector3(0f, centerOfMassHeight, 0f);

                // 设置更科学的阻力值
                // 降低线性阻力到极小值，让车辆在滑行时几乎没有阻力
                vehicleRigidbody.linearDamping = 0.0005f;  // 降低到极小值，确保车辆保持最大惯性
                
                // 保持足够的角阻力以防止车辆过度旋转
                vehicleRigidbody.angularDamping = 0.05f;

                // 确保使用连续动态碰撞检测，防止高速时穿过物体
                vehicleRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

                // 禁用自动睡眠，确保物理模拟始终活跃
                vehicleRigidbody.sleepThreshold = 0.0f;

                // 锁定旋转 - 车辆应该只能围绕Y轴旋转
                vehicleRigidbody.constraints = RigidbodyConstraints.None;

                // 调整刚体插值模式为平滑插值，提供更平滑的移动
                vehicleRigidbody.interpolation = RigidbodyInterpolation.Interpolate;

                // 调整求解器迭代次数，提高物理精度
                vehicleRigidbody.solverIterations = 8;
                vehicleRigidbody.solverVelocityIterations = 8;
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

        /// <summary>
        /// 设置单个车轮的摩擦力 - 优化版本，更适合漂移
        /// 平衡版本：提供更好的基础抓地力，但仍然允许漂移
        /// </summary>
        /// <param name="wheel">车轮碰撞器</param>
        /// <param name="forwardStiffness">前向摩擦力刚度</param>
        /// <param name="sidewaysStiffness">侧向摩擦力刚度</param>
        private void SetWheelFriction(WheelCollider wheel, float forwardStiffness, float sidewaysStiffness)
        {
            if (wheel == null) return;

            // 获取当前曲线但仅修改刚度
            WheelFrictionCurve forwardFriction = wheel.forwardFriction;

            // 保留曲线形状，只调整刚度
            // 增加基础刚度，提供更好的抓地力和惯性感
            // 前向摩擦力增加，提高直线行驶时的惯性
            forwardFriction.stiffness = forwardStiffness * 1.3f;

            // 前轮和后轮使用不同的摩擦曲线参数
            bool isRearWheel = (wheel == rearLeftWheel || wheel == rearRightWheel);

            if (isRearWheel)
            {
                // 后轮前向摩擦曲线 - 适合漂移但提供更好的基础抓地力
                forwardFriction.extremumSlip = 0.4f;      // 峰值滑移点
                forwardFriction.extremumValue = 1.5f;     // 增加峰值摩擦力，提高加速性能
                forwardFriction.asymptoteSlip = 0.8f;     // 渐近滑移点
                forwardFriction.asymptoteValue = 1.3f;    // 增加渐近摩擦力，提高高速稳定性
            }
            else
            {
                // 前轮前向摩擦曲线 - 提供更好的加速和制动感
                forwardFriction.extremumSlip = 0.4f;      // 峰值滑移点
                forwardFriction.extremumValue = 1.6f;     // 增加峰值摩擦力，提高制动性能
                forwardFriction.asymptoteSlip = 0.7f;     // 渐近滑移点
                forwardFriction.asymptoteValue = 1.4f;    // 增加渐近摩擦力，提高高速稳定性
            }

            wheel.forwardFriction = forwardFriction;

            WheelFrictionCurve sidewaysFriction = wheel.sidewaysFriction;

            // 侧向摩擦力刚度
            sidewaysFriction.stiffness = sidewaysStiffness; // 直接使用 Inspector 中设置的值

            if (isRearWheel)
            {
                // 后轮侧向摩擦曲线 - 适合漂移但提供更好的基础抓地力
                sidewaysFriction.extremumSlip = 0.35f;    // 峰值滑移点
                sidewaysFriction.extremumValue = 1.2f;    // 稍微降低峰值摩擦力
                sidewaysFriction.asymptoteSlip = 0.65f;   // 增加渐近滑移点，使漂移更可控
                sidewaysFriction.asymptoteValue = 1.0f;   // 降低渐近摩擦力，提高漂移性能
            }
            else
            {
                // 前轮侧向摩擦曲线 - 提供更好的转向感
                sidewaysFriction.extremumSlip = 0.3f;     // 峰值滑移点
                sidewaysFriction.extremumValue = 1.4f;    // 提高峰值摩擦力，增强转向响应
                sidewaysFriction.asymptoteSlip = 0.55f;   // 增加渐近滑移点，使转向更可控
                sidewaysFriction.asymptoteValue = 1.2f;   // 提高渐近摩擦力，增强高速转向稳定性
            }

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
            if (wheel == null) return;

            JointSpring spring = wheel.suspensionSpring;
            // 增加弹簧强度，减少车身抖动
            spring.spring = suspensionSpring * 1.2f;
            // 增加阻尼，减少弹跳
            spring.damper = suspensionDamper * 1.3f;
            // 调整目标位置，使车身稍微抬高，减少与车轮的碰撞
            spring.targetPosition = 0.4f;

            wheel.suspensionSpring = spring;

            // 调整悬挂距离，减少车身与车轮的碰撞
            wheel.suspensionDistance = wheel.suspensionDistance * 0.9f;
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

                // 应用空气阻力 (仅当车辆有速度时)
                ApplyAirResistance();

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

            // 更新车辆驱动系统中的状态
            if (vehicleDriveSystem != null)
            {
                vehicleDriveSystem.SetVehicleState(isInAir, isFlipped, isUpsideDown);
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

        /// <summary>
        /// 检查车辆是否在坡道上
        /// </summary>
        /// <returns>如果车辆在坡道上返回true，否则返回false</returns>
        public bool IsOnSlope()
        {
            if (isInAir) return false;

            // 获取地面法线
            Vector3 averageGroundNormal = GetAverageGroundNormal();

            // 计算地面法线与世界上方向的夹角
            float groundAngle = Vector3.Angle(averageGroundNormal, Vector3.up);

            // 如果角度大于阈值，认为车辆在坡道上
            // 5度是一个较小的坡度，可以根据需要调整
            return groundAngle > 5f;
        }

        /// <summary>
        /// 获取车辆所在坡道的角度
        /// </summary>
        /// <returns>坡道角度（单位：度），0表示平地</returns>
        public float GetSlopeAngle()
        {
            if (isInAir) return 0f;

            // 获取地面法线
            Vector3 averageGroundNormal = GetAverageGroundNormal();

            // 计算地面法线与世界上方向的夹角
            float groundAngle = Vector3.Angle(averageGroundNormal, Vector3.up);

            return groundAngle;
        }

        /// <summary>
        /// 获取车轮接触点的平均地面法线
        /// </summary>
        private Vector3 GetAverageGroundNormal()
        {
            Vector3 averageNormal = Vector3.up; // 默认为向上
            int groundedWheels = 0;

            // 检查所有车轮
            WheelHit hit;

            if (frontLeftWheel != null && frontLeftWheel.GetGroundHit(out hit))
            {
                averageNormal += hit.normal;
                groundedWheels++;
            }

            if (frontRightWheel != null && frontRightWheel.GetGroundHit(out hit))
            {
                averageNormal += hit.normal;
                groundedWheels++;
            }

            if (rearLeftWheel != null && rearLeftWheel.GetGroundHit(out hit))
            {
                averageNormal += hit.normal;
                groundedWheels++;
            }

            if (rearRightWheel != null && rearRightWheel.GetGroundHit(out hit))
            {
                averageNormal += hit.normal;
                groundedWheels++;
            }

            // 如果有车轮接地，计算平均法线
            if (groundedWheels > 0)
            {
                averageNormal /= (groundedWheels + 1); // +1是因为我们初始化时加了一个Vector3.up
            }

            return averageNormal.normalized;
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

        /// <summary>
        /// 应用下压力，增加高速稳定性
        /// 优化版本：避免高速时车身下沉，更加平滑的下压力曲线
        /// </summary>
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
                if (isNitroActive && vehicleDriveSystem.GetNitroAmount() > 0)
                {
                    // 使用氮气加速系数来计算有效最大速度
                    // 使用较低的系数，减少下压力
                    float effectiveNitroFactor = Mathf.Lerp(1.0f, nitroBoostFactor, 0.7f);
                    maxSpeed *= effectiveNitroFactor;
                }
            }

            // 使用实际最大速度的百分比计算下压力
            float speedPercentage = Mathf.Clamp01(currentSpeed / maxSpeed);

            // 使用更平缓的曲线，减少高速下压力增长
            // 使用平方根曲线，在低速时增长较快，高速时增长缓慢
            float smoothCurve = Mathf.Sqrt(speedPercentage) * 0.8f;

            // 计算下压力，整体减少下压力系数
            // 大幅降低基础系数，避免车身下沉
            float downforce = smoothCurve * (downforceCoefficient * 0.6f) * vehicleRigidbody.mass * 6f;

            // 如果氮气激活，更大幅度减少下压力
            if (isNitroActive)
            {
                downforce *= 0.7f; // 减少30%的下压力
            }

            // 在接近最大速度时大幅减少下压力，避免车身下沉
            // 使用更激进的减少曲线
            if (speedPercentage > 0.7f)
            {
                // 在接近最大速度时减少下压力
                float reductionFactor = (speedPercentage - 0.7f) * 3.33f; // 0-1范围
                downforce *= (1f - reductionFactor * 0.6f); // 最多减少60%
            }

            // 平滑应用下压力，减少抖动
            float deltaTime = Time.fixedDeltaTime;
            float smoothFactor = Mathf.Clamp01(deltaTime * 2.5f); // 减小平滑因子，使变化更缓慢
            downforce = Mathf.Lerp(0, downforce, smoothFactor);

            // 向下施加力 - 使用ForceMode.Force确保力随时间平滑应用
            vehicleRigidbody.AddForce(-transform.up * downforce, ForceMode.Force);

            // 在高速转弯时增加角阻尼减少过度摆动
            if (speedPercentage > 0.3f)
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
                    // 增加角阻尼以稳定高速转弯，但使用更平滑的曲线
                    float angularDamping = turningFactor * speedPercentage * 2.0f;
                    vehicleRigidbody.angularDamping = Mathf.Lerp(0.7f, 2.5f, angularDamping);
                }
                else
                {
                    // 不转弯时恢复默认值，但保持较高的基础角阻尼
                    vehicleRigidbody.angularDamping = 0.7f;
                }
            }
            else
            {
                // 低速时使用较低的角阻尼，提高低速操控性
                vehicleRigidbody.angularDamping = 0.5f;
            }
        }

        /// <summary>
        /// 应用空气阻力
        /// </summary>
        private void ApplyAirResistance()
        {
            if (vehicleRigidbody == null || vehicleRigidbody.linearVelocity.magnitude < 0.1f) return;

            // 阻力与速度平方成正比，方向与速度相反
            Vector3 velocity = vehicleRigidbody.linearVelocity;
            float speedSqr = velocity.sqrMagnitude;
            Vector3 dragForce = -velocity.normalized * speedSqr * dragFactor;

            // 施加阻力
            vehicleRigidbody.AddForce(dragForce, ForceMode.Force);
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

            // 获取当前车速，用于判断是否应该考虑打滑音效
            float currentSpeed = Mathf.Abs(GetForwardSpeed());
            
            // 获取漂移状态
            bool isDriftingState = false;
            if (vehicleDriveSystem != null)
            {
                isDriftingState = vehicleDriveSystem.IsDrifting();
            }
            
            // 车速过低时不应考虑打滑（低于5km/h）
            if (currentSpeed < 5.0f)
            {
                // 如果当前正在播放，则淡出
                if (isPlayingSkidSound)
                {
                    currentSkidVolume = Mathf.Lerp(currentSkidVolume, 0f, Time.deltaTime * 8f);
                    tireSkidAudioSource.volume = currentSkidVolume;
                    
                    if (currentSkidVolume < 0.05f)
                    {
                        tireSkidAudioSource.Stop();
                        isPlayingSkidSound = false;
                    }
                }
                return;
            }

            // 检测任何车轮是否打滑
            float slipSum = 0f;
            int wheelCount = 0;

            if (!isInAir)
            {
                CheckWheelSlip(frontLeftWheel, ref slipSum, ref wheelCount, currentSpeed);
                CheckWheelSlip(frontRightWheel, ref slipSum, ref wheelCount, currentSpeed);
                CheckWheelSlip(rearLeftWheel, ref slipSum, ref wheelCount, currentSpeed);
                CheckWheelSlip(rearRightWheel, ref slipSum, ref wheelCount, currentSpeed);
            }

            // 计算平均打滑值
            float averageSlip = wheelCount > 0 ? slipSum / wheelCount : 0f;

            // 根据速度动态调整打滑阈值：速度越高，阈值越低
            float speedBasedThreshold = Mathf.Lerp(0.4f, 0.2f, Mathf.InverseLerp(5f, 50f, currentSpeed));
            
            // 当打滑值超过阈值时播放声音，或者当系统判断为漂移状态时
            float targetVolume = 0f;
            
            // 优先考虑系统漂移状态，其次考虑实际车轮打滑情况
            if (isDriftingState)
            {
                // 如果系统判断正在漂移，则使用较大的音量
                targetVolume = Mathf.Lerp(0.4f, 0.8f, Mathf.InverseLerp(20f, 80f, currentSpeed));
            }
            else if (averageSlip > speedBasedThreshold && !isInAir && currentSpeed > 15f)
            {
                // 如果不是系统漂移但车轮确实有打滑，且速度足够大，使用基于打滑值的音量
                float slipFactor = Mathf.InverseLerp(speedBasedThreshold, 0.8f, averageSlip);
                float speedFactor = Mathf.InverseLerp(15f, 80f, currentSpeed);
                targetVolume = Mathf.Lerp(0.1f, 0.6f, slipFactor * speedFactor);
            }

            // 平滑过渡音量，较快淡入，较慢淡出
            float transitionSpeed = targetVolume > currentSkidVolume ? 8f : 5f;
            currentSkidVolume = Mathf.Lerp(currentSkidVolume, targetVolume, Time.deltaTime * transitionSpeed);
            tireSkidAudioSource.volume = currentSkidVolume;

            // 管理声音播放状态，提高开始播放的阈值
            if (currentSkidVolume > 0.05f && !tireSkidAudioSource.isPlaying)
            {
                tireSkidAudioSource.Play();
                isPlayingSkidSound = true;
            }
            else if (currentSkidVolume < 0.05f && isPlayingSkidSound)
            {
                tireSkidAudioSource.Stop();
                isPlayingSkidSound = false;
            }
        }

        /// 检查单个车轮的打滑情况
        private void CheckWheelSlip(WheelCollider wheel, ref float slipSum, ref int wheelCount, float currentSpeed)
        {
            if (wheel == null) return;

            WheelHit hit;
            if (wheel.GetGroundHit(out hit))
            {
                // 考虑速度因素
                float speedFactor = Mathf.Clamp01(currentSpeed / 30f); // 30km/h时达到满系数
                
                // 根据速度调整侧向滑动的权重
                float sidewaysWeight = Mathf.Lerp(0.5f, 1.0f, speedFactor);
                float forwardWeight = 0.5f;
                
                // 计算滑动值，侧向滑动权重更高以更好地检测漂移
                float slipValue = (Mathf.Abs(hit.sidewaysSlip) * sidewaysWeight) + 
                                  (Mathf.Abs(hit.forwardSlip) * forwardWeight);
                
                // 根据速度调整滑动值的影响
                slipValue *= speedFactor;
                
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

        /// <summary>
        /// 设置车轮的制动扭矩。
        /// </summary>
        /// <param name="frontTorque">施加到前轮的制动扭矩。</param>
        /// <param name="rearTorque">施加到后轮的制动扭矩。</param>
        public void SetBrakeTorque(float frontTorque, float rearTorque)
        {
            // 直接将扭矩应用到相应的车轮
            // 确保扭矩值为非负数
            frontTorque = Mathf.Max(0f, frontTorque);
            rearTorque = Mathf.Max(0f, rearTorque);

            if (frontLeftWheel != null)
            {
                frontLeftWheel.brakeTorque = frontTorque;
            }
            if (frontRightWheel != null)
            {
                frontRightWheel.brakeTorque = frontTorque;
            }
            if (rearLeftWheel != null)
            {
                rearLeftWheel.brakeTorque = rearTorque;
            }
            if (rearRightWheel != null)
            {
                rearRightWheel.brakeTorque = rearTorque;
            }
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
            return IsVehicleInAir();
        }

        /// 获取是否侧翻
        public bool GetIsFlipped()
        {
            return IsVehicleFlipped();
        }

        /// 获取是否倒置
        public bool GetIsUpsideDown()
        {
            return IsVehicleUpsideDown();
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

        /// <summary>
        /// 设置后轮的侧向刚度因子，用于漂移
        /// </summary>
        /// <param name="stiffnessFactor">刚度乘数因子 (例如 0.5 表示 50% 的原始刚度)</param>
        public void SetRearWheelStiffness(float stiffnessFactor)
        {
            if (rearLeftWheel == null || rearRightWheel == null || originalSidewaysFriction == null || originalSidewaysFriction.Length < 4)
            {
                Debug.LogWarning("【车辆物理】无法设置后轮刚度，缺少车轮或原始摩擦力数据。");
                return;
            }

            // 确保因子在合理范围内
            stiffnessFactor = Mathf.Clamp(stiffnessFactor, 0.05f, 2.0f); // 允许稍微增加刚度，但主要用于降低

            // 获取原始侧向摩擦力曲线
            WheelFrictionCurve originalLeftCurve = originalSidewaysFriction[2];
            WheelFrictionCurve originalRightCurve = originalSidewaysFriction[3];

            // 创建新的侧向摩擦力曲线并修改刚度
            WheelFrictionCurve leftCurve = rearLeftWheel.sidewaysFriction;
            WheelFrictionCurve rightCurve = rearRightWheel.sidewaysFriction;

            leftCurve.stiffness = originalLeftCurve.stiffness * stiffnessFactor;
            rightCurve.stiffness = originalRightCurve.stiffness * stiffnessFactor;

            // 可选：同时调整 extremumValue 和 asymptoteValue 以获得不同的漂移手感
            // leftCurve.extremumValue = originalLeftCurve.extremumValue * stiffnessFactor;
            // leftCurve.asymptoteValue = originalLeftCurve.asymptoteValue * stiffnessFactor;
            // rightCurve.extremumValue = originalRightCurve.extremumValue * stiffnessFactor;
            // rightCurve.asymptoteValue = originalRightCurve.asymptoteValue * stiffnessFactor;

            // 应用修改后的摩擦力曲线
            rearLeftWheel.sidewaysFriction = leftCurve;
            rearRightWheel.sidewaysFriction = rightCurve;

            if (stiffnessFactor < 0.99f)
            {
                // Debug.Log($"【车辆物理】后轮侧向刚度设置为: {stiffnessFactor * 100f:F1}% (左:{leftCurve.stiffness:F2}, 右:{rightCurve.stiffness:F2})");
            }
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

        /// 获取车辆是否在空中
        public bool IsVehicleInAir()
        {
            return isInAir;
        }

        /// 获取车辆是否侧翻
        public bool IsVehicleFlipped()
        {
            return isFlipped;
        }

        /// 获取车辆是否倒置
        public bool IsVehicleUpsideDown()
        {
            return isUpsideDown;
        }

        /// <summary>
        /// 恢复原始的后轮侧向刚度
        /// </summary>
        public void RestoreOriginalRearWheelStiffness()
        {
            if (rearLeftWheel == null || rearRightWheel == null || originalSidewaysFriction == null || originalSidewaysFriction.Length < 4)
            {
                Debug.LogWarning("【车辆物理】无法恢复后轮刚度，缺少车轮或原始摩擦力数据。");
                return;
            }

            // 直接从保存的原始数据恢复
            rearLeftWheel.sidewaysFriction = originalSidewaysFriction[2];
            rearRightWheel.sidewaysFriction = originalSidewaysFriction[3];

            // Debug.Log("【车辆物理】已恢复原始后轮侧向刚度。");
        }

        #endregion
    }
}