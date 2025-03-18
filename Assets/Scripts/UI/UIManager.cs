using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// UI管理器 - 统一管理所有游戏界面
/// </summary>
public class UIManager : MonoBehaviour
{
    #region 单例实现
    private static UIManager s_Instance;
    public static UIManager Instance 
    { 
        get 
        {
            if (s_Instance == null)
            {
                GameObject managerObj = GameObject.Find("UIManager");
                if (managerObj == null)
                {
                    managerObj = new GameObject("UIManager");
                    s_Instance = managerObj.AddComponent<UIManager>();
                }
                else
                {
                    s_Instance = managerObj.GetComponent<UIManager>();
                    if (s_Instance == null)
                    {
                        s_Instance = managerObj.AddComponent<UIManager>();
                    }
                }
                DontDestroyOnLoad(managerObj);
            }
            return s_Instance;
        }
    }
    #endregion
    
    #region 事件定义
    // UI界面打开事件
    public event Action<string> OnUIOpened;
    // UI界面关闭事件
    public event Action<string> OnUIClosed;
    #endregion
    
    #region 序列化字段
    [Header("UI界面预制体")]
    [Tooltip("主菜单界面")]
    [SerializeField] private GameObject m_MainMenuPrefab;
    
    [Tooltip("游戏HUD界面")]
    [SerializeField] private GameObject m_GameHUDPrefab;
    
    [Tooltip("暂停界面")]
    [SerializeField] private GameObject m_PausePrefab;
    
    [Tooltip("游戏结束界面")]
    [SerializeField] private GameObject m_GameOverPrefab;
    
    [Tooltip("升级界面")]
    [SerializeField] private GameObject m_UpgradePrefab;
    
    [Tooltip("仓库界面")]
    [SerializeField] private GameObject m_InventoryPrefab;
    
    [Tooltip("商店界面")]
    [SerializeField] private GameObject m_ShopPrefab;
    
    [Tooltip("加载界面")]
    [SerializeField] private GameObject m_LoadingPrefab;
    
    [Header("UI设置")]
    [Tooltip("UI动画持续时间")]
    [SerializeField] private float m_UIAnimationDuration = 0.3f;
    
    [Tooltip("UI淡入淡出效果")]
    [SerializeField] private bool m_UseFadeEffect = true;
    #endregion
    
    #region 私有变量
    // 当前打开的UI界面字典
    private Dictionary<string, GameObject> m_ActiveUIs = new Dictionary<string, GameObject>();
    
    // UI层级设置
    private Transform m_UIRoot;
    private Transform m_BackgroundLayer;
    private Transform m_GameplayLayer;
    private Transform m_PopupLayer;
    private Transform m_LoadingLayer;
    #endregion
    
    #region Unity生命周期
    private void Awake()
    {
        // 单例实现检查
        if (s_Instance != null && s_Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        s_Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // 初始化UI层级
        InitializeUIHierarchy();
        
        // 注册游戏状态变更事件
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
            GameManager.Instance.OnSceneLoaded += OnSceneLoaded;
        }
    }
    
    private void OnDestroy()
    {
        // 取消注册事件
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
            GameManager.Instance.OnSceneLoaded -= OnSceneLoaded;
        }
    }
    #endregion
    
    #region 公共方法
    /// <summary>
    /// 打开升级界面
    /// </summary>
    public void OpenUpgradeUI()
    {
        OpenUI("Upgrade", m_UpgradePrefab, m_PopupLayer);
    }
    
    /// <summary>
    /// 打开仓库界面
    /// </summary>
    public void OpenInventoryUI()
    {
        OpenUI("Inventory", m_InventoryPrefab, m_PopupLayer);
    }
    
    /// <summary>
    /// 打开商店界面
    /// </summary>
    public void OpenShopUI()
    {
        OpenUI("Shop", m_ShopPrefab, m_PopupLayer);
    }
    
    /// <summary>
    /// 打开暂停界面
    /// </summary>
    public void OpenPauseUI()
    {
        OpenUI("Pause", m_PausePrefab, m_PopupLayer);
    }
    
    /// <summary>
    /// 打开游戏结束界面
    /// </summary>
    public void OpenGameOverUI()
    {
        OpenUI("GameOver", m_GameOverPrefab, m_PopupLayer);
    }
    
    /// <summary>
    /// 打开主菜单界面
    /// </summary>
    public void OpenMainMenuUI()
    {
        OpenUI("MainMenu", m_MainMenuPrefab, m_BackgroundLayer);
    }
    
    /// <summary>
    /// 打开游戏HUD界面
    /// </summary>
    public void OpenGameHUD()
    {
        OpenUI("GameHUD", m_GameHUDPrefab, m_GameplayLayer);
    }
    
    /// <summary>
    /// 显示加载界面
    /// </summary>
    public void ShowLoadingScreen()
    {
        OpenUI("Loading", m_LoadingPrefab, m_LoadingLayer);
    }
    
    /// <summary>
    /// 隐藏加载界面
    /// </summary>
    public void HideLoadingScreen()
    {
        CloseUI("Loading");
    }
    
    /// <summary>
    /// 更新加载进度
    /// </summary>
    public void UpdateLoadingProgress(float progress)
    {
        if (m_ActiveUIs.TryGetValue("Loading", out GameObject loadingUI))
        {
            LoadingScreen loadingScreen = loadingUI.GetComponent<LoadingScreen>();
            if (loadingScreen != null)
            {
                loadingScreen.UpdateProgress(progress);
            }
        }
    }
    
    /// <summary>
    /// 关闭指定UI界面
    /// </summary>
    public void CloseUI(string uiName)
    {
        if (m_ActiveUIs.TryGetValue(uiName, out GameObject uiObj))
        {
            // 触发UI关闭事件
            OnUIClosed?.Invoke(uiName);
            
            // 销毁UI对象
            Destroy(uiObj);
            m_ActiveUIs.Remove(uiName);
            
            Debug.Log($"关闭界面：{uiName}");
        }
    }
    
    /// <summary>
    /// 关闭所有UI界面
    /// </summary>
    public void CloseAllUI()
    {
        // 创建一个新列表来存储所有要关闭的UI名称
        List<string> uiToClose = new List<string>(m_ActiveUIs.Keys);
        
        // 遍历并关闭每个UI
        foreach (string uiName in uiToClose)
        {
            CloseUI(uiName);
        }
        
        m_ActiveUIs.Clear();
        Debug.Log("已关闭所有UI界面");
    }
    #endregion
    
    #region 私有方法
    /// <summary>
    /// 初始化UI层级结构
    /// </summary>
    private void InitializeUIHierarchy()
    {
        // 创建UI根节点
        m_UIRoot = new GameObject("UI_Root").transform;
        m_UIRoot.SetParent(transform);
        
        // 创建不同的UI层级
        m_BackgroundLayer = CreateUILayer("BackgroundLayer", 0);
        m_GameplayLayer = CreateUILayer("GameplayLayer", 1);
        m_PopupLayer = CreateUILayer("PopupLayer", 2);
        m_LoadingLayer = CreateUILayer("LoadingLayer", 3);
    }
    
    /// <summary>
    /// 创建UI层级
    /// </summary>
    private Transform CreateUILayer(string layerName, int siblingIndex)
    {
        GameObject layerObj = new GameObject(layerName);
        Transform layerTransform = layerObj.transform;
        layerTransform.SetParent(m_UIRoot);
        layerTransform.SetSiblingIndex(siblingIndex);
        
        return layerTransform;
    }
    
    /// <summary>
    /// 打开UI界面
    /// </summary>
    private void OpenUI(string uiName, GameObject uiPrefab, Transform parent)
    {
        // 如果该UI已经打开，则直接返回
        if (m_ActiveUIs.ContainsKey(uiName))
        {
            Debug.Log($"界面 {uiName} 已经打开");
            return;
        }
        
        // 检查预制体是否存在
        if (uiPrefab == null)
        {
            Debug.LogError($"找不到界面预制体：{uiName}");
            return;
        }
        
        // 实例化UI预制体
        GameObject uiInstance = Instantiate(uiPrefab, parent);
        uiInstance.name = uiName;
        
        // 添加到活动UI字典
        m_ActiveUIs[uiName] = uiInstance;
        
        // 设置UI动画（如果需要）
        if (m_UseFadeEffect)
        {
            StartCoroutine(FadeInUI(uiInstance));
        }
        
        // 触发UI打开事件
        OnUIOpened?.Invoke(uiName);
        
        Debug.Log($"打开界面：{uiName}");
    }
    
    /// <summary>
    /// UI淡入效果协程
    /// </summary>
    private IEnumerator FadeInUI(GameObject uiObj)
    {
        CanvasGroup canvasGroup = uiObj.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = uiObj.AddComponent<CanvasGroup>();
        }
        
        canvasGroup.alpha = 0f;
        
        float time = 0f;
        while (time < m_UIAnimationDuration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, time / m_UIAnimationDuration);
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
    }
    
    /// <summary>
    /// 游戏状态变更回调
    /// </summary>
    private void OnGameStateChanged(GameState oldState, GameState newState)
    {
        switch (newState)
        {
            case GameState.MainMenu:
                CloseAllUI();
                OpenMainMenuUI();
                break;
                
            case GameState.Gameplay:
                // 关闭主菜单和暂停界面，显示游戏HUD
                CloseUI("MainMenu");
                CloseUI("Pause");
                OpenGameHUD();
                break;
                
            case GameState.Paused:
                OpenPauseUI();
                break;
                
            case GameState.GameOver:
                OpenGameOverUI();
                break;
                
            case GameState.Loading:
                ShowLoadingScreen();
                break;
        }
    }
    
    /// <summary>
    /// 场景加载完成回调
    /// </summary>
    private void OnSceneLoaded(string sceneName)
    {
        // 场景加载完成后隐藏加载界面
        HideLoadingScreen();
    }
    #endregion
}

/// <summary>
/// 加载界面脚本（示例，实际使用时完善）
/// </summary>
public class LoadingScreen : MonoBehaviour
{
    public void UpdateProgress(float progress)
    {
        // 更新加载进度显示
    }
} 