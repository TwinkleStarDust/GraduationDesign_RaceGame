using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections;

namespace Vehicle
{
    /// <summary>
    /// 车辆控制器
    /// 作为车辆的主控制器，协调各个子系统
    /// </summary>
    public class VehicleController : MonoBehaviour
    {
        [Header("车辆状态")]
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

        /// <summary>
        /// 初始化组件
        /// </summary>
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

        /// <summary>
        /// 更新车辆状态
        /// </summary>
        private void Update()
        {
            if (showDebugInfo)
            {
                DisplayDebugInfo();
            }
        }

        /// <summary>
        /// 显示调试信息
        /// </summary>
        private void DisplayDebugInfo()
        {
            Debug.Log($"车辆状态: 速度={GetCurrentSpeed():F1}km/h, 在空中={isInAir}, 侧翻={isFlipped}, 倒置={isUpsideDown}, 漂移={isDrifting}");
        }

        /// <summary>
        /// 设置车辆状态
        /// </summary>
        public void SetVehicleState(bool inAir, bool flipped, bool upsideDown)
        {
            isInAir = inAir;
            isFlipped = flipped;
            isUpsideDown = upsideDown;
        }

        /// <summary>
        /// 设置漂移状态
        /// </summary>
        public void SetDriftState(bool drifting, float driftFactor)
        {
            isDrifting = drifting;
            currentDriftFactor = driftFactor;
        }

        /// <summary>
        /// 重置车辆
        /// </summary>
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
            if (vehiclePhysics != null)
            {
                vehiclePhysics.ResetPhysics();
            }

            // 触发传送后事件
            OnAfterTeleport?.Invoke();
        }

        /// <summary>
        /// 重置车辆状态
        /// </summary>
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

        /// <summary>
        /// 获取当前车速
        /// </summary>
        public float GetCurrentSpeed()
        {
            if (vehicleDriveSystem != null)
            {
                return vehicleDriveSystem.GetCurrentSpeed();
            }
            return 0f;
        }

        /// <summary>
        /// 获取最大车速
        /// </summary>
        public float GetMaxSpeed()
        {
            if (vehicleDriveSystem != null)
            {
                return vehicleDriveSystem.GetMaxSpeed();
            }
            return 100f;
        }

        /// <summary>
        /// 获取当前驱动类型
        /// </summary>
        public VehicleDriveSystem.DriveType GetDriveType()
        {
            if (vehicleDriveSystem != null)
            {
                return vehicleDriveSystem.GetDriveType();
            }
            return VehicleDriveSystem.DriveType.FrontWheelDrive;
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
        /// 获取是否在空中
        /// </summary>
        public bool IsInAir()
        {
            return isInAir;
        }

        /// <summary>
        /// 获取是否侧翻
        /// </summary>
        public bool IsFlipped()
        {
            return isFlipped;
        }

        /// <summary>
        /// 获取是否倒置
        /// </summary>
        public bool IsUpsideDown()
        {
            return isUpsideDown;
        }

        /// <summary>
        /// 获取当前氮气量（0-1）
        /// </summary>
        public float GetNitroAmount()
        {
            if (vehicleDriveSystem != null)
            {
                return vehicleDriveSystem.GetNitroAmount();
            }
            return 0f;
        }

        /// <summary>
        /// 获取氮气是否激活
        /// </summary>
        public bool IsNitroActive()
        {
            if (vehicleDriveSystem != null)
            {
                return vehicleDriveSystem.IsNitroActive();
            }
            return false;
        }

        /// <summary>
        /// 获取油门输入
        /// </summary>
        public float GetThrottleInput()
        {
            if (vehicleDriveSystem != null)
            {
                return vehicleDriveSystem.GetThrottleInput();
            }
            return 0f;
        }

        /// <summary>
        /// 获取刹车输入
        /// </summary>
        public float GetBrakeInput()
        {
            if (vehicleDriveSystem != null)
            {
                return vehicleDriveSystem.GetBrakeInput();
            }
            return 0f;
        }

        /// <summary>
        /// 获取转向输入
        /// </summary>
        public float GetSteeringInput()
        {
            if (vehicleDriveSystem != null)
            {
                return vehicleDriveSystem.GetSteeringInput();
            }
            return 0f;
        }

        /// <summary>
        /// 获取手刹状态
        /// </summary>
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