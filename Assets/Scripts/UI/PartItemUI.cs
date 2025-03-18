using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 零部件项目UI - 在车库/商店中展示零部件信息
/// </summary>
public class PartItemUI : MonoBehaviour
{
    [Header("UI引用")]
    [Tooltip("零部件图标")]
    [SerializeField] private Image m_Icon;
    
    [Tooltip("零部件名称文本")]
    [SerializeField] private TextMeshProUGUI m_NameText;
    
    [Tooltip("零部件属性文本")]
    [SerializeField] private TextMeshProUGUI m_StatsText;
    
    [Tooltip("选择按钮")]
    [SerializeField] private Button m_SelectButton;
    
    [Tooltip("选中指示器")]
    [SerializeField] private GameObject m_SelectedIndicator;
    
    // 零部件选择事件
    public event Action<GarageController.TempPartData> OnPartSelected;
    
    // 当前零部件数据
    private GarageController.TempPartData m_PartData;
    
    private void Start()
    {
        if (m_SelectButton != null)
        {
            m_SelectButton.onClick.AddListener(OnButtonClicked);
        }
    }
    
    private void OnDestroy()
    {
        // 取消注册事件
        if (m_SelectButton != null)
        {
            m_SelectButton.onClick.RemoveListener(OnButtonClicked);
        }
    }
    
    /// <summary>
    /// 设置零部件数据
    /// </summary>
    public void SetPartData(GarageController.TempPartData partData)
    {
        m_PartData = partData;
        
        if (m_Icon != null && partData.Icon != null)
        {
            m_Icon.sprite = partData.Icon;
            m_Icon.enabled = true;
        }
        else if (m_Icon != null)
        {
            // 设置默认图标
            m_Icon.enabled = true;
        }
        
        if (m_NameText != null)
        {
            m_NameText.text = partData.PartName;
            m_NameText.color = partData.GetRarityColor();
        }
        
        if (m_StatsText != null)
        {
            string stats = "";
            
            if (partData.SpeedModifier != 0)
                stats += $"速度: {(partData.SpeedModifier >= 0 ? "+" : "")}{partData.SpeedModifier * 100:F0}%\n";
                
            if (partData.AccelerationModifier != 0)
                stats += $"加速: {(partData.AccelerationModifier >= 0 ? "+" : "")}{partData.AccelerationModifier * 100:F0}%\n";
                
            if (partData.HandlingModifier != 0)
                stats += $"操控: {(partData.HandlingModifier >= 0 ? "+" : "")}{partData.HandlingModifier * 100:F0}%\n";
                
            if (partData.BrakeForceModifier != 0)
                stats += $"制动: {(partData.BrakeForceModifier >= 0 ? "+" : "")}{partData.BrakeForceModifier * 100:F0}%\n";
            
            m_StatsText.text = stats;
        }
    }
    
    /// <summary>
    /// 设置选中状态
    /// </summary>
    public void SetSelected(bool isSelected)
    {
        if (m_SelectedIndicator != null)
        {
            m_SelectedIndicator.SetActive(isSelected);
        }
    }
    
    /// <summary>
    /// 按钮点击回调
    /// </summary>
    private void OnButtonClicked()
    {
        if (m_PartData != null)
        {
            OnPartSelected?.Invoke(m_PartData);
        }
    }
} 