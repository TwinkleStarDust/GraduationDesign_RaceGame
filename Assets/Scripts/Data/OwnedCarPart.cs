using System;
using UnityEngine;

namespace RaceGame.Data
{
    /// <summary>
    /// 玩家拥有的部件实例
    /// </summary>
    [Serializable]
    public class OwnedCarPart
    {
        /// <summary>
        /// 对应部件ID
        /// </summary>
        public string m_PartID;
        
        /// <summary>
        /// 是否已装备
        /// </summary>
        public bool m_IsEquipped;
        
        /// <summary>
        /// 是否锁定（不可出售）
        /// </summary>
        public bool m_IsLocked;
    }
} 