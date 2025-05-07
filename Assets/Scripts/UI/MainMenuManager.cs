using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RaceGame.UI
{
    /// <summary>
    /// 主菜单管理器，负责处理所有菜单面板的显示、隐藏和切换
    /// </summary>
    public class MainMenuManager : MonoBehaviour
    {
        #region 私有字段
        [Header("菜单面板")]
        [SerializeField] private GameObject m_MainPanel;         // 主菜单面板
        [SerializeField] private GameObject m_SettingsPanel;     // 设置面板
        [SerializeField] private GameObject m_CreditsPanel;      // 制作人员面板
        [SerializeField] private GameObject m_RaceModePanel;     // 比赛模式面板
        [SerializeField] private GameObject m_CarSelectionPanel; // 车辆选择面板
        [SerializeField] private GameObject m_TrackSelectionPanel; // 赛道选择面板

        [Header("音频设置")]
        [SerializeField] private AudioSource m_ButtonClickSound; // 按钮点击音效

        // 所有面板的字典集合，用于快速访问
        private Dictionary<string, GameObject> m_PanelDictionary = new Dictionary<string, GameObject>();
        
        // 当前活动面板
        private GameObject m_CurrentActivePanel;
        #endregion

        #region Unity生命周期
        private void Awake()
        {
            InitializePanelDictionary();
            HideAllPanels();
            
            // 默认显示主面板
            ShowPanel("Main");
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 初始化面板字典
        /// </summary>
        private void InitializePanelDictionary()
        {
            if (m_MainPanel != null) m_PanelDictionary.Add("Main", m_MainPanel);
            if (m_SettingsPanel != null) m_PanelDictionary.Add("Settings", m_SettingsPanel);
            if (m_CreditsPanel != null) m_PanelDictionary.Add("Credits", m_CreditsPanel);
            if (m_RaceModePanel != null) m_PanelDictionary.Add("RaceMode", m_RaceModePanel);
            if (m_CarSelectionPanel != null) m_PanelDictionary.Add("CarSelection", m_CarSelectionPanel);
            if (m_TrackSelectionPanel != null) m_PanelDictionary.Add("TrackSelection", m_TrackSelectionPanel);
        }

        /// <summary>
        /// 隐藏所有面板
        /// </summary>
        private void HideAllPanels()
        {
            foreach (var panel in m_PanelDictionary.Values)
            {
                if (panel != null)
                {
                    panel.SetActive(false);
                }
            }
            m_CurrentActivePanel = null;
        }

        /// <summary>
        /// 播放按钮点击音效
        /// </summary>
        private void PlayButtonClickSound()
        {
            if (m_ButtonClickSound != null)
            {
                m_ButtonClickSound.Play();
            }
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 显示指定面板
        /// </summary>
        /// <param name="_panelName">面板名称</param>
        public void ShowPanel(string _panelName)
        {
            // 播放按钮音效
            PlayButtonClickSound();
            
            // 隐藏当前面板
            if (m_CurrentActivePanel != null)
            {
                m_CurrentActivePanel.SetActive(false);
            }
            
            // 显示新面板
            if (m_PanelDictionary.TryGetValue(_panelName, out GameObject panel))
            {
                panel.SetActive(true);
                m_CurrentActivePanel = panel;
                Debug.Log($"显示面板: {_panelName}");
            }
            else
            {
                Debug.LogWarning($"面板 '{_panelName}' 不存在!");
            }
        }

        /// <summary>
        /// 返回主菜单
        /// </summary>
        public void ReturnToMainMenu()
        {
            ShowPanel("Main");
        }

        /// <summary>
        /// 打开设置面板
        /// </summary>
        public void OpenSettings()
        {
            ShowPanel("Settings");
        }

        /// <summary>
        /// 打开制作人员面板
        /// </summary>
        public void OpenCredits()
        {
            ShowPanel("Credits");
        }

        /// <summary>
        /// 打开比赛模式选择面板
        /// </summary>
        public void OpenRaceMode()
        {
            ShowPanel("RaceMode");
        }

        /// <summary>
        /// 打开车辆选择面板
        /// </summary>
        public void OpenCarSelection()
        {
            ShowPanel("CarSelection");
        }

        /// <summary>
        /// 打开赛道选择面板
        /// </summary>
        public void OpenTrackSelection()
        {
            ShowPanel("TrackSelection");
        }

        /// <summary>
        /// 退出游戏
        /// </summary>
        public void QuitGame()
        {
            PlayButtonClickSound();
            Debug.Log("退出游戏");
            
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
        #endregion
    }
} 