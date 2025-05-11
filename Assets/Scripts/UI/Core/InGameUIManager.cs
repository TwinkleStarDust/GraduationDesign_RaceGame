using UnityEngine;
using UnityEngine.SceneManagement; // 用于返回主菜单等场景操作

public class InGameUIManager : MonoBehaviour
{
    #region Singleton
    public static InGameUIManager Instance { get; private set; }
    #endregion

    #region 私有字段
    [Header("游戏内 UI 面板")]
    [Tooltip("游戏内暂停菜单面板")]
    [SerializeField] private GameObject m_InGamePauseMenuPanel;
    [Tooltip("游戏内设置面板 (如果与主菜单设置面板不同)")]
    [SerializeField] private GameObject m_InGameSettingsPanel; 
    // 可以添加其他游戏内UI元素，如HUD、比赛结束面板等

    private bool m_IsPauseMenuUIActive = false;
    private CarController m_PlayerCarController; // 缓存玩家车辆控制器
    #endregion

    #region Unity生命周期
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 尝试查找玩家车辆控制器，游戏开始时车辆可能已存在
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            m_PlayerCarController = playerObject.GetComponent<CarController>();
        }
        if (m_PlayerCarController == null)
        {
            Debug.LogWarning("InGameUIManager: 未在场景中找到带有 'Player' 标签且挂有CarController的玩家车辆! 输入禁用功能可能受影响。");
        }
    }

    private void Start()
    {
        if (m_InGamePauseMenuPanel != null) m_InGamePauseMenuPanel.SetActive(false);
        if (m_InGameSettingsPanel != null) m_InGameSettingsPanel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) // 游戏内ESC键打开暂停菜单
        {
            ToggleInGamePauseMenu();
        }
    }
    #endregion

    #region 公共方法
    public void ToggleInGamePauseMenu()
    {
        if (m_InGamePauseMenuPanel == null) return;

        m_IsPauseMenuUIActive = !m_InGamePauseMenuPanel.activeSelf;
        m_InGamePauseMenuPanel.SetActive(m_IsPauseMenuUIActive);

        if (m_PlayerCarController != null)
        {
            m_PlayerCarController.SetInputDisabled(m_IsPauseMenuUIActive);
        }
        else
        {
            // 如果在Awake时没找到，再尝试找一次，以防车辆是后生成的
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null) m_PlayerCarController = playerObject.GetComponent<CarController>();
            if (m_PlayerCarController != null) m_PlayerCarController.SetInputDisabled(m_IsPauseMenuUIActive);
            else Debug.LogWarning("ToggleInGamePauseMenu: CarController 仍然未找到，无法禁用/启用输入。");
        }

        if (m_IsPauseMenuUIActive)
        {
            Debug.Log("游戏内暂停菜单已显示 (UI Only)");
            // 根据需要处理光标状态
            // Cursor.lockState = CursorLockMode.None;
            // Cursor.visible = true;
            if (m_InGameSettingsPanel != null) m_InGameSettingsPanel.SetActive(false); // 打开暂停菜单时确保设置菜单是关闭的
        }
        else
        {
            Debug.Log("游戏内暂停菜单已隐藏");
            // 根据需要处理光标状态
            // Cursor.lockState = CursorLockMode.Locked; // 如果您的游戏锁定光标
            // Cursor.visible = false;
        }
    }

    public void ResumeGame()
    {
        if (m_InGamePauseMenuPanel != null) m_InGamePauseMenuPanel.SetActive(false);
        if (m_PlayerCarController != null) m_PlayerCarController.SetInputDisabled(false);
        m_IsPauseMenuUIActive = false;
        Debug.Log("从游戏内菜单继续游戏");
        // 恢复光标状态等
    }

    public void GoToMainMenu()
    {
        // 在返回主菜单前，确保玩家输入已启用，时间尺度正常 (如果之前修改过)
        if (m_PlayerCarController != null) m_PlayerCarController.SetInputDisabled(false);
        Time.timeScale = 1f; // 以防万一

        // TODO: 替换为您的主菜单场景的确切名称
        SceneManager.LoadScene("MainMenu");
    }

    public void OpenSettings()
    {
        if (m_InGameSettingsPanel == null)
        {
            Debug.LogWarning("游戏内设置面板 (InGameSettingsPanel) 未在InGameUIManager中分配!");
            return;
        }

        // 可以选择先隐藏主暂停菜单，或者让设置面板叠加
        if (m_InGamePauseMenuPanel != null) m_InGamePauseMenuPanel.SetActive(false); 
        m_InGameSettingsPanel.SetActive(true);
        Debug.Log("打开游戏内设置面板");
    }

    public void CloseSettingsAndReturnToPauseMenu() // 由游戏内设置面板的返回按钮调用
    {
        if (m_InGameSettingsPanel != null) m_InGameSettingsPanel.SetActive(false);
        
        // 重新显示暂停菜单 (如果它之前是激活的并被OpenSettings隐藏了)
        // 或者，如果设计是暂停菜单和设置菜单不能同时打开，确保暂停菜单是下一个要显示的
        if (m_InGamePauseMenuPanel != null && m_IsPauseMenuUIActive) // m_IsPauseMenuUIActive 在Toggle时设置
        {
             m_InGamePauseMenuPanel.SetActive(true);
        } else if (m_InGamePauseMenuPanel != null) {
            // 如果暂停菜单因为打开设置而关闭，且我们想返回它，则强制打开
            // 这部分逻辑需要根据您的UI流程仔细设计
            // 一个简单的方式是，如果设置被打开，总是认为暂停菜单是其"父"菜单
            m_InGamePauseMenuPanel.SetActive(true); 
            // 确保m_IsPauseMenuUIActive也同步，但这通常应该由ToggleInGamePauseMenu管理
        }
        Debug.Log("关闭游戏内设置并返回暂停菜单");
    }
    
    // 如果游戏内的设置面板与主菜单的设置面板是同一个预制件/场景对象，
    // 并且其返回按钮逻辑写在 SettingsUI.cs 中，那么 SettingsUI.cs 中的返回按钮
    // 需要知道是返回到 MainMenuUIManager 还是 InGameUIManager。
    // 这可以通过在打开设置时传递一个参数，或者让 SettingsUI 检查当前激活的是哪个 UIManager 实例。
    // 为简单起见，这里提供了 InGameUIManager 控制下的关闭设置方法。

    public void HideAllInGamePanels() // 用于例如比赛结束等情况
    {
        if (m_InGamePauseMenuPanel != null) m_InGamePauseMenuPanel.SetActive(false);
        if (m_InGameSettingsPanel != null) m_InGameSettingsPanel.SetActive(false);
        m_IsPauseMenuUIActive = false;
        // 可以在此启用玩家输入，如果需要的话
        // if (m_PlayerCarController != null) m_PlayerCarController.SetInputDisabled(false);
    }

    /// <summary>
    /// 检查游戏内暂停菜单UI是否当前处于激活状态。
    /// </summary>
    /// <returns>如果暂停菜单UI是激活的，则返回true；否则返回false。</returns>
    public bool IsPauseMenuActive()
    {
        return m_IsPauseMenuUIActive;
    }
    #endregion
} 