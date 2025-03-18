using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 商店物品UI - 在商店中展示商品信息
/// </summary>
public class ShopItemUI : MonoBehaviour
{
    [Header("UI引用")]
    [Tooltip("物品图标")]
    [SerializeField] private Image m_Icon;
    
    [Tooltip("物品名称文本")]
    [SerializeField] private TextMeshProUGUI m_NameText;
    
    [Tooltip("物品描述文本")]
    [SerializeField] private TextMeshProUGUI m_DescriptionText;
    
    [Tooltip("物品价格文本")]
    [SerializeField] private TextMeshProUGUI m_PriceText;
    
    [Tooltip("购买按钮")]
    [SerializeField] private Button m_BuyButton;
    
    [Tooltip("已拥有标记")]
    [SerializeField] private GameObject m_OwnedTag;
    
    // 购买事件
    public event Action<string, int> OnPurchaseClicked;
    
    // 当前物品数据
    private GarageController.TempPartData m_ItemData;
    private int m_Price;
    private bool m_IsOwned = false;
    
    private void Start()
    {
        if (m_BuyButton != null)
        {
            m_BuyButton.onClick.AddListener(OnBuyButtonClicked);
        }
    }
    
    private void OnDestroy()
    {
        if (m_BuyButton != null)
        {
            m_BuyButton.onClick.RemoveListener(OnBuyButtonClicked);
        }
    }
    
    /// <summary>
    /// 设置商店物品数据
    /// </summary>
    public void SetShopItemData(GarageController.TempPartData itemData, int price)
    {
        m_ItemData = itemData;
        m_Price = price;
        
        if (m_Icon != null && itemData.Icon != null)
        {
            m_Icon.sprite = itemData.Icon;
            m_Icon.enabled = true;
        }
        else if (m_Icon != null)
        {
            // 使用默认图标
            m_Icon.enabled = true;
        }
        
        if (m_NameText != null)
        {
            m_NameText.text = itemData.PartName;
            m_NameText.color = itemData.GetRarityColor();
        }
        
        if (m_DescriptionText != null)
        {
            m_DescriptionText.text = itemData.Description;
        }
        
        if (m_PriceText != null)
        {
            m_PriceText.text = $"{price} 金币";
        }
        
        // 更新按钮状态
        UpdateBuyButtonState();
    }
    
    /// <summary>
    /// 设置是否已拥有
    /// </summary>
    public void SetOwned(bool isOwned)
    {
        m_IsOwned = isOwned;
        
        if (m_OwnedTag != null)
        {
            m_OwnedTag.SetActive(isOwned);
        }
        
        if (isOwned)
        {
            if (m_BuyButton != null)
            {
                m_BuyButton.interactable = false;
            }
            
            if (m_PriceText != null)
            {
                m_PriceText.text = "已拥有";
            }
        }
        else
        {
            UpdateBuyButtonState();
        }
    }
    
    /// <summary>
    /// 更新购买按钮状态
    /// </summary>
    private void UpdateBuyButtonState()
    {
        if (m_BuyButton != null)
        {
            // 这里使用1000作为测试金额，后面会替换为实际值
            bool canAfford = 1000 >= m_Price;
            m_BuyButton.interactable = canAfford && !m_IsOwned;
        }
    }
    
    /// <summary>
    /// 购买按钮点击回调
    /// </summary>
    private void OnBuyButtonClicked()
    {
        if (m_ItemData != null)
        {
            OnPurchaseClicked?.Invoke(m_ItemData.PartID, m_Price);
        }
    }
} 