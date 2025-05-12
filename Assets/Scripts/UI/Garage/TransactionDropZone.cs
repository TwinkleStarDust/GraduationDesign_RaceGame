using UnityEngine;
using UnityEngine.EventSystems;

public class TransactionDropZone : MonoBehaviour, IDropHandler
{
    public GarageUI m_GarageUIInstance;

    public void OnDrop(PointerEventData eventData)
    {
        PartItemUI droppedItemUI = eventData.pointerDrag?.GetComponent<PartItemUI>();
        if (droppedItemUI != null)
        {
            PartDataSO partData = droppedItemUI.GetPartData();
            if (partData != null)
            {
                GarageUI.GarageViewMode currentMode = m_GarageUIInstance.GetCurrentUIMode();
                if (currentMode == GarageUI.GarageViewMode.OwnedParts) // 如果在车库视图，尝试出售
                {
                    Debug.Log($"TransactionDropZone: Attempting to sell part '{partData.PartName}'");
                    m_GarageUIInstance.AttemptSellPart(partData);
                }
                else if (currentMode == GarageUI.GarageViewMode.Shop) // 如果在商店视图，尝试购买
                {
                    Debug.Log($"TransactionDropZone: Attempting to purchase part '{partData.PartName}'");
                    m_GarageUIInstance.AttemptPurchasePart(partData, droppedItemUI);
                }
                else
                {
                    Debug.LogWarning($"TransactionDropZone: Unhandled GarageViewMode '{currentMode}' on drop.");
                }
            }
        }
    }
} 