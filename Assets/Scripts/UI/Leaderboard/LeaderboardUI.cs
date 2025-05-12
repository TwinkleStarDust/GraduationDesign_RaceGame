using UnityEngine;
using UnityEngine.UI; 
using TMPro;
using System.Collections.Generic;
using System.Linq; 

public class LeaderboardUI : MonoBehaviour
{
    #region 比赛结果条目结构体
    public struct PlayerRankEntry
    {
        public int Rank;
        public string PlayerName;
        public float TotalTime;
        public bool IsPlayer; 

        public PlayerRankEntry(int _rank, string _name, float _time, bool _isPlayer = false)
        {
            Rank = _rank;
            PlayerName = _name;
            TotalTime = _time;
            IsPlayer = _isPlayer;
        }
    }
    #endregion

    #region 私有字段
    [Header("UI引用")]
    [Tooltip("ScrollView的Content Transform，用于放置排行榜条目")]
    [SerializeField] private Transform m_ScrollViewContent;
    [Tooltip("排行榜条目的预制件 (应挂载RankItemUI脚本)")]
    [SerializeField] private GameObject m_RankItemPrefab;
    [Tooltip("返回主菜单按钮")]
    [SerializeField] private Button m_BackButton;
    [Tooltip("继续驾驶按钮")]
    [SerializeField] private Button m_ContinueDrivingButton;
    [Tooltip("显示总金币的TextMeshProUGUI")]
    [SerializeField] private TextMeshProUGUI m_TotalCoinsText;
    [Tooltip("显示本局获得金币的TextMeshProUGUI")]
    [SerializeField] private TextMeshProUGUI m_EarnedCoinsText;

    [Header("行为配置")]
    [Tooltip("是否自动隐藏WinPanel (在游戏开始时)")]
    [SerializeField] private bool m_AutoHideOnStart = true;
    [Tooltip("自动隐藏判断的时间阈值 (秒)，小于此值视为游戏初始化阶段")]
    [SerializeField] private float m_InitialGameTimeThreshold = 5f;

    [Header("数据源引用")]
    [Tooltip("对玩家比赛状态的引用 (用于获取玩家成绩)")]
    [SerializeField] private PlayerRaceState m_PlayerRaceState;
    private CarController m_PlayerCarController;

    [Header("排行榜配置")]
    [Tooltip("模拟AI玩家的数量")]
    [SerializeField] private int m_SimulatedAICount = 7;
    [Tooltip("AI玩家名称前缀")]
    [SerializeField] private string m_AINamePrefix = "AI Racer ";
    [Tooltip("AI玩家时间的最小浮动范围 (相对于玩家时间, 秒)")]
    [SerializeField, Range(-30f, 5f)] private float m_AITimeMinOffset = -15f;
    [Tooltip("AI玩家时间的最大浮动范围 (相对于玩家时间, 秒)")]
    [SerializeField, Range(0f, 60f)] private float m_AITimeMaxOffset = 30f;

    [Header("金币奖励配置")]
    [SerializeField, Tooltip("对PlayerInventorySO资产的引用")]
    private PlayerInventorySO m_PlayerInventorySO;
    [SerializeField, Tooltip("第一名奖励")] private int m_FirstPlaceBonus = 200;
    [SerializeField, Tooltip("第二名奖励")] private int m_SecondPlaceBonus = 100;
    [SerializeField, Tooltip("第三名奖励")] private int m_ThirdPlaceBonus = 50;
    [SerializeField, Tooltip("时间奖励的基础值")] private float m_BaseTimeReward = 120f;
    [SerializeField, Tooltip("每秒的时间惩罚率")] private float m_TimePenaltyRate = 1.0f;

    private List<RankItemUI> m_InstantiatedRankItems = new List<RankItemUI>();
    private bool m_CoinsAwardedThisRace = false; // 防止重复奖励的标志
    private int m_LastRaceReward = 0; // 新增：存储本局获得的金币
    #endregion

    #region Unity生命周期
    private void Awake()
    {
        Debug.Log($"[LeaderboardUI] Awake 被调用 - TimeSinceLevelLoad: {Time.timeSinceLevelLoad}s");
        
        if (m_ScrollViewContent == null) Debug.LogError("LeaderboardUI: m_ScrollViewContent 未分配!", this);
        if (m_RankItemPrefab == null) Debug.LogError("LeaderboardUI: m_RankItemPrefab 未分配!", this);
        if (m_RankItemPrefab != null && m_RankItemPrefab.GetComponent<RankItemUI>() == null)
        {
            Debug.LogError("LeaderboardUI: m_RankItemPrefab 上缺少 RankItemUI 组件!", this);
        }

        if (m_PlayerRaceState == null)
        {
            m_PlayerRaceState = FindObjectOfType<PlayerRaceState>();
            if (m_PlayerRaceState == null)
            {
                Debug.LogError("LeaderboardUI: 未能自动找到 PlayerRaceState 实例。请在Inspector中分配，或确保其在场景中存在。", this);
            }
        }

        if (m_PlayerRaceState != null)
        {
            m_PlayerCarController = m_PlayerRaceState.GetComponentInParent<CarController>();
        }
        if (m_PlayerCarController == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null) m_PlayerCarController = playerObject.GetComponent<CarController>();
        }
        if (m_PlayerCarController == null)
        {
            Debug.LogWarning("LeaderboardUI: 未能找到玩家的CarController。继续驾驶功能可能无法正确启用输入。", this);
        }
        
        // 确保Content有垂直布局组件
        EnsureVerticalLayoutOnContent();
    }

    private void Start()
    {
        Debug.Log($"[LeaderboardUI] Start 被调用 - TimeSinceLevelLoad: {Time.timeSinceLevelLoad}s, GameObject激活状态: {gameObject.activeSelf}");
        
        // 使用Time.timeSinceLevelLoad判断是游戏刚开始还是比赛结束后被激活
        bool isInitialGameStart = Time.timeSinceLevelLoad < m_InitialGameTimeThreshold; // 使用可配置的阈值
        
        // 获取WinPanel
        GameObject panelToHide = GetPanelToHide();
        
        // 只在启用了自动隐藏选项，并且是游戏初始启动时才自动隐藏WinPanel
        if (m_AutoHideOnStart && isInitialGameStart && panelToHide.activeSelf)
        {
            Debug.Log($"[LeaderboardUI] 游戏开始时自动隐藏WinPanel (已启用自动隐藏) - TimeSinceLevelLoad: {Time.timeSinceLevelLoad}s");
            panelToHide.SetActive(false);
        }
        else if (!isInitialGameStart && panelToHide.activeSelf)
        {
            Debug.Log($"[LeaderboardUI] 检测到非游戏初始化阶段的激活，保持WinPanel显示 - TimeSinceLevelLoad: {Time.timeSinceLevelLoad}s");
            // 当是比赛结束后被激活时，确保填充排行榜
            PopulateLeaderboard();
        }
        else if (isInitialGameStart && panelToHide.activeSelf && !m_AutoHideOnStart)
        {
            Debug.Log($"[LeaderboardUI] 游戏开始阶段，但已禁用自动隐藏，保持WinPanel显示 - TimeSinceLevelLoad: {Time.timeSinceLevelLoad}s");
        }
        
        // 初始化按钮监听器，这样在面板激活时就已经设置好了
        SetupButtonListeners();
    }

    private void OnEnable()
    {
        // 重置奖励标志和上次奖励记录，确保每次激活面板时都可以重新计算奖励
        m_CoinsAwardedThisRace = false; 
        m_LastRaceReward = 0;

        // 清空或设置文本默认值
        if (m_TotalCoinsText != null) m_TotalCoinsText.text = "总金币: -";
        if (m_EarnedCoinsText != null) m_EarnedCoinsText.text = "本局获得: -";

        Debug.Log($"[LeaderboardUI] OnEnable 被调用 - TimeSinceLevelLoad: {Time.timeSinceLevelLoad}s, GameObject激活状态: {gameObject.activeSelf}");
        
        // 只在非初始化阶段才填充排行榜（比赛结束后）
        if (Time.timeSinceLevelLoad > m_InitialGameTimeThreshold) // 使用可配置的阈值
        {
            Debug.Log($"[LeaderboardUI] OnEnable - 填充排行榜 - TimeSinceLevelLoad: {Time.timeSinceLevelLoad}s");
            PopulateLeaderboard();
        }
        else
        {
            Debug.Log($"[LeaderboardUI] OnEnable - 跳过填充排行榜(初始化阶段) - TimeSinceLevelLoad: {Time.timeSinceLevelLoad}s");
        }
    }
    
    // 提取按钮监听器设置到单独的方法，避免代码重复
    private void SetupButtonListeners()
    {
        if (m_BackButton != null)
        {
            m_BackButton.onClick.RemoveAllListeners();
            m_BackButton.onClick.AddListener(OnBackToMainMenuPressed);
        }
        if (m_ContinueDrivingButton != null)
        {
            m_ContinueDrivingButton.onClick.RemoveAllListeners();
            m_ContinueDrivingButton.onClick.AddListener(OnContinueDrivingPressed);
        }
    }

    // 新增方法：确保Content有垂直布局组件
    private void EnsureVerticalLayoutOnContent()
    {
        if (m_ScrollViewContent == null) return;
        
        UnityEngine.UI.VerticalLayoutGroup verticalLayout = m_ScrollViewContent.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
        if (verticalLayout == null)
        {
            Debug.LogWarning("LeaderboardUI: ScrollViewContent缺少VerticalLayoutGroup组件，正在自动添加一个默认的。请在Inspector中根据需要调整其参数。", this.gameObject);
            verticalLayout = m_ScrollViewContent.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            // 不再修改verticalLayout的spacing, padding, childAlignment等属性，让用户在Inspector中设置
        }
        
        // 对于RectTransform的设置，主要是为了确保Content能够正确地从顶部开始并向下扩展，
        // 这通常是期望的行为，但如果你的设计不同，这部分也可以考虑移除或调整。
        // 暂时保留，因为这对于ContentSizeFitter配合VerticalLayoutGroup正确工作比较重要。
        RectTransform contentRect = m_ScrollViewContent as RectTransform;
        if (contentRect != null)
        {
            contentRect.anchorMin = new Vector2(0, 1);    // 顶部左边
            contentRect.anchorMax = new Vector2(1, 1);    // 顶部右边
            contentRect.pivot = new Vector2(0.5f, 1);   // 轴心在顶部中心
            // 不再修改sizeDelta.y，让ContentSizeFitter或用户设置决定
            contentRect.sizeDelta = new Vector2(0, contentRect.sizeDelta.y); 
        }
        
        UnityEngine.UI.ContentSizeFitter sizeFitter = m_ScrollViewContent.GetComponent<UnityEngine.UI.ContentSizeFitter>();
        if (sizeFitter == null)
        {
            Debug.LogWarning("LeaderboardUI: ScrollViewContent缺少ContentSizeFitter组件，正在自动添加一个默认的。请在Inspector中根据需要调整其参数。", this.gameObject);
            sizeFitter = m_ScrollViewContent.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            // 设置默认的FitMode，用户可以在Inspector中更改
            sizeFitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
            sizeFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize; 
        }
    }
    #endregion

    #region 公共方法
    public void PopulateLeaderboard()
    {
        if (m_ScrollViewContent == null || m_RankItemPrefab == null) return;

        // 确保布局组件存在
        EnsureVerticalLayoutOnContent();

        // 清理现有项目
        foreach (RankItemUI item in m_InstantiatedRankItems)
        {
            if (item != null) Destroy(item.gameObject);
        }
        m_InstantiatedRankItems.Clear();

        // 确保销毁后布局重新计算
        LayoutRebuilder.ForceRebuildLayoutImmediate(m_ScrollViewContent as RectTransform);

        List<PlayerRankEntry> raceResults = new List<PlayerRankEntry>();

        float playerTotalTime = 0f;
        bool playerResultValid = false;

        if (m_PlayerRaceState != null)
        {
            playerTotalTime = m_PlayerRaceState.TotalRaceTime;
            if (playerTotalTime > 0.001f) // 确保有有效时间, 避免浮点数精度问题
            {
                // 假设玩家名称从 PlayerInventorySO 获取，如果没有则用默认
                string playerName = "玩家"; 
                // if (m_PlayerRaceState.playerInventory != null && !string.IsNullOrEmpty(m_PlayerRaceState.playerInventory.PlayerName))
                // {
                //    playerName = m_PlayerRaceState.playerInventory.PlayerName;
                // }
                raceResults.Add(new PlayerRankEntry(0, playerName, playerTotalTime, true));
                playerResultValid = true;
            }
            else
            {
                Debug.LogWarning("LeaderboardUI: 玩家比赛时间无效或未完成比赛。将使用模拟时间。", this);
            }
        }
        else
        {
            Debug.LogError("LeaderboardUI: PlayerRaceState 未引用，无法获取玩家成绩。将使用模拟时间。", this);
        }
        
        // 如果玩家数据无效，给一个基础时间用于AI模拟
        if (!playerResultValid) playerTotalTime = Random.Range(120f, 240f); 

        for (int i = 0; i < m_SimulatedAICount; i++)
        {
            string aiName = m_AINamePrefix + (i + 1).ToString();
            float aiTime = playerTotalTime + Random.Range(m_AITimeMinOffset, m_AITimeMaxOffset) + Random.Range(-5f, 5f) * ( (i % 3) -1 ); // 增加一点系统性随机
            aiTime = Mathf.Max(playerTotalTime * 0.8f, aiTime); // AI不会比玩家快太多
            aiTime = Mathf.Max(60f, aiTime); // AI成绩至少1分钟，除非玩家更快
            raceResults.Add(new PlayerRankEntry(0, aiName, aiTime, false));
        }

        List<PlayerRankEntry> sortedResults = raceResults.OrderBy(entry => entry.TotalTime).ToList();

        for (int i = 0; i < sortedResults.Count; i++)
        {
            GameObject itemGO = Instantiate(m_RankItemPrefab, m_ScrollViewContent);
            RankItemUI rankItemUI = itemGO.GetComponent<RankItemUI>();
            
            if (rankItemUI != null)
            {
                PlayerRankEntry currentEntry = sortedResults[i];
                rankItemUI.Setup(i + 1, currentEntry.PlayerName, currentEntry.TotalTime);
                m_InstantiatedRankItems.Add(rankItemUI);
                
                // 高亮玩家的排名项目并计算奖励
                if (currentEntry.IsPlayer)
                {
                    HighlightPlayerEntry(itemGO);

                    // --- 金币奖励计算 --- 
                    if (!m_CoinsAwardedThisRace)
                    { 
                        if (m_PlayerInventorySO != null)
                        {
                            int playerRank = i + 1;
                            int rankBonus = 0;
                            if (playerRank == 1) rankBonus = m_FirstPlaceBonus;
                            else if (playerRank == 2) rankBonus = m_SecondPlaceBonus;
                            else if (playerRank == 3) rankBonus = m_ThirdPlaceBonus;

                            float timeBonus = Mathf.Max(0f, m_BaseTimeReward - currentEntry.TotalTime * m_TimePenaltyRate);
                            int totalReward = rankBonus + Mathf.FloorToInt(timeBonus);

                            m_LastRaceReward = totalReward; // 存储本次奖励

                            if (totalReward > 0)
                            {
                                m_PlayerInventorySO.AddCoins(totalReward);
                                Debug.Log($"[LeaderboardUI] 比赛结算：玩家排名 {playerRank} (奖励 {rankBonus}), 时间 {currentEntry.TotalTime:F2}s (奖励 {Mathf.FloorToInt(timeBonus)})。总奖励: {totalReward} 金币。", this);
                            }
                            else
                            {
                                Debug.Log($"[LeaderboardUI] 比赛结算：玩家排名 {playerRank} (奖励 {rankBonus}), 时间 {currentEntry.TotalTime:F2}s (奖励 {Mathf.FloorToInt(timeBonus)})。总奖励为0或负数，不发放金币。", this);
                            }
                            m_CoinsAwardedThisRace = true; // 标记已奖励

                            // 更新金币显示文本
                            UpdateCoinDisplayTexts();
                        }
                        else
                        {
                            Debug.LogError("[LeaderboardUI] PlayerInventorySO 未在Inspector中分配，无法发放金币奖励！", this);
                        }
                    }
                    // --- 金币奖励计算结束 --- 
                }
            }
        }
        
        // 强制重新计算布局
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(m_ScrollViewContent as RectTransform);
    }
    #endregion

    #region 私有回调方法
    private void OnBackToMainMenuPressed()
    {
        Debug.Log("LeaderboardUI: 返回主菜单按钮被按下");
        
        // 确保游戏状态重置，例如时间尺度
        Time.timeScale = 1f;

        // 尝试使用MainMenuUIManager返回主菜单，如果它存在
        if (MainMenuUIManager.Instance != null)
        {
            // 关闭当前的WinPanel
            // 假设此脚本的GameObject是WinPanel本身，或者WinPanel的直接子对象
            GetPanelToHide().SetActive(false);
            
            MainMenuUIManager.Instance.ShowMainMenuPanel(); 
        }
        else
        {
            Debug.LogWarning("LeaderboardUI: MainMenuUIManager 实例未找到！将直接加载主菜单场景。");
            // 作为备用，直接尝试加载主菜单场景 (假设场景名为 "MainMenu")
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }

    private void OnContinueDrivingPressed()
    {
        Debug.Log("LeaderboardUI: 继续驾驶按钮被按下");

        // 关闭WinPanel
        GetPanelToHide().SetActive(false);

        // 恢复时间
        Time.timeScale = 1f;

        // 重新启用玩家车辆控制
        if (m_PlayerCarController != null)
        {
            m_PlayerCarController.SetInputDisabled(false);
        }
        else
        {
            Debug.LogWarning("LeaderboardUI: CarController引用丢失，无法重新启用玩家输入。", this);
        }
    }

    private GameObject GetPanelToHide()
    {
        if (transform.parent != null && transform.parent.name.ToLower().Contains("winpanel"))
        {
            return transform.parent.gameObject;
        }
        return gameObject; 
    }

    // 新增：提取高亮逻辑
    private void HighlightPlayerEntry(GameObject _playerItemGO)
    {
        // 获取背景图像组件
        Image backgroundImage = _playerItemGO.GetComponent<Image>();
        if (backgroundImage != null)
        { 
            // 设置不同的颜色以突出显示玩家
            backgroundImage.color = new Color(1f, 0.92f, 0.016f, 0.5f); // 半透明金色
        }
                    
        // 获取所有文本组件以设置不同的颜色
        TextMeshProUGUI[] texts = _playerItemGO.GetComponentsInChildren<TextMeshProUGUI>();
        foreach (TextMeshProUGUI text in texts)
        {
            text.color = Color.black; // 设置为黑色以便在金色背景上更清晰
            text.fontStyle = FontStyles.Bold; // 设置为粗体
        }
    }

    // 新增：更新金币显示文本的方法
    private void UpdateCoinDisplayTexts()
    {
        if (m_PlayerInventorySO != null)
        {
            if (m_TotalCoinsText != null)
            { 
                m_TotalCoinsText.text = $" {m_PlayerInventorySO.PlayerCoins}";
            }
            else
            { 
                Debug.LogWarning("[LeaderboardUI] m_TotalCoinsText 未在Inspector中分配。", this);
            }

            if (m_EarnedCoinsText != null)
            {
                m_EarnedCoinsText.text = $" +{m_LastRaceReward}";
            }
            else
            {
                 Debug.LogWarning("[LeaderboardUI] m_EarnedCoinsText 未在Inspector中分配。", this);
            }
        }
        else
        {
            // 如果库存数据都没有，则显示错误或默认值
             if (m_TotalCoinsText != null) m_TotalCoinsText.text = "总金币: N/A";
             if (m_EarnedCoinsText != null) m_EarnedCoinsText.text = "本局获得: N/A";
        }
    }
    #endregion
} 