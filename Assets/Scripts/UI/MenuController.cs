using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 菜单控制器 - 管理主菜单界面的交互
/// </summary>
public class MenuController : MonoBehaviour
{
    [Header("按钮引用")]
    [Tooltip("开始游戏按钮")]
    [SerializeField] private Button m_StartGameButton;
    
    [Tooltip("车库按钮")]
    [SerializeField] private Button m_GarageButton;
    
    [Tooltip("商店按钮")]
    [SerializeField] private Button m_ShopButton;
    
    [Tooltip("设置按钮")]
    [SerializeField] private Button m_SettingsButton;
    
    [Tooltip("退出按钮")]
    [SerializeField] private Button m_QuitButton;
    
    [Header("玩家信息")]
    [Tooltip("玩家金钱文本")]
    [SerializeField] private TextMeshProUGUI m_MoneyText;
    
    private void Start()
    {
        Debug.Log("MenuController启动");
        
        // 注册按钮点击事件
        if (m_StartGameButton != null)
            m_StartGameButton.onClick.AddListener(OnStartGameClicked);
            
        if (m_GarageButton != null)
            m_GarageButton.onClick.AddListener(OnGarageClicked);
            
        if (m_ShopButton != null)
            m_ShopButton.onClick.AddListener(OnShopClicked);
            
        if (m_SettingsButton != null)
            m_SettingsButton.onClick.AddListener(OnSettingsClicked);
            
        if (m_QuitButton != null)
            m_QuitButton.onClick.AddListener(OnQuitClicked);
        
        // 更新玩家金钱显示
        UpdateMoneyDisplay();
    }
    
    private void OnStartGameClicked()
    {
        // 开始游戏
        Debug.Log("开始游戏按钮点击");
    }
    
    private void OnGarageClicked()
    {
        // 打开车库界面
        Debug.Log("车库按钮点击");
    }
    
    private void OnShopClicked()
    {
        // 打开商店界面
        Debug.Log("商店按钮点击");
    }
    
    private void OnSettingsClicked()
    {
        // 打开设置界面
        Debug.Log("打开设置界面");
    }
    
    private void OnQuitClicked()
    {
        // 退出游戏
        Debug.Log("退出游戏");
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    private void UpdateMoneyDisplay()
    {
        if (m_MoneyText != null)
        {
            m_MoneyText.text = "金币: 1000";
        }
    }
} 