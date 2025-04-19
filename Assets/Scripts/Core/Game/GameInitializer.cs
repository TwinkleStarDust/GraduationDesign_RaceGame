using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 游戏初始化器 - 目前仅加载主菜单场景
/// </summary>
public class GameInitializer : MonoBehaviour
{
    [Header("场景设置")]
    [Tooltip("主菜单场景名称")]
    [SerializeField] private string m_MainMenuSceneName = "MainMenu";
    
    [Tooltip("是否自动加载主菜单")]
    [SerializeField] private bool m_AutoLoadMainMenu = true;
    
    private void Awake()
    {
        // 确保此对象不会被销毁
        DontDestroyOnLoad(gameObject);
    }
    
    private void Start()
    {
        // 自动加载主菜单场景
        if (m_AutoLoadMainMenu)
        {
            LoadMainMenu();
        }
    }
    
    /// <summary>
    /// 加载主菜单场景
    /// </summary>
    public void LoadMainMenu()
    {
        // 检查场景是否存在
        if (SceneExists(m_MainMenuSceneName))
        {
            SceneManager.LoadScene(m_MainMenuSceneName);
        }
        else
        {
            Debug.LogError($"场景 {m_MainMenuSceneName} 不存在，请确保它已添加到Build Settings中");
        }
    }
    
    /// <summary>
    /// 检查场景是否存在
    /// </summary>
    private bool SceneExists(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return false;
            
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            
            if (name == sceneName)
                return true;
        }
        
        return false;
    }
} 