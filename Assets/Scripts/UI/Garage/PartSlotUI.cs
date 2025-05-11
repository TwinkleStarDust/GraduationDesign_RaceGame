using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI; // If you use an Image component on the slot
using TMPro; // If you use TextMeshPro for slot name/info

public class PartSlotUI : MonoBehaviour, IDropHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("插槽配置")]
    [Tooltip("此插槽接受的零件类型")]
    public PartCategory m_SlotCategory; // Assign this in the Inspector for each slot

    [Header("UI引用 (可选，用于显示已装备零件)")]
    [Tooltip("用于显示当前装备在此插槽的零件图标")]
    [SerializeField] private Image m_EquippedPartIconDisplay;
    [Tooltip("用于显示当前装备在此插槽的零件名称")]
    [SerializeField] private TextMeshProUGUI m_EquippedPartNameDisplay;
    // You can add a reference to a default "empty slot" sprite here if you want

    private GarageUI m_GarageUIInstance;
    private PartDataSO m_CurrentlyDisplayedPart; // For visual representation in the slot

    /// <summary>
    /// Initializes the slot with a reference to the GarageUI.
    /// </summary>
    public void Initialize(GarageUI garageUI)
    {
        m_GarageUIInstance = garageUI;
    }

    /// <summary>
    /// Updates the visual representation of this slot based on the provided part data.
    /// </summary>
    public void UpdateSlotDisplay(PartDataSO equippedPart)
    {
        m_CurrentlyDisplayedPart = equippedPart;
        if (m_EquippedPartIconDisplay != null)
        {
            if (equippedPart != null && equippedPart.Icon != null)
            {
                m_EquippedPartIconDisplay.sprite = equippedPart.Icon;
                m_EquippedPartIconDisplay.gameObject.SetActive(true);
            }
            else
            {
                m_EquippedPartIconDisplay.gameObject.SetActive(false); // Or show a default empty slot icon
            }
        }

        if (m_EquippedPartNameDisplay != null)
        {
            m_EquippedPartNameDisplay.text = equippedPart != null ? equippedPart.PartName : $"<空置 ({m_SlotCategory})>";
        }
    }
    
    /// <summary>
    /// Gets the category of this slot.
    /// </summary>
    public PartCategory GetSlotCategory()
    {
        return m_SlotCategory;
    }

    /// <summary>
    /// Gets the part data currently visually represented in this slot.
    /// </summary>
    public PartDataSO GetCurrentlyDisplayedPart()
    {
        return m_CurrentlyDisplayedPart;
    }


    #region IDropHandler 实现
    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log($"PartSlotUI ({m_SlotCategory}): OnDrop event triggered by {eventData.pointerDrag?.name ?? "Unknown Object"}.");
        if (m_GarageUIInstance == null)
        {
            Debug.LogError("PartSlotUI: GarageUI instance is not set!");
            return;
        }

        Debug.Log($"{gameObject.name} detected OnDrop from {eventData.pointerDrag.name}");

        PartItemUI draggedPartItemUI = eventData.pointerDrag.GetComponent<PartItemUI>();
        if (draggedPartItemUI != null)
        {
            PartDataSO partDataToDrop = draggedPartItemUI.GetPartData();
            if (partDataToDrop != null)
            {
                // Check if the part category matches the slot category
                if (partDataToDrop.PartCategoryProperty == m_SlotCategory)
                {
                    Debug.Log($"Part {partDataToDrop.PartName} ({partDataToDrop.PartCategoryProperty}) is compatible with slot {m_SlotCategory}.");
                    // Notify GarageUI to handle the equipping logic
                    m_GarageUIInstance.HandleDropOnPartSlot(this, draggedPartItemUI);
                }
                else
                {
                    Debug.LogWarning($"Part {partDataToDrop.PartName} ({partDataToDrop.PartCategoryProperty}) is NOT compatible with slot {m_SlotCategory}!");
                    // The PartItemUI will return to its original position if GarageUI doesn't mark the drag as handled.
                }
            }
            else
            {
                Debug.LogWarning("Dropped item's PartDataSO is null.");
            }
        }
        else
        {
            Debug.LogWarning("Dropped item does not have a PartItemUI component.");
        }
    }
    #endregion

    #region IBeginDragHandler, IDragHandler, IEndDragHandler 实现 (用于从插槽拖拽)
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (m_CurrentlyDisplayedPart != null && m_GarageUIInstance != null)
        {
            Debug.Log($"PartSlotUI ({m_SlotCategory}): OnBeginDrag for part {m_CurrentlyDisplayedPart.PartName}.");
            // 通知 GarageUI，我们正在尝试从这个插槽拖拽一个零件。
            // GarageUI 会找到对应的 PartItemUI，并使其开始响应拖拽。
            m_GarageUIInstance.StartDragFromSlot(this, m_CurrentlyDisplayedPart, eventData);
        }
        else
        {
            Debug.LogWarning($"PartSlotUI ({m_SlotCategory}): OnBeginDrag called, but no part is equipped or GarageUI is missing.");
            eventData.pointerDrag = null; // 取消拖拽，因为没有东西可以拖
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 实际的拖拽由 GarageUI 协调的 PartItemUI 处理。
        // 这个方法可以留空，或者用于某些特定的插槽视觉反馈（如果需要）。
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 拖拽的结束逻辑主要由 PartItemUI 和 GarageUI 处理。
        // PartItemUI 会判断是否被放置在有效位置，或者是否需要返回原位。
        // GarageUI 会处理卸载逻辑（如果从插槽拖出后未放置到有效位置）。
        Debug.Log($"PartSlotUI ({m_SlotCategory}): OnEndDrag. Actual handling is done by PartItemUI/GarageUI.");
    }
    #endregion
} 