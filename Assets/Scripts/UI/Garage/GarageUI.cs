using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro; // 确保已导入TextMeshPro
using UnityEngine.EventSystems;

public class GarageUI : MonoBehaviour
{
    #region 枚举
    public enum GarageViewMode { OwnedParts, Shop }
    #endregion

    #region 私有字段
    [Header("核心数据和管理器")] // 分组
    [SerializeField] private PlayerInventorySO m_PlayerInventory; // 引用玩家库存ScriptableObject
    [SerializeField] private ShopManager m_ShopManager; // <-- 新增：商店管理器引用

    [Header("视图切换 UI")] // 新增分组
    [SerializeField] private Button m_GarageTabButton;
    [SerializeField] private Button m_ShopTabButton;
    [SerializeField] private GameObject m_GaragePanelContent; // 车库专属内容的父对象
    [SerializeField] private GameObject m_ShopPanelContent;   // 商店专属内容的父对象
    [SerializeField] private TransactionDropZone m_TransactionDropZone; // <-- 新增：交易区域引用
    [SerializeField] private TextMeshProUGUI m_TransactionDropZoneLabel; // <-- 新增：交易区域标签

    [Header("当前车辆展示 (车库视图)")]
    [SerializeField] private TextMeshProUGUI m_CurrentVehicleNameText;
    [SerializeField] private Image m_CurrentVehicleImage; // 或者使用RawImage + RenderTexture来展示3D模型
    [SerializeField] private Transform m_VehicleDisplayPoint; // 用于实例化和展示车辆3D模型的挂点
    private GameObject m_CurrentVehicleInstance;

    // 修改：使用 PartSlotUI 引用来显示已装备零件
    [Header("零件插槽UI引用")]
    [SerializeField] private PartSlotUI m_EngineSlotUI;
    [SerializeField] private PartSlotUI m_TiresSlotUI;
    [SerializeField] private PartSlotUI m_NOSSlotUI;

    [Header("拥有零件列表UI (车库视图)")]
    [SerializeField] private Transform m_OwnedPartsContainer; // <--- 确保这个在Inspector中被赋值
    [SerializeField] private GameObject m_PartItemPrefab;    // <--- 确保这个预制件存在并被赋值
    [SerializeField] private RectTransform m_OwnedPartsScrollViewport; // <--- 新增：请在Inspector中赋值ScrollView的Viewport RectTransform

    #region 新增：零件详情面板UI引用
    [Header("零件详情面板")]
    [SerializeField] private GameObject m_PartDetailPanel; // 零件详情面板的父级GameObject
    [SerializeField] private Image m_DetailPartIconImage;
    [SerializeField] private TextMeshProUGUI m_DetailPartNameText;
    [SerializeField] private TextMeshProUGUI m_DetailPartDescriptionText;
    private PartDataSO m_CurrentlySelectedPartForDetail; // 用于追踪当前选中的零件
    #endregion

    [Header("导航和车辆切换按钮")] // 更新Header
    [SerializeField] private Button m_SelectCurrentVehicleButton; // 新增：选择当前显示车辆的按钮
    [SerializeField] private Button m_NextVehicleButton;
    [SerializeField] private Button m_PreviousVehicleButton;
    [SerializeField] private Button m_BackButton;
    // 可以添加切换车辆、更换零件的按钮等

    #region 新增：金币显示
    [Header("玩家信息显示")]
    [SerializeField] private TextMeshProUGUI m_PlayerCoinsText; // <--- 新增: 引用显示金币的Text
    #endregion

    private int m_CurrentVehicleIndex = 0; // 新增：追踪当前显示的车辆索引

    // 新增：视图模式
    private GarageViewMode m_CurrentMode = GarageViewMode.OwnedParts; // 默认为车库视图

    // 新增：拖放辅助字段
    private Canvas m_MainCanvas; // 用于拖拽坐标转换
    private RectTransform m_DraggingItemsParent; // 拖拽时零件项的临时父对象 (通常是顶层Canvas的RectTransform)
    // 重命名: 用于帮助判断拖放是否被任何有效区域处理 (插槽或出售区)
    private bool m_DragWasHandledThisFrame = false; 
    private List<PartItemUI> m_InstantiatedPartItems = new List<PartItemUI>(); // 追踪列表中的UI项

    // 新增：用于管理从插槽拖拽的零件
    private PartItemUI m_DraggingItemFromSlot_OriginalItemUI;
    private PartCategory m_DraggingItemFromSlot_SourceCategory = PartCategory.None;
    #endregion

    #region Unity生命周期
    private void Awake()
    {
        m_MainCanvas = GetComponentInParent<Canvas>(); // 获取父级Canvas
        if (m_MainCanvas == null) Debug.LogError("GarageUI 必须位于Canvas的子对象中才能进行拖放！");
        m_DraggingItemsParent = m_MainCanvas != null ? m_MainCanvas.GetComponent<RectTransform>() : null; // 通常是Canvas本身
        
        // <-- 新增：检查 ShopManager 引用
        if (m_ShopManager == null) Debug.LogError("GarageUI: ShopManager 未在Inspector中分配!", this);

        // 初始化零件插槽
        if (m_EngineSlotUI != null) m_EngineSlotUI.Initialize(this);
        if (m_TiresSlotUI != null) m_TiresSlotUI.Initialize(this);
        if (m_NOSSlotUI != null) m_NOSSlotUI.Initialize(this);

        if (m_PartDetailPanel != null) m_PartDetailPanel.SetActive(false); // 初始隐藏详情面板
    }

    private void OnEnable() // 当面板激活时更新显示
    {
        if (m_PlayerInventory == null)
        {
            Debug.LogError("玩家库存 PlayerInventorySO 未在GarageUI中分配!");
            return;
        }
        m_CurrentVehicleIndex = FindPlayerCurrentVehicleIndex();
        // UpdateGarageDisplay(); // 不再直接调用这个，由切换视图方法处理
        SwitchToGarageView(); // <-- 默认激活时显示车库视图
        ClearPartDetails(); // 当车库面板激活时，清除/隐藏详情
    }

    private int FindPlayerCurrentVehicleIndex()
    {
        if (m_PlayerInventory != null && m_PlayerInventory.m_CurrentVehicle != null && m_PlayerInventory.m_OwnedVehicles.Count > 0)
        {
            for (int i = 0; i < m_PlayerInventory.m_OwnedVehicles.Count; i++)
            {
                if (m_PlayerInventory.m_OwnedVehicles[i] == m_PlayerInventory.m_CurrentVehicle)
                {
                    return i;
                }
            }
        }
        return 0; // 默认返回第一个或者0如果没有车辆
    }

    private void Start()
    {
        if (m_BackButton != null) m_BackButton.onClick.AddListener(OnBackButtonPressed);
        if (m_NextVehicleButton != null) m_NextVehicleButton.onClick.AddListener(ShowNextVehicle);
        if (m_PreviousVehicleButton != null) m_PreviousVehicleButton.onClick.AddListener(ShowPreviousVehicle);
        if (m_SelectCurrentVehicleButton != null) m_SelectCurrentVehicleButton.onClick.AddListener(OnSelectCurrentVehicleButtonPressed);

        // <-- 新增：标签按钮监听器
        if (m_GarageTabButton != null) m_GarageTabButton.onClick.AddListener(SwitchToGarageView);
        if (m_ShopTabButton != null) m_ShopTabButton.onClick.AddListener(SwitchToShopView);
    }
    #endregion

    #region 私有方法
    private void UpdateGarageDisplay()
    {
        if (m_PlayerInventory == null) return;
        m_DragWasHandledThisFrame = false; // 重置拖拽处理标记

        // 更新车辆显示
        UpdateVehicleDisplay();

        // 更新零件插槽的显示
        if (m_EngineSlotUI != null) m_EngineSlotUI.UpdateSlotDisplay(m_PlayerInventory.m_EquippedEngine);
        if (m_TiresSlotUI != null) m_TiresSlotUI.UpdateSlotDisplay(m_PlayerInventory.m_EquippedTires);
        if (m_NOSSlotUI != null) m_NOSSlotUI.UpdateSlotDisplay(m_PlayerInventory.m_EquippedNOS);

        PopulateOrRefreshOwnedPartsList(); 

        // 更新金币显示
        UpdateCoinDisplay(); // <--- 提取金币更新逻辑到单独方法

        // 可选：如果希望在刷新时保持当前选中的零件详情，可以取消注释下一行
        // ShowPartDetails(m_CurrentlySelectedPartForDetail);
    }

    private void UpdateVehicleDisplay()
    {
        VehicleData vehicleToDisplay = null;
        if (m_PlayerInventory.m_OwnedVehicles.Count > 0)
        {
            // 确保索引在范围内
            m_CurrentVehicleIndex = Mathf.Clamp(m_CurrentVehicleIndex, 0, m_PlayerInventory.m_OwnedVehicles.Count - 1);
            vehicleToDisplay = m_PlayerInventory.m_OwnedVehicles[m_CurrentVehicleIndex];
        }

        if (vehicleToDisplay != null)
        {
            if (m_CurrentVehicleNameText != null) m_CurrentVehicleNameText.text = vehicleToDisplay.m_VehicleName;
            if (m_CurrentVehicleImage != null && vehicleToDisplay.m_VehicleIcon != null) 
            {
                m_CurrentVehicleImage.sprite = vehicleToDisplay.m_VehicleIcon;
                m_CurrentVehicleImage.gameObject.SetActive(true);
            }
            else if (m_CurrentVehicleImage != null) m_CurrentVehicleImage.gameObject.SetActive(false);

            if (m_VehicleDisplayPoint != null && vehicleToDisplay.m_VehiclePrefab != null)
            {
                if (m_CurrentVehicleInstance != null) Destroy(m_CurrentVehicleInstance);
                m_CurrentVehicleInstance = Instantiate(vehicleToDisplay.m_VehiclePrefab, m_VehicleDisplayPoint.position, m_VehicleDisplayPoint.rotation, m_VehicleDisplayPoint);
            }
            if (m_SelectCurrentVehicleButton != null) 
                m_SelectCurrentVehicleButton.interactable = (m_PlayerInventory.m_CurrentVehicle != vehicleToDisplay);
        }
        else
        {
            if (m_CurrentVehicleNameText != null) m_CurrentVehicleNameText.text = "没有拥有的车辆";
            if (m_CurrentVehicleImage != null) m_CurrentVehicleImage.gameObject.SetActive(false);
            if (m_CurrentVehicleInstance != null) Destroy(m_CurrentVehicleInstance);
            if (m_SelectCurrentVehicleButton != null) m_SelectCurrentVehicleButton.interactable = false; 
        }

        if (m_PreviousVehicleButton != null) m_PreviousVehicleButton.interactable = (m_PlayerInventory.m_OwnedVehicles.Count > 1 && m_CurrentVehicleIndex > 0);
        if (m_NextVehicleButton != null) m_NextVehicleButton.interactable = (m_PlayerInventory.m_OwnedVehicles.Count > 1 && m_CurrentVehicleIndex < m_PlayerInventory.m_OwnedVehicles.Count - 1);
    }

    private void PopulateOrRefreshOwnedPartsList()
    {
        if (m_OwnedPartsContainer == null || m_PartItemPrefab == null || m_PlayerInventory == null) return;

        // 简单起见，先全部销毁再重新创建。可以优化为对象池或仅更新现有项。
        foreach (Transform child in m_OwnedPartsContainer) Destroy(child.gameObject);
        m_InstantiatedPartItems.Clear();

        foreach (PartDataSO partData in m_PlayerInventory.m_OwnedParts)
        {
            GameObject partItemGO = Instantiate(m_PartItemPrefab, m_OwnedPartsContainer);
            PartItemUI partItemUI = partItemGO.GetComponent<PartItemUI>();
            if (partItemUI != null)
            {
                partItemUI.Setup(partData, this);
                m_InstantiatedPartItems.Add(partItemUI);

                // 如果零件已被装备，则在列表中隐藏该零件项
                bool isEquipped = (m_PlayerInventory.m_EquippedEngine == partData && partData.PartCategoryProperty == PartCategory.Engine) ||
                                  (m_PlayerInventory.m_EquippedTires == partData && partData.PartCategoryProperty == PartCategory.Tire) ||
                                  (m_PlayerInventory.m_EquippedNOS == partData && partData.PartCategoryProperty == PartCategory.Nitro);
                
                partItemGO.SetActive(!isEquipped); 
            }
            else Debug.LogWarning($"PartItemPrefab ({m_PartItemPrefab.name}) 上缺少 PartItemUI 组件！");
        }
         // 确保布局生效
        // LayoutRebuilder.ForceRebuildLayoutImmediate(m_OwnedPartsContainer as RectTransform);
        // Canvas.ForceUpdateCanvases();
    }

    public void OnPartSelectedForEquipping(PartDataSO _selectedPart) // 此方法现在由拖放处理，可能不再需要
    {
       // Deprecated by drag and drop, but can be kept for other click interactions if needed
       // if (m_PlayerInventory == null || _selectedPart == null) return;
       // m_PlayerInventory.EquipPart(_selectedPart);
       // Debug.Log($"(通过点击)已装备零件: {_selectedPart.PartName} 到库存记录中。");
       // UpdateGarageDisplay();
    }

    private void ShowNextVehicle()
    {
        if (m_PlayerInventory != null && m_PlayerInventory.m_OwnedVehicles.Count > 0)
        {
            m_CurrentVehicleIndex = (m_CurrentVehicleIndex + 1) % m_PlayerInventory.m_OwnedVehicles.Count;
            UpdateGarageDisplay();
        }
    }

    private void ShowPreviousVehicle()
    {
        if (m_PlayerInventory != null && m_PlayerInventory.m_OwnedVehicles.Count > 0)
        {
            m_CurrentVehicleIndex--;
            if (m_CurrentVehicleIndex < 0) m_CurrentVehicleIndex = m_PlayerInventory.m_OwnedVehicles.Count - 1;
            UpdateGarageDisplay();
        }
    }

    private void OnSelectCurrentVehicleButtonPressed()
    {
        if (m_PlayerInventory != null && m_PlayerInventory.m_OwnedVehicles.Count > 0 &&
            m_CurrentVehicleIndex >= 0 && m_CurrentVehicleIndex < m_PlayerInventory.m_OwnedVehicles.Count)
            {
            VehicleData selectedVehicle = m_PlayerInventory.m_OwnedVehicles[m_CurrentVehicleIndex];
            m_PlayerInventory.m_CurrentVehicle = selectedVehicle; 
            Debug.Log($"玩家已选择车辆: {selectedVehicle.m_VehicleName}");
            UpdateGarageDisplay(); 
        }
    }

    private void OnBackButtonPressed()
    {
        Debug.Log("从车库/商店返回主菜单");
        if (MainMenuUIManager.Instance != null) 
        {
            MainMenuUIManager.Instance.ShowMainMenuPanel(); 
        }
        else Debug.LogError("MainMenuUIManager 实例未找到！"); 
    }
    #endregion

    #region 新增：零件详情逻辑
    /// <summary>
    /// 当玩家选择一个零件时（点击列表项或装备槽），调用此方法显示其详情。
    /// </summary>
    public void ShowPartDetails(PartDataSO _partData)
    {
        m_CurrentlySelectedPartForDetail = _partData; // 记录当前显示的零件

        if (m_PartDetailPanel == null || m_DetailPartIconImage == null || m_DetailPartNameText == null || m_DetailPartDescriptionText == null)
        {
            Debug.LogWarning("GarageUI: 零件详情面板的部分或全部UI引用未设置！");
            if (m_PartDetailPanel != null) m_PartDetailPanel.SetActive(false);
            return;
        }

        if (_partData != null)
        {
            m_PartDetailPanel.SetActive(true);
            if (_partData.Icon != null) // 假设 PartDataSO 有 Icon 属性
            {
                m_DetailPartIconImage.sprite = _partData.Icon;
                m_DetailPartIconImage.gameObject.SetActive(true);
            }
            else
            {
                m_DetailPartIconImage.gameObject.SetActive(false);
            }
            m_DetailPartNameText.text = _partData.PartName; // 假设 PartDataSO 有 PartName 属性
            m_DetailPartDescriptionText.text = _partData.Description; // 假设 PartDataSO 有 Description 属性
        }
        else
        {
            // 如果传入的_partData为null，则清除并隐藏详情面板
            ClearPartDetails();
        }
    }

    /// <summary>
    /// 清除并隐藏零件详情面板。
    /// </summary>
    public void ClearPartDetails()
    {
        m_CurrentlySelectedPartForDetail = null; // 清除记录
        if (m_PartDetailPanel != null)
        {
            m_PartDetailPanel.SetActive(false);
        }
        // 清除内容（可选，如果仅隐藏面板可能不需要）
        if (m_DetailPartIconImage != null) m_DetailPartIconImage.sprite = null;
        if (m_DetailPartNameText != null) m_DetailPartNameText.text = "";
        if (m_DetailPartDescriptionText != null) m_DetailPartDescriptionText.text = "";
    }
    #endregion

    #region 拖放公共辅助方法
    public Canvas GetMainCanvas()
    {
        return m_MainCanvas;
    }

    public Transform GetDragDropCanvasTransform() // 拖拽时，UI项的临时父级
    {
        return m_DraggingItemsParent != null ? m_DraggingItemsParent : transform; // Fallback to self if null
    }

    public void OnPartDragStateChanged(PartDataSO partData, bool isDragging)
    {
        if (isDragging)
        {
            // 重置标记，等待任何有效的 OnDrop 处理
            m_DragWasHandledThisFrame = false; 
            Debug.Log($"GarageUI: Part {partData.PartName} 开始拖拽。");
        }
        else // 拖拽结束
        {
            Debug.Log($"GarageUI: Part {partData.PartName} 结束拖拽。");

            // (如果 m_DragWasHandledThisFrame 为 true, HandleDropOnPartSlot 或 AttemptSellPart 会处理逻辑)
            // 检查这是否是一个从插槽开始的拖拽
            if (m_DraggingItemFromSlot_OriginalItemUI != null && m_DraggingItemFromSlot_OriginalItemUI.GetPartData() == partData)
            {
                if (!m_DragWasHandledThisFrame) // 并且它没有被放置到有效的插槽上
                {
                    Debug.Log($"GarageUI: Part {partData.PartName} from slot {m_DraggingItemFromSlot_SourceCategory} was not dropped on a valid target. Unequipping.");
                    m_PlayerInventory.UnequipPart(m_DraggingItemFromSlot_SourceCategory);
                    // PartItemUI 应该通过它自己的 OnEndDrag 逻辑返回到零件列表中了。
                    // 我们需要确保它在列表中是可见的。
                    m_DraggingItemFromSlot_OriginalItemUI.gameObject.SetActive(true); 
                }
                // 如果 m_DragWasHandledThisFrame 为 true, HandleDropOnPartSlot 会处理逻辑。
                // 零件项如果被重新装备可能会失活，或者如果被交换则保持激活。
                
                // 重置从插槽拖拽的状态
                m_DraggingItemFromSlot_OriginalItemUI = null;
                m_DraggingItemFromSlot_SourceCategory = PartCategory.None;
                UpdateGarageDisplay(); // 刷新UI以反映更改（零件列表，插槽）
            }
            // 如果它不是从插槽拖拽，或者已经被处理，
            // UpdateGarageDisplay 将由 HandleDropOnPartSlot 调用，
            // 或者我们也可能需要在这里调用一次，以防零件列表项被拖拽到空白区域
            // (它会返回，没有状态改变，但为了安全起见)。
            else if (m_DraggingItemFromSlot_OriginalItemUI == null && !m_DragWasHandledThisFrame)
            {
                // 这意味着一个来自零件列表的项被拖拽到空白区域。
                // 它返回到列表中。没有实际数据变化，但为了保持一致性调用。
                // UpdateGarageDisplay(); // 如果 PopulateOrRefresh 已经正确处理了可见性，则可能多余。
            }
            else // Drag was handled
            {
                 // If drag was handled and it came from a slot, ensure state is reset
                 if (m_DraggingItemFromSlot_OriginalItemUI != null && m_DraggingItemFromSlot_OriginalItemUI.GetPartData() == partData)
                 {
                      m_DraggingItemFromSlot_OriginalItemUI = null;
                      m_DraggingItemFromSlot_SourceCategory = PartCategory.None;
                      // UI update likely happened in the handler (Slot or Sell)
                 }
            }
        }
    }
    
    /// <summary>
    /// 由 PartItemUI 在 OnEndDrag 时调用，以检查拖放是否已被有效处理 (插槽或出售区)。
    /// </summary>
    public bool WasDragHandled(PartItemUI itemUIWhichFinishedDragging) 
    {
        // 返回通用标记
        return m_DragWasHandledThisFrame;
    }

    /// <summary>
    /// 当一个零件从装备槽开始被拖拽时，由 PartSlotUI 调用。
    /// </summary>
    public void StartDragFromSlot(PartSlotUI sourceSlot, PartDataSO partToDrag, PointerEventData eventData)
    {
        if (m_PlayerInventory == null || sourceSlot == null || partToDrag == null)
        {
            Debug.LogError("GarageUI: StartDragFromSlot called with null arguments.");
            return;
        }

        PartItemUI foundItem = null;
        Debug.Log($"GarageUI: Trying to find PartItemUI for drag target: {(partToDrag != null ? partToDrag.PartName : "NULL PartToDrag")} (Instance ID: {(partToDrag != null ? partToDrag.GetInstanceID().ToString() : "N/A")})");
        Debug.Log($"GarageUI: Searching in m_InstantiatedPartItems which has {m_InstantiatedPartItems.Count} items.");

        foreach (PartItemUI itemUI in m_InstantiatedPartItems)
        {
            if (itemUI == null)
            {
                Debug.LogWarning("GarageUI: Encountered a null PartItemUI in m_InstantiatedPartItems list.");
                continue;
            }
            PartDataSO currentItemPartData = itemUI.GetPartData();
            if (currentItemPartData == null)
            {
                Debug.LogWarning($"GarageUI: PartItemUI {itemUI.gameObject.name} has null PartData.");
                continue;
            }
            bool isMatch = currentItemPartData == partToDrag;
            Debug.Log($"GarageUI: Checking against item: {currentItemPartData.PartName} (Instance ID: {currentItemPartData.GetInstanceID()}). Match: {isMatch}");
            if (isMatch)
            {
                foundItem = itemUI;
                Debug.Log($"GarageUI: Found matching PartItemUI: {foundItem.gameObject.name}");
                break;
            }
        }

        if (foundItem != null)
            {
            m_DraggingItemFromSlot_OriginalItemUI = foundItem;
            m_DraggingItemFromSlot_SourceCategory = sourceSlot.GetSlotCategory();

            // 立即在视觉上清空源插槽
            sourceSlot.UpdateSlotDisplay(null); 
            
            // 准备零件项以进行拖拽
            foundItem.gameObject.SetActive(true); // 确保在开始拖拽前是激活的
            foundItem.StartExternalDrag(eventData); // 这会将该项设置为 pointerDrag

            m_DragWasHandledThisFrame = false; // 为这个新的拖拽操作重置标记
            Debug.Log($"GarageUI: Initiated drag for {partToDrag.PartName} from slot {sourceSlot.GetSlotCategory()}.");
            }
            else
            {
            Debug.LogError($"GarageUI: Could not find PartItemUI for {partToDrag.PartName} to start drag from slot.");
            }
        }

    /// <summary>
    /// 当一个 PartItemUI 被拖放到一个 PartSlotUI 上时，由 PartSlotUI 调用。
    /// </summary>
    public void HandleDropOnPartSlot(PartSlotUI targetSlot, PartItemUI draggedPartItemUI)
    {
        if (m_PlayerInventory == null || targetSlot == null || draggedPartItemUI == null) return;

        PartDataSO partToEquip = draggedPartItemUI.GetPartData();
        if (partToEquip == null) return;

        // 1. 获取当前插槽中已装备的零件 (如果有的话)
        PartDataSO currentlyEquippedInSlot = null;
        switch (targetSlot.GetSlotCategory())
        {
            case PartCategory.Engine: currentlyEquippedInSlot = m_PlayerInventory.m_EquippedEngine; break;
            case PartCategory.Tire:   currentlyEquippedInSlot = m_PlayerInventory.m_EquippedTires;  break;
            case PartCategory.Nitro:  currentlyEquippedInSlot = m_PlayerInventory.m_EquippedNOS;    break;
        }

        // 2. 如果要装备的零件与插槽中已有的零件相同
        if (currentlyEquippedInSlot == partToEquip)
        {
            Debug.Log("尝试将同一零件装备到其已在的插槽中。无操作。");
            m_DragWasHandledThisFrame = false; 
            return;
        }

        // 3. 装备新零件
        m_PlayerInventory.EquipPart(partToEquip);
        Debug.Log($"零件 {partToEquip.PartName} 已通过拖放装备到 {targetSlot.GetSlotCategory()} 插槽。");

        // 新增：装备成功后，显示该零件的详情
        ShowPartDetails(partToEquip);

        // 如果被拖放的零件是从一个插槽拖拽出来的，现在它已经被成功安放，清除拖拽自插槽的状态。
        if (m_DraggingItemFromSlot_OriginalItemUI == draggedPartItemUI)
        {
            m_DraggingItemFromSlot_OriginalItemUI = null;
            m_DraggingItemFromSlot_SourceCategory = PartCategory.None;
        }

        // 4. 标记拖拽已成功处理
        m_DragWasHandledThisFrame = true;
        draggedPartItemUI.gameObject.SetActive(false); // 隐藏从列表中拖拽的项，因为它现在"在"插槽里

        // 5. 刷新整个车库UI (包括插槽显示和零件列表的可见性)
        UpdateGarageDisplay();
    }

    /// <summary>
    /// 由 SellDropZone 调用，尝试出售零件。
    /// </summary>
    public void AttemptSellPart(PartDataSO partToSell)
    {
        if (m_PlayerInventory == null || partToSell == null)
        {
            Debug.LogError("AttemptSellPart: Invalid arguments received.");
            return;
        }

        // 再次检查是否装备（虽然 PlayerInventorySO.RemovePart 也会检查，但在这里提供早期反馈更好）
        if (m_PlayerInventory.m_EquippedEngine == partToSell || 
            m_PlayerInventory.m_EquippedTires == partToSell || 
            m_PlayerInventory.m_EquippedNOS == partToSell)
        {
            Debug.LogWarning($"无法出售已装备的零件: {partToSell.PartName}");
            // 不要设置 m_DragWasHandledThisFrame = true; 让物品弹回
            // 可以考虑添加一个短暂的UI提示给玩家
            return;
        }

        // 计算售价 (100%)
        int sellPrice = partToSell.UnlockPrice; 
        // 可以在这里添加逻辑，比如根据稀有度打折等
        // sellPrice = Mathf.RoundToInt(partToSell.UnlockPrice * 0.5f); // 50% 售价示例

        // 尝试从库存移除
        if (m_PlayerInventory.RemovePart(partToSell))
        {
            // 移除成功，增加金币
            m_PlayerInventory.AddCoins(sellPrice);
            
            Debug.Log($"成功出售零件 '{partToSell.PartName}'，获得 {sellPrice} 金币。");

            // 标记拖拽已成功处理，这样 PartItemUI 就不会弹回
            m_DragWasHandledThisFrame = true;

            // 清除可能显示的该零件的详情
            if (m_CurrentlySelectedPartForDetail == partToSell)
            {
                ClearPartDetails();
            }

            // 刷新车库显示（主要为了更新零件列表和可能的金币显示）
            UpdateGarageDisplay(); 
            // 注意：如果 UpdateGarageDisplay 开销很大，可以考虑只更新必要部分
        }
        else
        {
            // 移除失败（可能因为已装备或其他原因，InventorySO应已记录警告）
            Debug.LogError($"出售零件 '{partToSell.PartName}' 失败，未能从库存中移除。");
            // 不要设置 m_DragWasHandledThisFrame = true; 让物品弹回
        }
    }

    public RectTransform GetOwnedPartsScrollViewportRect()
    {
        return m_OwnedPartsScrollViewport;
    }

    public bool AttemptAutoEquip(PartItemUI draggedItemUI)
    {
        // --- 开始修改 ---
        // 增加模式检查：只有在车库视图下才允许自动装备
        if (m_CurrentMode != GarageViewMode.OwnedParts)
        {
            Debug.Log("GarageUI: AttemptAutoEquip called while not in OwnedParts view. Ignoring.");
            return false;
        }
        // --- 结束修改 ---

        if (draggedItemUI == null || m_PlayerInventory == null)
        {
            return false;
        }

        PartDataSO partToEquip = draggedItemUI.GetPartData();
        if (partToEquip == null)
        {
            return false;
        }

        PartSlotUI targetSlot = null;
        switch (partToEquip.PartCategoryProperty)
        {
            case PartCategory.Engine:
                targetSlot = m_EngineSlotUI;
                break;
            case PartCategory.Tire:
                targetSlot = m_TiresSlotUI;
                break;
            case PartCategory.Nitro:
                targetSlot = m_NOSSlotUI;
                break;
            default:
                Debug.LogWarning($"GarageUI: AttemptAutoEquip - Part {partToEquip.PartName} has unhandled category {partToEquip.PartCategoryProperty}");
                return false;
        }

        if (targetSlot != null)
        {
            Debug.Log($"GarageUI: Attempting auto-equip of {partToEquip.PartName} to slot {targetSlot.GetSlotCategory()}.");
            // 调用现有的处理逻辑，它会负责装备和UI更新
            HandleDropOnPartSlot(targetSlot, draggedItemUI);
            // 假设 HandleDropOnPartSlot 总是成功处理（它内部有各种检查）
            // 我们需要确保 m_DragWasHandledThisFrame 在这种情况下也被正确设置
            // HandleDropOnPartSlot 内部已经设置了 m_DragWasHandledThisFrame = true;
            return true; 
        }
        else
        {
            Debug.LogWarning($"GarageUI: AttemptAutoEquip - No target slot UI found for category {partToEquip.PartCategoryProperty}.");
        }
        return false;
    }

    /// <summary>
    /// 获取PartItem的预制件，供ShopManager使用。
    /// </summary>
    public GameObject GetPartItemPrefab()
    {
        return m_PartItemPrefab;
    }

    /// <summary>
    /// 查找当前列表中与给定PartDataSO匹配的PartItemUI实例。
    /// </summary>
    private PartItemUI FindInstantiatedPartItemUI(PartDataSO partData)
    {
        foreach (PartItemUI itemUI in m_InstantiatedPartItems)
        {
            if (itemUI != null && itemUI.GetPartData() == partData)
            {
                return itemUI;
            }
        }
        return null;
    }
    #endregion

    #region 新增：视图切换和商店集成

    /// <summary>
    /// 获取当前GarageUI的视图模式。
    /// </summary>
    public GarageViewMode GetCurrentUIMode()
    {
        return m_CurrentMode;
    }

    /// <summary>
    /// 切换到车库视图。
    /// </summary>
    public void SwitchToGarageView()
    {
        m_CurrentMode = GarageViewMode.OwnedParts;
        Debug.Log("Switching to Garage View");

        if (m_ShopPanelContent != null) m_ShopPanelContent.SetActive(false);
        if (m_GaragePanelContent != null) m_GaragePanelContent.SetActive(true);

        if (m_ShopManager != null) m_ShopManager.DeactivateShop();

        // 更新标签视觉效果 (例如，改变按钮颜色)
        UpdateTabVisuals();

        // 更新交易区域标签
        if (m_TransactionDropZoneLabel != null) m_TransactionDropZoneLabel.text = "拖拽到此处出售";

        // 刷新车库显示
        UpdateGarageDisplay();
    }

    /// <summary>
    /// 切换到商店视图。
    /// </summary>
    public void SwitchToShopView()
    {
        m_CurrentMode = GarageViewMode.Shop;
        Debug.Log("Switching to Shop View");

        if (m_GaragePanelContent != null) m_GaragePanelContent.SetActive(false);
        if (m_ShopPanelContent != null) m_ShopPanelContent.SetActive(true);

        if (m_ShopManager != null) m_ShopManager.ActivateShop(); // 激活商店逻辑（包括刷新等）

        UpdateTabVisuals();

        if (m_TransactionDropZoneLabel != null) m_TransactionDropZoneLabel.text = "拖拽到此处购买";

        // 更新金币显示 (商店模式也需要看到金币)
        UpdateCoinDisplay();
        // 商店物品由ShopManager填充，这里不需要调用UpdateGarageDisplay
    }

    private void UpdateTabVisuals()
    {
        // 示例：简单地改变按钮的 Interactable 状态或颜色
        if (m_GarageTabButton != null) {
           // m_GarageTabButton.interactable = (m_CurrentMode != GarageViewMode.OwnedParts);
           var colors = m_GarageTabButton.colors;
           colors.normalColor = (m_CurrentMode == GarageViewMode.OwnedParts) ? Color.white : Color.gray;
           m_GarageTabButton.colors = colors;
        }
        if (m_ShopTabButton != null) {
            //m_ShopTabButton.interactable = (m_CurrentMode != GarageViewMode.Shop);
            var colors = m_ShopTabButton.colors;
            colors.normalColor = (m_CurrentMode == GarageViewMode.Shop) ? Color.white : Color.gray;
            m_ShopTabButton.colors = colors;
        }
    }

    /// <summary>
    /// 由 TransactionDropZone 调用 (仅在商店模式下)，尝试购买零件。
    /// </summary>
    public void AttemptPurchasePart(PartDataSO partToBuy, PartItemUI sourceItemUI)
    {
        if (m_CurrentMode != GarageViewMode.Shop)
        {
            Debug.LogWarning("AttemptPurchasePart called while not in Shop view. Ignoring.");
            return;
        }
        if (m_ShopManager == null)
        {
            Debug.LogError("AttemptPurchasePart: ShopManager is null!");
            return;
        }

        bool purchaseSuccess = m_ShopManager.AttemptPurchasePart(partToBuy, sourceItemUI);

        // 如果购买成功，则标记拖拽已处理，物品不会弹回
        if (purchaseSuccess)
        {
            m_DragWasHandledThisFrame = true;
            // 购买成功后，物品UI会保留在商店中，不需要隐藏它
            // sourceItemUI.gameObject.SetActive(false); // <--- 不再隐藏
            // 强制让被拖拽的物品回到商店列表（因为购买后它还在商店里）
            sourceItemUI.ReturnToOriginalParent();
        }
        // 如果购买失败（例如金币不足），m_DragWasHandledThisFrame 保持 false，物品会自动弹回
    }

    /// <summary>
    /// 当 ShopManager 成功购买物品时调用，用于更新金币显示和可能的车库列表刷新（如果设计需要在购买后立即看到）。
    /// </summary>
    public void NotifyPurchaseSuccessful(PartDataSO purchasedPart)
    {
        UpdateCoinDisplay();
        // 可选：如果你希望购买后，切换回车库视图能立即看到新零件，
        // 可以在这里调用 PopulateOrRefreshOwnedPartsList()，但这可能会有点奇怪，
        // 用户可能想连续购买。更好的做法是等用户手动切换回车库时再刷新。
    }

    #endregion

    #region 新增：更新金币显示的独立方法
    private void UpdateCoinDisplay()
    {
        if (m_PlayerCoinsText != null && m_PlayerInventory != null)
        {
            m_PlayerCoinsText.text = $"金币: {m_PlayerInventory.PlayerCoins}";
        }
    }
    #endregion
}

// 如果你创建了 PartItemUI.cs 用于动态列表中的零件项，它可能看起来像这样：
/*
public class PartItemUI : MonoBehaviour
{
    public Image m_PartIconImage;
    public TextMeshProUGUI m_PartNameText;
    public Button m_SelectButton;

    private PartDataSO m_PartData;
    private GarageUI m_GarageUIInstance;

    public void Setup(PartDataSO _data, GarageUI _garageUI)
    {
        m_PartData = _data;
        m_GarageUIInstance = _garageUI;

        if (m_PartIconImage != null && _data.m_PartIcon != null) m_PartIconImage.sprite = _data.m_PartIcon;
        if (m_PartNameText != null) m_PartNameText.text = _data.m_PartName;
        if (m_SelectButton != null) m_SelectButton.onClick.AddListener(OnSelect);
    }

    private void OnSelect()
    {
        if (m_GarageUIInstance != null && m_PartData != null)
        {
            // m_GarageUIInstance.OnPartSelectedForEquipping(m_PartData);
        }
    }
}
*/ 