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
        [Tooltip("车辆驱动系统引用")]
        [SerializeField] private VehicleDriveSystem vehicleDriveSystem;

        [Header("键盘按键映射")]
        [Tooltip("油门键")]
        [SerializeField] private KeyCode accelerateKey = KeyCode.W;

        [Tooltip("刹车/倒车键 - 用于减速和倒车，均匀制动所有车轮")]
        [SerializeField] private KeyCode brakeKey = KeyCode.S;

        [Tooltip("左转向键")]
        [SerializeField] private KeyCode leftKey = KeyCode.A;

        [Tooltip("右转向键")]
        [SerializeField] private KeyCode rightKey = KeyCode.D;

        [Tooltip("手刹键 - 用于漂移控制，主要制动后轮")]
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
        private bool isDriftingRequested;

        // 目标输入值（用于平滑过渡）
        private float targetThrottleInput;
        private float targetBrakeInput;
        private float targetSteeringInput;

        // 平滑过渡系数
        private const float SMOOTH_FACTOR = 10f;
        private const float THROTTLE_RATE = 2.0f;

        /// <summary>
        /// 初始化组件
        /// </summary>
        private void Start()
        {
            // 如果没有指定车辆驱动系统，尝试获取
            if (vehicleDriveSystem == null)
            {
                vehicleDriveSystem = GetComponent<VehicleDriveSystem>();
            }

            // 检查车辆驱动系统是否存在
            if (vehicleDriveSystem == null)
            {
                Debug.LogError("【车辆输入】未找到VehicleDriveSystem组件！请检查Inspector中的引用。");
                this.enabled = false;
                return;
            }

            Debug.Log("【车辆输入】初始化完成。使用键盘输入。");
        }

        /// <summary>
        /// 处理输入
        /// </summary>
        private void Update()
        {
            HandleKeyboardInput();
            SmoothInputs();
            ApplyInput();

            // 重置车辆
            if (Input.GetKeyDown(resetKey))
            {
                vehicleDriveSystem.ResetVehicle();
            }
        }

        /// <summary>
        /// 处理键盘输入
        /// </summary>
        private void HandleKeyboardInput()
        {
            // 渐进式油门控制
            if (Input.GetKey(accelerateKey))
            {
                targetThrottleInput = Mathf.MoveTowards(targetThrottleInput, 1.0f, Time.deltaTime * THROTTLE_RATE);
            }
            else
            {
                targetThrottleInput = Mathf.MoveTowards(targetThrottleInput, 0.0f, Time.deltaTime * THROTTLE_RATE);
            }

            // 渐进式刹车控制
            if (Input.GetKey(brakeKey))
            {
                targetBrakeInput = Mathf.MoveTowards(targetBrakeInput, 1.0f, Time.deltaTime * THROTTLE_RATE);
            }
            else
            {
                targetBrakeInput = Mathf.MoveTowards(targetBrakeInput, 0.0f, Time.deltaTime * THROTTLE_RATE);
            }

            // 处理转向输入
            targetSteeringInput = 0.0f;
            if (Input.GetKey(leftKey)) targetSteeringInput -= 1.0f;
            if (Input.GetKey(rightKey)) targetSteeringInput += 1.0f;

            // 处理氮气输入
            nitroInput = Input.GetKey(nitroKey);
        }

        /// <summary>
        /// 平滑过渡输入值
        /// </summary>
        private void SmoothInputs()
        {
            // 平滑过渡油门输入
            throttleInput = Mathf.Lerp(throttleInput, targetThrottleInput, Time.deltaTime * SMOOTH_FACTOR);

            // 平滑过渡刹车输入
            brakeInput = Mathf.Lerp(brakeInput, targetBrakeInput, Time.deltaTime * SMOOTH_FACTOR);

            // 平滑过渡转向输入
            steeringInput = Mathf.Lerp(steeringInput, targetSteeringInput, Time.deltaTime * SMOOTH_FACTOR);
        }

        /// <summary>
        /// 应用输入到车辆驱动系统
        /// </summary>
        private void ApplyInput()
        {
            // 获取当前手刹状态
            bool currentHandbrakeInput = Input.GetKey(handbrakeKey);

            // 计算是否满足漂移条件
            isDriftingRequested = currentHandbrakeInput && Mathf.Abs(steeringInput) > 0.1f;

            // 如果手刹状态发生变化，立即应用
            if (currentHandbrakeInput != handbrakeInput)
            {
                handbrakeInput = currentHandbrakeInput;
                vehicleDriveSystem.SetHandbrakeActive(handbrakeInput);
            }

            // 将所有输入传递给驱动系统
            vehicleDriveSystem.SetInput(
                throttleInput,
                brakeInput,
                steeringInput,
                handbrakeInput,
                nitroInput,
                isDriftingRequested
            );
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