using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RaceGame.Data;

namespace RaceGame.Managers
{
    /// <summary>
    /// 游戏数据管理器 - 管理玩家金币、部件库存等游戏数据
    /// </summary>
    public class GameDataManager : MonoBehaviour
    {
        #region 单例实现
        private static GameDataManager s_Instance;
        public static GameDataManager Instance => s_Instance;

        private void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            s_Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadGameData();
        }
        #endregion

        #region 玩家数据
        [Header("玩家金币")]
        [SerializeField] private int m_PlayerCoins = 1000;
        public int PlayerCoins => m_PlayerCoins;

        [Header("拥有的部件")]
        [SerializeField] private List<OwnedCarPart> m_OwnedParts = new List<OwnedCarPart>();
        public List<OwnedCarPart> OwnedParts => m_OwnedParts;

        [Header("部件数据库")]
        [SerializeField] private List<CarPartData> m_AllParts = new List<CarPartData>();
        public List<CarPartData> AllParts => m_AllParts;
        #endregion

        #region 游戏存储
        /// <summary>
        /// 加载游戏数据
        /// </summary>
        private void LoadGameData()
        {
            // 从PlayerPrefs或云存储加载数据
            if (PlayerPrefs.HasKey("PlayerCoins"))
            {
                m_PlayerCoins = PlayerPrefs.GetInt("PlayerCoins");
            }

            // 简化版 - 实际应从JSON加载拥有的部件数据
            // 此处仅为演示用，实际项目需要实现完整的存储逻辑
        }

        /// <summary>
        /// 保存游戏数据
        /// </summary>
        public void SaveGameData()
        {
            PlayerPrefs.SetInt("PlayerCoins", m_PlayerCoins);
            
            // 简化版 - 实际应序列化为JSON
            // 此处仅为演示用，实际项目需要实现完整的存储逻辑
            
            PlayerPrefs.Save();
        }
        #endregion

        #region 部件操作
        /// <summary>
        /// 通过ID查找部件数据
        /// </summary>
        public CarPartData GetPartData(string partID)
        {
            return m_AllParts.Find(part => part.m_PartID == partID);
        }

        /// <summary>
        /// 获取指定类型的所有部件
        /// </summary>
        public List<CarPartData> GetPartsByType(PartType type)
        {
            if (type == PartType.All)
                return new List<CarPartData>(m_AllParts);
                
            return m_AllParts.Where(part => part.m_PartType == type).ToList();
        }

        /// <summary>
        /// 购买部件
        /// </summary>
        public bool BuyPart(string partID)
        {
            CarPartData partData = GetPartData(partID);
            if (partData == null) return false;
            
            // 检查金币是否足够
            if (m_PlayerCoins < partData.m_BuyPrice) return false;
            
            // 检查是否已拥有
            if (m_OwnedParts.Exists(p => p.m_PartID == partID)) return false;
            
            // 扣除金币
            m_PlayerCoins -= partData.m_BuyPrice;
            
            // 添加到拥有列表
            OwnedCarPart newPart = new OwnedCarPart
            {
                m_PartID = partID,
                m_IsEquipped = false,
                m_IsLocked = false
            };
            m_OwnedParts.Add(newPart);
            
            SaveGameData();
            return true;
        }
        
        /// <summary>
        /// 出售部件
        /// </summary>
        public bool SellPart(string partID)
        {
            CarPartData partData = GetPartData(partID);
            if (partData == null) return false;
            
            // 查找拥有的部件
            OwnedCarPart ownedPart = m_OwnedParts.Find(p => p.m_PartID == partID);
            if (ownedPart == null) return false;
            
            // 如果已装备或被锁定，不能出售
            if (ownedPart.m_IsEquipped || ownedPart.m_IsLocked) return false;
            
            // 移除部件并增加金币
            m_OwnedParts.Remove(ownedPart);
            m_PlayerCoins += partData.m_SellPrice;
            
            SaveGameData();
            return true;
        }
        
        /// <summary>
        /// 装备部件
        /// </summary>
        public bool EquipPart(string partID)
        {
            CarPartData partData = GetPartData(partID);
            if (partData == null) return false;
            
            // 查找拥有的部件
            OwnedCarPart ownedPart = m_OwnedParts.Find(p => p.m_PartID == partID);
            if (ownedPart == null) return false;
            
            // 先卸下同类型部件
            foreach (var part in m_OwnedParts)
            {
                if (GetPartData(part.m_PartID).m_PartType == partData.m_PartType && part.m_IsEquipped)
                {
                    part.m_IsEquipped = false;
                }
            }
            
            // 装备新部件
            ownedPart.m_IsEquipped = true;
            
            SaveGameData();
            
            // 通知车辆管理器更新性能
            if (VehicleManager.Instance != null)
            {
                VehicleManager.Instance.UpdateVehiclePerformance();
            }
            
            return true;
        }
        
        /// <summary>
        /// 卸载部件
        /// </summary>
        public bool UnequipPart(string partID)
        {
            // 查找拥有的部件
            OwnedCarPart ownedPart = m_OwnedParts.Find(p => p.m_PartID == partID);
            if (ownedPart == null || !ownedPart.m_IsEquipped) return false;
            
            // 卸载部件
            ownedPart.m_IsEquipped = false;
            
            SaveGameData();
            
            // 通知车辆管理器更新性能
            if (VehicleManager.Instance != null)
            {
                VehicleManager.Instance.UpdateVehiclePerformance();
            }
            
            return true;
        }
        
        /// <summary>
        /// 通过比赛获得金币
        /// </summary>
        public void AddCoinsFromRace(int amount)
        {
            if (amount <= 0) return;
            
            m_PlayerCoins += amount;
            SaveGameData();
        }
        #endregion
    }
} 