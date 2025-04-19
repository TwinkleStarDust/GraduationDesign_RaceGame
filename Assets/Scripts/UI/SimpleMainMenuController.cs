using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 简化的主菜单控制器
/// </summary>
public class SimpleMainMenuController : MonoBehaviour
{
    public Button startButton;
    public Button quitButton;
    
    private void Start()
    {
        if (startButton != null)
            startButton.onClick.AddListener(OnStartClicked);
            
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);
    }
    
    private void OnStartClicked()
    {
        Debug.Log("开始游戏按钮点击");
    }
    
    private void OnQuitClicked()
    {
        Debug.Log("退出游戏按钮点击");
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
} 