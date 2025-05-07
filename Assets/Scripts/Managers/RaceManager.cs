using UnityEngine;
using Ricimi; // ModularGameUIKit的命名空间
using UnityEngine.SceneManagement;

namespace RaceGame.Managers
{
    /// <summary>
    /// 比赛管理器 - 管理比赛和奖励
    /// </summary>
    public class RaceManager : MonoBehaviour
    {
        #region 单例实现
        private static RaceManager s_Instance;
        public static RaceManager Instance => s_Instance;

        private void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            s_Instance = this;
        }
        #endregion
        
        #region 比赛奖励设置
        [Header("奖励设置")]
        [SerializeField] private int m_FirstPlaceCoins = 500;
        [SerializeField] private int m_SecondPlaceCoins = 300;
        [SerializeField] private int m_ThirdPlaceCoins = 200;
        [SerializeField] private int m_ParticipationCoins = 50;
        
        [Header("UI引用")]
        [SerializeField] private ModularPopupOpener m_RaceResultPopupOpener;
        [SerializeField] private string m_MainMenuSceneName = "MainMenu";
        #endregion
        
        #region 比赛结果处理
        /// <summary>
        /// 结束比赛并发放奖励
        /// </summary>
        public void EndRace(int playerPosition)
        {
            int rewardCoins = 0;
            string positionText = "";
            
            switch (playerPosition)
            {
                case 1:
                    rewardCoins = m_FirstPlaceCoins;
                    positionText = "第一名";
                    break;
                case 2:
                    rewardCoins = m_SecondPlaceCoins;
                    positionText = "第二名";
                    break;
                case 3:
                    rewardCoins = m_ThirdPlaceCoins;
                    positionText = "第三名";
                    break;
                default:
                    rewardCoins = m_ParticipationCoins;
                    positionText = "第" + playerPosition + "名";
                    break;
            }
            
            // 添加金币
            if (GameDataManager.Instance != null)
            {
                GameDataManager.Instance.AddCoinsFromRace(rewardCoins);
            }
            
            // 显示结果弹窗
            ShowRaceResults(positionText, rewardCoins);
        }
        
        /// <summary>
        /// 显示比赛结果
        /// </summary>
        private void ShowRaceResults(string position, int coins)
        {
            if (m_RaceResultPopupOpener != null)
            {
                m_RaceResultPopupOpener.Title = "比赛结束";
                m_RaceResultPopupOpener.Subtitle = "您的名次: " + position;
                m_RaceResultPopupOpener.Message = "恭喜您完成比赛！\n\n奖励: " + coins + " 金币";
                
                // 配置按钮
                m_RaceResultPopupOpener.Buttons.Clear();
                
                // 继续按钮
                ButtonInfo continueButton = new ButtonInfo
                {
                    Label = "继续",
                    ClosePopupWhenClicked = true
                };
                continueButton.OnClickedEvent.AddListener(ReturnToMainMenu);
                m_RaceResultPopupOpener.Buttons.Add(continueButton);
                
                m_RaceResultPopupOpener.OpenPopup();
            }
        }
        
        /// <summary>
        /// 返回主菜单
        /// </summary>
        private void ReturnToMainMenu()
        {
            SceneManager.LoadScene(m_MainMenuSceneName);
        }
        #endregion
    }
} 