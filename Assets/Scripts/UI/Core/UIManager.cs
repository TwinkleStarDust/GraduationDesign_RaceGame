using UnityEngine;

public class UIManager : MonoBehaviour
{
    #region Singleton
    public static UIManager Instance { get; private set; }
    #endregion

    #region 私有字段
    [Header("UI 面板")]
    [SerializeField] private GameObject m_MainMenuPanel;
    [SerializeField] private GameObject m_MapSelectionPanel;
    [SerializeField] private GameObject m_GaragePanel;
    [SerializeField] private GameObject m_SettingsPanel;
    // 可以根据需要添加其他面板，例如加载界面、游戏内UI等
    #endregion

    #region Unity生命周期
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // 如果需要在场景切换时保留UIManager，取消此行注释
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
        HideAllPanels();
        if (m_MainMenuPanel != null)
        {
            m_MainMenuPanel.SetActive(true);
        }
    }

    public void ShowMapSelectionPanel()
    {
        HideAllPanels();
        if (m_MapSelectionPanel != null)
        {
            m_MapSelectionPanel.SetActive(true);
        }
    }

    public void ShowGaragePanel()
    {
        HideAllPanels();
        if (m_GaragePanel != null)
        {
            m_GaragePanel.SetActive(true);
        }
    }

    public void ShowSettingsPanel()
    {
        HideAllPanels();
        if (m_SettingsPanel != null)
        {
            m_SettingsPanel.SetActive(true);
        }
    }

    public void HideAllPanels()
    {
        if (m_MainMenuPanel != null) m_MainMenuPanel.SetActive(false);
        if (m_MapSelectionPanel != null) m_MapSelectionPanel.SetActive(false);
        if (m_GaragePanel != null) m_GaragePanel.SetActive(false);
        if (m_SettingsPanel != null) m_SettingsPanel.SetActive(false);
    }
    #endregion
} 