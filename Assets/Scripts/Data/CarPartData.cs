using UnityEngine;

namespace RaceGame.Data
{
    /// <summary>
    /// 赛车部件数据，使用ScriptableObject便于在编辑器中创建和管理部件
    /// </summary>
    [CreateAssetMenu(fileName = "新部件", menuName = "赛车游戏/部件数据")]
    public class CarPartData : ScriptableObject
    {
        #region 基本信息
        [Header("基本信息")]
        [Tooltip("部件唯一ID")]
        public string m_PartID;
        [Tooltip("部件名称")]
        public string m_PartName;
        [Tooltip("部件描述")]
        [TextArea(3, 5)]
        public string m_PartDescription;
        [Tooltip("部件类型")]
        public PartType m_PartType;
        [Tooltip("部件稀有度")]
        public PartRarity m_Rarity;
        [Tooltip("部件图标")]
        public Sprite m_PartIcon;
        [Tooltip("购买价格")]
        public int m_BuyPrice;
        [Tooltip("售出价格")]
        public int m_SellPrice;
        #endregion

        #region 性能参数
        [Header("性能参数")]
        [Tooltip("速度加成")]
        public float m_SpeedBonus;
        [Tooltip("加速度加成")]
        public float m_AccelerationBonus;
        [Tooltip("操控性加成")]
        public float m_HandlingBonus;
        [Tooltip("氮气效率加成")]
        public float m_NitroBonus;
        #endregion
    }

    /// <summary>
    /// 部件类型枚举
    /// </summary>
    public enum PartType
    {
        Engine,     // 引擎
        Tire,       // 轮胎
        Nitro,      // 氮气系统
        All         // 用于UI过滤
    }

    /// <summary>
    /// 稀有度枚举
    /// </summary>
    public enum PartRarity
    {
        Common,     // 普通
        Uncommon,   // 不常见
        Rare,       // 稀有
        Epic,       // 史诗
        Legendary   // 传奇
    }
} 