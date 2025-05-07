using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using RaceGame.Managers;

namespace RaceGame.UI
{
    /// <summary>
    /// 菜单按钮处理器，连接UI按钮与菜单管理器
    /// </summary>
    public class MenuButtonHandler : MonoBehaviour, IPointerEnterHandler
    {
        #region 私有字段
        [Header("菜单引用")]
        [SerializeField] private MainMenuManager m_MenuManager;
        [SerializeField] private MenuAudioController m_AudioController;

        [Header("按钮功能")]
        [SerializeField] private MenuButtonType m_ButtonType = MenuButtonType.None;
        [SerializeField] private string m_TargetPanelName; // 如果按钮类型是"ShowPanel"时使用
        [SerializeField] private string m_SceneName; // 如果按钮类型是"LoadScene"时使用
        [SerializeField] private string m_TrackName; // 如果按钮类型是"LoadTrack"时使用

        // 私有变量
        private Button m_Button;
        #endregion

        #region Unity生命周期
        private void Awake()
        {
            // 获取按钮组件
            m_Button = GetComponent<Button>();
            
            // 自动查找管理器（如果未设置）
            if (m_MenuManager == null)
            {
                m_MenuManager = FindObjectOfType<MainMenuManager>();
            }
            
            if (m_AudioController == null)
            {
                m_AudioController = FindObjectOfType<MenuAudioController>();
            }
            
            // 设置按钮点击事件
            if (m_Button != null)
            {
                m_Button.onClick.AddListener(OnButtonClick);
            }
        }
        #endregion

        #region 接口实现
        public void OnPointerEnter(PointerEventData eventData)
        {
            // 播放悬停音效
            if (m_AudioController != null)
            {
                m_AudioController.PlayHoverSound();
            }
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 按钮点击事件
        /// </summary>
        private void OnButtonClick()
        {
            // 播放点击音效
            if (m_AudioController != null)
            {
                m_AudioController.PlayButtonClickSound();
            }
            
            // 根据按钮类型执行相应操作
            switch (m_ButtonType)
            {
                case MenuButtonType.ShowPanel:
                    ShowPanel();
                    break;
                case MenuButtonType.LoadScene:
                    LoadScene();
                    break;
                case MenuButtonType.ReturnToMainMenu:
                    ReturnToMainMenu();
                    break;
                case MenuButtonType.OpenSettings:
                    OpenSettings();
                    break;
                case MenuButtonType.OpenCredits:
                    OpenCredits();
                    break;
                case MenuButtonType.LoadTrack:
                    LoadTrack();
                    break;
                case MenuButtonType.QuitGame:
                    QuitGame();
                    break;
                case MenuButtonType.OpenRaceMode:
                    OpenRaceMode();
                    break;
                case MenuButtonType.OpenCarSelection:
                    OpenCarSelection();
                    break;
                case MenuButtonType.None:
                default:
                    Debug.LogWarning($"按钮 '{gameObject.name}' 没有设置功能类型！");
                    break;
            }
        }

        /// <summary>
        /// 显示指定面板
        /// </summary>
        private void ShowPanel()
        {
            if (m_MenuManager != null && !string.IsNullOrEmpty(m_TargetPanelName))
            {
                m_MenuManager.ShowPanel(m_TargetPanelName);
            }
            else
            {
                Debug.LogError($"无法显示面板：菜单管理器为null或面板名称为空！");
            }
        }

        /// <summary>
        /// 加载场景
        /// </summary>
        private void LoadScene()
        {
            if (!string.IsNullOrEmpty(m_SceneName))
            {
                if (SceneController.Instance != null)
                {
                    SceneController.Instance.LoadScene(m_SceneName);
                }
                else
                {
                    Debug.LogError("场景控制器未找到！");
                }
            }
            else
            {
                Debug.LogError("场景名称未设置！");
            }
        }

        /// <summary>
        /// 返回主菜单
        /// </summary>
        private void ReturnToMainMenu()
        {
            if (m_MenuManager != null)
            {
                m_MenuManager.ReturnToMainMenu();
            }
        }

        /// <summary>
        /// 打开设置面板
        /// </summary>
        private void OpenSettings()
        {
            if (m_MenuManager != null)
            {
                m_MenuManager.OpenSettings();
            }
        }

        /// <summary>
        /// 打开制作人员面板
        /// </summary>
        private void OpenCredits()
        {
            if (m_MenuManager != null)
            {
                m_MenuManager.OpenCredits();
            }
        }

        /// <summary>
        /// 打开比赛模式面板
        /// </summary>
        private void OpenRaceMode()
        {
            if (m_MenuManager != null)
            {
                m_MenuManager.OpenRaceMode();
            }
        }

        /// <summary>
        /// 打开车辆选择面板
        /// </summary>
        private void OpenCarSelection()
        {
            if (m_MenuManager != null)
            {
                m_MenuManager.OpenCarSelection();
            }
        }

        /// <summary>
        /// 加载指定赛道
        /// </summary>
        private void LoadTrack()
        {
            if (!string.IsNullOrEmpty(m_TrackName))
            {
                if (SceneController.Instance != null)
                {
                    SceneController.Instance.LoadGameScene(m_TrackName);
                }
                else
                {
                    Debug.LogError("场景控制器未找到！");
                }
            }
            else
            {
                Debug.LogError("赛道名称未设置！");
            }
        }

        /// <summary>
        /// 退出游戏
        /// </summary>
        private void QuitGame()
        {
            if (m_MenuManager != null)
            {
                m_MenuManager.QuitGame();
            }
            else
            {
                Debug.Log("退出游戏");
                
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #else
                Application.Quit();
                #endif
            }
        }
        #endregion
    }

    /// <summary>
    /// 菜单按钮类型枚举
    /// </summary>
    public enum MenuButtonType
    {
        None,
        ShowPanel,
        LoadScene,
        ReturnToMainMenu,
        OpenSettings,
        OpenCredits,
        OpenRaceMode,
        OpenCarSelection,
        LoadTrack,
        QuitGame
    }
} 