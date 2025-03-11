using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 车辆输入处理器
/// 负责处理玩家输入并传递给车辆控制器
/// </summary>
public class VehicleInputHandler : MonoBehaviour
{
    [Tooltip("车辆控制器引用")]
    [SerializeField] private VehicleController vehicleController;

    [Tooltip("是否使用键盘输入")]
    [SerializeField] private bool useKeyboardInput = true;

    // 输入值
    private float throttleInput;
    private float brakeInput;
    private float steeringInput;
    private bool handbrakeInput;

    // 键盘输入映射
    private KeyCode accelerateKey = KeyCode.W;
    private KeyCode brakeKey = KeyCode.S;
    private KeyCode leftKey = KeyCode.A;
    private KeyCode rightKey = KeyCode.D;
    private KeyCode handbrakeKey = KeyCode.Space;
    private KeyCode resetKey = KeyCode.R;

    /// <summary>
    /// 初始化组件
    /// </summary>
    private void Awake()
    {
        // 如果没有指定车辆控制器，尝试获取
        if (vehicleController == null)
        {
            vehicleController = GetComponent<VehicleController>();

            if (vehicleController == null)
            {
                Debug.LogError("未找到车辆控制器组件！");
            }
        }
    }

    /// <summary>
    /// 处理输入
    /// </summary>
    private void Update()
    {
        if (useKeyboardInput)
        {
            HandleKeyboardInput();
        }

        // 重置车辆
        if (Input.GetKeyDown(resetKey))
        {
            vehicleController.ResetVehicle();
        }
    }

    /// <summary>
    /// 处理键盘输入
    /// </summary>
    private void HandleKeyboardInput()
    {
        // 油门和刹车
        throttleInput = Input.GetKey(accelerateKey) ? 1.0f : 0.0f;
        brakeInput = Input.GetKey(brakeKey) ? 1.0f : 0.0f;

        // 转向
        steeringInput = 0.0f;
        if (Input.GetKey(leftKey)) steeringInput -= 1.0f;
        if (Input.GetKey(rightKey)) steeringInput += 1.0f;

        // 手刹
        handbrakeInput = Input.GetKey(handbrakeKey);

        // 应用输入到车辆控制器
        ApplyInput();
    }

    /// <summary>
    /// 处理新输入系统的油门输入
    /// </summary>
    public void OnThrottle(InputValue value)
    {
        if (!useKeyboardInput)
        {
            throttleInput = value.Get<float>();
            ApplyInput();
        }
    }

    /// <summary>
    /// 处理新输入系统的刹车输入
    /// </summary>
    public void OnBrake(InputValue value)
    {
        if (!useKeyboardInput)
        {
            brakeInput = value.Get<float>();
            ApplyInput();
        }
    }

    /// <summary>
    /// 处理新输入系统的转向输入
    /// </summary>
    public void OnSteer(InputValue value)
    {
        if (!useKeyboardInput)
        {
            steeringInput = value.Get<Vector2>().x;
            ApplyInput();
        }
    }

    /// <summary>
    /// 处理新输入系统的手刹输入
    /// </summary>
    public void OnHandbrake(InputValue value)
    {
        if (!useKeyboardInput)
        {
            handbrakeInput = value.isPressed;
            ApplyInput();
        }
    }

    /// <summary>
    /// 处理新输入系统的重置输入
    /// </summary>
    public void OnReset(InputValue value)
    {
        if (value.isPressed)
        {
            vehicleController.ResetVehicle();
        }
    }

    /// <summary>
    /// 应用输入到车辆控制器
    /// </summary>
    private void ApplyInput()
    {
        // 如果车辆控制器不存在，返回
        if (vehicleController == null) return;

        // 应用油门和刹车
        // 如果同时按下油门和刹车，优先使用刹车
        if (brakeInput > 0)
        {
            // 发送刹车输入
            SendBrakeInput(brakeInput);
            SendThrottleInput(0);
        }
        else
        {
            // 发送油门输入
            SendThrottleInput(throttleInput);
            SendBrakeInput(0);
        }

        // 应用转向
        SendSteeringInput(steeringInput);

        // 应用手刹
        SendHandbrakeInput(handbrakeInput);
    }

    /// <summary>
    /// 发送油门输入到车辆控制器
    /// </summary>
    private void SendThrottleInput(float input)
    {
        // 直接设置车辆控制器的油门输入
        vehicleController.GetType().GetField("throttleInput", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(vehicleController, input);
    }

    /// <summary>
    /// 发送刹车输入到车辆控制器
    /// </summary>
    private void SendBrakeInput(float input)
    {
        // 直接设置车辆控制器的刹车输入
        vehicleController.GetType().GetField("brakeInput", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(vehicleController, input);
    }

    /// <summary>
    /// 发送转向输入到车辆控制器
    /// </summary>
    private void SendSteeringInput(float input)
    {
        // 直接设置车辆控制器的转向输入
        vehicleController.GetType().GetField("steeringInput", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(vehicleController, input);
    }

    /// <summary>
    /// 发送手刹输入到车辆控制器
    /// </summary>
    private void SendHandbrakeInput(bool input)
    {
        // 直接设置车辆控制器的手刹输入
        vehicleController.GetType().GetField("isHandbrakeActive", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(vehicleController, input);
    }
}