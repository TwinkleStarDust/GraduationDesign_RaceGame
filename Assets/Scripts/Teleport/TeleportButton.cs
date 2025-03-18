using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;


/// 传送点按钮
/// 处理传送按钮的UI效果

public class TeleportButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("按钮文本")]
    [SerializeField] private TextMeshProUGUI buttonText;

    [Tooltip("按钮图标")]
    [SerializeField] private Image buttonIcon;

    [Tooltip("按钮背景")]
    [SerializeField] private Image buttonBackground;

    [Tooltip("描述文本")]
    [SerializeField] private TextMeshProUGUI descriptionText;

    [Tooltip("悬停颜色")]
    [SerializeField] private Color hoverColor = new Color(0.8f, 0.8f, 0.8f, 1f);

    [Tooltip("正常颜色")]
    [SerializeField] private Color normalColor = Color.white;

    // 传送点引用
    private TeleportPoint teleportPoint;

    
    /// 初始化按钮
    
    public void Initialize(TeleportPoint point)
    {
        teleportPoint = point;

        if (teleportPoint == null) return;

        // 设置按钮文本
        if (buttonText != null)
        {
            buttonText.text = teleportPoint.PointName;
        }

        // 设置按钮图标
        if (buttonIcon != null && teleportPoint.Icon != null)
        {
            buttonIcon.sprite = teleportPoint.Icon;
            buttonIcon.color = teleportPoint.PointColor;
        }

        // 设置描述文本
        if (descriptionText != null)
        {
            descriptionText.text = teleportPoint.Description;
            descriptionText.gameObject.SetActive(false); // 默认隐藏描述
        }
    }

    
    /// 鼠标进入按钮
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 改变按钮颜色
        if (buttonBackground != null)
        {
            buttonBackground.color = hoverColor;
        }

        // 显示描述
        if (descriptionText != null)
        {
            descriptionText.gameObject.SetActive(true);
        }
    }

    
    /// 鼠标离开按钮
    
    public void OnPointerExit(PointerEventData eventData)
    {
        // 恢复按钮颜色
        if (buttonBackground != null)
        {
            buttonBackground.color = normalColor;
        }

        // 隐藏描述
        if (descriptionText != null)
        {
            descriptionText.gameObject.SetActive(false);
        }
    }
}