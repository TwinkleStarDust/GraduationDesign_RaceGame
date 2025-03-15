using UnityEngine;
using System.Collections;
using Vehicle; // 添加Vehicle命名空间的引用

/// <summary>
/// 车辆物理系统
/// 处理车辆的物理行为，如悬挂、摩擦力、稳定性和碰撞
/// </summary>
public class VehiclePhysics : MonoBehaviour
{
    [Header("物理参数")]
    [Tooltip("重心高度")]
    [SerializeField] private float centerOfMassHeight = -0.5f;

    [Tooltip("下压力系数")]
    [SerializeField] private float downforceCoefficient = 1.0f;

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
    [SerializeField] private float antiRollForce = 12000f;

    // 引用其他组件
    private Rigidbody vehicleRigidbody;
    private VehicleController vehicleController;

    // 车辆状态
    private bool isInAir = false;
    private bool isFlipped = false;
    private bool isUpsideDown = false;
    private float airTime = 0f;

    // 缓存原始摩擦力设置
    private WheelFrictionCurve originalFrontWheelFriction;
    private WheelFrictionCurve originalRearWheelFriction;

    // 速度相关
    private float currentSpeed = 0f;
    private const float KMH_TO_MS = 3.6f; // 1 km/h = 3.6 m/s

    /// <summary>
    /// 初始化物理系统
    /// </summary>
    private void Awake()
    {
        // 获取组件引用
        vehicleRigidbody = GetComponent<Rigidbody>();
        vehicleController = GetComponent<VehicleController>();

        // 配置刚体
        SetupRigidbody();

        // 配置车轮
        SetupWheels();
    }

    /// <summary>
    /// 配置刚体参数
    /// </summary>
    private void SetupRigidbody()
    {
        if (vehicleRigidbody != null)
        {
            // 进一步降低重心，更合理地分配
            vehicleRigidbody.centerOfMass = new Vector3(0, centerOfMassHeight - 0.3f, 0.1f); // 轻微后移重心，进一步降低

            // 调整惯性张量，使车辆更加稳定，同时改善前后重量分配
            vehicleRigidbody.inertiaTensor = new Vector3(1800, 1600, 1200); // 横向稳定，前后平衡

            // 进一步降低阻尼
            vehicleRigidbody.linearDamping = 0.01f;  // 大幅降低线性阻尼
            vehicleRigidbody.angularDamping = 0.12f;  // 略微减小角阻尼，保持转向灵活性

            // 物理计算参数
            vehicleRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            vehicleRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            vehicleRigidbody.solverIterations = 12; // 进一步增加解算器迭代次数
            vehicleRigidbody.solverVelocityIterations = 12; // 增加速度迭代次数
        }
        else
        {
            Debug.LogError("车辆缺少Rigidbody组件！");
        }
    }

    /// <summary>
    /// 配置车轮
    /// </summary>
    private void SetupWheels()
    {
        // 检查车轮碰撞器是否已分配
        if (frontLeftWheel == null || frontRightWheel == null ||
            rearLeftWheel == null || rearRightWheel == null)
        {
            Debug.LogWarning("车轮碰撞器未完全分配！");
            return;
        }

        // 重置前轮转向角度
        frontLeftWheel.steerAngle = 0;
        frontRightWheel.steerAngle = 0;

        // 保存原始摩擦力设置
        originalFrontWheelFriction = frontLeftWheel.forwardFriction;
        originalRearWheelFriction = rearLeftWheel.forwardFriction;

        // 设置车轮摩擦力
        SetupWheelFriction();

        // 设置车轮悬挂
        SetupWheelSuspension();
    }

    /// <summary>
    /// 设置车轮摩擦力
    /// </summary>
    private void SetupWheelFriction()
    {
        // 保证四个轮子的摩擦力完全一致，避免不平衡导致的偏移
        float forwardStiffness = 1.1f; // 增加前向摩擦力
        float sidewaysStiffness = 1.0f;

        // 设置所有车轮摩擦力完全相同
        SetWheelFriction(frontLeftWheel, forwardStiffness, sidewaysStiffness);
        SetWheelFriction(frontRightWheel, forwardStiffness, sidewaysStiffness);
        SetWheelFriction(rearLeftWheel, forwardStiffness, sidewaysStiffness);
        SetWheelFriction(rearRightWheel, forwardStiffness, sidewaysStiffness);
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
    /// 设置车轮悬挂参数，使车辆更加稳定
    /// </summary>
    private void SetupWheelSuspension()
    {
        // 保证左右车轮参数完全对称
        float frontSpring = 22000f;
        float frontDamper = 2200f;
        float rearSpring = 24000f;
        float rearDamper = 2400f;
        float suspensionTarget = 0.3f;

        // 统一设置前轮悬挂 - 左右参数完全一致
        ConfigureWheelSuspension(frontLeftWheel, frontSpring, frontDamper, suspensionTarget);
        ConfigureWheelSuspension(frontRightWheel, frontSpring, frontDamper, suspensionTarget);

        // 统一设置后轮悬挂 - 左右参数完全一致
        ConfigureWheelSuspension(rearLeftWheel, rearSpring, rearDamper, suspensionTarget);
        ConfigureWheelSuspension(rearRightWheel, rearSpring, rearDamper, suspensionTarget);
    }

    /// <summary>
    /// 配置单个车轮的悬挂参数
    /// </summary>
    private void ConfigureWheelSuspension(WheelCollider wheel, float spring, float damper, float targetPosition)
    {
        if (wheel == null) return;

        JointSpring suspension = wheel.suspensionSpring;

        // 弹簧力值越大，悬挂越硬
        suspension.spring = spring;

        // 阻尼值越大，悬挂回弹越慢
        suspension.damper = damper;

        // 目标位置表示静止时悬挂的压缩程度，0表示完全不压缩，1表示完全压缩
        suspension.targetPosition = targetPosition;

        wheel.suspensionSpring = suspension;

        // 设置其他车轮参数
        wheel.suspensionDistance = 0.25f;  // 增加悬挂行程，提高越障能力
        wheel.forceAppPointDistance = 0;  // 力应用点，0表示在车轮中心
        wheel.mass = 20f;  // 车轮质量
    }

    /// <summary>
    /// 物理更新
    /// </summary>
    private void FixedUpdate()
    {
        // 更新车辆状态
        UpdateVehicleState();

        // 更新车轮模型位置
        UpdateWheelModels();

        // 防翻滚保护
        if (!isFlipped && !isUpsideDown && !isInAir)
        {
            StabilizeVehicle();
        }

        // 应用下压力
        ApplyDownforce();

        // 处理空中控制
        HandleAirControl();
    }

    /// <summary>
    /// 更新车辆状态
    /// </summary>
    private void UpdateVehicleState()
    {
        // 更新空中状态
        isInAir = !IsAnyWheelGrounded();

        // 更新翻滚状态
        isFlipped = Vector3.Dot(transform.up, Vector3.up) < 0.3f;
        isUpsideDown = Vector3.Dot(transform.up, Vector3.up) < -0.5f;

        // 通知控制器状态变化
        if (vehicleController != null)
        {
            vehicleController.SetVehicleState(isInAir, isFlipped, isUpsideDown);
        }
    }

    /// <summary>
    /// 检查是否有任何车轮接触地面
    /// </summary>
    private bool IsAnyWheelGrounded()
    {
        // 使用更可靠的方法检测车轮是否接触地面
        int groundedWheels = 0;

        // 检查四个车轮是否接触地面
        if (IsWheelGrounded(frontLeftWheel)) groundedWheels++;
        if (IsWheelGrounded(frontRightWheel)) groundedWheels++;
        if (IsWheelGrounded(rearLeftWheel)) groundedWheels++;
        if (IsWheelGrounded(rearRightWheel)) groundedWheels++;

        // 发现并纠正左右车轮接触不平衡的情况
        bool leftSideGrounded = IsWheelGrounded(frontLeftWheel) || IsWheelGrounded(rearLeftWheel);
        bool rightSideGrounded = IsWheelGrounded(frontRightWheel) || IsWheelGrounded(rearRightWheel);

        // 如果检测到左右不平衡且车速足够高，应用修正力防止偏移
        if (leftSideGrounded != rightSideGrounded && vehicleController != null &&
            vehicleController.GetCurrentSpeed() > 20f && !isInAir)
        {
            // 计算需要的修正方向
            Vector3 correctionTorque = Vector3.up * (leftSideGrounded ? 1f : -1f) * 500f;
            vehicleRigidbody.AddTorque(correctionTorque * Time.fixedDeltaTime, ForceMode.Acceleration);
        }

        // 当有两个以上的车轮接触地面时，才认为车辆在地面上
        return groundedWheels >= 2;
    }

    /// <summary>
    /// 检查单个车轮是否接触地面
    /// </summary>
    private bool IsWheelGrounded(WheelCollider wheel)
    {
        if (wheel == null) return false;

        WheelHit hit;
        // 获取碰撞信息
        bool isGrounded = wheel.GetGroundHit(out hit);

        // 增加精确性和稳定性
        if (isGrounded)
        {
            // 降低力量阈值，避免过度敏感的接地判定
            float forceThreshold = 50f; // 降低到50N，更容易判定为接地

            // 从判断条件中移除悬挂压缩检查，因为它可能导致判定不一致
            return hit.force > forceThreshold;
        }

        return false;
    }

    /// <summary>
    /// 更新车轮模型位置和旋转
    /// </summary>
    private void UpdateWheelModels()
    {
        UpdateWheelModel(frontLeftWheel, frontLeftWheelModel);
        UpdateWheelModel(frontRightWheel, frontRightWheelModel);
        UpdateWheelModel(rearLeftWheel, rearLeftWheelModel);
        UpdateWheelModel(rearRightWheel, rearRightWheelModel);
    }

    /// <summary>
    /// 更新单个车轮模型
    /// </summary>
    private void UpdateWheelModel(WheelCollider wheelCollider, Transform wheelModel)
    {
        if (wheelCollider == null || wheelModel == null) return;

        // 获取车轮碰撞器的位置和旋转
        Vector3 position;
        Quaternion rotation;
        wheelCollider.GetWorldPose(out position, out rotation);

        // 应用到车轮模型
        wheelModel.position = position;
        wheelModel.rotation = rotation;
    }

    /// <summary>
    /// 加强车辆稳定性，防止翻滚
    /// </summary>
    private void StabilizeVehicle()
    {
        // 获取当前速度和转向输入，以便在高速转向时提供额外稳定性
        float currentSpeed = 0f;
        float steeringInput = 0f;
        if (vehicleController != null)
        {
            currentSpeed = vehicleController.GetCurrentSpeed();
            steeringInput = Mathf.Abs(vehicleController.GetSteeringInput());
        }

        // 根据速度和转向输入计算额外的防翻滚力
        float speedFactor = Mathf.Clamp01(currentSpeed / 80.0f); // 速度因子
        float steeringFactor = Mathf.Clamp01(steeringInput); // 转向因子

        // 高速转向时增加额外的防翻滚力
        float extraStabilization = speedFactor * steeringFactor * 1.5f;

        // 如果车辆开始倾斜或处于高速转向状态，施加修正力
        if (transform.up.y < 0.95f || (speedFactor > 0.5f && steeringFactor > 0.3f))
        {
            // 计算修正力的方向
            Vector3 stabilizationTorque = Vector3.Cross(transform.up, Vector3.up);

            // 根据倾斜程度和车速/转向状态调整修正力大小
            float tiltFactor = 1.0f - transform.up.y; // 0到1之间的值
            float baseTorque = antiRollForce * (tiltFactor + extraStabilization);

            // 增加修正力的最小值，确保即使轻微倾斜也有足够的稳定力
            float torqueMagnitude = Mathf.Max(baseTorque, antiRollForce * 0.2f * speedFactor);

            stabilizationTorque *= torqueMagnitude;

            // 应用修正力，使用更直接的力模式
            vehicleRigidbody.AddTorque(stabilizationTorque * Time.fixedDeltaTime, ForceMode.Impulse);

            // 在高速转向时，额外增加一个向下的力以防止侧翻
            if (speedFactor > 0.6f && steeringFactor > 0.5f)
            {
                float downwardForce = vehicleRigidbody.mass * 9.81f * speedFactor * steeringFactor * 0.5f;
                vehicleRigidbody.AddForce(-transform.up * downwardForce, ForceMode.Force);
            }
        }
    }

    /// <summary>
    /// 应用下压力增加高速稳定性
    /// </summary>
    private void ApplyDownforce()
    {
        if (!isInAir && vehicleController != null)
        {
            float currentSpeed = vehicleController.GetCurrentSpeed();
            float maxSpeed = vehicleController.GetMaxSpeed();

            // 大幅提高应用下压力的速度阈值，从50km/h提高到70km/h
            if (currentSpeed > 70f)
            {
                // 使用更合理的下压力计算方式
                float speedFactor = Mathf.Clamp01((currentSpeed - 70f) / (maxSpeed - 70f)); // 从70km/h开始计算

                // 大幅降低下压力系数
                float downforce = speedFactor * downforceCoefficient * 1200f; // 从1800f降低到1200f

                // 修改下压力的作用点，使其更多地作用在车辆后部，减少车头下压
                Vector3 forcePosition = transform.position + transform.forward * 0.5f; // 向后偏移作用点

                // 应用向下的力，使用更温和的力模式，并指定作用点在车辆后部
                vehicleRigidbody.AddForceAtPosition(-transform.up * downforce * Time.fixedDeltaTime, forcePosition, ForceMode.Acceleration);
            }
        }
    }

    /// <summary>
    /// 计算当前速度
    /// </summary>
    private void UpdateSpeed()
    {
        if (vehicleController != null)
        {
            // 获取车辆在本地空间的速度
            Vector3 localVelocity = transform.InverseTransformDirection(vehicleRigidbody.linearVelocity);

            // 只考虑水平面上的速度（忽略垂直方向）
            Vector3 horizontalVelocity = new Vector3(localVelocity.x, 0, localVelocity.z);

            // 将m/s转换为km/h
            currentSpeed = horizontalVelocity.magnitude / KMH_TO_MS;

            // 更新车辆状态
            vehicleController.SetVehicleState(isInAir, isFlipped, isUpsideDown);
        }
    }

    /// <summary>
    /// 处理空中控制
    /// </summary>
    private void HandleAirControl()
    {
        if (isInAir && vehicleController != null)
        {
            airTime += Time.fixedDeltaTime;

            // 获取控制输入
            float throttleInput = vehicleController.GetThrottleInput();
            float brakeInput = vehicleController.GetBrakeInput();
            float steeringInput = vehicleController.GetSteeringInput();
            bool isHandbrakeActive = vehicleController.IsHandbrakeActive();

            // 计算当前角速度
            Vector3 angularVel = vehicleRigidbody.angularVelocity;

            // 如果使用手刹，增加阻尼以减缓旋转
            if (isHandbrakeActive)
            {
                vehicleRigidbody.angularDamping = 1.5f;
                // 保持当前速度方向，但允许重力影响
                Vector3 currentVelocity = vehicleRigidbody.linearVelocity;
                Vector3 horizontalVelocity = Vector3.ProjectOnPlane(currentVelocity, Vector3.up);
                vehicleRigidbody.linearVelocity = horizontalVelocity + Vector3.up * currentVelocity.y;
            }
            else
            {
                vehicleRigidbody.angularDamping = 0.05f;
            }

            // 空中控制 - 更真实的物理表现
            float airControlFactor = Mathf.Min(airTime * 0.8f, 0.5f); // 降低控制强度

            // 基于方向输入添加扭矩
            Vector3 torque = Vector3.zero;

            // 前后倾斜控制 (使用油门/刹车)
            if (Mathf.Abs(throttleInput) > 0.1f || Mathf.Abs(brakeInput) > 0.1f)
            {
                float pitchInput = throttleInput - brakeInput;
                // 考虑当前旋转速度，防止过度旋转
                float currentPitchVel = Vector3.Dot(angularVel, transform.right);
                float maxPitchVel = 2.0f; // 最大允许的俯仰角速度

                if (Mathf.Abs(currentPitchVel) < maxPitchVel)
                {
                    torque += transform.right * pitchInput * 250f * airControlFactor;
                }
            }

            // 左右翻滚控制 (使用转向)
            if (Mathf.Abs(steeringInput) > 0.1f)
            {
                float currentRollVel = Vector3.Dot(angularVel, transform.forward);
                float maxRollVel = 2.0f; // 最大允许的翻滚角速度

                if (Mathf.Abs(currentRollVel) < maxRollVel)
                {
                    // 使用非线性曲线使小幅度输入效果更小
                    float adjustedSteeringInput = Mathf.Sign(steeringInput) * Mathf.Pow(Mathf.Abs(steeringInput), 1.5f);
                    torque += transform.forward * adjustedSteeringInput * 200f * airControlFactor;
                }
            }

            // 添加惯性稳定
            Vector3 worldAngularVel = vehicleRigidbody.angularVelocity;
            Vector3 stabilizationTorque = -worldAngularVel * 0.3f; // 增加角速度阻尼

            // 合并所有扭矩
            Vector3 finalTorque = torque + stabilizationTorque;

            // 平滑应用扭矩
            vehicleRigidbody.AddTorque(finalTorque * Time.fixedDeltaTime, ForceMode.Acceleration);

            // 保持车辆在空中时的稳定性
            if (airTime > 0.2f)
            {
                // 计算当前旋转与水平面的夹角
                float upDot = Vector3.Dot(transform.up, Vector3.up);
                if (upDot < 0.8f) // 当倾斜超过一定角度时
                {
                    // 添加更强的自稳定力矩
                    Vector3 correctionTorque = Vector3.Cross(transform.up, Vector3.up) * 2000f;
                    vehicleRigidbody.AddTorque(correctionTorque * Time.fixedDeltaTime, ForceMode.Acceleration);
                }
            }
        }
        else
        {
            // 车辆着地后重置空中时间和阻尼
            airTime = 0f;
            vehicleRigidbody.angularDamping = 0.05f;
        }
    }

    /// <summary>
    /// 碰撞发生时的处理
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        // 处理严重碰撞
        float impactForce = collision.impulse.magnitude;

        // 计算碰撞点相对于车辆的方向
        Vector3 impactDirection = transform.InverseTransformDirection(collision.impulse.normalized);

        // 如果是较强的侧面碰撞
        if (impactForce > 5000f && Mathf.Abs(impactDirection.x) > 0.7f)
        {
            // 添加小的减速效果
            vehicleRigidbody.linearVelocity *= 0.8f;

            // 如果速度很快，添加一些阻尼以减少弹跳
            if (vehicleController != null && vehicleController.GetCurrentSpeed() > 40f)
            {
                // 临时增加阻尼
                StartCoroutine(TemporaryIncreaseDamping());
            }
        }
        // 如果是减速带类型的较弱垂直碰撞，使用更温和的处理
        else if (impactForce > 1000f && impactForce < 3000f && impactDirection.y < -0.7f)
        {
            // 对减速带的专门处理，减少垂直反弹
            vehicleRigidbody.linearVelocity = new Vector3(
                vehicleRigidbody.linearVelocity.x,
                vehicleRigidbody.linearVelocity.y * 0.6f, // 减少垂直反弹
                vehicleRigidbody.linearVelocity.z
            );
        }
    }

    /// <summary>
    /// 临时增加阻尼以减少碰撞后的弹跳
    /// </summary>
    private IEnumerator TemporaryIncreaseDamping()
    {
        // 保存原始值
        float originalLinearDamping = vehicleRigidbody.linearDamping;
        float originalAngularDamping = vehicleRigidbody.angularDamping;

        // 增加阻尼
        vehicleRigidbody.linearDamping = 0.5f;
        vehicleRigidbody.angularDamping = 0.5f;

        // 等待短暂时间
        yield return new WaitForSeconds(0.2f);

        // 恢复原始阻尼
        vehicleRigidbody.linearDamping = originalLinearDamping;
        vehicleRigidbody.angularDamping = originalAngularDamping;
    }

    /// <summary>
    /// 调整车轮摩擦力用于漂移
    /// </summary>
    public void AdjustRearWheelFriction(float forwardStiffness, float sidewaysStiffness)
    {
        SetWheelFriction(rearLeftWheel, forwardStiffness, sidewaysStiffness);
        SetWheelFriction(rearRightWheel, forwardStiffness, sidewaysStiffness);
    }

    /// <summary>
    /// 重置车轮摩擦力到默认值
    /// </summary>
    public void ResetWheelFriction()
    {
        SetupWheelFriction();
    }

    /// <summary>
    /// 设置转向角度
    /// </summary>
    public void SetSteeringAngle(float leftAngle, float rightAngle)
    {
        if (frontLeftWheel != null) frontLeftWheel.steerAngle = leftAngle;
        if (frontRightWheel != null) frontRightWheel.steerAngle = rightAngle;
    }

    /// <summary>
    /// 应用驱动力到车轮
    /// </summary>
    public void ApplyMotorTorque(float frontTorque, float rearTorque)
    {
        if (frontLeftWheel != null) frontLeftWheel.motorTorque = frontTorque;
        if (frontRightWheel != null) frontRightWheel.motorTorque = frontTorque;
        if (rearLeftWheel != null) rearLeftWheel.motorTorque = rearTorque;
        if (rearRightWheel != null) rearRightWheel.motorTorque = rearTorque;
    }

    /// <summary>
    /// 应用制动力到车轮
    /// </summary>
    public void ApplyBrakeTorque(float frontBrakeTorque, float rearBrakeTorque)
    {
        if (frontLeftWheel != null) frontLeftWheel.brakeTorque = frontBrakeTorque;
        if (frontRightWheel != null) frontRightWheel.brakeTorque = frontBrakeTorque;
        if (rearLeftWheel != null) rearLeftWheel.brakeTorque = rearBrakeTorque;
        if (rearRightWheel != null) rearRightWheel.brakeTorque = rearBrakeTorque;
    }

    /// <summary>
    /// 重置车辆物理状态
    /// </summary>
    public void ResetPhysics()
    {
        if (vehicleRigidbody != null)
        {
            vehicleRigidbody.linearVelocity = Vector3.zero;
            vehicleRigidbody.angularVelocity = Vector3.zero;
        }
    }

    /// <summary>
    /// 获取车辆是否在空中
    /// </summary>
    public bool GetIsInAir()
    {
        return isInAir;
    }

    /// <summary>
    /// 获取车辆是否侧翻
    /// </summary>
    public bool GetIsFlipped()
    {
        return isFlipped;
    }

    /// <summary>
    /// 获取车辆是否倒置
    /// </summary>
    public bool GetIsUpsideDown()
    {
        return isUpsideDown;
    }
}