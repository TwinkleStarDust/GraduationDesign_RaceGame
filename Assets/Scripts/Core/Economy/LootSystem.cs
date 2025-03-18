using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 物品掉落系统 - 管理比赛后的物品随机掉落
/// </summary>
public class LootSystem : MonoBehaviour
{
    #region 单例实现
    private static LootSystem s_Instance;
    public static LootSystem Instance 
    { 
        get 
        {
            if (s_Instance == null)
            {
                GameObject managerObj = GameObject.Find("LootSystem");
                if (managerObj == null)
                {
                    managerObj = new GameObject("LootSystem");
                    s_Instance = managerObj.AddComponent<LootSystem>();
                }
                else
                {
                    s_Instance = managerObj.GetComponent<LootSystem>();
                    if (s_Instance == null)
                    {
                        s_Instance = managerObj.AddComponent<LootSystem>();
                    }
                }
                DontDestroyOnLoad(managerObj);
            }
            return s_Instance;
        }
    }
    #endregion

    #region 序列化字段
    [Header("掉落设置")]
    [Tooltip("普通物品池")]
    [SerializeField] private List<PartDataSO> m_CommonPool = new List<PartDataSO>();
    
    [Tooltip("稀有物品池")]
    [SerializeField] private List<PartDataSO> m_RarePool = new List<PartDataSO>();
    
    [Tooltip("史诗物品池")]
    [SerializeField] private List<PartDataSO> m_EpicPool = new List<PartDataSO>();
    
    [Header("掉落概率")]
    [Tooltip("稀有物品基础掉落概率")]
    [Range(0f, 1f)]
    [SerializeField] private float m_BaseRareDropChance = 0.3f;
    
    [Tooltip("史诗物品基础掉落概率")]
    [Range(0f, 1f)]
    [SerializeField] private float m_BaseEpicDropChance = 0.1f;
    
    [Tooltip("获得重复物品的概率")]
    [Range(0f, 1f)]
    [SerializeField] private float m_DuplicateDropChance = 0.2f;
    
    [Header("关卡修正")]
    [Tooltip("最小难度系数")]
    [SerializeField] private float m_MinDifficultyFactor = 1.0f;
    
    [Tooltip("最大难度系数")]
    [SerializeField] private float m_MaxDifficultyFactor = 2.0f;
    
    [Tooltip("每关卡增加的难度系数")]
    [SerializeField] private float m_DifficultyIncrementPerLevel = 0.1f;
    #endregion

    #region 私有变量
    // 随机数生成器
    private System.Random m_Random;
    #endregion

    #region Unity生命周期
    private void Awake()
    {
        // 单例实现检查
        if (s_Instance != null && s_Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        s_Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // 初始化随机数生成器
        m_Random = new System.Random();
    }
    
    private void Start()
    {
        // 注册比赛结束事件
        if (RaceManager.Instance != null)
        {
            RaceManager.Instance.OnRaceCompleted += OnRaceCompleted;
        }
    }
    
    private void OnDestroy()
    {
        // 取消注册事件
        if (RaceManager.Instance != null)
        {
            RaceManager.Instance.OnRaceCompleted -= OnRaceCompleted;
        }
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 生成掉落物品
    /// </summary>
    /// <param name="minItems">最小物品数量</param>
    /// <param name="maxItems">最大物品数量</param>
    /// <param name="rareChanceModifier">稀有度修正（0-1）</param>
    /// <returns>掉落的物品ID列表</returns>
    public List<string> GenerateLoot(int minItems = 1, int maxItems = 3, float rareChanceModifier = 0f)
    {
        // 结果列表
        List<string> droppedItems = new List<string>();
        
        // 如果物品池为空，直接返回
        if (m_CommonPool.Count == 0 && m_RarePool.Count == 0 && m_EpicPool.Count == 0)
        {
            Debug.LogWarning("物品池为空，无法生成掉落");
            return droppedItems;
        }
        
        // 确定掉落物品数量
        int itemCount = Random.Range(minItems, maxItems + 1);
        
        // 当前关卡难度系数
        float difficultyFactor = GetCurrentDifficultyFactor();
        
        // 计算实际掉落概率
        float rareDropChance = Mathf.Clamp01(m_BaseRareDropChance * difficultyFactor + rareChanceModifier);
        float epicDropChance = Mathf.Clamp01(m_BaseEpicDropChance * difficultyFactor + rareChanceModifier * 0.5f);
        
        // 已解锁的零部件集合（用于避免重复掉落）
        HashSet<string> unlockedParts = GetUnlockedPartIDs();
        
        // 生成每个物品
        for (int i = 0; i < itemCount; i++)
        {
            string droppedItemID = GenerateSingleLoot(rareDropChance, epicDropChance, unlockedParts);
            
            if (!string.IsNullOrEmpty(droppedItemID))
            {
                droppedItems.Add(droppedItemID);
                
                // 如果已成功添加到背包，暂时在解锁集合中添加该物品，避免连续掉落相同物品
                unlockedParts.Add(droppedItemID);
            }
        }
        
        // 将掉落的物品添加到库存
        if (InventorySystem.Instance != null)
        {
            foreach (string itemID in droppedItems)
            {
                InventorySystem.Instance.AddItem(itemID);
            }
        }
        
        return droppedItems;
    }
    
    /// <summary>
    /// 设置物品池
    /// </summary>
    public void SetLootPools(List<PartDataSO> commonPool, List<PartDataSO> rarePool, List<PartDataSO> epicPool)
    {
        m_CommonPool = commonPool;
        m_RarePool = rarePool;
        m_EpicPool = epicPool;
    }
    #endregion

    #region 私有方法
    /// <summary>
    /// 比赛完成回调
    /// </summary>
    private void OnRaceCompleted(float time, int position)
    {
        // 比赛结束时自动调用生成掉落
        // 这里可以根据完成时间和名次调整稀有度修正
        float rareModifier = 0f;
        
        // 名次越高，稀有度修正越大
        if (position <= 1)
        {
            rareModifier += 0.2f;
        }
        else if (position <= 3)
        {
            rareModifier += 0.1f;
        }
        
        // 生成掉落物品（暂不直接调用，由RaceManager统一控制）
        // GenerateLoot(1, 3, rareModifier);
    }
    
    /// <summary>
    /// 生成单个掉落物品
    /// </summary>
    private string GenerateSingleLoot(float rareChance, float epicChance, HashSet<string> unlockedParts)
    {
        // 先决定稀有度
        float rarityRoll = Random.value;
        List<PartDataSO> selectedPool;
        
        if (rarityRoll < epicChance && m_EpicPool.Count > 0)
        {
            selectedPool = m_EpicPool;
        }
        else if (rarityRoll < rareChance + epicChance && m_RarePool.Count > 0)
        {
            selectedPool = m_RarePool;
        }
        else
        {
            selectedPool = m_CommonPool;
        }
        
        // 如果选中的物品池为空，回退到普通物品池
        if (selectedPool.Count == 0)
        {
            selectedPool = m_CommonPool;
            
            // 如果所有池都为空，返回空
            if (selectedPool.Count == 0)
            {
                return string.Empty;
            }
        }
        
        // 尝试最多10次找到未解锁的物品
        for (int attempt = 0; attempt < 10; attempt++)
        {
            // 随机选择物品
            int index = Random.Range(0, selectedPool.Count);
            PartDataSO selectedPart = selectedPool[index];
            
            // 如果物品未解锁，或者允许重复掉落
            if (!unlockedParts.Contains(selectedPart.PartID) || Random.value < m_DuplicateDropChance)
            {
                return selectedPart.PartID;
            }
        }
        
        // 如果多次尝试后仍找不到合适的物品，随机选一个
        if (selectedPool.Count > 0)
        {
            int index = Random.Range(0, selectedPool.Count);
            return selectedPool[index].PartID;
        }
        
        return string.Empty;
    }
    
    /// <summary>
    /// 获取当前关卡难度系数
    /// </summary>
    private float GetCurrentDifficultyFactor()
    {
        int currentLevel = 1;
        
        // 如果有GameManager，获取当前关卡
        if (GameManager.Instance != null)
        {
            currentLevel = GameManager.Instance.CurrentLevelIndex + 1;
        }
        
        // 计算难度系数
        float difficultyFactor = m_MinDifficultyFactor + (currentLevel - 1) * m_DifficultyIncrementPerLevel;
        
        // 限制在最小/最大值范围内
        return Mathf.Clamp(difficultyFactor, m_MinDifficultyFactor, m_MaxDifficultyFactor);
    }
    
    /// <summary>
    /// 获取已解锁零部件ID集合
    /// </summary>
    private HashSet<string> GetUnlockedPartIDs()
    {
        HashSet<string> unlockedParts = new HashSet<string>();
        
        // 如果有PartUpgradeSystem，获取已解锁零部件
        if (PartUpgradeSystem.Instance != null)
        {
            List<PartDataSO> allParts = PartUpgradeSystem.Instance.GetAllParts();
            
            foreach (PartDataSO part in allParts)
            {
                if (PartUpgradeSystem.Instance.IsPartUnlocked(part.PartID))
                {
                    unlockedParts.Add(part.PartID);
                }
            }
        }
        
        // 如果有InventorySystem，添加库存中的物品
        if (InventorySystem.Instance != null)
        {
            Dictionary<string, int> inventory = InventorySystem.Instance.GetAllItems();
            foreach (string itemID in inventory.Keys)
            {
                unlockedParts.Add(itemID);
            }
        }
        
        return unlockedParts;
    }
    #endregion
} 