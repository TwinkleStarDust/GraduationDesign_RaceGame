using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 背包系统 - 管理玩家物品库存
/// </summary>
public class InventorySystem : MonoBehaviour
{
    #region 单例实现
    private static InventorySystem s_Instance;
    public static InventorySystem Instance 
    { 
        get 
        {
            if (s_Instance == null)
            {
                GameObject managerObj = GameObject.Find("InventorySystem");
                if (managerObj == null)
                {
                    managerObj = new GameObject("InventorySystem");
                    s_Instance = managerObj.AddComponent<InventorySystem>();
                }
                else
                {
                    s_Instance = managerObj.GetComponent<InventorySystem>();
                    if (s_Instance == null)
                    {
                        s_Instance = managerObj.AddComponent<InventorySystem>();
                    }
                }
                DontDestroyOnLoad(managerObj);
            }
            return s_Instance;
        }
    }
    #endregion

    #region 事件定义
    // 物品添加事件
    public event Action<string, int> OnItemAdded;
    // 物品移除事件
    public event Action<string, int> OnItemRemoved;
    // 物品数量变更事件
    public event Action<string, int> OnItemQuantityChanged;
    #endregion

    #region 序列化字段
    [Header("库存设置")]
    [Tooltip("默认库存容量")]
    [SerializeField] private int m_DefaultCapacity = 100;
    
    [Tooltip("是否启用容量限制")]
    [SerializeField] private bool m_EnableCapacityLimit = false;
    
    [Header("持久化设置")]
    [Tooltip("库存存储的PlayerPrefs键")]
    [SerializeField] private string m_InventoryPrefsKey = "PlayerInventory";
    
    [Tooltip("是否自动保存")]
    [SerializeField] private bool m_AutoSave = true;
    #endregion

    #region 公共属性
    /// <summary>
    /// 当前库存容量
    /// </summary>
    public int Capacity { get; private set; }
    
    /// <summary>
    /// 当前已用空间
    /// </summary>
    public int UsedSpace { get; private set; }
    
    /// <summary>
    /// 剩余空间
    /// </summary>
    public int RemainingSpace => Capacity - UsedSpace;
    
    /// <summary>
    /// 库存项目数量
    /// </summary>
    public int ItemCount => m_Inventory.Count;
    #endregion

    #region 私有变量
    // 库存数据 - 物品ID:数量
    private Dictionary<string, int> m_Inventory = new Dictionary<string, int>();
    
    // 物品数据缓存 - 物品ID:物品数据
    private Dictionary<string, PartDataSO> m_ItemDataCache = new Dictionary<string, PartDataSO>();
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
        
        // 初始化库存容量
        Capacity = m_DefaultCapacity;
        UsedSpace = 0;
        
        // 加载库存数据
        LoadInventory();
    }
    
    private void Start()
    {
        // 初始化物品数据缓存
        InitializeItemDataCache();
    }
    
    private void OnApplicationQuit()
    {
        // 应用退出时保存库存
        if (m_AutoSave)
        {
            SaveInventory();
        }
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 添加物品
    /// </summary>
    public bool AddItem(string itemId, int quantity = 1)
    {
        if (string.IsNullOrEmpty(itemId) || quantity <= 0)
        {
            Debug.LogWarning("添加物品失败: 无效的参数");
            return false;
        }
        
        // 检查库存容量
        if (m_EnableCapacityLimit && UsedSpace + quantity > Capacity)
        {
            Debug.LogWarning("添加物品失败: 库存已满");
            return false;
        }
        
        // 检查物品是否存在于库存中
        if (m_Inventory.TryGetValue(itemId, out int currentQuantity))
        {
            // 更新数量
            m_Inventory[itemId] = currentQuantity + quantity;
            
            // 触发数量变更事件
            OnItemQuantityChanged?.Invoke(itemId, m_Inventory[itemId]);
        }
        else
        {
            // 添加新物品
            m_Inventory[itemId] = quantity;
            
            // 触发物品添加事件
            OnItemAdded?.Invoke(itemId, quantity);
        }
        
        // 更新已用空间
        UsedSpace += quantity;
        
        // 自动保存
        if (m_AutoSave)
        {
            SaveInventory();
        }
        
        Debug.Log($"添加物品: {itemId} x{quantity}");
        return true;
    }
    
    /// <summary>
    /// 移除物品
    /// </summary>
    public bool RemoveItem(string itemId, int quantity = 1)
    {
        if (string.IsNullOrEmpty(itemId) || quantity <= 0)
        {
            Debug.LogWarning("移除物品失败: 无效的参数");
            return false;
        }
        
        // 检查物品是否存在于库存中
        if (!m_Inventory.TryGetValue(itemId, out int currentQuantity) || currentQuantity < quantity)
        {
            Debug.LogWarning($"移除物品失败: 物品 {itemId} 数量不足");
            return false;
        }
        
        // 更新数量
        int newQuantity = currentQuantity - quantity;
        
        if (newQuantity <= 0)
        {
            // 完全移除物品
            m_Inventory.Remove(itemId);
            
            // 触发物品移除事件
            OnItemRemoved?.Invoke(itemId, currentQuantity);
        }
        else
        {
            // 减少数量
            m_Inventory[itemId] = newQuantity;
            
            // 触发数量变更事件
            OnItemQuantityChanged?.Invoke(itemId, newQuantity);
        }
        
        // 更新已用空间
        UsedSpace -= quantity;
        
        // 自动保存
        if (m_AutoSave)
        {
            SaveInventory();
        }
        
        Debug.Log($"移除物品: {itemId} x{quantity}");
        return true;
    }
    
    /// <summary>
    /// 获取物品数量
    /// </summary>
    public int GetItemQuantity(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            return 0;
        }
        
        return m_Inventory.TryGetValue(itemId, out int quantity) ? quantity : 0;
    }
    
    /// <summary>
    /// 检查是否有足够数量的物品
    /// </summary>
    public bool HasItem(string itemId, int requiredQuantity = 1)
    {
        if (string.IsNullOrEmpty(itemId) || requiredQuantity <= 0)
        {
            return false;
        }
        
        return GetItemQuantity(itemId) >= requiredQuantity;
    }
    
    /// <summary>
    /// 获取物品数据
    /// </summary>
    public PartDataSO GetItemData(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            return null;
        }
        
        if (m_ItemDataCache.TryGetValue(itemId, out PartDataSO itemData))
        {
            return itemData;
        }
        
        // 尝试从PartUpgradeSystem获取数据
        PartDataSO partData = FindPartDataFromUpgradeSystem(itemId);
        if (partData != null)
        {
            // 更新缓存
            m_ItemDataCache[itemId] = partData;
            return partData;
        }
        
        return null;
    }
    
    /// <summary>
    /// 获取所有物品
    /// </summary>
    public Dictionary<string, int> GetAllItems()
    {
        return new Dictionary<string, int>(m_Inventory);
    }
    
    /// <summary>
    /// 清空库存
    /// </summary>
    public void ClearInventory()
    {
        // 保存清空前的物品
        Dictionary<string, int> oldInventory = new Dictionary<string, int>(m_Inventory);
        
        // 清空库存
        m_Inventory.Clear();
        UsedSpace = 0;
        
        // 触发物品移除事件
        foreach (var item in oldInventory)
        {
            OnItemRemoved?.Invoke(item.Key, item.Value);
        }
        
        // 保存
        if (m_AutoSave)
        {
            SaveInventory();
        }
        
        Debug.Log("已清空库存");
    }
    
    /// <summary>
    /// 设置库存容量
    /// </summary>
    public void SetCapacity(int newCapacity)
    {
        if (newCapacity < UsedSpace)
        {
            Debug.LogWarning($"无法设置容量: 新容量 {newCapacity} 小于当前已用空间 {UsedSpace}");
            return;
        }
        
        Capacity = newCapacity;
        Debug.Log($"设置库存容量为: {newCapacity}");
    }
    #endregion

    #region 私有方法
    /// <summary>
    /// 初始化物品数据缓存
    /// </summary>
    private void InitializeItemDataCache()
    {
        m_ItemDataCache.Clear();
        
        // 如果有PartUpgradeSystem，获取所有零部件数据
        if (PartUpgradeSystem.Instance != null)
        {
            List<PartDataSO> allParts = PartUpgradeSystem.Instance.GetAllParts();
            
            foreach (PartDataSO part in allParts)
            {
                if (part != null && !string.IsNullOrEmpty(part.PartID))
                {
                    m_ItemDataCache[part.PartID] = part;
                }
            }
        }
    }
    
    /// <summary>
    /// 从升级系统查找零部件数据
    /// </summary>
    private PartDataSO FindPartDataFromUpgradeSystem(string partId)
    {
        if (PartUpgradeSystem.Instance != null)
        {
            List<PartDataSO> allParts = PartUpgradeSystem.Instance.GetAllParts();
            
            foreach (PartDataSO part in allParts)
            {
                if (part != null && part.PartID == partId)
                {
                    return part;
                }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// 保存库存数据
    /// </summary>
    private void SaveInventory()
    {
        string inventoryData = SerializeInventory();
        PlayerPrefs.SetString(m_InventoryPrefsKey, inventoryData);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// 加载库存数据
    /// </summary>
    private void LoadInventory()
    {
        if (PlayerPrefs.HasKey(m_InventoryPrefsKey))
        {
            string inventoryData = PlayerPrefs.GetString(m_InventoryPrefsKey);
            DeserializeInventory(inventoryData);
        }
    }
    
    /// <summary>
    /// 序列化库存数据
    /// </summary>
    private string SerializeInventory()
    {
        List<string> serializedItems = new List<string>();
        
        foreach (var item in m_Inventory)
        {
            serializedItems.Add($"{item.Key}:{item.Value}");
        }
        
        return string.Join(";", serializedItems);
    }
    
    /// <summary>
    /// 反序列化库存数据
    /// </summary>
    private void DeserializeInventory(string inventoryData)
    {
        m_Inventory.Clear();
        UsedSpace = 0;
        
        if (string.IsNullOrEmpty(inventoryData))
        {
            return;
        }
        
        string[] items = inventoryData.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (string item in items)
        {
            string[] parts = item.Split(':');
            if (parts.Length == 2 && !string.IsNullOrEmpty(parts[0]) && int.TryParse(parts[1], out int quantity) && quantity > 0)
            {
                m_Inventory[parts[0]] = quantity;
                UsedSpace += quantity;
            }
        }
    }
    #endregion
}

/// <summary>
/// 库存更改类型枚举
/// </summary>
public enum InventoryChangeType
{
    Added,     // 添加
    Removed,   // 移除
    Changed    // 数量变更
} 