using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

/// <summary>
/// 商店控制器 - 管理商店界面
/// </summary>
public class ShopController : MonoBehaviour
{
    // 临时商店物品类
    [Serializable]
    public class TempShopItem
    {
        public string PartID;
        public int Price;
        public GarageController.TempPartCategory Category;
    }
    
    [Header("UI引用")]
    [Tooltip("商品容器")]
    [SerializeField] private Transform m_ItemsContainer;
    
    [Tooltip("商店物品预制体")]
    [SerializeField] private GameObject m_ShopItemPrefab;
    
    [Tooltip("刷新按钮")]
    [SerializeField] private Button m_RefreshButton;
    
    [Tooltip("返回按钮")]
    [SerializeField] private Button m_BackButton;
    
    [Tooltip("金钱文本")]
    [SerializeField] private TextMeshProUGUI m_MoneyText;
    
    [Tooltip("刷新倒计时文本")]
    [SerializeField] private TextMeshProUGUI m_RefreshTimeText;
    
    [Header("设置")]
    [Tooltip("Tab页切换")]
    [SerializeField] private Toggle[] m_CategoryToggles;
    
    [Tooltip("空状态提示")]
    [SerializeField] private GameObject m_EmptyStatePrompt;
    
    // 更新计时器
    private float m_UpdateTimer = 0f;
    
    // 临时商店库存和测试数据
    private List<TempShopItem> m_ShopInventory = new List<TempShopItem>();
    private List<GarageController.TempPartData> m_TestPartData = new List<GarageController.TempPartData>();
    private int m_PlayerMoney = 1000;
    
    private void Start()
    {
        // 初始化测试数据
        InitializeTestData();
        
        // 注册按钮事件
        if (m_RefreshButton != null)
            m_RefreshButton.onClick.AddListener(OnRefreshClicked);
            
        if (m_BackButton != null)
            m_BackButton.onClick.AddListener(OnBackClicked);
        
        // 加载商店物品
        LoadShopItems();
        
        // 更新金钱显示
        UpdateMoneyDisplay();
        
        // 设置刷新时间
        UpdateRefreshTimeDisplay();
    }
    
    private void InitializeTestData()
    {
        // 创建测试零部件数据
        m_TestPartData.Add(new GarageController.TempPartData { 
            PartID = "tire_01", 
            PartName = "标准轮胎", 
            Description = "基础性能轮胎",
            PartCategory = GarageController.TempPartCategory.Tire,
            HandlingModifier = 0.1f,
            BrakeForceModifier = 0.1f
        });
        
        m_TestPartData.Add(new GarageController.TempPartData { 
            PartID = "tire_02", 
            PartName = "高级轮胎", 
            Description = "提升操控性能",
            PartCategory = GarageController.TempPartCategory.Tire,
            HandlingModifier = 0.2f,
            BrakeForceModifier = 0.15f
        });
        
        m_TestPartData.Add(new GarageController.TempPartData { 
            PartID = "engine_01", 
            PartName = "标准引擎", 
            Description = "基础性能引擎",
            PartCategory = GarageController.TempPartCategory.Engine,
            SpeedModifier = 0.1f,
            AccelerationModifier = 0.1f
        });
        
        m_TestPartData.Add(new GarageController.TempPartData { 
            PartID = "nitro_01", 
            PartName = "标准氮气", 
            Description = "基础氮气系统",
            PartCategory = GarageController.TempPartCategory.Nitro,
            SpeedModifier = 0.05f,
            AccelerationModifier = 0.15f
        });
        
        // 创建商店物品
        m_ShopInventory.Add(new TempShopItem { 
            PartID = "tire_01", 
            Price = 500,
            Category = GarageController.TempPartCategory.Tire
        });
        
        m_ShopInventory.Add(new TempShopItem { 
            PartID = "engine_01", 
            Price = 800,
            Category = GarageController.TempPartCategory.Engine
        });
        
        m_ShopInventory.Add(new TempShopItem { 
            PartID = "nitro_01", 
            Price = 1200,
            Category = GarageController.TempPartCategory.Nitro
        });
    }
    
    private void Update()
    {
        // 每秒更新一次刷新时间
        m_UpdateTimer += Time.deltaTime;
        if (m_UpdateTimer >= 1f)
        {
            m_UpdateTimer = 0f;
            UpdateRefreshTimeDisplay();
        }
    }
    
    /// <summary>
    /// 加载商店物品
    /// </summary>
    private void LoadShopItems()
    {
        if (m_ShopItemPrefab == null || m_ItemsContainer == null)
            return;
        
        // 清空现有商品
        foreach (Transform child in m_ItemsContainer)
        {
            Destroy(child.gameObject);
        }
        
        // 检查是否为空
        if (m_ShopInventory.Count == 0 && m_EmptyStatePrompt != null)
        {
            m_EmptyStatePrompt.SetActive(true);
            return;
        }
        else if (m_EmptyStatePrompt != null)
        {
            m_EmptyStatePrompt.SetActive(false);
        }
        
        // 为每个商品创建UI项
        foreach (TempShopItem item in m_ShopInventory)
        {
            GarageController.TempPartData itemData = null;
            
            // 查找物品数据
            foreach (var part in m_TestPartData)
            {
                if (part.PartID == item.PartID)
                {
                    itemData = part;
                    break;
                }
            }
            
            if (itemData != null)
            {
                GameObject itemGO = Instantiate(m_ShopItemPrefab, m_ItemsContainer);
                ShopItemUI itemUI = itemGO.GetComponent<ShopItemUI>();
                
                if (itemUI != null)
                {
                    // 设置商品数据
                    itemUI.SetShopItemData(itemData, item.Price);
                    
                    // 注册购买事件
                    itemUI.OnPurchaseClicked += OnItemPurchased;
                }
            }
        }
    }
    
    /// <summary>
    /// 商品购买回调
    /// </summary>
    private void OnItemPurchased(string partID, int price)
    {
        // 检查是否有足够金钱
        if (m_PlayerMoney >= price)
        {
            // 扣除金钱
            m_PlayerMoney -= price;
            
            // 更新显示
            UpdateMoneyDisplay();
            
            // 从商店中移除物品
            m_ShopInventory.RemoveAll(item => item.PartID == partID);
            
            // 刷新商店显示
            LoadShopItems();
            
            Debug.Log($"成功购买物品: {partID}，价格: {price}");
        }
        else
        {
            Debug.Log("金钱不足，无法购买");
        }
    }
    
    /// <summary>
    /// 刷新按钮点击回调
    /// </summary>
    private void OnRefreshClicked()
    {
        // 重新生成随机商品
        m_ShopInventory.Clear();
        
        // 添加随机商品
        for (int i = 0; i < 3; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, m_TestPartData.Count);
            GarageController.TempPartData part = m_TestPartData[randomIndex];
            
            // 基于类别生成价格
            int price = 500;
            switch (part.PartCategory)
            {
                case GarageController.TempPartCategory.Tire:
                    price = 500 + UnityEngine.Random.Range(-100, 200);
                    break;
                case GarageController.TempPartCategory.Engine:
                    price = 800 + UnityEngine.Random.Range(-200, 300);
                    break;
                case GarageController.TempPartCategory.Nitro:
                    price = 1200 + UnityEngine.Random.Range(-300, 400);
                    break;
            }
            
            m_ShopInventory.Add(new TempShopItem { 
                PartID = part.PartID, 
                Price = price,
                Category = part.PartCategory
            });
        }
        
        // 刷新商店显示
        LoadShopItems();
        
        Debug.Log("商店已刷新");
    }
    
    /// <summary>
    /// 返回按钮点击回调
    /// </summary>
    private void OnBackClicked()
    {
        // 关闭商店界面
        Debug.Log("关闭商店界面");
        // 尝试隐藏当前面板
        if (transform.parent != null)
            transform.parent.gameObject.SetActive(false);
    }
    
    /// <summary>
    /// 更新金钱显示
    /// </summary>
    private void UpdateMoneyDisplay()
    {
        if (m_MoneyText != null)
        {
            m_MoneyText.text = $"金币: {m_PlayerMoney}";
        }
    }
    
    /// <summary>
    /// 更新刷新时间显示
    /// </summary>
    private void UpdateRefreshTimeDisplay()
    {
        if (m_RefreshTimeText != null)
        {
            // 这里使用模拟时间，30秒一个循环
            float timeUntilRefresh = 30 - (Time.time % 30);
            
            if (timeUntilRefresh <= 3)
            {
                m_RefreshTimeText.text = "可刷新";
                
                // 启用刷新按钮
                if (m_RefreshButton != null)
                {
                    m_RefreshButton.interactable = true;
                }
            }
            else
            {
                int minutes = Mathf.FloorToInt(timeUntilRefresh / 60);
                int seconds = Mathf.FloorToInt(timeUntilRefresh % 60);
                
                m_RefreshTimeText.text = $"刷新倒计时: {minutes:00}:{seconds:00}";
                
                // 禁用刷新按钮
                if (m_RefreshButton != null)
                {
                    m_RefreshButton.interactable = false;
                }
            }
        }
    }
} 