using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 游戏管理器 - 全局游戏状态控制与场景管理
/// </summary>
public class GameManager : MonoBehaviour
{
    #region 单例实现
    private static GameManager s_Instance;
    public static GameManager Instance 
    { 
        get 
        {
            if (s_Instance == null)
            {
                GameObject managerObj = new GameObject("GameManager");
                s_Instance = managerObj.AddComponent<GameManager>();
                DontDestroyOnLoad(managerObj);
            }
            return s_Instance;
        }
    }
    #endregion

    #region 事件定义
    // 游戏状态变更事件
    public event Action<GameState, GameState> OnGameStateChanged;
    // 场景加载完成事件
    public event Action<string> OnSceneLoaded;
    // 游戏暂停/继续事件
    public event Action<bool> OnGamePaused;
    #endregion

    #region 公共属性
    /// <summary>
    /// 当前游戏状态
    /// </summary>
    public GameState CurrentGameState { get; private set; } = GameState.MainMenu;

    /// <summary>
    /// 当前关卡索引
    /// </summary>
    public int CurrentLevelIndex { get; private set; } = 0;

    /// <summary>
    /// 游戏是否暂停
    /// </summary>
    public bool IsPaused { get; private set; } = false;
    #endregion

    #region 私有变量
    private bool m_IsTransitioning = false;
    private AsyncOperation m_LoadOperation;
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
        
        // 注册场景加载事件
        SceneManager.sceneLoaded += OnSceneLoadedCallback;
    }

    private void OnDestroy()
    {
        // 取消注册场景加载事件
        SceneManager.sceneLoaded -= OnSceneLoadedCallback;
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 更改游戏状态
    /// </summary>
    public void ChangeGameState(GameState newState)
    {
        if (CurrentGameState == newState) return;

        GameState oldState = CurrentGameState;
        CurrentGameState = newState;

        // 触发状态更改事件
        OnGameStateChanged?.Invoke(oldState, newState);
        
        Debug.Log($"游戏状态从 {oldState} 变更为 {newState}");
    }

    /// <summary>
    /// 加载场景
    /// </summary>
    public void LoadScene(string sceneName, bool showLoadingScreen = true)
    {
        if (m_IsTransitioning) return;
        
        StartCoroutine(LoadSceneAsync(sceneName, showLoadingScreen));
    }

    /// <summary>
    /// 加载关卡
    /// </summary>
    public void LoadLevel(int levelIndex, bool showLoadingScreen = true)
    {
        if (levelIndex < 0 || m_IsTransitioning) return;
        
        CurrentLevelIndex = levelIndex;
        string levelSceneName = $"Level_{levelIndex}";
        
        StartCoroutine(LoadSceneAsync(levelSceneName, showLoadingScreen));
    }

    /// <summary>
    /// 重新加载当前场景
    /// </summary>
    public void ReloadCurrentScene()
    {
        LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// 控制游戏暂停
    /// </summary>
    public void SetPaused(bool paused)
    {
        if (IsPaused == paused) return;
        
        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
        
        // 触发暂停事件
        OnGamePaused?.Invoke(paused);
        
        Debug.Log(paused ? "游戏已暂停" : "游戏已继续");
    }

    /// <summary>
    /// 切换暂停状态
    /// </summary>
    public void TogglePause()
    {
        SetPaused(!IsPaused);
    }

    /// <summary>
    /// 退出游戏
    /// </summary>
    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    #endregion

    #region 私有方法
    /// <summary>
    /// 场景加载回调
    /// </summary>
    private void OnSceneLoadedCallback(Scene scene, LoadSceneMode mode)
    {
        string sceneName = scene.name;
        
        // 根据场景名称自动设置状态
        if (sceneName.Contains("MainMenu"))
        {
            ChangeGameState(GameState.MainMenu);
        }
        else if (sceneName.Contains("Level"))
        {
            ChangeGameState(GameState.Gameplay);
        }
        
        // 触发场景加载完成事件
        OnSceneLoaded?.Invoke(sceneName);
        
        Debug.Log($"场景 {sceneName} 已加载完成");
    }

    /// <summary>
    /// 异步加载场景
    /// </summary>
    private IEnumerator LoadSceneAsync(string sceneName, bool showLoadingScreen)
    {
        m_IsTransitioning = true;
        
        // 显示加载界面
        if (showLoadingScreen)
        {
            // 此处可以调用UI管理器显示加载界面
            // UIManager.Instance.ShowLoadingScreen();
        }
        
        // 触发状态变更
        ChangeGameState(GameState.Loading);
        
        // 等待一帧以确保状态更改事件被处理
        yield return null;
        
        // 加载新场景
        m_LoadOperation = SceneManager.LoadSceneAsync(sceneName);
        
        // 等待场景加载完成
        while (!m_LoadOperation.isDone)
        {
            float progress = Mathf.Clamp01(m_LoadOperation.progress / 0.9f);
            // 可以更新加载进度
            // if (showLoadingScreen) UIManager.Instance.UpdateLoadingProgress(progress);
            
            yield return null;
        }
        
        m_IsTransitioning = false;
    }
    #endregion
}

/// <summary>
/// 游戏状态枚举
/// </summary>
public enum GameState
{
    MainMenu,   // 主菜单
    Loading,    // 加载中
    Gameplay,   // 游戏进行中
    Paused,     // 暂停
    GameOver,   // 游戏结束
    Victory     // 胜利
} 