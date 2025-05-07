using UnityEngine;
using RaceGame.Data;

namespace RaceGame.Managers
{
    /// <summary>
    /// 车辆管理器 - 管理车辆性能参数
    /// </summary>
    public class VehicleManager : MonoBehaviour
    {
        #region 单例实现
        private static VehicleManager s_Instance;
        public static VehicleManager Instance => s_Instance;

        private void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            s_Instance = this;
            
            UpdateVehiclePerformance();
        }
        #endregion
        
        #region 车辆性能参数
        [Header("基础性能参数")]
        [SerializeField] private float m_BaseSpeed = 100f;
        [SerializeField] private float m_BaseAcceleration = 10f;
        [SerializeField] private float m_BaseHandling = 5f;
        [SerializeField] private float m_BaseNitroEfficiency = 1f;
        
        [Header("当前性能参数")]
        [SerializeField] private float m_CurrentSpeed;
        [SerializeField] private float m_CurrentAcceleration;
        [SerializeField] private float m_CurrentHandling;
        [SerializeField] private float m_CurrentNitroEfficiency;
        
        // 公开属性用于UI显示和游戏逻辑访问
        public float CurrentSpeed => m_CurrentSpeed;
        public float CurrentAcceleration => m_CurrentAcceleration;
        public float CurrentHandling => m_CurrentHandling;
        public float CurrentNitroEfficiency => m_CurrentNitroEfficiency;
        #endregion
        
        #region 性能更新
        /// <summary>
        /// 根据装备更新车辆性能
        /// </summary>
        public void UpdateVehiclePerformance()
        {
            if (GameDataManager.Instance == null) return;
            
            // 重置为基础值
            m_CurrentSpeed = m_BaseSpeed;
            m_CurrentAcceleration = m_BaseAcceleration;
            m_CurrentHandling = m_BaseHandling;
            m_CurrentNitroEfficiency = m_BaseNitroEfficiency;
            
            // 应用装备加成
            foreach (var ownedPart in GameDataManager.Instance.OwnedParts)
            {
                if (ownedPart.m_IsEquipped)
                {
                    CarPartData partData = GameDataManager.Instance.GetPartData(ownedPart.m_PartID);
                    if (partData != null)
                    {
                        m_CurrentSpeed += partData.m_SpeedBonus;
                        m_CurrentAcceleration += partData.m_AccelerationBonus;
                        m_CurrentHandling += partData.m_HandlingBonus;
                        m_CurrentNitroEfficiency += partData.m_NitroBonus;
                    }
                }
            }
            
            // 通知UI更新
            // 如果已经实现了UI管理器，可以在这里调用UI更新方法
            // 例如: InventoryUIManager.Instance?.UpdatePerformanceDisplay();
        }
        
        /// <summary>
        /// 应用性能参数到实际车辆物理系统
        /// </summary>
        public void ApplyPerformanceToVehicle(GameObject carObject)
        {
            // 这里需要根据您的车辆控制器实现来修改
            // 例如，可能需要获取车辆控制器组件并设置其物理参数
            
            // 示例代码:
            // CarController carController = carObject.GetComponent<CarController>();
            // if (carController != null)
            // {
            //     carController.SetMaxSpeed(m_CurrentSpeed);
            //     carController.SetAcceleration(m_CurrentAcceleration);
            //     carController.SetHandling(m_CurrentHandling);
            //     carController.SetNitroEfficiency(m_CurrentNitroEfficiency);
            // }
        }
        #endregion
    }
} 