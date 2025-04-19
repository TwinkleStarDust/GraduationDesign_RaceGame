using UnityEngine;
using UnityEngine.InputSystem;

namespace Vehicle
{
    /// 车辆输入处理器
    /// 负责处理玩家输入并传递给车辆驱动系统
    /// 简化版本，适合街机风格赛车游戏
    public class VehicleInputHandler : MonoBehaviour
    {
        [Header("引用设置")]
        [Tooltip("车辆控制器引用")]
        [SerializeField] private VehicleController vehicleController;

        [Tooltip("车辆驱动系统引用")]
        [SerializeField] private VehicleDriveSystem vehicleDriveSystem;

        [Header("输入设置")]
        [Tooltip("是否使用键盘输入")]
        [SerializeField] private bool useKeyboardInput = true;

        [Tooltip("输入平滑度 (值越小响应越快)")]
        [Range(0.01f, 0.5f)]
        [SerializeField] private float inputSmoothFactor = 0.1f;

        [Tooltip("是否启用渐进式油门/刹车控制")]
        [SerializeField] private bool useProgressiveThrottle = true;

        [Tooltip("油门/刹车增加速率")]
        [Range(0.5f, 5.0f)]
        [SerializeField] private float throttleIncreaseRate = 2.0f;

        [Tooltip("油门/刹车减少速率")]
        [Range(0.5f, 5.0f)]
        [SerializeField] private float throttleDecreaseRate = 3.0f;

        [Header("键盘按键映射")]
        [Tooltip("油门键")]
        [SerializeField] private KeyCode accelerateKey = KeyCode.W;

        [Tooltip("刹车/倒车键")]
        [SerializeField] private KeyCode brakeKey = KeyCode.S;

        [Tooltip("左转向键")]
        [SerializeField] private KeyCode leftKey = KeyCode.A;

        [Tooltip("右转向键")]
        [SerializeField] private KeyCode rightKey = KeyCode.D;

        [Tooltip("手刹键")]
        [SerializeField] private KeyCode handbrakeKey = KeyCode.Space;

        [Tooltip("重置车辆键")]
        [SerializeField] private KeyCode resetKey = KeyCode.R;

        [Tooltip("氮气键")]
        [SerializeField] private KeyCode nitroKey = KeyCode.LeftShift;

        // 输入值
        private float throttleInput;
        private float brakeInput;
        private float steeringInput;
        private bool handbrakeInput;
        private bool nitroInput;

        // 目标输入值（用于平滑过渡）
        private float targetThrottleInput;
        private float targetBrakeInput;
        private float targetSteeringInput;

        /// <summary>
        /// 初始化组件
        /// </summary>
        private void Start()
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
                Debug.LogError("【车辆输入】未找到VehicleController组件！请检查Inspector中的引用。");
                this.enabled = false;
                return;
            }

            if (vehicleDriveSystem == null)
            {
                Debug.LogError("【车辆输入】未找到VehicleDriveSystem组件！请检查Inspector中的引用。");
                this.enabled = false;
                return;
            }

            Debug.Log("【车辆输入】初始化完成。使用" + (useKeyboardInput ? "键盘输入" : "新输入系统") + "。");
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

            // 平滑过渡输入值
            SmoothInputs();

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
            if (useProgressiveThrottle)
            {
                // 渐进式油门控制
                if (Input.GetKey(accelerateKey))
                {
                    targetThrottleInput = Mathf.MoveTowards(targetThrottleInput, 1.0f, Time.deltaTime * throttleIncreaseRate);
                }
                else
                {
                    targetThrottleInput = Mathf.MoveTowards(targetThrottleInput, 0.0f, Time.deltaTime * throttleDecreaseRate);
                }

                // 渐进式刹车控制
                if (Input.GetKey(brakeKey))
                {
                    targetBrakeInput = Mathf.MoveTowards(targetBrakeInput, 1.0f, Time.deltaTime * throttleIncreaseRate);
                }
                else
                {
                    targetBrakeInput = Mathf.MoveTowards(targetBrakeInput, 0.0f, Time.deltaTime * throttleDecreaseRate);
                }
            }
            else
            {
                // 直接油门控制（老方式，但更加灵敏）
                targetThrottleInput = Input.GetKey(accelerateKey) ? 1.0f : 0.0f;
                targetBrakeInput = Input.GetKey(brakeKey) ? 1.0f : 0.0f;
            }

            // 处理转向输入
            targetSteeringInput = 0.0f;
            if (Input.GetKey(leftKey)) targetSteeringInput -= 1.0f;
            if (Input.GetKey(rightKey)) targetSteeringInput += 1.0f;

            // 注意：手刹输入在ApplyInput()中直接处理，确保立即响应
            // 这里不再设置handbrakeInput

            // 处理氮气输入
            nitroInput = Input.GetKey(nitroKey);
        }

        /// <summary>
        /// 平滑过渡输入值
        /// </summary>
        private void SmoothInputs()
        {
            // 平滑过渡油门输入
            throttleInput = Mathf.Lerp(throttleInput, targetThrottleInput, inputSmoothFactor / Time.deltaTime);

            // 平滑过渡刹车输入
            brakeInput = Mathf.Lerp(brakeInput, targetBrakeInput, inputSmoothFactor / Time.deltaTime);

            // 平滑过渡转向输入
            steeringInput = Mathf.Lerp(steeringInput, targetSteeringInput, inputSmoothFactor / Time.deltaTime);
        }

        /// <summary>
        /// 处理新输入系统的油门输入
        /// </summary>
        public void OnThrottle(InputValue value)
        {
            if (!useKeyboardInput)
            {
                targetThrottleInput = value.Get<float>();
            }
        }

        /// <summary>
        /// 处理新输入系统的转向输入
        /// </summary>
        public void OnSteer(InputValue value)
        {
            if (!useKeyboardInput)
            {
                targetSteeringInput = value.Get<float>();
            }
        }

        /// <summary>
        /// 处理新输入系统的刹车输入
        /// </summary>
        public void OnBrake(InputValue value)
        {
            if (!useKeyboardInput)
            {
                targetBrakeInput = value.Get<float>();
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

            // 处理W+S同时按下的情况 - 允许油门和刹车同时存在，实现强力制动
            // 但刹车优先级更高，降低油门效果
            if (throttleInput > 0.01f && brakeInput > 0.01f)
            {
                // W+S同时按下时，降低油门效果但保留一些油门输入
                // 这将使车辆保持一定引擎转速但同时有明显的减速效果
                float reducedThrottle = throttleInput * 0.3f; // 只保留30%的油门
                vehicleDriveSystem.SetThrottleInput(reducedThrottle);
                vehicleDriveSystem.SetBrakeInput(brakeInput * 1.5f); // 增强刹车效果
            }
            // 单独按刹车键
            else if (brakeInput > 0.01f)
            {
                vehicleDriveSystem.SetThrottleInput(0);
                vehicleDriveSystem.SetBrakeInput(brakeInput);
            }
            // 单独按油门键
            else
            {
                vehicleDriveSystem.SetThrottleInput(throttleInput);
                vehicleDriveSystem.SetBrakeInput(0);
            }

            // 处理手刹键按下情况，允许油门和手刹同时存在
            // 手刹与油门共存时应该提供明显的减速和漂移效果
            // 手刹输入不进行平滑处理，确保立即响应
            bool currentHandbrakeInput = Input.GetKey(handbrakeKey);

            // 如果手刹状态发生变化，立即应用
            if (currentHandbrakeInput != handbrakeInput)
            {
                handbrakeInput = currentHandbrakeInput;

                // 立即应用手刹状态变化
                vehicleDriveSystem.SetHandbrakeActive(handbrakeInput);
            }

            if (handbrakeInput)
            {
                // 如果是W+空格同时按下，应保留漂移效果但有明显的减速
                // 如果同时有油门输入，减弱油门效果以模拟现实中的制动情况
                if (throttleInput > 0.1f)
                {
                    // 已经在SetThrottleInput时设置了油门，这里不需要重复设置
                    // 但告诉驱动系统这是"带油门的手刹"状态
                    vehicleDriveSystem.SetBrakingWithThrottle(true);
                }
                else
                {
                    vehicleDriveSystem.SetBrakingWithThrottle(false);
                }
            }
            else
            {
                vehicleDriveSystem.SetBrakingWithThrottle(false);
            }

            // 应用转向
            vehicleDriveSystem.SetSteeringInput(steeringInput);

            // 应用氮气
            vehicleDriveSystem.SetNitroActive(nitroInput);

            // 调试信息
            if (Debug.isDebugBuild && Input.GetKey(KeyCode.BackQuote))
            {
                Debug.Log($"油门: {throttleInput:F2} | 刹车: {brakeInput:F2} | 转向: {steeringInput:F2} | 手刹: {handbrakeInput}");
            }
        }

        /// <summary>
        /// 音频系统使用指南
        ///
        /// 在VehiclePhysics组件中配置以下音频源：
        /// 1. 引擎音频源(Engine Audio Source)：负责播放引擎声音，音量和音调会根据车速和油门自动调整
        /// 2. 轮胎打滑音频源(Tire Skid Audio Source)：负责播放漂移和急转弯时的轮胎摩擦声
        /// 3. 碰撞音频源(Crash Audio Source)：负责播放车辆碰撞时的声音
        /// 4. 氮气音频源(Nitro Audio Source)：负责播放使用氮气时的声音
        ///
        /// 使用方法：
        /// 1. 在Inspector中为每个音频源字段分配一个AudioSource组件
        /// 2. 为每个AudioSource设置相应的音频剪辑(Audio Clip)
        /// 3. 系统会自动处理音量、音调变化和播放时机
        /// </summary>
        public void AudioSystemGuide()
        {
            // 此方法仅作为文档使用，无实际功能
        }
    }
}