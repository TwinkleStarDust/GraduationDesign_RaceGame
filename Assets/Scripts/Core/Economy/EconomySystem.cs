using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 经济系统 - 管理玩家货币、奖励和消费
/// </summary>
public class EconomySystem : MonoBehaviour
{
    #region 单例实现
    private static EconomySystem s_Instance;
    public static EconomySystem Instance 
    { 
        get 
        {
            if (s_Instance == null)
            {
                GameObject managerObj = GameObject.Find("EconomySystem");
                if (managerObj == null)
                {
                    managerObj = new GameObject("EconomySystem");
                    s_Instance = managerObj.AddComponent<EconomySystem>();
                }
                else
                {
                    s_Instance = managerObj.GetComponent<EconomySystem>();
                    if (s_Instance == null)
                    {
                        s_Instance = managerObj.AddComponent<EconomySystem>();
                    }
                }
                DontDestroyOnLoad(managerObj);
            }
            return s_Instance;
        }
    }
    #endregion

    #region 事件定义
    // 货币变动事件
    public event Action<int, int, bool> OnMoneyChanged;
    // 购买完成事件
    public event Action<string, int> OnPurchaseComplete;
    // 出售完成事件
    public event Action<string, int> OnSellComplete;
    #endregion

    #region 序列化字段
    [Header("经济设置")]
    [Tooltip("初始金钱")]
    [SerializeField] private int m_InitialMoney = 1000;
    
    [Tooltip("出售折扣（相对于原价）")]
    [Range(0.1f, 1.0f)]
    [SerializeField] private float m_SellDiscount = 0.5f;
    
    [Header("持久化设置")]
    [Tooltip("是否自动保存")]
    [SerializeField] private bool m_AutoSave = true;
    
    [Tooltip("货币存储的PlayerPrefs键")]
    [SerializeField] private string m_MoneyPrefsKey = "PlayerMoney";
    #endregion

    #region 公共属性
    /// <summary>
    /// 当前金钱
    /// </summary>
    public int CurrentMoney { get; private set; }
    #endregion

    #region 私有变量
    // 交易历史记录
    private List<TransactionRecord> m_TransactionHistory = new List<TransactionRecord>();
    #endregion

    #region Unity生命周期
    private void Awake()
    {
        // 单例实现检查
        if (s_Instance != null && s_Instance != this)
        {
            Destroy(this);
            return;
        }

        s_Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // 载入玩家金钱
        LoadMoney();
    }
    
    private void OnApplicationQuit()
    {
        // 应用退出时保存金钱
        if (m_AutoSave)
        {
            SaveMoney();
        }
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 添加金钱
    /// </summary>
    public void AddMoney(int amount)
    {
        if (amount <= 0) return;
        
        int oldMoney = CurrentMoney;
        CurrentMoney += amount;
        
        // 触发金钱变动事件
        OnMoneyChanged?.Invoke(oldMoney, CurrentMoney, true);
        
        // 记录交易
        m_TransactionHistory.Add(new TransactionRecord
        {
            TransactionType = TransactionType.Income,
            Amount = amount,
            Balance = CurrentMoney,
            Timestamp = DateTime.Now
        });
        
        // 自动保存
        if (m_AutoSave)
        {
            SaveMoney();
        }
        
        Debug.Log($"添加 {amount} 金钱，当前余额: {CurrentMoney}");
    }
    
    /// <summary>
    /// 扣除金钱
    /// </summary>
    public bool SpendMoney(int amount, string spendReason = "")
    {
        if (amount <= 0) return true;
        
        // 检查余额是否足够
        if (CurrentMoney < amount)
        {
            Debug.LogWarning($"金钱不足！需要 {amount}，当前余额: {CurrentMoney}");
            return false;
        }
        
        int oldMoney = CurrentMoney;
        CurrentMoney -= amount;
        
        // 触发金钱变动事件
        OnMoneyChanged?.Invoke(oldMoney, CurrentMoney, false);
        
        // 记录交易
        m_TransactionHistory.Add(new TransactionRecord
        {
            TransactionType = TransactionType.Expense,
            Description = spendReason,
            Amount = amount,
            Balance = CurrentMoney,
            Timestamp = DateTime.Now
        });
        
        // 自动保存
        if (m_AutoSave)
        {
            SaveMoney();
        }
        
        Debug.Log($"花费 {amount} 金钱，原因: {spendReason}，当前余额: {CurrentMoney}");
        return true;
    }
    
    /// <summary>
    /// 尝试购买物品
    /// </summary>
    public bool TryPurchase(string itemId, int price)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            Debug.LogError("购买失败: 无效的物品ID");
            return false;
        }
        
        // 尝试扣款
        if (SpendMoney(price, $"购买物品 {itemId}"))
        {
            // 触发购买完成事件
            OnPurchaseComplete?.Invoke(itemId, price);
            
            Debug.Log($"成功购买物品: {itemId}，价格: {price}");
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 出售物品
    /// </summary>
    public void SellItem(string itemId, int originalPrice)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            Debug.LogError("出售失败: 无效的物品ID");
            return;
        }
        
        // 计算出售价格（应用折扣）
        int sellPrice = Mathf.RoundToInt(originalPrice * m_SellDiscount);
        
        // 添加金钱
        AddMoney(sellPrice);
        
        // 触发出售完成事件
        OnSellComplete?.Invoke(itemId, sellPrice);
        
        Debug.Log($"成功出售物品: {itemId}，价格: {sellPrice}");
    }
    
    /// <summary>
    /// 重置金钱（调试用）
    /// </summary>
    public void ResetMoney()
    {
        int oldMoney = CurrentMoney;
        CurrentMoney = m_InitialMoney;
        
        // 触发金钱变动事件
        OnMoneyChanged?.Invoke(oldMoney, CurrentMoney, false);
        
        // 记录交易
        m_TransactionHistory.Add(new TransactionRecord
        {
            TransactionType = TransactionType.System,
            Description = "重置金钱",
            Amount = m_InitialMoney - oldMoney,
            Balance = CurrentMoney,
            Timestamp = DateTime.Now
        });
        
        // 保存
        SaveMoney();
        
        Debug.Log($"金钱已重置为初始值: {m_InitialMoney}");
    }
    
    /// <summary>
    /// 获取交易历史
    /// </summary>
    public List<TransactionRecord> GetTransactionHistory()
    {
        return new List<TransactionRecord>(m_TransactionHistory);
    }
    
    /// <summary>
    /// 清除交易历史
    /// </summary>
    public void ClearTransactionHistory()
    {
        m_TransactionHistory.Clear();
        Debug.Log("交易历史已清除");
    }
    #endregion

    #region 私有方法
    /// <summary>
    /// 保存金钱到PlayerPrefs
    /// </summary>
    private void SaveMoney()
    {
        PlayerPrefs.SetInt(m_MoneyPrefsKey, CurrentMoney);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// 从PlayerPrefs加载金钱
    /// </summary>
    private void LoadMoney()
    {
        // 如果存在存档则读取，否则使用初始值
        if (PlayerPrefs.HasKey(m_MoneyPrefsKey))
        {
            CurrentMoney = PlayerPrefs.GetInt(m_MoneyPrefsKey);
        }
        else
        {
            CurrentMoney = m_InitialMoney;
        }
        
        Debug.Log($"已加载金钱: {CurrentMoney}");
    }
    #endregion
}

/// <summary>
/// 交易类型枚举
/// </summary>
public enum TransactionType
{
    Income,     // 收入
    Expense,    // 支出
    System      // 系统操作
}

/// <summary>
/// 交易记录数据结构
/// </summary>
[System.Serializable]
public class TransactionRecord
{
    public TransactionType TransactionType;   // 交易类型
    public string Description;                // 交易描述
    public int Amount;                        // 交易金额
    public int Balance;                       // 交易后余额
    public DateTime Timestamp;                // 交易时间
} 