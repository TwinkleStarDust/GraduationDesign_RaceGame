using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class PartItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [Header("UI元素引用")]
    [SerializeField] private Image m_PartIconImage;

    private PartDataSO m_PartData;
    private GarageUI m_GarageUIInstance;
    private CanvasGroup m_CanvasGroup;
    private RectTransform m_RectTransform;
    private Transform m_OriginalParent;
    private Vector3 m_OriginalPosition;

    private void Awake()
    {
        m_CanvasGroup = GetComponent<CanvasGroup>();
        if (m_CanvasGroup == null)
        {
            m_CanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        m_RectTransform = GetComponent<RectTransform>();
    }

    /// <summary>
    /// 设置零件条目的UI内容。
    /// </summary>
    /// <param name="_data">要显示的零件数据 (PartDataSO)。</param>
    /// <param name="_garageUI">GarageUI的实例，用于回调。</param>
    public void Setup(PartDataSO _data, GarageUI _garageUI)
    {
        if (m_RectTransform == null)
        {
            m_RectTransform = GetComponent<RectTransform>();
        }
        if (m_CanvasGroup == null)
        {
            m_CanvasGroup = GetComponent<CanvasGroup>();
            if (m_CanvasGroup == null)
            {
                m_CanvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (m_RectTransform == null)
        {
            Debug.LogError("PartItemUI.Setup: Failed to get RectTransform component! Cannot proceed.", this.gameObject);
            if(m_CanvasGroup != null) m_CanvasGroup.alpha = 0;
            enabled = false;
            return;
        }
        if (m_CanvasGroup == null)
        {
             Debug.LogError("PartItemUI.Setup: Failed to get or add CanvasGroup component! Dragging might fail.", this.gameObject);
        }

        Debug.Log($"PartItemUI Setup called. RectTransform acquired. Data: {(_data != null ? _data.name : "NULL DATA")}", this.gameObject);

        m_PartData = _data;
        m_GarageUIInstance = _garageUI;
        m_OriginalParent = transform.parent;
        m_OriginalPosition = m_RectTransform.localPosition;

        if (m_PartData == null)
        {
            Debug.LogError("传递给PartItemUI的PartDataSO为空!", this.gameObject);
            if (m_PartIconImage != null) m_PartIconImage.gameObject.SetActive(false);
            gameObject.SetActive(false);
            return;
        }
        else
        {
            gameObject.SetActive(true);
        }

        if (m_PartIconImage != null)
        {
            if (m_PartData.Icon != null)
            {
                m_PartIconImage.sprite = m_PartData.Icon;
                m_PartIconImage.gameObject.SetActive(true);
            }
            else
            {
                m_PartIconImage.gameObject.SetActive(false);
            }
        }
    }

    public PartDataSO GetPartData()
    {
        return m_PartData;
    }

    /// <summary>
    /// 允许外部脚本（如GarageUI）强制此项开始拖拽。
    /// 通常用于从装备槽拖出已装备零件时，实际被拖拽的还是这个列表项。
    /// </summary>
    public void StartExternalDrag(PointerEventData eventData)
    {
        if (m_PartData == null) 
        {
            Debug.LogError("StartExternalDrag: PartData is null!");
            return;
        }

        Debug.Log($"外部启动拖拽: {m_PartData.PartName}");
        m_OriginalParent = transform.parent; // 确保记录当前（可能是列表）的父级
        m_OriginalPosition = m_RectTransform.localPosition;

        transform.SetParent(m_GarageUIInstance.GetDragDropCanvasTransform());
        transform.SetAsLastSibling();
        
        m_CanvasGroup.alpha = 0.6f;
        m_CanvasGroup.blocksRaycasts = false;

        // 关键：将此gameObject设置为当前拖拽的指针目标
        eventData.pointerDrag = gameObject; 

        if(m_GarageUIInstance != null) m_GarageUIInstance.OnPartDragStateChanged(m_PartData, true);
    }

    #region 拖拽接口实现
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (m_PartData == null) return;

        Debug.Log($"开始拖拽: {m_PartData.PartName}");
        m_OriginalParent = transform.parent;
        m_OriginalPosition = m_RectTransform.localPosition;

        transform.SetParent(m_GarageUIInstance.GetDragDropCanvasTransform());
        transform.SetAsLastSibling();
        
        m_CanvasGroup.alpha = 0.6f;
        m_CanvasGroup.blocksRaycasts = false;

        if(m_GarageUIInstance != null) m_GarageUIInstance.OnPartDragStateChanged(m_PartData, true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (m_PartData == null) return;
        m_RectTransform.anchoredPosition += eventData.delta / m_GarageUIInstance.GetMainCanvas().scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (m_PartData == null) return;

        Debug.Log($"结束拖拽: {m_PartData.PartName}");
        m_CanvasGroup.alpha = 1f;
        m_CanvasGroup.blocksRaycasts = true;

        bool wasHandledByDirectDrop = m_GarageUIInstance.WasDragHandled(this);

        if (wasHandledByDirectDrop)
        {
            // 成功通过拖放到 PartSlotUI 进行处理。
            // GarageUI.HandleDropOnPartSlot 已经处理了逻辑。
            // PartItemUI 的可见性/状态由 HandleDropOnPartSlot 管理。
            Debug.Log($"{m_PartData.PartName} was handled by direct drop.");
        }
        else
        {
            // 没有直接拖放到有效的 PartSlotUI 上。
            // 检查是否拖拽出了 ScrollView 以尝试自动装备。
            bool autoEquipped = false;
            RectTransform scrollViewRect = m_GarageUIInstance.GetOwnedPartsScrollViewportRect();
            
            if (scrollViewRect != null)
            {
                // 获取Canvas，因为RectTransformUtility.ScreenPointToLocalPointInRectangle需要它
                Canvas canvas = m_GarageUIInstance.GetMainCanvas();
                if (canvas != null)
                {
                    // 确定摄像机，对于Screen Space - Overlay，camera可以为null
                    Camera pressCamera = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : canvas.worldCamera; 
                    
                    // 检查鼠标指针是否在 ScrollView 的 Viewport 之外
                    // eventData.position 是屏幕空间坐标
                    bool isOutsideScrollView = !RectTransformUtility.RectangleContainsScreenPoint(
                        scrollViewRect, 
                        eventData.position, 
                        pressCamera // 使用与Canvas渲染模式匹配的摄像机
                    );

                    Debug.Log($"PartItemUI ({m_PartData.PartName}): OnEndDrag. Pointer screen pos: {eventData.position}. ScrollViewRect: {scrollViewRect.rect}. IsOutsideScrollView: {isOutsideScrollView}");

                    if (isOutsideScrollView)
                    {
                        Debug.Log($"PartItemUI ({m_PartData.PartName}): Dragged outside ScrollView. Attempting auto-equip.");
                        autoEquipped = m_GarageUIInstance.AttemptAutoEquip(this);
                    }
                }
                else
                {
                    Debug.LogWarning("PartItemUI: MainCanvas not found in GarageUI for bounds check.");
                }
            }
            else
            {
                Debug.LogWarning("PartItemUI: OwnedPartsScrollViewportRect not found in GarageUI for bounds check.");
            }

            if (!autoEquipped) // 如果没有直接处理，也没有自动装备成功
            {
                Debug.Log($"PartItemUI ({m_PartData.PartName}): Not auto-equipped, returning to original parent.");
                ReturnToOriginalParent();
                // 可选：如果返回原位且当前详情是此物品，可以保持显示或清除
                // if (m_GarageUIInstance != null && m_GarageUIInstance.m_CurrentlySelectedPartForDetail == m_PartData) 
                // { 
                //     m_GarageUIInstance.ClearPartDetails(); 
                // }
            }
        }
        
        if(m_GarageUIInstance != null) m_GarageUIInstance.OnPartDragStateChanged(m_PartData, false);
    }

    public void ReturnToOriginalParent()
    {
        transform.SetParent(m_OriginalParent);
        // 使用localPosition确保其在父级内的相对位置正确
        if (m_RectTransform != null) m_RectTransform.localPosition = m_OriginalPosition;
        // 如果有缩放或旋转变化，也需要在这里重置，例如：
        // if (m_RectTransform != null) m_RectTransform.localScale = Vector3.one;
        // if (m_RectTransform != null) m_RectTransform.localRotation = Quaternion.identity;

        // 确保CanvasGroup状态恢复
        if (m_CanvasGroup != null) 
        {
            m_CanvasGroup.alpha = 1f;
            m_CanvasGroup.blocksRaycasts = true;
        }

        // 如果m_OriginalParent是布局组，它会自动处理顺序。
        // 如果需要精确恢复原始siblingIndex，需要额外记录和设置。
        // transform.SetSiblingIndex(m_OriginalSiblingIndex); 
        gameObject.SetActive(true); // 确保物品是可见的
    }
    #endregion

    #region 新增：IPointerClickHandler 实现
    public void OnPointerClick(PointerEventData eventData)
    {
        // 只在非拖拽情况下响应点击，避免拖拽开始时也触发详情显示
        if (m_GarageUIInstance != null && m_PartData != null && !eventData.dragging)
        {
            // 通常是鼠标左键点击或触摸点击
            if (eventData.button == PointerEventData.InputButton.Left || eventData.pointerId < 0)
            {
                m_GarageUIInstance.ShowPartDetails(m_PartData);
                Debug.Log($"PartItemUI: Clicked on {m_PartData.PartName}, showing details.");
            }
        }
    }
    #endregion
} 