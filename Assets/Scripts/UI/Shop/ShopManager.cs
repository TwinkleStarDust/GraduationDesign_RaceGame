using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class ShopManager : MonoBehaviour
{
    #region 私有字段
    [Header("核心引用")]
    [SerializeField] private PlayerInventorySO m_PlayerInventory;
    [SerializeField] private GarageUI m_GarageUI; // To notify on purchase, and get PartItemPrefab & DragDropCanvas
    [SerializeField] private List<PartDataSO> m_AllPossibleShopParts = new List<PartDataSO>(); // 在Inspector中填充所有商店可出现的零件

    [Header("商店UI元素")]
    [SerializeField] private Transform m_ShopItemsContainer; // 商店物品UI的父容器
    [SerializeField] private Button m_RefreshShopButton;
    [SerializeField] private TextMeshProUGUI m_RefreshButtonText; // 显示刷新价格或状态
    [SerializeField] private TextMeshProUGUI m_AutoRefreshTimerText;

    [Header("商店配置")]
    [SerializeField] private int m_ShopItemCount = 6;
    [SerializeField] private int m_ManualRefreshCost = 2000;
    [SerializeField] private float m_AutoRefreshIntervalSeconds = 300f; // 5 分钟

    private List<PartDataSO> m_CurrentShopItems = new List<PartDataSO>();
    private List<PartItemUI> m_InstantiatedShopItems = new List<PartItemUI>();
    private float m_TimeUntilAutoRefresh;
    private bool m_IsShopActive = false;
    #endregion

    #region Unity生命周期
    private void Awake()
    {
        if (m_PlayerInventory == null) Debug.LogError("ShopManager: PlayerInventorySO 未分配!", this);
        if (m_GarageUI == null) Debug.LogError("ShopManager: GarageUI 未分配!", this);
        if (m_ShopItemsContainer == null) Debug.LogError("ShopManager: ShopItemsContainer 未分配!", this);
        if (m_AllPossibleShopParts.Count == 0) Debug.LogWarning("ShopManager: AllPossibleShopParts 列表为空，商店将没有物品!", this);

        if (m_RefreshShopButton != null)
        {
            m_RefreshShopButton.onClick.AddListener(AttemptManualRefresh);
        }
        UpdateRefreshButtonText();
    }

    private void Update()
    {
        if (!m_IsShopActive) return;

        if (m_TimeUntilAutoRefresh > 0)
        {
            m_TimeUntilAutoRefresh -= Time.deltaTime;
            UpdateAutoRefreshTimerText();
            if (m_TimeUntilAutoRefresh <= 0)
            {
                Debug.Log("商店自动刷新！");
                PopulateShopDisplay();
                StartAutoRefreshTimer();
            }
        }
    }
    #endregion

    #region 公共方法
    public void ActivateShop()
    {
        m_IsShopActive = true;
        gameObject.SetActive(true); // 确保ShopManager的GameObject是激活的
        if (m_TimeUntilAutoRefresh <= 0 && m_CurrentShopItems.Count == 0) // 首次激活或计时器结束时刷新
        {
            PopulateShopDisplay();
        }
        StartAutoRefreshTimer(); // 每次激活时都重置或启动计时器
        UpdateRefreshButtonText();
        UpdateAutoRefreshTimerText(); // 立即更新一次计时器显示
    }

    public void DeactivateShop()
    {
        m_IsShopActive = false;
        // gameObject.SetActive(false); // 不隐藏ShopManager本身，而是控制其内部逻辑和UI元素的可见性由GarageUI完成
    }

    public void PopulateShopDisplay()
    {
        if (m_ShopItemsContainer == null || m_GarageUI == null || m_GarageUI.GetPartItemPrefab() == null)
        {
            Debug.LogError("ShopManager: PopulateShopDisplay缺少必要引用 (Container, GarageUI, or PartItemPrefab from GarageUI)");
            return;
        }

        // 清理现有商店物品
        foreach (PartItemUI itemUI in m_InstantiatedShopItems)
        {
            if (itemUI != null) Destroy(itemUI.gameObject);
        }
        m_InstantiatedShopItems.Clear();
        m_CurrentShopItems.Clear();

        if (m_AllPossibleShopParts.Count == 0)
        {
            Debug.LogWarning("ShopManager: 没有可供选择的零件来填充商店 (AllPossibleShopParts为空)。");
            return;
        }

        for (int i = 0; i < m_ShopItemCount; i++)
        {
            if (m_AllPossibleShopParts.Count > 0) // 确保有零件可选
            {
                // 允许重复零件出现在商店中
                PartDataSO randomPart = m_AllPossibleShopParts[Random.Range(0, m_AllPossibleShopParts.Count)];
                
                if (randomPart == null)
                {
                    Debug.LogWarning("ShopManager: 从 AllPossibleShopParts 随机选取了一个 null 零件，将跳过此商店槽位。请检查Inspector中的列表。", this);
                    continue; // 跳过这个迭代，不创建空的商店物品
                }

                m_CurrentShopItems.Add(randomPart);

                GameObject itemGO = Instantiate(m_GarageUI.GetPartItemPrefab(), m_ShopItemsContainer);
                PartItemUI partItemUI = itemGO.GetComponent<PartItemUI>();
                if (partItemUI != null)
                {
                    partItemUI.Setup(randomPart, m_GarageUI); // PartItemUI现在也用于商店
                    m_InstantiatedShopItems.Add(partItemUI);
                }
            }
        }
        Debug.Log($"商店已填充 {m_CurrentShopItems.Count} 个物品。");
        StartAutoRefreshTimer(); // 每次填充后重置计时器
    }

    public bool AttemptPurchasePart(PartDataSO partToBuy, PartItemUI sourceShopItemUI)
    {
        if (m_PlayerInventory == null || partToBuy == null)
        {
            Debug.LogError("ShopManager: AttemptPurchasePart - PlayerInventory or PartDataSO is null.");
            return false;
        }

        if (m_PlayerInventory.PlayerCoins >= partToBuy.UnlockPrice)
        {
            if (m_PlayerInventory.TrySpendCoins(partToBuy.UnlockPrice))
            {
                if (m_PlayerInventory.AddPart(partToBuy))
                {
                    Debug.Log($"成功购买零件: {partToBuy.PartName} 花费: {partToBuy.UnlockPrice}");
                    m_GarageUI.NotifyPurchaseSuccessful(partToBuy); // 通知GarageUI更新
                    // 物品保留在商店中，因为它们是可重复的
                    return true; // 购买成功
                }
                else
                {
                    Debug.LogError($"购买零件 '{partToBuy.PartName}' 后未能添加到库存 (但金币已扣除!)。正在尝试退款...");
                    m_PlayerInventory.AddCoins(partToBuy.UnlockPrice); // 退款
                    return false;
                }
            }
            else
            {
                 Debug.LogWarning($"尝试购买零件 '{partToBuy.PartName}' 时扣款失败，即使检查时金币足够。");
                 return false;
            }
        }
        else
        {
            Debug.LogWarning($"金币不足，无法购买零件: {partToBuy.PartName}. 需要: {partToBuy.UnlockPrice}, 拥有: {m_PlayerInventory.PlayerCoins}");
            // 可选：显示UI提示给玩家
            return false; // 购买失败
        }
    }
    #endregion

    #region 私有方法
    private void AttemptManualRefresh()
    {
        if (m_PlayerInventory == null) return;

        if (m_PlayerInventory.PlayerCoins >= m_ManualRefreshCost)
        {
            if (m_PlayerInventory.TrySpendCoins(m_ManualRefreshCost))
            {
                Debug.Log($"花费 {m_ManualRefreshCost} 金币手动刷新商店。");
                PopulateShopDisplay();
                StartAutoRefreshTimer(); // 手动刷新后也重置自动刷新计时器
                UpdateRefreshButtonText(); // 更新按钮文本可能因为金币变化
            }
            else
            {
                 Debug.LogWarning("手动刷新商店：扣款失败，即使检查时金币足够。");
            }
        }
        else
        {
            Debug.LogWarning($"金币不足，无法手动刷新商店。需要: {m_ManualRefreshCost}, 拥有: {m_PlayerInventory.PlayerCoins}");
            // 可选：显示UI提示
        }
    }

    private void StartAutoRefreshTimer()
    {
        m_TimeUntilAutoRefresh = m_AutoRefreshIntervalSeconds;
        UpdateAutoRefreshTimerText();
    }

    private void UpdateAutoRefreshTimerText()
    {
        if (m_AutoRefreshTimerText != null)
        {
            if (m_TimeUntilAutoRefresh > 0)
            {
                int minutes = Mathf.FloorToInt(m_TimeUntilAutoRefresh / 60);
                int seconds = Mathf.FloorToInt(m_TimeUntilAutoRefresh % 60);
                m_AutoRefreshTimerText.text = $"下次刷新: {minutes:00}:{seconds:00}";
            }
            else
            {
                m_AutoRefreshTimerText.text = "下次刷新: 00:00";
            }
        }
    }

    private void UpdateRefreshButtonText()
    {
        if (m_RefreshButtonText != null)
        {
            m_RefreshButtonText.text = $"刷新 ({m_ManualRefreshCost}金币)";
        }
        if (m_RefreshShopButton != null && m_PlayerInventory != null)
        {
            m_RefreshShopButton.interactable = m_PlayerInventory.PlayerCoins >= m_ManualRefreshCost;
        }
    }
    #endregion
} 