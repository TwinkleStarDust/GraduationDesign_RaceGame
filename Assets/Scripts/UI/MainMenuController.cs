using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 主菜单控制器 - 管理主菜单界面的交互
/// </summary>
public class MainMenuController : MonoBehaviour
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
        
        // 注册金钱变更事件
        if (GameObject.Find("EconomySystem") != null)
        {
            // 注意：如果EconomySystem不可用，我们只是不注册事件
            var economySystem = GameObject.Find("EconomySystem").GetComponent<MonoBehaviour>();
            if (economySystem != null)
            {
                // 这里应该使用EconomySystem.OnMoneyChanged事件，但我们先跳过这个步骤
                Debug.Log("EconomySystem找到但暂不注册事件");
            }
        }
    }
    
    private void OnDestroy()
    {
        // 取消注册事件 - 现在先跳过
    }
    
    private void OnStartGameClicked()
    {
        // 使用GameManager加载第一个关卡
        Debug.Log("开始游戏按钮点击");
        LoadLevel(0);
    }
    
    private void OnGarageClicked()
    {
        // 打开车库界面
        Debug.Log("车库按钮点击");
        // 现在先用直接方法
        OpenUpgradeUI();
    }
    
    private void OnShopClicked()
    {
        // 打开商店界面
        Debug.Log("商店按钮点击");
        // 现在先用直接方法
        OpenShopUI();
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
    
    // 临时方法，直到GameManager可用
    private void LoadLevel(int levelIndex)
    {
        Debug.Log($"加载关卡 {levelIndex}");
        // 将在GameManager可用后实现
    }
    
    // 临时方法，直到UIManager可用
    private void OpenUpgradeUI()
    {
        Debug.Log("打开车库界面");
        // 将在UIManager可用后实现
    }
    
    // 临时方法，直到UIManager可用
    private void OpenShopUI()
    {
        Debug.Log("打开商店界面");
        // 将在UIManager可用后实现
    }
    
    private void UpdateMoneyDisplay()
    {
        if (m_MoneyText != null)
        {
            m_MoneyText.text = "金币: 1000";
            // 将在EconomySystem可用后更新为实际值
        }
    }
} 