using UnityEngine;
// using UnityEngine.SceneManagement; // 如果需要场景加载，保留

public class MainMenuUIManager : MonoBehaviour // 类名已更改
{
    #region Singleton
    public static MainMenuUIManager Instance { get; private set; }
    #endregion

    #region 私有字段
    [Header("主菜单 UI 面板")]
    [SerializeField] private GameObject m_MainMenuPanel;
    [SerializeField] private GameObject m_MapSelectionPanel;
    [SerializeField] private GameObject m_GaragePanel;
    [SerializeField] private GameObject m_SettingsPanel; // 这是主菜单的设置面板
    // 可以根据需要添加其他主菜单相关面板
    #endregion

    #region Unity生命周期
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // 通常主菜单UI管理器不需要跨场景保留
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 初始时，通常显示主菜单，隐藏其他面板
        ShowMainMenuPanel();
    }
    #endregion

    #region 公共方法
    public void ShowMainMenuPanel()
    {
        HideAllMainMenuPanels();
        if (m_MainMenuPanel != null)
        {
            m_MainMenuPanel.SetActive(true);
        }
    }

    public void ShowMapSelectionPanel()
    {
        HideAllMainMenuPanels();
        if (m_MapSelectionPanel != null)
        {
            m_MapSelectionPanel.SetActive(true);
        }
    }

    public void ShowGaragePanel()
    {
        HideAllMainMenuPanels();
        if (m_GaragePanel != null)
        {
            m_GaragePanel.SetActive(true);
        }
    }

    public void ShowSettingsPanel() // 主菜单的设置面板
    {
        HideAllMainMenuPanels();
        if (m_SettingsPanel != null)
        {
            m_SettingsPanel.SetActive(true);
        }
    }

    public void HideAllMainMenuPanels() // 只隐藏主菜单相关面板
    {
        if (m_MainMenuPanel != null) m_MainMenuPanel.SetActive(false);
        if (m_MapSelectionPanel != null) m_MapSelectionPanel.SetActive(false);
        if (m_GaragePanel != null) m_GaragePanel.SetActive(false);
        if (m_SettingsPanel != null) m_SettingsPanel.SetActive(false);
    }
    #endregion
} 