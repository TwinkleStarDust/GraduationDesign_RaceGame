using UnityEngine;
using UnityEngine.InputSystem;
using System;

/// <summary>
/// 车辆控制器脚本
/// 负责处理车辆的移动、转向和物理碰撞
/// </summary>
public class VehicleController : MonoBehaviour
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

    [Header("车辆基本设置")]
    [Tooltip("驱动类型")]
    [SerializeField] private DriveType driveType = DriveType.FrontWheelDrive;

    [Tooltip("前轮驱动力分配 (0-1)")]
    [Range(0, 1)]
    [SerializeField] private float frontWheelDriveFactor = 1.0f;

    [Tooltip("后轮驱动力分配 (0-1)")]
    [Range(0, 1)]
    [SerializeField] private float rearWheelDriveFactor = 0.0f;

    [Header("车辆参数")]
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

    [Tooltip("重心高度")]
    [SerializeField] private float centerOfMassHeight = -0.5f;

    [Header("漂移设置")]
    [Tooltip("漂移强度")]
    [Range(0, 1)]
    [SerializeField] private float driftFactor = 0.5f;

    [Tooltip("漂移时后轮侧向摩擦力减少系数")]
    [Range(0, 1)]
    [SerializeField] private float driftSlipFactor = 0.4f;

    [Tooltip("漂移恢复速度")]
    [SerializeField] private float driftRecoverySpeed = 2.0f;

    [Tooltip("漂移时转向灵敏度增加")]
    [Range(1, 2)]
    [SerializeField] private float driftSteeringFactor = 1.3f;

    [Header("车轮碰撞器")]
    [Tooltip("前左轮")]
    [SerializeField] private WheelCollider frontLeftWheel;

    [Tooltip("前右轮")]
    [SerializeField] private WheelCollider frontRightWheel;

    [Tooltip("后左轮")]
    [SerializeField] private WheelCollider rearLeftWheel;

    [Tooltip("后右轮")]
    [SerializeField] private WheelCollider rearRightWheel;

    [Header("车轮模型")]
    [Tooltip("前左轮模型")]
    [SerializeField] private Transform frontLeftWheelTransform;

    [Tooltip("前右轮模型")]
    [SerializeField] private Transform frontRightWheelTransform;

    [Tooltip("后左轮模型")]
    [SerializeField] private Transform rearLeftWheelTransform;

    [Tooltip("后右轮模型")]
    [SerializeField] private Transform rearRightWheelTransform;

    [Header("音效")]
    [Tooltip("引擎音效")]
    [SerializeField] private AudioSource engineSound;

    [Tooltip("漂移音效")]
    [SerializeField] private AudioSource driftSound;

    [Tooltip("最小音调")]
    [SerializeField] private float minPitch = 0.5f;

    [Tooltip("最大音调")]
    [SerializeField] private float maxPitch = 2.0f;

    [Header("车轮视觉效果")]
    [Tooltip("车轮位置平滑度")]
    [Range(5, 30)]
    [SerializeField] private float wheelPositionSmoothing = 15f;

    [Tooltip("车轮旋转平滑度")]
    [Range(5, 30)]
    [SerializeField] private float wheelRotationSmoothing = 15f;

    [Header("车辆翻转设置")]
    [Tooltip("车辆翻转力矩")]
    [SerializeField] private float flipTorque = 20000f;

    [Tooltip("检测侧翻的角度阈值")]
    [SerializeField] private float flipDetectionAngle = 45f;

    [Tooltip("检测倒置的角度阈值")]
    [SerializeField] private float upsideDownDetectionAngle = 120f;

    [Tooltip("翻转恢复速度")]
    [SerializeField] private float flipRecoverySpeed = 2f;

    [Header("空中控制设置")]
    [Tooltip("空中翻滚力矩")]
    [SerializeField] private float airRollTorque = 100000f;

    [Tooltip("空中俯仰力矩")]
    [SerializeField] private float airPitchTorque = 80000f;

    [Tooltip("检测空中状态的时间阈值(秒)")]
    [SerializeField] private float airTimeThreshold = 0.3f;

    // 私有变量
    private Rigidbody vehicleRigidbody;
    private float currentSpeed;
    private float currentSteeringAngle;
    private float throttleInput;
    private float steeringInput;
    private float brakeInput;
    private bool isHandbrakeActive;
    private bool isDrifting = false;
    private float currentDriftFactor = 0f;

    // 保存原始摩擦力设置
    private WheelFrictionCurve originalRearWheelForwardFriction;
    private WheelFrictionCurve originalRearWheelSidewaysFriction;

    // 将km/h转换为m/s的系数
    private const float KMH_TO_MS = 0.2778f;

    // 添加检测车辆是否卡住的方法
    private float lastPositionCheckTime = 0f;
    private Vector3 lastPosition;
    private bool isStuck = false;

    // 添加车轮旋转相关变量
    private float[] wheelRotationAngles = new float[4];

    private bool isFlipped = false;
    private bool isUpsideDown = false;
    private float currentFlipTorque = 0f;
    private bool isInAir = false;
    private float airTime = 0f;
    private bool showFlipPrompt = false;
    private bool showAirControlPrompt = false;

    // 传送事件
    public event Action OnBeforeTeleport;
    public event Action OnAfterTeleport;

    /// <summary>
    /// 初始化组件和设置
    /// </summary>
    private void Awake()
    {
        vehicleRigidbody = GetComponent<Rigidbody>();

        // 设置车辆重心
        if (vehicleRigidbody != null)
        {
            vehicleRigidbody.centerOfMass = new Vector3(0, centerOfMassHeight, 0);

            // 确保车辆在静止时不会旋转，但在空中时能够自由旋转
            vehicleRigidbody.inertiaTensor = new Vector3(1000, 1000, 1000);

            // 设置合适的阻力
            vehicleRigidbody.linearDamping = 0.1f;
            vehicleRigidbody.angularDamping = 0.05f;

            // 确保使用连续动态碰撞检测，避免高速穿透
            vehicleRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
        else
        {
            Debug.LogError("车辆缺少Rigidbody组件！");
        }

        // 检查车轮碰撞器是否已分配
        if (frontLeftWheel == null || frontRightWheel == null ||
            rearLeftWheel == null || rearRightWheel == null)
        {
            Debug.LogWarning("车轮碰撞器未完全分配！");
        }

        // 确保车轮对齐
        if (frontLeftWheel != null && frontRightWheel != null)
        {
            // 重置前轮转向角度
            frontLeftWheel.steerAngle = 0;
            frontRightWheel.steerAngle = 0;
        }

        // 根据驱动类型设置驱动力分配
        SetupDriveTypeFactors();

        // 保存原始摩擦力设置
        if (rearLeftWheel != null)
        {
            originalRearWheelForwardFriction = rearLeftWheel.forwardFriction;
            originalRearWheelSidewaysFriction = rearLeftWheel.sidewaysFriction;
        }

        // 设置车轮摩擦力
        SetupWheelFriction();
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
    /// 设置车轮摩擦力
    /// </summary>
    private void SetupWheelFriction()
    {
        // 设置前轮摩擦力
        SetWheelFriction(frontLeftWheel, 1.0f, 1.0f);
        SetWheelFriction(frontRightWheel, 1.0f, 1.0f);

        // 设置后轮摩擦力
        SetWheelFriction(rearLeftWheel, 1.0f, 1.0f);
        SetWheelFriction(rearRightWheel, 1.0f, 1.0f);
    }

    /// <summary>
    /// 设置单个车轮的摩擦力
    /// </summary>
    private void SetWheelFriction(WheelCollider wheel, float forwardStiffness, float sidewaysStiffness)
    {
        if (wheel == null) return;

        // 设置前向摩擦
        WheelFrictionCurve forwardFriction = wheel.forwardFriction;
        forwardFriction.stiffness = forwardStiffness;
        wheel.forwardFriction = forwardFriction;

        // 设置侧向摩擦
        WheelFrictionCurve sidewaysFriction = wheel.sidewaysFriction;
        sidewaysFriction.stiffness = sidewaysStiffness;
        wheel.sidewaysFriction = sidewaysFriction;
    }

    /// <summary>
    /// 处理输入和更新车辆状态
    /// </summary>
    private void Update()
    {
        // 更新当前速度 (km/h)
        currentSpeed = vehicleRigidbody.linearVelocity.magnitude / KMH_TO_MS;

        // 检测车辆是否卡住
        DetectIfStuck();

        // 更新引擎声音
        UpdateEngineSound();

        // 更新漂移声音
        UpdateDriftSound();

        // 更新空中状态
        UpdateAirState();
    }

    /// <summary>
    /// 处理物理相关的更新
    /// </summary>
    private void FixedUpdate()
    {
        // 检查车辆状态
        CheckVehicleState();

        // 应用转向
        ApplySteering();

        // 应用驱动力
        ApplyDrive();

        // 处理漂移
        HandleDrifting();

        // 更新车轮模型
        UpdateWheelModels();

        // 处理车辆翻转
        HandleVehicleFlip();

        // 处理空中控制
        HandleAirControl();

        // 防翻滚保护 (仅在非侧翻和非空中状态下启用)
        if (!isFlipped && !isUpsideDown && !isInAir)
        {
            StabilizeVehicle();
        }

        // 限制最大速度
        LimitMaxSpeed();
    }

    /// <summary>
    /// 处理加速输入
    /// </summary>
    public void OnThrottle(InputValue value)
    {
        throttleInput = value.Get<float>();
    }

    /// <summary>
    /// 处理转向输入
    /// </summary>
    public void OnSteer(InputValue value)
    {
        steeringInput = value.Get<float>();
    }

    /// <summary>
    /// 处理制动输入
    /// </summary>
    public void OnBrake(InputValue value)
    {
        brakeInput = value.Get<float>();
    }

    /// <summary>
    /// 处理手刹输入
    /// </summary>
    public void OnHandbrake(InputValue value)
    {
        isHandbrakeActive = value.isPressed;
    }

    /// <summary>
    /// 应用转向到前轮
    /// </summary>
    private void ApplySteering()
    {
        // 确保在车辆几乎停止时不应用转向
        if (Mathf.Abs(currentSpeed) < 2.0f)
        {
            // 逐渐将转向角度归零
            currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, 0f, Time.fixedDeltaTime * steeringSpeed * 2.0f);

            // 应用转向角度到前轮
            frontLeftWheel.steerAngle = currentSteeringAngle;
            frontRightWheel.steerAngle = currentSteeringAngle;
            return;
        }

        // 根据速度调整转向灵敏度
        float speedFactor = Mathf.Clamp01(currentSpeed / 50.0f);
        float steeringFactor = Mathf.Lerp(1.0f, 0.5f, speedFactor);

        // 漂移时增加转向灵敏度
        if (isDrifting)
        {
            steeringFactor *= driftSteeringFactor;
        }

        // 计算目标转向角度
        float targetSteeringAngle = steeringInput * maxSteeringAngle * steeringFactor;

        // 平滑转向
        currentSteeringAngle = Mathf.Lerp(
            currentSteeringAngle,
            targetSteeringAngle,
            Time.fixedDeltaTime * steeringSpeed
        );

        // 应用转向角度到前轮
        frontLeftWheel.steerAngle = currentSteeringAngle;
        frontRightWheel.steerAngle = currentSteeringAngle;
    }

    /// <summary>
    /// 应用驱动力和制动力到车轮
    /// </summary>
    private void ApplyDrive()
    {
        // 获取本地空间中的速度
        Vector3 localVelocity = transform.InverseTransformDirection(vehicleRigidbody.linearVelocity);
        float forwardSpeed = localVelocity.z;

        // 清除所有车轮的驱动力和制动力
        frontLeftWheel.motorTorque = 0;
        frontRightWheel.motorTorque = 0;
        rearLeftWheel.motorTorque = 0;
        rearRightWheel.motorTorque = 0;
        frontLeftWheel.brakeTorque = 0;
        frontRightWheel.brakeTorque = 0;
        rearLeftWheel.brakeTorque = 0;
        rearRightWheel.brakeTorque = 0;

        // 手刹逻辑
        if (isHandbrakeActive)
        {
            float handBrakeTorque = 10000f;
            rearLeftWheel.brakeTorque = handBrakeTorque;
            rearRightWheel.brakeTorque = handBrakeTorque;
            return;
        }

        // 处理油门输入 (W键)
        if (throttleInput > 0.1f)
        {
            // 如果车辆正在向后移动，先刹车
            if (forwardSpeed < -0.5f)
            {
                float brakeTorque = brakeForce * 200;
                ApplyBrakesToAllWheels(brakeTorque);
            }
            // 否则加速前进
            else
            {
                float motorTorque = acceleration * 100 * throttleInput;

                // 根据驱动类型分配驱动力
                if (frontWheelDriveFactor > 0)
                {
                    frontLeftWheel.motorTorque = motorTorque * frontWheelDriveFactor;
                    frontRightWheel.motorTorque = motorTorque * frontWheelDriveFactor;
                }

                if (rearWheelDriveFactor > 0)
                {
                    rearLeftWheel.motorTorque = motorTorque * rearWheelDriveFactor;
                    rearRightWheel.motorTorque = motorTorque * rearWheelDriveFactor;
                }
            }
        }
        // 处理刹车/倒车输入 (S键)
        else if (brakeInput > 0.1f)
        {
            // 如果车辆正在向前移动，先刹车
            if (forwardSpeed > 0.5f)
            {
                float brakeTorque = brakeForce * 200;
                ApplyBrakesToAllWheels(brakeTorque);
            }
            // 如果车辆几乎停止或向后移动，则倒车
            else
            {
                // 应用倒车驱动力
                float motorTorque = -acceleration * 100 * brakeInput;

                // 根据驱动类型分配驱动力
                if (frontWheelDriveFactor > 0)
                {
                    frontLeftWheel.motorTorque = motorTorque * frontWheelDriveFactor;
                    frontRightWheel.motorTorque = motorTorque * frontWheelDriveFactor;
                }

                if (rearWheelDriveFactor > 0)
                {
                    rearLeftWheel.motorTorque = motorTorque * rearWheelDriveFactor;
                    rearRightWheel.motorTorque = motorTorque * rearWheelDriveFactor;
                }
            }
        }
        // 没有输入时，应用小的制动力防止溜车
        else if (Mathf.Abs(forwardSpeed) < 3.0f)
        {
            float brakeTorque = 500f;
            ApplyBrakesToAllWheels(brakeTorque);
        }
        // 高速滑行时应用小的制动力模拟发动机制动
        else
        {
            float brakeTorque = 100f;
            ApplyBrakesToAllWheels(brakeTorque);
        }
    }

    /// <summary>
    /// 处理漂移
    /// </summary>
    private void HandleDrifting()
    {
        // 获取本地空间中的速度
        Vector3 localVelocity = transform.InverseTransformDirection(vehicleRigidbody.linearVelocity);

        // 计算侧向速度
        float lateralSpeed = Mathf.Abs(localVelocity.x);

        // 检测是否应该漂移
        bool shouldDrift = isHandbrakeActive && currentSpeed > 30.0f && Mathf.Abs(steeringInput) > 0.5f;

        // 更新漂移状态
        if (shouldDrift)
        {
            // 逐渐增加漂移因子
            currentDriftFactor = Mathf.Lerp(currentDriftFactor, driftFactor, Time.fixedDeltaTime * 5.0f);
            isDrifting = currentDriftFactor > 0.1f;

            // 减少后轮侧向摩擦力以增加漂移效果
            if (isDrifting)
            {
                AdjustRearWheelFriction(1.0f, driftSlipFactor);
            }
        }
        else
        {
            // 逐渐恢复正常摩擦力
            currentDriftFactor = Mathf.Lerp(currentDriftFactor, 0f, Time.fixedDeltaTime * driftRecoverySpeed);
            isDrifting = currentDriftFactor > 0.1f;

            // 恢复后轮摩擦力
            if (!isDrifting)
            {
                AdjustRearWheelFriction(1.0f, 1.0f);
            }
            else
            {
                // 平滑过渡
                float sidewaysStiffness = Mathf.Lerp(driftSlipFactor, 1.0f, 1.0f - currentDriftFactor);
                AdjustRearWheelFriction(1.0f, sidewaysStiffness);
            }
        }

        // 如果正在漂移，添加一些侧向力以增强漂移效果
        if (isDrifting && currentSpeed > 20.0f)
        {
            // 计算漂移力方向
            Vector3 driftForce = transform.right * steeringInput * currentDriftFactor * 2000f;

            // 应用漂移力
            vehicleRigidbody.AddForce(driftForce * Time.fixedDeltaTime);
        }
    }

    /// <summary>
    /// 调整后轮摩擦力
    /// </summary>
    private void AdjustRearWheelFriction(float forwardStiffness, float sidewaysStiffness)
    {
        if (rearLeftWheel == null || rearRightWheel == null) return;

        // 调整后左轮摩擦力
        WheelFrictionCurve rearLeftForward = rearLeftWheel.forwardFriction;
        rearLeftForward.stiffness = originalRearWheelForwardFriction.stiffness * forwardStiffness;
        rearLeftWheel.forwardFriction = rearLeftForward;

        WheelFrictionCurve rearLeftSideways = rearLeftWheel.sidewaysFriction;
        rearLeftSideways.stiffness = originalRearWheelSidewaysFriction.stiffness * sidewaysStiffness;
        rearLeftWheel.sidewaysFriction = rearLeftSideways;

        // 调整后右轮摩擦力
        WheelFrictionCurve rearRightForward = rearRightWheel.forwardFriction;
        rearRightForward.stiffness = originalRearWheelForwardFriction.stiffness * forwardStiffness;
        rearRightWheel.forwardFriction = rearRightForward;

        WheelFrictionCurve rearRightSideways = rearRightWheel.sidewaysFriction;
        rearRightSideways.stiffness = originalRearWheelSidewaysFriction.stiffness * sidewaysStiffness;
        rearRightWheel.sidewaysFriction = rearRightSideways;
    }

    // 辅助方法：应用制动力到所有车轮
    private void ApplyBrakesToAllWheels(float brakeTorque)
    {
        frontLeftWheel.brakeTorque = brakeTorque;
        frontRightWheel.brakeTorque = brakeTorque;
        rearLeftWheel.brakeTorque = brakeTorque;
        rearRightWheel.brakeTorque = brakeTorque;
    }

    /// <summary>
    /// 更新车轮模型的位置和旋转
    /// </summary>
    private void UpdateWheelModels()
    {
        UpdateWheelModel(frontLeftWheel, frontLeftWheelTransform, 0);
        UpdateWheelModel(frontRightWheel, frontRightWheelTransform, 1);
        UpdateWheelModel(rearLeftWheel, rearLeftWheelTransform, 2);
        UpdateWheelModel(rearRightWheel, rearRightWheelTransform, 3);
    }

    /// <summary>
    /// 更新单个车轮模型
    /// </summary>
    private void UpdateWheelModel(WheelCollider collider, Transform wheelTransform, int wheelIndex)
    {
        if (collider == null || wheelTransform == null) return;

        // 获取车轮位置和旋转
        Vector3 position;
        Quaternion rotation;
        collider.GetWorldPose(out position, out rotation);

        // 平滑过渡位置
        wheelTransform.position = Vector3.Lerp(wheelTransform.position, position, Time.deltaTime * wheelPositionSmoothing);

        // 处理车轮旋转
        // 1. 获取车轮的RPM并计算旋转增量
        float rpm = collider.rpm;
        float rotationDelta = rpm * 6f * Time.deltaTime; // 6 = 360/60 (一分钟一圈)

        // 2. 累积旋转角度
        wheelRotationAngles[wheelIndex] += rotationDelta;

        // 3. 创建最终旋转
        // 先应用车轮的基本方向（由WheelCollider决定，包含转向和悬挂）
        Quaternion baseRotation = rotation;

        // 然后应用车轮自身的旋转（绕X轴）
        Quaternion spinRotation = Quaternion.Euler(wheelRotationAngles[wheelIndex], 0, 0);

        // 组合旋转
        Quaternion finalRotation = baseRotation * spinRotation;

        // 4. 平滑过渡旋转
        wheelTransform.rotation = Quaternion.Slerp(wheelTransform.rotation, finalRotation, Time.deltaTime * wheelRotationSmoothing);
    }

    /// <summary>
    /// 更新引擎声音
    /// </summary>
    private void UpdateEngineSound()
    {
        if (engineSound == null) return;

        // 根据速度和油门调整音调
        float speedFactor = Mathf.Clamp01(currentSpeed / maxSpeed);
        float throttleFactor = Mathf.Clamp01(Mathf.Abs(throttleInput));
        float targetPitch = Mathf.Lerp(minPitch, maxPitch, Mathf.Max(speedFactor, throttleFactor * 0.5f));

        // 平滑过渡音调
        engineSound.pitch = Mathf.Lerp(engineSound.pitch, targetPitch, Time.deltaTime * 2.0f);
    }

    /// <summary>
    /// 更新漂移声音
    /// </summary>
    private void UpdateDriftSound()
    {
        if (driftSound == null) return;

        // 根据漂移状态调整音量
        if (isDrifting)
        {
            if (!driftSound.isPlaying)
            {
                driftSound.Play();
            }

            // 根据漂移强度和速度调整音量
            float driftVolume = Mathf.Clamp01(currentDriftFactor * (currentSpeed / 50.0f));
            driftSound.volume = driftVolume;
        }
        else
        {
            // 平滑淡出
            driftSound.volume = Mathf.Lerp(driftSound.volume, 0f, Time.deltaTime * 5.0f);

            // 当音量足够小时停止播放
            if (driftSound.volume < 0.05f && driftSound.isPlaying)
            {
                driftSound.Stop();
            }
        }
    }

    /// <summary>
    /// 获取当前车速
    /// </summary>
    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }

    /// <summary>
    /// 获取当前驱动类型
    /// </summary>
    public DriveType GetDriveType()
    {
        return driveType;
    }

    /// <summary>
    /// 获取是否正在漂移
    /// </summary>
    public bool IsDrifting()
    {
        return isDrifting;
    }

    /// <summary>
    /// 获取当前漂移强度
    /// </summary>
    public float GetDriftFactor()
    {
        return currentDriftFactor;
    }

    /// <summary>
    /// 重置车辆
    /// </summary>
    public void ResetVehicle()
    {
        // 重置速度和角速度
        vehicleRigidbody.linearVelocity = Vector3.zero;
        vehicleRigidbody.angularVelocity = Vector3.zero;

        // 重置位置和旋转
        transform.position = new Vector3(transform.position.x, transform.position.y + 1.0f, transform.position.z);
        transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
    }

    // 添加车辆稳定方法
    private void StabilizeVehicle()
    {
        // 如果车辆开始倾斜过多，施加修正力
        if (transform.up.y < 0.8f)
        {
            // 计算修正力的方向
            Vector3 stabilizationTorque = Vector3.Cross(transform.up, Vector3.up);
            stabilizationTorque *= 10000f; // 修正力大小

            // 应用修正力
            vehicleRigidbody.AddTorque(stabilizationTorque * Time.fixedDeltaTime);
        }

        // 如果车辆几乎停止，确保它不会继续滚动
        if (currentSpeed < 3.0f && !isHandbrakeActive && Mathf.Abs(throttleInput) < 0.1f && Mathf.Abs(brakeInput) < 0.1f)
        {
            // 应用小的制动力
            float stabilizingBrakeTorque = 200f;
            ApplyBrakesToAllWheels(stabilizingBrakeTorque);
        }
    }

    // 添加检测车辆是否卡住的方法
    private void DetectIfStuck()
    {
        // 每2秒检查一次
        if (Time.time - lastPositionCheckTime > 2f)
        {
            // 如果有油门输入但车辆几乎没有移动
            if (Mathf.Abs(throttleInput) > 0.5f &&
                Vector3.Distance(transform.position, lastPosition) < 0.5f &&
                currentSpeed < 5f)
            {
                isStuck = true;

                // 应用小的向上力帮助车辆脱困
                vehicleRigidbody.AddForce(Vector3.up * 5000f);
            }
            else
            {
                isStuck = false;
            }

            lastPosition = transform.position;
            lastPositionCheckTime = Time.time;
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

        // 将最大速度从km/h转换为m/s
        float maxSpeedMS = maxSpeed * KMH_TO_MS;
        float maxReverseSpeedMS = maxReverseSpeed * KMH_TO_MS;

        // 检查是否超过最大速度
        if (currentSpeedMS > maxSpeedMS)
        {
            // 获取速度方向
            Vector3 velocityDirection = vehicleRigidbody.linearVelocity.normalized;

            // 设置为最大允许速度
            vehicleRigidbody.linearVelocity = velocityDirection * maxSpeedMS;
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
    /// 检查车辆状态
    /// </summary>
    private void CheckVehicleState()
    {
        // 计算车辆与地面的角度
        float angle = Vector3.Angle(transform.up, Vector3.up);

        // 检查是否侧翻
        bool wasFlipped = isFlipped;
        isFlipped = angle > flipDetectionAngle && angle < upsideDownDetectionAngle;

        // 检查是否倒置
        bool wasUpsideDown = isUpsideDown;
        isUpsideDown = angle > upsideDownDetectionAngle;

        // 更新UI提示状态
        showFlipPrompt = isFlipped || isUpsideDown;

        // 如果刚刚进入侧翻状态，禁用所有车轮的驱动力
        if (isFlipped && !wasFlipped)
        {
            DisableWheelDrive();
        }
        // 如果刚刚进入倒置状态，禁用所有车轮的驱动力
        else if (isUpsideDown && !wasUpsideDown)
        {
            DisableWheelDrive();
        }
        // 如果刚刚恢复正常状态，恢复车轮驱动力
        else if (!isFlipped && !isUpsideDown && (wasFlipped || wasUpsideDown))
        {
            SetupDriveTypeFactors();
        }
    }

    /// <summary>
    /// 禁用所有车轮的驱动力
    /// </summary>
    private void DisableWheelDrive()
    {
        frontLeftWheel.motorTorque = 0;
        frontRightWheel.motorTorque = 0;
        rearLeftWheel.motorTorque = 0;
        rearRightWheel.motorTorque = 0;
    }

    /// <summary>
    /// 处理车辆翻转
    /// </summary>
    private void HandleVehicleFlip()
    {
        // 如果车辆没有侧翻或倒置，不需要处理
        if (!isFlipped && !isUpsideDown) return;

        // 获取当前速度
        float currentSpeedMS = vehicleRigidbody.linearVelocity.magnitude;

        // 如果速度过高，不允许翻转（但允许更高的速度阈值）
        if (currentSpeedMS > 8f) return;

        // 确定车辆的翻转方向
        Vector3 localRight = Vector3.Cross(transform.up, Vector3.up);
        float rightDot = Vector3.Dot(transform.right, localRight);

        // 确定车辆是向左侧翻还是向右侧翻
        bool isFlippedToRight = rightDot < 0; // 车辆右侧着地（向左侧翻）
        bool isFlippedToLeft = rightDot > 0;  // 车辆左侧着地（向右侧翻）

        // 根据车辆翻转方向调整输入逻辑
        float adjustedInput = 0f;

        if (isFlippedToRight && steeringInput < 0) // 右侧着地，按A键（向左）
        {
            adjustedInput = -steeringInput; // 反转输入，使A键产生向右的力
        }
        else if (isFlippedToLeft && steeringInput > 0) // 左侧着地，按D键（向右）
        {
            adjustedInput = -steeringInput; // 反转输入，使D键产生向左的力
        }
        else
        {
            adjustedInput = steeringInput; // 其他情况保持原输入
        }

        // 计算目标翻转力矩
        float targetFlipTorque = 0f;
        if (adjustedInput != 0)
        {
            targetFlipTorque = adjustedInput * flipTorque;
        }

        // 平滑过渡翻转力矩
        currentFlipTorque = Mathf.Lerp(currentFlipTorque, targetFlipTorque, Time.fixedDeltaTime * flipRecoverySpeed);

        // 应用翻转力矩
        if (Mathf.Abs(currentFlipTorque) > 0.1f)
        {
            // 根据车辆状态选择翻转轴
            Vector3 flipAxis;

            if (isUpsideDown)
            {
                // 倒置时，使用车辆的右方向作为翻转轴
                flipAxis = transform.right;
            }
            else
            {
                // 侧翻时，使用车辆的前进方向作为翻转轴
                flipAxis = transform.forward;
            }

            // 应用更强的力矩，并确保它能克服车辆的惯性
            vehicleRigidbody.AddTorque(flipAxis * currentFlipTorque * Time.fixedDeltaTime, ForceMode.Impulse);
        }

        // 添加调试信息
        if (Mathf.Abs(steeringInput) > 0.1f)
        {
            Debug.Log($"翻转状态: 向右侧翻={isFlippedToRight}, 向左侧翻={isFlippedToLeft}, " +
                      $"原始输入={steeringInput}, 调整后输入={adjustedInput}, 力矩={currentFlipTorque}");
        }
    }

    /// <summary>
    /// 更新空中状态
    /// </summary>
    private void UpdateAirState()
    {
        // 检查车轮接触地面的情况
        int groundedWheelsCount = CountGroundedWheels();
        bool anyWheelGrounded = groundedWheelsCount > 0;

        // 获取车辆垂直速度
        Vector3 localVelocity = transform.InverseTransformDirection(vehicleRigidbody.linearVelocity);
        float verticalSpeed = Mathf.Abs(localVelocity.y);

        // 如果没有车轮接触地面，增加空中时间
        if (!anyWheelGrounded)
        {
            airTime += Time.deltaTime;

            // 只有当空中时间超过阈值且垂直速度足够大时，才判定为空中状态
            if (airTime > airTimeThreshold && verticalSpeed > 1.5f)
            {
                if (!isInAir)
                {
                    Debug.Log($"车辆进入空中状态: 空中时间={airTime:F2}秒, 垂直速度={verticalSpeed:F2}m/s");
                }
                isInAir = true;
                showAirControlPrompt = true;
            }
        }
        else
        {
            // 如果有车轮接地，但不是全部车轮，且车辆正在经过减速带（垂直速度较小）
            if (groundedWheelsCount < 4 && verticalSpeed < 3.0f)
            {
                // 减少空中时间，但不立即重置，以平滑过渡
                airTime = Mathf.Max(0, airTime - Time.deltaTime * 2);

                // 如果空中时间低于阈值的一半，不再判定为空中状态
                if (airTime < airTimeThreshold * 0.5f && isInAir)
                {
                    Debug.Log($"车辆部分着地: 接地车轮数={groundedWheelsCount}, 空中时间={airTime:F2}秒");
                    isInAir = false;
                    showAirControlPrompt = false;
                }
            }
            else
            {
                // 全部车轮接地或垂直速度较大，立即重置空中状态
                if (isInAir)
                {
                    Debug.Log($"车辆完全着陆: 接地车轮数={groundedWheelsCount}");
                }
                airTime = 0f;
                isInAir = false;
                showAirControlPrompt = false;
            }
        }
    }

    /// <summary>
    /// 计算接地的车轮数量
    /// </summary>
    private int CountGroundedWheels()
    {
        int count = 0;

        // 检查每个车轮是否接触地面
        if (frontLeftWheel != null && IsWheelGrounded(frontLeftWheel)) count++;
        if (frontRightWheel != null && IsWheelGrounded(frontRightWheel)) count++;
        if (rearLeftWheel != null && IsWheelGrounded(rearLeftWheel)) count++;
        if (rearRightWheel != null && IsWheelGrounded(rearRightWheel)) count++;

        return count;
    }

    /// <summary>
    /// 检查单个车轮是否真正接地
    /// </summary>
    private bool IsWheelGrounded(WheelCollider wheel)
    {
        if (wheel == null) return false;

        // 首先检查WheelCollider的isGrounded属性
        if (!wheel.isGrounded) return false;

        // 获取车轮接触点信息
        WheelHit hit;
        if (!wheel.GetGroundHit(out hit)) return false;

        // 检查接触力是否足够大（过滤轻微接触）
        if (hit.force < 500) return false;

        // 获取车轮世界位置
        Vector3 wheelPosition;
        Quaternion wheelRotation;
        wheel.GetWorldPose(out wheelPosition, out wheelRotation);

        // 从车轮向下发射射线，检查是否真正接触地面
        RaycastHit rayHit;
        float rayDistance = wheel.suspensionDistance + wheel.radius + 0.2f; // 稍微增加一点距离
        if (Physics.Raycast(wheelPosition, -transform.up, out rayHit, rayDistance))
        {
            // 如果射线检测到的距离合理，认为车轮确实接地
            return true;
        }

        return false;
    }

    /// <summary>
    /// 检查是否有任何车轮接触地面
    /// </summary>
    private bool IsAnyWheelGrounded()
    {
        return CountGroundedWheels() > 0;
    }

    /// <summary>
    /// 获取是否显示翻转提示
    /// </summary>
    public bool ShouldShowFlipPrompt()
    {
        return showFlipPrompt;
    }

    /// <summary>
    /// 获取是否显示空中控制提示
    /// </summary>
    public bool ShouldShowAirControlPrompt()
    {
        return showAirControlPrompt;
    }

    /// <summary>
    /// 获取是否在空中
    /// </summary>
    public bool IsInAir()
    {
        return isInAir;
    }

    /// <summary>
    /// 在传送前调用
    /// </summary>
    public void PrepareForTeleport()
    {
        // 触发传送前事件
        OnBeforeTeleport?.Invoke();

        // 重置车辆状态
        ResetVehicleState();
    }

    /// <summary>
    /// 在传送后调用
    /// </summary>
    public void FinishTeleport()
    {
        // 重置车轮状态
        ResetWheelState();

        // 触发传送后事件
        OnAfterTeleport?.Invoke();
    }

    /// <summary>
    /// 重置车辆状态
    /// </summary>
    private void ResetVehicleState()
    {
        // 重置输入
        throttleInput = 0f;
        brakeInput = 0f;
        steeringInput = 0f;
        currentSteeringAngle = 0f;

        // 重置物理状态
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    /// <summary>
    /// 重置车轮状态
    /// </summary>
    private void ResetWheelState()
    {
        // 重置所有车轮的状态
        if (frontLeftWheel != null) ResetWheelCollider(frontLeftWheel);
        if (frontRightWheel != null) ResetWheelCollider(frontRightWheel);
        if (rearLeftWheel != null) ResetWheelCollider(rearLeftWheel);
        if (rearRightWheel != null) ResetWheelCollider(rearRightWheel);

        // 更新车轮模型
        UpdateWheelModels();
    }

    /// <summary>
    /// 重置单个车轮碰撞器状态
    /// </summary>
    private void ResetWheelCollider(WheelCollider wheel)
    {
        wheel.motorTorque = 0f;
        wheel.brakeTorque = 0f;
        wheel.steerAngle = 0f;
    }

    /// <summary>
    /// 处理空中控制
    /// </summary>
    private void HandleAirControl()
    {
        // 如果不在空中，不处理空中控制
        if (!isInAir) return;

        // 获取输入
        float pitchInput = throttleInput - brakeInput; // W/S控制俯仰
        float rollInput = steeringInput; // A/D控制翻滚

        // 输出调试信息
        if (Mathf.Abs(rollInput) > 0.1f || Mathf.Abs(pitchInput) > 0.1f)
        {
            Debug.Log($"空中控制: 翻滚输入={rollInput}, 俯仰输入={pitchInput}, 是否在空中={isInAir}");
        }

        // 计算当前角速度
        Vector3 angularVelocity = vehicleRigidbody.angularVelocity;
        float currentRollSpeed = Vector3.Dot(angularVelocity, transform.forward);
        float currentPitchSpeed = Vector3.Dot(angularVelocity, transform.right);

        // 限制最大角速度，防止旋转过快
        float maxAngularSpeed = 3.0f;
        bool canApplyRoll = Mathf.Abs(currentRollSpeed) < maxAngularSpeed;
        bool canApplyPitch = Mathf.Abs(currentPitchSpeed) < maxAngularSpeed;

        // 应用俯仰控制（前后翻转）
        if (Mathf.Abs(pitchInput) > 0.1f && canApplyPitch)
        {
            // 使用更强的力矩
            Vector3 pitchTorque = transform.right * pitchInput * airPitchTorque;

            // 使用Impulse模式以获得更强的瞬时效果
            vehicleRigidbody.AddTorque(pitchTorque * Time.fixedDeltaTime, ForceMode.Impulse);

            // 添加额外的向上力以保持高度
            if (Mathf.Abs(pitchInput) > 0.5f)
            {
                vehicleRigidbody.AddForce(Vector3.up * 5000f * Time.fixedDeltaTime, ForceMode.Force);
            }
        }
        else
        {
            // 当没有输入时，添加阻尼以减缓旋转
            if (Mathf.Abs(currentPitchSpeed) > 0.5f)
            {
                // 计算反向阻尼力矩
                Vector3 pitchDamping = -transform.right * Mathf.Sign(currentPitchSpeed) *
                                      Mathf.Min(Mathf.Abs(currentPitchSpeed) * 0.5f, 1.0f) * airPitchTorque * 0.2f;

                // 应用阻尼力矩
                vehicleRigidbody.AddTorque(pitchDamping * Time.fixedDeltaTime, ForceMode.Force);
            }
        }

        // 应用翻滚控制（左右翻转）
        if (Mathf.Abs(rollInput) > 0.1f && canApplyRoll)
        {
            // 使用更强的力矩
            Vector3 rollTorque = transform.forward * rollInput * airRollTorque;

            // 使用Impulse模式以获得更强的瞬时效果
            vehicleRigidbody.AddTorque(rollTorque * Time.fixedDeltaTime, ForceMode.Impulse);

            // 添加额外的向上力以保持高度
            if (Mathf.Abs(rollInput) > 0.5f)
            {
                vehicleRigidbody.AddForce(Vector3.up * 5000f * Time.fixedDeltaTime, ForceMode.Force);
            }
        }
        else
        {
            // 当没有输入时，添加阻尼以减缓旋转
            if (Mathf.Abs(currentRollSpeed) > 0.5f)
            {
                // 计算反向阻尼力矩
                Vector3 rollDamping = -transform.forward * Mathf.Sign(currentRollSpeed) *
                                     Mathf.Min(Mathf.Abs(currentRollSpeed) * 0.5f, 1.0f) * airRollTorque * 0.2f;

                // 应用阻尼力矩
                vehicleRigidbody.AddTorque(rollDamping * Time.fixedDeltaTime, ForceMode.Force);
            }
        }

        // 添加空中姿态稳定（减弱稳定效果，让车辆更容易翻滚）
        if (Mathf.Abs(rollInput) < 0.1f && Mathf.Abs(pitchInput) < 0.1f)
        {
            // 当没有输入时，尝试使车辆保持水平，但强度非常弱
            Vector3 upDirection = transform.up;
            Vector3 targetUp = Vector3.up;

            // 计算当前朝上方向与目标朝上方向的差异
            Vector3 stabilizeTorque = Vector3.Cross(upDirection, targetUp) * 3000f;

            // 应用稳定力矩，但强度非常弱，几乎不会影响玩家的控制
            vehicleRigidbody.AddTorque(stabilizeTorque * Time.fixedDeltaTime * 0.1f, ForceMode.Force);
        }
    }
}