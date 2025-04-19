using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections;

namespace Vehicle
{
    /// 车辆控制器
    /// 作为车辆的主控制器，协调各个子系统
    /// 简化版本，适合街机风格赛车游戏
    public class VehicleController : MonoBehaviour
    {
        [Header("调试选项")]
        [SerializeField] private bool showDebugInfo = false;

        // 引用其他组件
        private VehiclePhysics vehiclePhysics;
        private VehicleDriveSystem vehicleDriveSystem;
        private Rigidbody vehicleRigidbody;

        // 车辆状态
        private bool isInAir = false;
        private bool isFlipped = false;
        private bool isUpsideDown = false;
        private bool isDrifting = false;
        private float currentDriftFactor = 0f;

        // 传送事件
        public event Action OnBeforeTeleport;
        public event Action OnAfterTeleport;

        /// 初始化组件
        private void Awake()
        {
            // 获取组件引用
            vehiclePhysics = GetComponent<VehiclePhysics>();
            vehicleDriveSystem = GetComponent<VehicleDriveSystem>();
            vehicleRigidbody = GetComponent<Rigidbody>();

            // 检查组件是否存在
            if (vehiclePhysics == null)
            {
                Debug.LogError("未找到VehiclePhysics组件！");
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

        /// 更新车辆状态
        private void Update()
        {
            if (showDebugInfo)
            {
                DisplayDebugInfo();
            }
        }

        /// 显示调试信息
        private void DisplayDebugInfo()
        {
            Debug.Log($"车辆状态: 速度={GetCurrentSpeed():F1}km/h, 在空中={isInAir}, 侧翻={isFlipped}, 倒置={isUpsideDown}, 漂移={isDrifting}");
        }

        /// 设置车辆状态
        public void SetVehicleState(bool inAir, bool flipped, bool upsideDown)
        {
            isInAir = inAir;
            isFlipped = flipped;
            isUpsideDown = upsideDown;
        }

        /// 设置漂移状态
        public void SetDriftState(bool drifting, float driftFactor)
        {
            isDrifting = drifting;
            currentDriftFactor = driftFactor;
        }

        /// 重置车辆
        public void ResetVehicle()
        {
            // 重置物理状态
            if (vehiclePhysics != null)
            {
                vehiclePhysics.ResetPhysics();
            }

            // 重置位置和旋转
            transform.position = new Vector3(transform.position.x, transform.position.y + 1.0f, transform.position.z);
            transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
        }

        /// 在传送前调用
        public void PrepareForTeleport()
        {
            // 触发传送前事件
            OnBeforeTeleport?.Invoke();

            // 重置车辆状态
            ResetVehicleState();
        }

        /// 在传送后调用
        public void FinishTeleport()
        {
            // 重置车轮状态
            if (vehiclePhysics != null)
            {
                vehiclePhysics.ResetPhysics();
            }

            // 触发传送后事件
            OnAfterTeleport?.Invoke();
        }

        /// 重置车辆状态
        private void ResetVehicleState()
        {
            // 重置物理状态
            if (vehicleRigidbody != null)
            {
                vehicleRigidbody.linearVelocity = Vector3.zero;
                vehicleRigidbody.angularVelocity = Vector3.zero;
            }
        }

        #region 公共接口

        /// 设置当前车速（由VehicleDriveSystem调用）
        public void SetVehicleSpeed(float speed)
        {
            // 存储由驱动系统传递的速度值
        }

        /// 设置氮气状态（由VehicleDriveSystem调用）
        public void SetNitroStatus(bool active, float amount)
        {
            // 存储由驱动系统传递的氮气状态
        }

        /// 获取当前车速
        public float GetCurrentSpeed()
        {
            if (vehicleDriveSystem != null)
            {
                return vehicleDriveSystem.GetCurrentSpeed();
            }
            return 0f;
        }

        /// 获取最大车速
        public float GetMaxSpeed()
        {
            if (vehicleDriveSystem != null)
            {
                return vehicleDriveSystem.GetMaxSpeed();
            }
            return 50f;
        }

        /// 获取当前驱动类型
        public VehicleDriveSystem.DriveType GetDriveType()
        {
            if (vehicleDriveSystem != null)
            {
                return vehicleDriveSystem.GetDriveType();
            }
            return VehicleDriveSystem.DriveType.FrontWheelDrive;
        }

        /// 获取是否正在漂移
        public bool IsDrifting()
        {
            return isDrifting;
        }

        /// 获取当前漂移强度
        public float GetDriftFactor()
        {
            return currentDriftFactor;
        }

        /// 获取是否在空中
        public bool IsInAir()
        {
            return isInAir;
        }

        /// 获取是否侧翻
        public bool IsFlipped()
        {
            return isFlipped;
        }

        /// 获取是否倒置
        public bool IsUpsideDown()
        {
            return isUpsideDown;
        }

        /// 获取当前氮气量（0-1）
        public float GetNitroAmount()
        {
            if (vehicleDriveSystem != null)
            {
                return vehicleDriveSystem.GetNitroAmount();
            }
            return 0f;
        }

        /// 获取氮气是否激活
        public bool IsNitroActive()
        {
            if (vehicleDriveSystem != null)
            {
                return vehicleDriveSystem.IsNitroActive();
            }
            return false;
        }

        /// 获取引擎转速
        public float GetEngineRPM()
        {
            if (vehicleDriveSystem != null)
            {
                return vehicleDriveSystem.GetEngineRPM();
            }
            return 0f;
        }

        /// 获取油门输入
        public float GetThrottleInput()
        {
            if (vehicleDriveSystem != null)
            {
                return vehicleDriveSystem.GetThrottleInput();
            }
            return 0f;
        }

        /// 获取刹车输入
        public float GetBrakeInput()
        {
            if (vehicleDriveSystem != null)
            {
                return vehicleDriveSystem.GetBrakeInput();
            }
            return 0f;
        }

        /// 获取转向输入
        public float GetSteeringInput()
        {
            if (vehicleDriveSystem != null)
            {
                return vehicleDriveSystem.GetSteeringInput();
            }
            return 0f;
        }

        /// 获取手刹状态
        public bool IsHandbrakeActive()
        {
            if (vehicleDriveSystem != null)
            {
                return vehicleDriveSystem.IsHandbrakeActive();
            }
            return false;
        }

        #endregion
    }
}