using UnityEngine;
using UnityEngine.InputSystem;

namespace Vehicle
{
    /// <summary>
    /// 车辆输入处理器
    /// 负责处理玩家输入并传递给车辆驱动系统
    /// </summary>
    public class VehicleInputHandler : MonoBehaviour
    {
        [Tooltip("车辆控制器引用")]
        [SerializeField] private VehicleController vehicleController;

        [Tooltip("车辆驱动系统引用")]
        [SerializeField] private VehicleDriveSystem vehicleDriveSystem;

        [Tooltip("是否使用键盘输入")]
        [SerializeField] private bool useKeyboardInput = true;

        // 输入值
        private float throttleInput;
        private float brakeInput;
        private float steeringInput;
        private bool handbrakeInput;
        private bool nitroInput;

        // 键盘输入映射
        private KeyCode accelerateKey = KeyCode.W;
        private KeyCode brakeKey = KeyCode.S;
        private KeyCode leftKey = KeyCode.A;
        private KeyCode rightKey = KeyCode.D;
        private KeyCode handbrakeKey = KeyCode.Space;
        private KeyCode resetKey = KeyCode.R;
        private KeyCode nitroKey = KeyCode.LeftShift;

        /// <summary>
        /// 初始化组件
        /// </summary>
        private void Awake()
        {
            // 如果没有指定车辆控制器，尝试获取
            if (vehicleController == null)
            {
                vehicleController = GetComponent<VehicleController>();
            }

            // 如果没有指定车辆驱动系统，尝试获取
            if (vehicleDriveSystem == null)
            {
                vehicleDriveSystem = GetComponent<VehicleDriveSystem>();
            }

            // 检查组件是否存在
            if (vehicleController == null)
            {
                Debug.LogError("未找到VehicleController组件！");
                this.enabled = false;
                return;
            }

            if (vehicleDriveSystem == null)
            {
                Debug.LogError("未找到VehicleDriveSystem组件！");
                this.enabled = false;
                return;
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

            // 应用输入到车辆驱动系统
            ApplyInput();

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
            // 处理油门输入
            throttleInput = Input.GetKey(accelerateKey) ? 1.0f : 0.0f;

            // 处理刹车输入
            brakeInput = Input.GetKey(brakeKey) ? 1.0f : 0.0f;

            // 处理转向输入
            steeringInput = 0.0f;
            if (Input.GetKey(leftKey)) steeringInput -= 1.0f;
            if (Input.GetKey(rightKey)) steeringInput += 1.0f;

            // 处理手刹输入
            handbrakeInput = Input.GetKey(handbrakeKey);

            // 处理氮气输入
            nitroInput = Input.GetKey(nitroKey);
        }

        /// <summary>
        /// 处理新输入系统的油门输入
        /// </summary>
        public void OnThrottle(InputValue value)
        {
            if (!useKeyboardInput)
            {
                throttleInput = value.Get<float>();
            }
        }

        /// <summary>
        /// 处理新输入系统的转向输入
        /// </summary>
        public void OnSteer(InputValue value)
        {
            if (!useKeyboardInput)
            {
                steeringInput = value.Get<float>();
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
        /// 处理新输入系统的氮气输入
        /// </summary>
        public void OnNitro(InputValue value)
        {
            if (!useKeyboardInput)
            {
                nitroInput = value.isPressed;
            }
        }

        /// <summary>
        /// 应用输入到车辆驱动系统
        /// </summary>
        private void ApplyInput()
        {
            // 如果车辆驱动系统不存在，返回
            if (vehicleDriveSystem == null) return;

            // 应用油门和刹车
            // 如果同时按下油门和刹车，优先使用刹车
            if (brakeInput > 0)
            {
                vehicleDriveSystem.SetThrottleInput(0);
                vehicleDriveSystem.SetBrakeInput(brakeInput);
            }
            else
            {
                vehicleDriveSystem.SetThrottleInput(throttleInput);
                vehicleDriveSystem.SetBrakeInput(0);
            }

            // 应用转向
            vehicleDriveSystem.SetSteeringInput(steeringInput);

            // 应用手刹
            vehicleDriveSystem.SetHandbrakeActive(handbrakeInput);

            // 应用氮气
            vehicleDriveSystem.SetNitroActive(nitroInput);
        }
    }
}