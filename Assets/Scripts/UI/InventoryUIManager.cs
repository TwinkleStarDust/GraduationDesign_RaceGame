using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Ricimi;
using RaceGame.Data;
using RaceGame.Managers;

namespace RaceGame.UI
{
    /// <summary>
    /// 库存UI管理器 - 管理物品库存UI和交互
    /// </summary>
    public class InventoryUIManager : MonoBehaviour
    {
        #region 单例实现
        private static InventoryUIManager s_Instance;
        public static InventoryUIManager Instance => s_Instance;

        private void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            s_Instance = this;
        }
        #endregion
        
        #region UI引用
        [Header("分类标签页")]
        [SerializeField] private TabMenu m_TabMenu;
        
        [Header("物品列表")]
        [SerializeField] private Transform m_AllPartsList;      // Inventory-List-1 对应全部
        [SerializeField] private Transform m_EnginePartsList;   // Inventory-List-2 对应引擎
        [SerializeField] private Transform m_TirePartsList;     // Inventory-List-3 对应轮胎
        [SerializeField] private Transform m_NitroPartsList;    // Inventory-List-4 对应氮气
        
        [Header("物品预制体")]
        [SerializeField] private GameObject m_InventoryItemPrefab;  // Inventory-Item预制体
        [SerializeField] private GameObject m_EmptyItemPrefab;      // Item-Empty预制体
        [SerializeField] private GameObject m_LockedItemPrefab;     // Item-Locked-Dot预制体
        
        [Header("性能显示")]
        [SerializeField] private TextMeshProUGUI m_SpeedText;
        [SerializeField] private TextMeshProUGUI m_AccelerationText;
        [SerializeField] private TextMeshProUGUI m_HandlingText;
        [SerializeField] private TextMeshProUGUI m_NitroText;
        
        [Header("金币显示")]
        [SerializeField] private TextMeshProUGUI m_CoinsText;
        
        [Header("消息弹窗")]
        [SerializeField] private ModularPopupOpener m_MessagePopupOpener;
        
        private Dictionary<PartType, Transform> m_ListsByType;
        #endregion
        
        #region 生命周期
        private void Start()
        {
            // 初始化列表字典
            m_ListsByType = new Dictionary<PartType, Transform>
            {
                { PartType.All, m_AllPartsList },
                { PartType.Engine, m_EnginePartsList },
                { PartType.Tire, m_TirePartsList },
                { PartType.Nitro, m_NitroPartsList }
            };
            
            // 初始加载物品
            LoadAllItems();
            
            // 更新性能显示
            UpdatePerformanceDisplay();
            
            // 更新金币显示
            UpdateCoinsDisplay();
        }
        #endregion
        
        #region UI更新方法
        /// <summary>
        /// 加载所有物品到不同分类
        /// </summary>
        public void LoadAllItems()
        {
            if (GameDataManager.Instance == null) return;
            
            // 清空现有物品
            ClearAllLists();
            
            // 获取每种类型的部件并加载到对应列表
            foreach (PartType type in System.Enum.GetValues(typeof(PartType)))
            {
                if (type == PartType.All) continue; // 全部分类单独处理
                
                LoadItemsByType(type);
            }
            
            // 加载全部分类
            LoadItemsByType(PartType.All);
        }
        
        /// <summary>
        /// 按类型加载物品
        /// </summary>
        private void LoadItemsByType(PartType type)
        {
            if (!m_ListsByType.TryGetValue(type, out Transform listParent)) return;
            
            List<CarPartData> parts = GameDataManager.Instance.GetPartsByType(type);
            foreach (var part in parts)
            {
                // 检查是否拥有
                OwnedCarPart ownedPart = GameDataManager.Instance.OwnedParts.Find(p => p.m_PartID == part.m_PartID);
                bool isOwned = ownedPart != null;
                bool isEquipped = isOwned && ownedPart.m_IsEquipped;
                
                // 创建物品UI
                CreatePartItem(part, listParent, isOwned, isEquipped);
            }
        }
        
        /// <summary>
        /// 创建单个部件UI项
        /// </summary>
        private void CreatePartItem(CarPartData partData, Transform parent, bool isOwned, bool isEquipped)
        {
            GameObject itemObj;
            
            if (isOwned)
            {
                // 使用 Inventory-Item 预制体
                itemObj = Instantiate(m_InventoryItemPrefab, parent);
                
                // 配置Tooltip
                ConfigureTooltip(itemObj, partData, isOwned);
                
                // 配置ModularPopupOpener (已拥有的部件)
                ConfigurePopupOpenerForOwnedPart(itemObj, partData, isEquipped);
                
                // 设置物品图标
                SetItemIcon(itemObj, partData.m_PartIcon);
                
                // 已装备标记
                SetEquippedIndicator(itemObj, isEquipped);
            }
            else
            {
                // 使用 Item-Locked-Dot 预制体
                itemObj = Instantiate(m_LockedItemPrefab, parent);
                
                // 配置Tooltip
                ConfigureTooltip(itemObj, partData, isOwned);
                
                // 配置ModularPopupOpener (未拥有的部件)
                ConfigurePopupOpenerForLockedPart(itemObj, partData);
            }
            
            // 存储部件ID用于事件处理
            itemObj.name = "Part_" + partData.m_PartID;
        }
        
        /// <summary>
        /// 配置Tooltip
        /// </summary>
        private void ConfigureTooltip(GameObject itemObj, CarPartData partData, bool isOwned)
        {
            Tooltip tooltip = itemObj.GetComponent<Tooltip>();
            if (tooltip != null && tooltip.tooltip != null)
            {
                // 查找tooltip中的文本组件并设置内容
                TextMeshProUGUI[] texts = tooltip.tooltip.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length > 0)
                {
                    texts[0].text = isOwned ? partData.m_PartName : "????? (未解锁)";
                    if (texts.Length > 1 && isOwned)
                    {
                        texts[1].text = GetRarityText(partData.m_Rarity);
                    }
                }
            }
        }
        
        /// <summary>
        /// 为已拥有部件配置弹窗
        /// </summary>
        private void ConfigurePopupOpenerForOwnedPart(GameObject itemObj, CarPartData partData, bool isEquipped)
        {
            ModularPopupOpener popupOpener = itemObj.GetComponent<ModularPopupOpener>();
            if (popupOpener != null)
            {
                // 设置弹窗内容
                popupOpener.Title = partData.m_PartName;
                popupOpener.Subtitle = GetPartTypeText(partData.m_PartType) + " | " + GetRarityText(partData.m_Rarity);
                popupOpener.Message = partData.m_PartDescription + "\n\n速度: +" + partData.m_SpeedBonus + 
                                      "\n加速度: +" + partData.m_AccelerationBonus + 
                                      "\n操控性: +" + partData.m_HandlingBonus + 
                                      "\n氮气效率: +" + partData.m_NitroBonus;
                popupOpener.Image = partData.m_PartIcon;
                
                // 配置按钮
                popupOpener.Buttons.Clear();
                
                if (isEquipped)
                {
                    // 已装备状态 - 添加卸载按钮
                    ButtonInfo unequipButton = new ButtonInfo
                    {
                        Label = "卸载",
                        ClosePopupWhenClicked = true
                    };
                    unequipButton.OnClickedEvent.AddListener(() => OnUnequipClicked(partData.m_PartID));
                    popupOpener.Buttons.Add(unequipButton);
                }
                else
                {
                    // 未装备状态 - 添加装备按钮
                    ButtonInfo equipButton = new ButtonInfo
                    {
                        Label = "装备",
                        ClosePopupWhenClicked = true
                    };
                    equipButton.OnClickedEvent.AddListener(() => OnEquipClicked(partData.m_PartID));
                    popupOpener.Buttons.Add(equipButton);
                    
                    // 添加出售按钮
                    ButtonInfo sellButton = new ButtonInfo
                    {
                        Label = "出售 (+" + partData.m_SellPrice + "金币)",
                        ClosePopupWhenClicked = true
                    };
                    sellButton.OnClickedEvent.AddListener(() => OnSellClicked(partData.m_PartID));
                    popupOpener.Buttons.Add(sellButton);
                }
                
                // 共用的取消按钮
                ButtonInfo cancelButton = new ButtonInfo
                {
                    Label = "取消",
                    ClosePopupWhenClicked = true
                };
                popupOpener.Buttons.Add(cancelButton);
            }
        }
        
        /// <summary>
        /// 为锁定部件配置弹窗
        /// </summary>
        private void ConfigurePopupOpenerForLockedPart(GameObject itemObj, CarPartData partData)
        {
            ModularPopupOpener popupOpener = itemObj.GetComponent<ModularPopupOpener>();
            if (popupOpener != null)
            {
                // 设置弹窗内容
                popupOpener.Title = partData.m_PartName + " (未拥有)";
                popupOpener.Subtitle = GetPartTypeText(partData.m_PartType) + " | " + GetRarityText(partData.m_Rarity);
                popupOpener.Message = "是否购买此部件?\n\n" + partData.m_PartDescription + 
                                      "\n\n价格: " + partData.m_BuyPrice + " 金币";
                popupOpener.Image = partData.m_PartIcon;
                
                // 配置按钮
                popupOpener.Buttons.Clear();
                
                // 购买按钮
                ButtonInfo buyButton = new ButtonInfo
                {
                    Label = "购买 (" + partData.m_BuyPrice + "金币)",
                    ClosePopupWhenClicked = true
                };
                buyButton.OnClickedEvent.AddListener(() => OnBuyClicked(partData.m_PartID));
                popupOpener.Buttons.Add(buyButton);
                
                // 取消按钮
                ButtonInfo cancelButton = new ButtonInfo
                {
                    Label = "取消",
                    ClosePopupWhenClicked = true
                };
                popupOpener.Buttons.Add(cancelButton);
            }
        }
        
        /// <summary>
        /// 设置物品图标
        /// </summary>
        private void SetItemIcon(GameObject itemObj, Sprite icon)
        {
            Transform itemTransform = itemObj.transform.Find("Item");
            if (itemTransform != null)
            {
                Image itemImage = itemTransform.GetComponent<Image>();
                if (itemImage != null)
                {
                    itemImage.sprite = icon;
                }
            }
        }
        
        /// <summary>
        /// 设置已装备指示器
        /// </summary>
        private void SetEquippedIndicator(GameObject itemObj, bool isEquipped)
        {
            Transform indicatorType = itemObj.transform.Find("Indicator-Type");
            if (indicatorType != null)
            {
                indicatorType.gameObject.SetActive(isEquipped);
            }
        }
        
        /// <summary>
        /// 清空所有列表
        /// </summary>
        private void ClearAllLists()
        {
            foreach (var list in m_ListsByType.Values)
            {
                if (list != null)
                {
                    ClearList(list);
                }
            }
        }
        
        /// <summary>
        /// 清空单个列表
        /// </summary>
        private void ClearList(Transform listTransform)
        {
            for (int i = listTransform.childCount - 1; i >= 0; i--)
            {
                Destroy(listTransform.GetChild(i).gameObject);
            }
        }
        
        /// <summary>
        /// 更新性能显示
        /// </summary>
        public void UpdatePerformanceDisplay()
        {
            if (VehicleManager.Instance == null) return;
            
            if (m_SpeedText != null)
                m_SpeedText.text = VehicleManager.Instance.CurrentSpeed.ToString("F1");
                
            if (m_AccelerationText != null)
                m_AccelerationText.text = VehicleManager.Instance.CurrentAcceleration.ToString("F1");
                
            if (m_HandlingText != null)
                m_HandlingText.text = VehicleManager.Instance.CurrentHandling.ToString("F1");
                
            if (m_NitroText != null)
                m_NitroText.text = VehicleManager.Instance.CurrentNitroEfficiency.ToString("F1");
        }
        
        /// <summary>
        /// 更新金币显示
        /// </summary>
        public void UpdateCoinsDisplay()
        {
            if (GameDataManager.Instance == null) return;
            
            if (m_CoinsText != null)
                m_CoinsText.text = GameDataManager.Instance.PlayerCoins.ToString();
        }
        #endregion
        
        #region 按钮回调
        /// <summary>
        /// 装备按钮回调
        /// </summary>
        private void OnEquipClicked(string partID)
        {
            if (GameDataManager.Instance == null) return;
            
            if (GameDataManager.Instance.EquipPart(partID))
            {
                // 刷新UI
                LoadAllItems();
                UpdatePerformanceDisplay();
            }
        }
        
        /// <summary>
        /// 卸载按钮回调
        /// </summary>
        private void OnUnequipClicked(string partID)
        {
            if (GameDataManager.Instance == null) return;
            
            if (GameDataManager.Instance.UnequipPart(partID))
            {
                // 刷新UI
                LoadAllItems();
                UpdatePerformanceDisplay();
            }
        }
        
        /// <summary>
        /// 购买按钮回调
        /// </summary>
        private void OnBuyClicked(string partID)
        {
            if (GameDataManager.Instance == null) return;
            
            if (GameDataManager.Instance.BuyPart(partID))
            {
                // 刷新UI
                LoadAllItems();
                UpdateCoinsDisplay();
                
                // 显示购买成功消息
                ShowMessage("购买成功！", "您已成功购买新部件。");
            }
            else
            {
                // 显示购买失败消息
                ShowMessage("购买失败", "金币不足或已拥有此部件。");
            }
        }
        
        /// <summary>
        /// 出售按钮回调
        /// </summary>
        private void OnSellClicked(string partID)
        {
            if (GameDataManager.Instance == null) return;
            
            if (GameDataManager.Instance.SellPart(partID))
            {
                // 刷新UI
                LoadAllItems();
                UpdateCoinsDisplay();
                
                // 显示出售成功消息
                ShowMessage("出售成功！", "您已成功出售部件。");
            }
        }
        #endregion
        
        #region 辅助方法
        /// <summary>
        /// 显示消息弹窗
        /// </summary>
        private void ShowMessage(string title, string message)
        {
            if (m_MessagePopupOpener != null)
            {
                m_MessagePopupOpener.Title = title;
                m_MessagePopupOpener.Message = message;
                m_MessagePopupOpener.OpenPopup();
            }
        }
        
        /// <summary>
        /// 获取部件类型文本
        /// </summary>
        private string GetPartTypeText(PartType type)
        {
            switch (type)
            {
                case PartType.Engine: return "引擎";
                case PartType.Tire: return "轮胎";
                case PartType.Nitro: return "氮气";
                default: return "未知";
            }
        }
        
        /// <summary>
        /// 获取稀有度文本
        /// </summary>
        private string GetRarityText(RaceGame.Data.PartRarity rarity)
        {
            switch (rarity)
            {
                case RaceGame.Data.PartRarity.Common: return "普通";
                case RaceGame.Data.PartRarity.Uncommon: return "不常见";
                case RaceGame.Data.PartRarity.Rare: return "稀有";
                case RaceGame.Data.PartRarity.Epic: return "史诗";
                case RaceGame.Data.PartRarity.Legendary: return "传奇";
                default: return "未知";
            }
        }
        #endregion
    }
} 