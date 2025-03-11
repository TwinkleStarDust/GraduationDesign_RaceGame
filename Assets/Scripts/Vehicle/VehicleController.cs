using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 车辆控制器脚本
/// 负责处理车辆的移动、转向和物理碰撞
/// </summary>
public class VehicleController : MonoBehaviour
{
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

    // 将km/h转换为m/s的系数
    private const float KMH_TO_MS = 0.2778f;

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
    }

    /// <summary>
    /// 处理输入和更新车辆状态
    /// </summary>
    private void Update()
    {
        // 更新当前速度 (km/h)
        currentSpeed = vehicleRigidbody.linearVelocity.magnitude / KMH_TO_MS;

        // 更新引擎声音
        UpdateEngineSound();
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

        // 更新车轮模型
        UpdateWheelModels();
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
                frontLeftWheel.motorTorque = motorTorque;
                frontRightWheel.motorTorque = motorTorque;
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
                frontLeftWheel.motorTorque = motorTorque;
                frontRightWheel.motorTorque = motorTorque;
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
    /// 获取当前车速
    /// </summary>
    public float GetCurrentSpeed()
    {
        return currentSpeed;
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
}