using UnityEngine;
using UnityEngine.SceneManagement; // 用于加载游戏场景

public class MainMenuUI : MonoBehaviour
{
    #region 公共方法
    public void OnStartGameButtonPressed()
    {
        Debug.Log("开始游戏按钮被按下");
        // 调用UIManager显示地图选择面板
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowMapSelectionPanel();
        }
        else
        {
            Debug.LogError("UIManager 实例未找到！");
        }
    }

    public void OnGarageButtonPressed()
    {
        Debug.Log("车库按钮被按下");
        // 调用UIManager显示车库面板
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGaragePanel();
        }
        else
        {
            Debug.LogError("UIManager 实例未找到！");
        }
    }

    public void OnSettingsButtonPressed()
    {
        Debug.Log("设置按钮被按下");
        // 调用UIManager显示设置面板
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowSettingsPanel();
        }
        else
        {
            Debug.LogError("UIManager 实例未找到！");
        }
    }

    public void OnExitButtonPressed()
    {
        Debug.Log("退出按钮被按下");
        // 退出游戏
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    #endregion
} 