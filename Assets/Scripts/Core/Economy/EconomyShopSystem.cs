using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 经济商店系统 - 管理物品的买卖功能
/// </summary>
public class EconomyShopSystem : MonoBehaviour
{
    #region 单例实现
    private static EconomyShopSystem s_Instance;
    public static EconomyShopSystem Instance
    {
        get
        {
            if (s_Instance == null)
            {
                GameObject managerObj = GameObject.Find("EconomyShopSystem");
                if (managerObj == null)
                {
                    managerObj = new GameObject("EconomyShopSystem");
                    s_Instance = managerObj.AddComponent<EconomyShopSystem>();
                }
                else
                {
                    s_Instance = managerObj.GetComponent<EconomyShopSystem>();
                    if (s_Instance == null)
                    {
                        s_Instance = managerObj.AddComponent<EconomyShopSystem>();
                    }
                }
                DontDestroyOnLoad(managerObj);
            }
            return s_Instance;
        }
    }
    #endregion

    #region 事件定义
    // 商店物品刷新事件
    public event Action OnShopRefreshed;
    // 购买成功事件
    public event Action<string, int> OnPurchaseSuccess;
    // 出售成功事件
    public event Action<string, int> OnSellSuccess;
    #endregion

    #region 序列化字段
    [Header("商店设置")]
    [Tooltip("商店物品刷新间隔（秒）")]
    [SerializeField] private float m_RefreshInterval = 600f;

    [Tooltip("商店物品数量")]
    [SerializeField] private int m_ShopItemCount = 8;

    [Tooltip("商店加价系数")]
    [Range(1.0f, 3.0f)]
    [SerializeField] private float m_BuyPriceMultiplier = 1.2f;

    [Tooltip("出售折扣系数")]
    [Range(0.1f, 1.0f)]
    [SerializeField] private float m_SellPriceMultiplier = 0.5f;

    [Tooltip("是否根据物品稀有度调整价格")]
    [SerializeField] private bool m_AdjustPriceByRarity = true;

    [Tooltip("稀有度价格系数（普通/非凡/稀有/史诗/传奇）")]
    [SerializeField] private float[] m_RarityPriceFactors = { 1.0f, 1.5f, 2.0f, 3.0f, 5.0f };

    [Header("物品池设置")]
    [Tooltip("商店可出售的所有物品")]
    [SerializeField] private List<PartDataSO> m_ShopItemsPool = new List<PartDataSO>();

    [Tooltip("普通物品出现概率")]
    [Range(0f, 1f)]
    [SerializeField] private float m_CommonItemChance = 0.6f;

    [Tooltip("非凡物品出现概率")]
    [Range(0f, 1f)]
    [SerializeField] private float m_UncommonItemChance = 0.25f;

    [Tooltip("稀有物品出现概率")]
    [Range(0f, 1f)]
    [SerializeField] private float m_RareItemChance = 0.1f;

    [Tooltip("史诗物品出现概率")]
    [Range(0f, 1f)]
    [SerializeField] private float m_EpicItemChance = 0.04f;

    [Tooltip("传奇物品出现概率")]
    [Range(0f, 1f)]
    [SerializeField] private float m_LegendaryItemChance = 0.01f;

    [Header("持久化设置")]
    [Tooltip("商店数据存储的PlayerPrefs键")]
    [SerializeField] private string m_ShopDataPrefsKey = "ShopData";

    [Tooltip("上次刷新时间存储的PlayerPrefs键")]
    [SerializeField] private string m_LastRefreshTimePrefsKey = "ShopLastRefreshTime";
    #endregion

    #region 私有变量
    // 商店库存
    private List<ShopItem> m_ShopInventory = new List<ShopItem>();

    // 上次刷新时间
    private DateTime m_LastRefreshTime;

    // 刷新计时器
    private float m_RefreshTimer = 0f;

    // 按稀有度分类的物品池
    private Dictionary<PartRarity, List<PartDataSO>> m_ItemPoolByRarity = new Dictionary<PartRarity, List<PartDataSO>>();
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

        // 初始化物品池
        InitializeItemPool();

        // 加载商店数据
        LoadShopData();
    }

    private void Start()
    {
        // 检查是否需要立即刷新商店
        CheckForRefresh();
    }

    private void Update()
    {
        // 更新刷新计时器
        if (m_RefreshInterval > 0)
        {
            m_RefreshTimer += Time.deltaTime;

            // 如果达到刷新间隔，刷新商店
            if (m_RefreshTimer >= m_RefreshInterval)
            {
                RefreshShop();
                m_RefreshTimer = 0f;
            }
        }
    }

    private void OnApplicationQuit()
    {
        // 应用退出时保存商店数据
        SaveShopData();
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 购买物品
    /// </summary>
    public bool BuyItem(int shopItemIndex)
    {
        // 检查索引是否有效
        if (shopItemIndex < 0 || shopItemIndex >= m_ShopInventory.Count)
        {
            Debug.LogWarning($"购买失败: 无效的商店物品索引 {shopItemIndex}");
            return false;
        }

        // 获取商店物品
        ShopItem shopItem = m_ShopInventory[shopItemIndex];

        // 从经济系统扣除金钱
        if (EconomySystem.Instance == null || !EconomySystem.Instance.TryPurchase(shopItem.PartID, shopItem.Price))
        {
            Debug.LogWarning("购买失败: 金钱不足");
            return false;
        }

        // 向库存系统添加物品
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.AddItem(shopItem.PartID);
        }

        // 从商店库存中移除物品
        m_ShopInventory.RemoveAt(shopItemIndex);

        // 触发购买成功事件
        OnPurchaseSuccess?.Invoke(shopItem.PartID, shopItem.Price);

        // 保存商店数据
        SaveShopData();

        Debug.Log($"成功购买物品: {shopItem.PartID}，价格: {shopItem.Price}");
        return true;
    }

    /// <summary>
    /// 出售物品
    /// </summary>
    public bool SellItem(string itemId, int quantity = 1)
    {
        if (string.IsNullOrEmpty(itemId) || quantity <= 0)
        {
            Debug.LogWarning("出售失败: 无效的参数");
            return false;
        }

        // 检查库存中是否有足够的物品
        if (InventorySystem.Instance == null || !InventorySystem.Instance.HasItem(itemId, quantity))
        {
            Debug.LogWarning($"出售失败: 库存中没有足够的物品 {itemId}");
            return false;
        }

        // 获取物品数据
        PartDataSO itemData = InventorySystem.Instance.GetItemData(itemId);
        if (itemData == null)
        {
            Debug.LogWarning($"出售失败: 找不到物品数据 {itemId}");
            return false;
        }

        // 计算出售价格
        int sellPrice = CalculateSellPrice(itemData) * quantity;

        // 从库存中移除物品
        if (!InventorySystem.Instance.RemoveItem(itemId, quantity))
        {
            Debug.LogWarning($"出售失败: 无法从库存中移除物品 {itemId}");
            return false;
        }

        // 添加金钱到经济系统
        if (EconomySystem.Instance != null)
        {
            EconomySystem.Instance.AddMoney(sellPrice);
        }

        // 触发出售成功事件
        OnSellSuccess?.Invoke(itemId, sellPrice);

        Debug.Log($"成功出售物品: {itemId} x{quantity}，获得: {sellPrice} 金钱");
        return true;
    }

    /// <summary>
    /// 计算物品的买入价格
    /// </summary>
    public int CalculateBuyPrice(PartDataSO itemData)
    {
        if (itemData == null)
        {
            return 0;
        }

        // 基础价格
        float basePrice = itemData.UnlockPrice;

        // 应用商店加价
        float adjustedPrice = basePrice * m_BuyPriceMultiplier;

        // 根据稀有度调整价格
        if (m_AdjustPriceByRarity)
        {
            int rarityIndex = (int)itemData.Rarity;
            if (rarityIndex >= 0 && rarityIndex < m_RarityPriceFactors.Length)
            {
                adjustedPrice *= m_RarityPriceFactors[rarityIndex];
            }
        }

        // 四舍五入到整数
        return Mathf.Max(1, Mathf.RoundToInt(adjustedPrice));
    }

    /// <summary>
    /// 计算物品的出售价格
    /// </summary>
    public int CalculateSellPrice(PartDataSO itemData)
    {
        if (itemData == null)
        {
            return 0;
        }

        // 基础价格
        float basePrice = itemData.UnlockPrice;

        // 应用出售折扣
        float adjustedPrice = basePrice * m_SellPriceMultiplier;

        // 根据稀有度调整价格
        if (m_AdjustPriceByRarity)
        {
            int rarityIndex = (int)itemData.Rarity;
            if (rarityIndex >= 0 && rarityIndex < m_RarityPriceFactors.Length)
            {
                // 出售时稀有度影响较小
                float rarityFactor = Mathf.Lerp(1.0f, m_RarityPriceFactors[rarityIndex], 0.5f);
                adjustedPrice *= rarityFactor;
            }
        }

        // 四舍五入到整数
        return Mathf.Max(1, Mathf.RoundToInt(adjustedPrice));
    }

    /// <summary>
    /// 获取商店库存
    /// </summary>
    public List<ShopItem> GetShopInventory()
    {
        return new List<ShopItem>(m_ShopInventory);
    }

    /// <summary>
    /// 手动刷新商店
    /// </summary>
    public void RefreshShop()
    {
        // 记录刷新时间
        m_LastRefreshTime = DateTime.Now;

        // 生成新的商店物品
        GenerateShopItems();

        // 保存商店数据
        SaveShopData();

        // 重置刷新计时器
        m_RefreshTimer = 0f;

        // 触发商店刷新事件
        OnShopRefreshed?.Invoke();

        Debug.Log("商店已刷新");
    }

    /// <summary>
    /// 获取距离下次刷新的剩余时间（秒）
    /// </summary>
    public float GetTimeUntilNextRefresh()
    {
        return Mathf.Max(0f, m_RefreshInterval - m_RefreshTimer);
    }
    #endregion

    #region 私有方法
    /// <summary>
    /// 初始化物品池
    /// </summary>
    private void InitializeItemPool()
    {
        // 清空稀有度物品池
        m_ItemPoolByRarity.Clear();

        // 初始化每个稀有度的列表
        foreach (PartRarity rarity in Enum.GetValues(typeof(PartRarity)))
        {
            m_ItemPoolByRarity[rarity] = new List<PartDataSO>();
        }

        // 将物品按稀有度分类
        foreach (PartDataSO item in m_ShopItemsPool)
        {
            if (item != null)
            {
                m_ItemPoolByRarity[item.Rarity].Add(item);
            }
        }

        // 如果商店物品池为空，尝试从PartUpgradeSystem获取物品
        if (m_ShopItemsPool.Count == 0 && PartUpgradeSystem.Instance != null)
        {
            m_ShopItemsPool = PartUpgradeSystem.Instance.GetAllParts();

            // 重新分类
            foreach (PartDataSO item in m_ShopItemsPool)
            {
                if (item != null)
                {
                    m_ItemPoolByRarity[item.Rarity].Add(item);
                }
            }
        }
    }

    /// <summary>
    /// 检查是否需要刷新商店
    /// </summary>
    private void CheckForRefresh()
    {
        // 如果商店库存为空，直接刷新
        if (m_ShopInventory.Count == 0)
        {
            RefreshShop();
            return;
        }

        // 计算距离上次刷新的时间
        TimeSpan timeSinceLastRefresh = DateTime.Now - m_LastRefreshTime;

        // 如果超过刷新间隔，刷新商店
        if (timeSinceLastRefresh.TotalSeconds >= m_RefreshInterval)
        {
            RefreshShop();
        }
        else
        {
            // 设置刷新计时器
            m_RefreshTimer = (float)timeSinceLastRefresh.TotalSeconds;
        }
    }

    /// <summary>
    /// 生成商店物品
    /// </summary>
    private void GenerateShopItems()
    {
        // 清空现有商店库存
        m_ShopInventory.Clear();

        // 已选物品ID的集合，避免重复
        HashSet<string> selectedItemIds = new HashSet<string>();

        // 生成指定数量的商店物品
        for (int i = 0; i < m_ShopItemCount; i++)
        {
            // 选择物品稀有度
            PartRarity rarity = SelectRandomRarity();

            // 获取对应稀有度的物品池
            List<PartDataSO> itemsPool = m_ItemPoolByRarity[rarity];

            // 如果物品池为空，尝试其他稀有度
            if (itemsPool.Count == 0)
            {
                foreach (var pool in m_ItemPoolByRarity.Values)
                {
                    if (pool.Count > 0)
                    {
                        itemsPool = pool;
                        break;
                    }
                }

                // 如果所有池都为空，跳过
                if (itemsPool.Count == 0)
                {
                    continue;
                }
            }

            // 尝试10次找到未选择的物品
            PartDataSO selectedItem = null;
            for (int attempt = 0; attempt < 10; attempt++)
            {
                int randomIndex = UnityEngine.Random.Range(0, itemsPool.Count);
                PartDataSO candidateItem = itemsPool[randomIndex];

                // 检查是否已选择
                if (!selectedItemIds.Contains(candidateItem.PartID))
                {
                    selectedItem = candidateItem;
                    selectedItemIds.Add(candidateItem.PartID);
                    break;
                }
            }

            // 如果找不到未选择的物品，随机选择一个
            if (selectedItem == null && itemsPool.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, itemsPool.Count);
                selectedItem = itemsPool[randomIndex];
            }

            // 如果成功选择了物品，添加到商店库存
            if (selectedItem != null)
            {
                // 计算价格
                int price = CalculateBuyPrice(selectedItem);

                // 创建商店物品
                ShopItem shopItem = new ShopItem
                {
                    PartID = selectedItem.PartID,
                    Price = price,
                    Rarity = selectedItem.Rarity
                };

                // 添加到商店库存
                m_ShopInventory.Add(shopItem);
            }
        }
    }

    /// <summary>
    /// 选择随机稀有度
    /// </summary>
    private PartRarity SelectRandomRarity()
    {
        float roll = UnityEngine.Random.value;
        float cumulativeChance = 0f;

        // 传奇
        cumulativeChance += m_LegendaryItemChance;
        if (roll < cumulativeChance)
        {
            return PartRarity.Legendary;
        }

        // 史诗
        cumulativeChance += m_EpicItemChance;
        if (roll < cumulativeChance)
        {
            return PartRarity.Epic;
        }

        // 稀有
        cumulativeChance += m_RareItemChance;
        if (roll < cumulativeChance)
        {
            return PartRarity.Rare;
        }

        // 非凡
        cumulativeChance += m_UncommonItemChance;
        if (roll < cumulativeChance)
        {
            return PartRarity.Uncommon;
        }

        // 默认为普通
        return PartRarity.Common;
    }

    /// <summary>
    /// 保存商店数据
    /// </summary>
    private void SaveShopData()
    {
        // 序列化商店库存
        string shopData = SerializeShopInventory();
        PlayerPrefs.SetString(m_ShopDataPrefsKey, shopData);

        // 保存上次刷新时间
        PlayerPrefs.SetString(m_LastRefreshTimePrefsKey, m_LastRefreshTime.ToString("o"));

        PlayerPrefs.Save();
    }

    /// <summary>
    /// 加载商店数据
    /// </summary>
    private void LoadShopData()
    {
        // 加载商店库存
        if (PlayerPrefs.HasKey(m_ShopDataPrefsKey))
        {
            string shopData = PlayerPrefs.GetString(m_ShopDataPrefsKey);
            DeserializeShopInventory(shopData);
        }

        // 加载上次刷新时间
        if (PlayerPrefs.HasKey(m_LastRefreshTimePrefsKey))
        {
            string lastRefreshTimeStr = PlayerPrefs.GetString(m_LastRefreshTimePrefsKey);
            if (DateTime.TryParse(lastRefreshTimeStr, out DateTime lastRefreshTime))
            {
                m_LastRefreshTime = lastRefreshTime;
            }
            else
            {
                m_LastRefreshTime = DateTime.Now;
            }
        }
        else
        {
            m_LastRefreshTime = DateTime.Now;
        }
    }

    /// <summary>
    /// 序列化商店库存
    /// </summary>
    private string SerializeShopInventory()
    {
        List<string> serializedItems = new List<string>();

        foreach (ShopItem item in m_ShopInventory)
        {
            serializedItems.Add($"{item.PartID}:{item.Price}:{(int)item.Rarity}");
        }

        return string.Join(";", serializedItems);
    }

    /// <summary>
    /// 反序列化商店库存
    /// </summary>
    private void DeserializeShopInventory(string shopData)
    {
        m_ShopInventory.Clear();

        if (string.IsNullOrEmpty(shopData))
        {
            return;
        }

        string[] items = shopData.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string item in items)
        {
            string[] parts = item.Split(':');
            if (parts.Length == 3 && !string.IsNullOrEmpty(parts[0]) &&
                int.TryParse(parts[1], out int price) &&
                int.TryParse(parts[2], out int rarityInt))
            {
                // 创建商店物品
                ShopItem shopItem = new ShopItem
                {
                    PartID = parts[0],
                    Price = price,
                    Rarity = (PartRarity)rarityInt
                };

                // 添加到商店库存
                m_ShopInventory.Add(shopItem);
            }
        }
    }
    #endregion
}

/// <summary>
/// 商店物品数据结构
/// </summary>
[System.Serializable]
public class ShopItem
{
    public string PartID;       // 零部件ID
    public int Price;           // 价格
    public PartRarity Rarity;   // 稀有度
}