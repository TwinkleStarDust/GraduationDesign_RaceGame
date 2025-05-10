using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PartItemUI : MonoBehaviour
{
    [Header("UI元素引用")]
    [SerializeField] private Image m_PartIconImage;
    [SerializeField] private TextMeshProUGUI m_PartNameText;
    [SerializeField] private TextMeshProUGUI m_PartDescriptionText; // 可选，显示零件描述
    [SerializeField] private Button m_SelectButton;

    private PartDataSO m_PartData;
    private GarageUI m_GarageUIInstance;

    /// <summary>
    /// 设置零件条目的UI内容。
    /// </summary>
    /// <param name="_data">要显示的零件数据 (PartDataSO)。</param>
    /// <param name="_garageUI">GarageUI的实例，用于回调。</param>
    public void Setup(PartDataSO _data, GarageUI _garageUI)
    {
        m_PartData = _data;
        m_GarageUIInstance = _garageUI;

        if (m_PartData == null)
        {
            Debug.LogError("传递给PartItemUI的PartDataSO为空!");
            // 可以考虑隐藏此GameObject或显示错误信息
            if (m_PartNameText != null) m_PartNameText.text = "错误";
            if (m_PartIconImage != null) m_PartIconImage.gameObject.SetActive(false);
            if (m_PartDescriptionText != null) m_PartDescriptionText.text = "";
            if (m_SelectButton != null) m_SelectButton.interactable = false;
            return;
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
                m_PartIconImage.gameObject.SetActive(false); // 没有图标则隐藏
            }
        }

        if (m_PartNameText != null)
        {
            m_PartNameText.text = m_PartData.PartName;
        }

        if (m_PartDescriptionText != null) // 如果有描述文本框
        {
            m_PartDescriptionText.text = m_PartData.Description;
        }

        if (m_SelectButton != null)
        {
            m_SelectButton.onClick.RemoveAllListeners(); // 移除旧的监听器，防止重复添加
            m_SelectButton.onClick.AddListener(OnSelect);
            m_SelectButton.interactable = true;
        }
    }

    /// <summary>
    /// 当选择按钮被点击时调用。
    /// </summary>
    private void OnSelect()
    {
        if (m_GarageUIInstance != null && m_PartData != null)
        {
            // 调用GarageUI中的方法来处理零件装备
            m_GarageUIInstance.OnPartSelectedForEquipping(m_PartData);
        }
        else
        {
            Debug.LogError("PartItemUI: GarageUI实例或PartDataSO为空，无法执行选择操作。");
        }
    }
} 