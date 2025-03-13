using UnityEngine;
using UnityEngine.InputSystem;

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

            // 确保车辆在静止时不会旋转
            vehicleRigidbody.inertiaTensor = new Vector3(1500, 1500, 1500);
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
    }

    /// <summary>
    /// 处理物理相关的更新
    /// </summary>
    private void FixedUpdate()
    {
        // 应用转向
        ApplySteering();

        // 应用驱动力
        ApplyDrive();

        // 处理漂移
        HandleDrifting();

        // 更新车轮模型
        UpdateWheelModels();

        // 防翻滚保护
        StabilizeVehicle();
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
        UpdateWheelModel(frontLeftWheel, frontLeftWheelTransform);
        UpdateWheelModel(frontRightWheel, frontRightWheelTransform);
        UpdateWheelModel(rearLeftWheel, rearLeftWheelTransform);
        UpdateWheelModel(rearRightWheel, rearRightWheelTransform);
    }

    /// <summary>
    /// 更新单个车轮模型
    /// </summary>
    private void UpdateWheelModel(WheelCollider collider, Transform wheelTransform)
    {
        if (collider == null || wheelTransform == null) return;

        // 获取车轮位置和旋转
        Vector3 position;
        Quaternion rotation;
        collider.GetWorldPose(out position, out rotation);

        // 应用到车轮模型
        wheelTransform.position = position;
        wheelTransform.rotation = rotation;
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
}