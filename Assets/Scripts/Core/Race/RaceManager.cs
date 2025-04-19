using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 比赛管理器 - 控制比赛流程与状态
/// </summary>
public class RaceManager : MonoBehaviour
{
    #region 单例实现
    private static RaceManager s_Instance;
    public static RaceManager Instance 
    { 
        get 
        {
            if (s_Instance == null)
            {
                GameObject managerObj = GameObject.Find("RaceManager");
                if (managerObj == null)
                {
                    managerObj = new GameObject("RaceManager");
                    s_Instance = managerObj.AddComponent<RaceManager>();
                }
                else
                {
                    s_Instance = managerObj.GetComponent<RaceManager>();
                    if (s_Instance == null)
                    {
                        s_Instance = managerObj.AddComponent<RaceManager>();
                    }
                }
            }
            return s_Instance;
        }
    }
    #endregion

    #region 事件定义
    // 比赛状态变更事件
    public event Action<RaceState, RaceState> OnRaceStateChanged;
    // 比赛开始倒计时事件
    public event Action<int> OnRaceCountdown;
    // 比赛完成事件
    public event Action<float, int> OnRaceCompleted;
    // 比赛奖励计算事件
    public event Action<RaceRewards> OnRewardsCalculated;
    // 掉落物品事件
    public event Action<List<string>> OnItemsDropped;
    #endregion

    #region 公共属性
    /// <summary>
    /// 当前比赛状态
    /// </summary>
    public RaceState CurrentRaceState { get; private set; } = RaceState.NotStarted;

    /// <summary>
    /// 当前比赛时间(秒)
    /// </summary>
    public float CurrentRaceTime { get; private set; } = 0f;

    /// <summary>
    /// 玩家当前名次
    /// </summary>
    public int CurrentPosition { get; private set; } = 1;

    /// <summary>
    /// 比赛是否进行中
    /// </summary>
    public bool IsRaceActive => CurrentRaceState == RaceState.Racing;
    #endregion

    #region 序列化字段
    [Header("比赛设置")]
    [Tooltip("倒计时秒数")]
    [SerializeField] private int m_CountdownTime = 3;
    
    [Tooltip("最大比赛时间(秒)")]
    [SerializeField] private float m_MaxRaceTime = 300f;
    
    [Tooltip("是否启用定时器")]
    [SerializeField] private bool m_UseTimer = true;
    
    [Header("奖励设置")]
    [Tooltip("基础金币奖励")]
    [SerializeField] private int m_BaseReward = 100;
    
    [Tooltip("完成时间系数")]
    [SerializeField] private float m_TimeRewardFactor = 0.5f;
    
    [Tooltip("名次奖励")]
    [SerializeField] private int[] m_PositionRewards = { 100, 50, 25, 10, 5 };
    
    [Header("掉落设置")]
    [Tooltip("最小掉落物品数量")]
    [SerializeField] private int m_MinDropItems = 1;
    
    [Tooltip("最大掉落物品数量")]
    [SerializeField] private int m_MaxDropItems = 3;
    
    [Tooltip("稀有物品掉落几率(0-1)")]
    [SerializeField] private float m_RareDropChance = 0.3f;
    #endregion

    #region 私有变量
    private bool m_IsTimerActive = false;
    private Coroutine m_RaceTimerCoroutine;
    private Coroutine m_CountdownCoroutine;
    
    // 记录玩家完成比赛时间
    private float m_FinishTime = 0f;
    #endregion

    #region Unity生命周期
    private void Awake()
    {
        // 单例实现检查
        if (s_Instance != null && s_Instance != this)
        {
            Destroy(this);
            return;
        }

        s_Instance = this;
    }

    private void Start()
    {
        // 注册游戏状态改变事件
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
        }
    }

    private void OnDestroy()
    {
        // 取消注册事件
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
        }
        
        // 停止所有协程
        StopAllCoroutines();
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 开始比赛准备阶段
    /// </summary>
    public void PrepareRace()
    {
        // 只有在未开始状态下才能准备比赛
        if (CurrentRaceState != RaceState.NotStarted && CurrentRaceState != RaceState.Finished)
        {
            Debug.LogWarning("比赛无法准备，当前状态：" + CurrentRaceState);
            return;
        }
        
        ChangeRaceState(RaceState.Preparing);
        
        // 重置比赛数据
        ResetRaceData();
        
        Debug.Log("比赛准备中...");
    }

    /// <summary>
    /// 开始比赛倒计时
    /// </summary>
    public void StartCountdown()
    {
        // 只有在准备状态才能开始倒计时
        if (CurrentRaceState != RaceState.Preparing)
        {
            Debug.LogWarning("无法开始倒计时，当前状态：" + CurrentRaceState);
            return;
        }
        
        ChangeRaceState(RaceState.Countdown);
        
        // 开始倒计时
        if (m_CountdownCoroutine != null)
        {
            StopCoroutine(m_CountdownCoroutine);
        }
        m_CountdownCoroutine = StartCoroutine(CountdownCoroutine());
        
        Debug.Log("比赛倒计时开始...");
    }

    /// <summary>
    /// 立即开始比赛(跳过倒计时)
    /// </summary>
    public void StartRaceImmediately()
    {
        // 停止倒计时协程（如果存在）
        if (m_CountdownCoroutine != null)
        {
            StopCoroutine(m_CountdownCoroutine);
            m_CountdownCoroutine = null;
        }
        
        StartRace();
    }

    /// <summary>
    /// 更新玩家位置
    /// </summary>
    public void UpdatePlayerPosition(int position)
    {
        if (position < 1) position = 1;
        CurrentPosition = position;
    }

    /// <summary>
    /// 玩家完成比赛
    /// </summary>
    public void PlayerFinishedRace()
    {
        // 只有在比赛中才能完成比赛
        if (CurrentRaceState != RaceState.Racing)
        {
            Debug.LogWarning("玩家无法完成比赛，当前状态：" + CurrentRaceState);
            return;
        }
        
        m_FinishTime = CurrentRaceTime;
        FinishRace();
    }

    /// <summary>
    /// 中止比赛(放弃)
    /// </summary>
    public void AbortRace()
    {
        if (CurrentRaceState == RaceState.NotStarted || CurrentRaceState == RaceState.Finished)
        {
            return;
        }
        
        // 停止计时器
        StopRaceTimer();
        
        ChangeRaceState(RaceState.Aborted);
        Debug.Log("比赛已中止");
    }

    /// <summary>
    /// 手动生成掉落物品
    /// </summary>
    public void GenerateDrops()
    {
        if (LootSystem.Instance != null)
        {
            List<string> droppedItems = LootSystem.Instance.GenerateLoot(
                m_MinDropItems,
                m_MaxDropItems,
                m_RareDropChance
            );
            
            if (droppedItems.Count > 0)
            {
                // 触发掉落事件
                OnItemsDropped?.Invoke(droppedItems);
                
                Debug.Log($"生成了 {droppedItems.Count} 个掉落物品");
            }
        }
    }
    #endregion

    #region 私有方法
    /// <summary>
    /// 游戏状态变更回调
    /// </summary>
    private void OnGameStateChanged(GameState oldState, GameState newState)
    {
        // 如果游戏切换到暂停状态
        if (newState == GameState.Paused)
        {
            // 暂停计时
            PauseRaceTimer();
        }
        // 如果游戏从暂停状态恢复
        else if (oldState == GameState.Paused && newState == GameState.Gameplay && IsRaceActive)
        {
            // 恢复计时
            ResumeRaceTimer();
        }
    }

    /// <summary>
    /// 改变比赛状态
    /// </summary>
    private void ChangeRaceState(RaceState newState)
    {
        if (CurrentRaceState == newState) return;
        
        RaceState oldState = CurrentRaceState;
        CurrentRaceState = newState;
        
        // 触发状态变更事件
        OnRaceStateChanged?.Invoke(oldState, newState);
    }

    /// <summary>
    /// 重置比赛数据
    /// </summary>
    private void ResetRaceData()
    {
        CurrentRaceTime = 0f;
        m_FinishTime = 0f;
        CurrentPosition = 1;
        m_IsTimerActive = false;
    }

    /// <summary>
    /// 开始比赛
    /// </summary>
    private void StartRace()
    {
        ChangeRaceState(RaceState.Racing);
        
        // 启动计时器
        if (m_UseTimer)
        {
            StartRaceTimer();
        }
        
        Debug.Log("比赛开始！");
    }

    /// <summary>
    /// 结束比赛
    /// </summary>
    private void FinishRace()
    {
        // 停止比赛计时器
        StopRaceTimer();
        
        ChangeRaceState(RaceState.Finished);
        
        // 触发比赛完成事件
        OnRaceCompleted?.Invoke(m_FinishTime, CurrentPosition);
        
        // 计算比赛奖励
        RaceRewards rewards = CalculateRewards();
        
        // 应用奖励
        ApplyRewards(rewards);
        
        // 生成掉落物品
        GenerateDrops();
        
        Debug.Log($"比赛结束！用时：{m_FinishTime:F2}秒，名次：{CurrentPosition}");
    }

    /// <summary>
    /// 计算比赛奖励
    /// </summary>
    private RaceRewards CalculateRewards()
    {
        RaceRewards rewards = new RaceRewards();
        
        // 基础奖励
        rewards.BaseReward = m_BaseReward;
        
        // 时间奖励 - 比赛时间越短，奖励越高
        if (m_FinishTime > 0 && m_MaxRaceTime > 0)
        {
            float timeRatio = Mathf.Clamp01(1f - (m_FinishTime / m_MaxRaceTime));
            rewards.TimeReward = Mathf.RoundToInt(m_BaseReward * timeRatio * m_TimeRewardFactor);
        }
        
        // 名次奖励
        int positionIndex = Mathf.Clamp(CurrentPosition - 1, 0, m_PositionRewards.Length - 1);
        rewards.PositionReward = m_PositionRewards[positionIndex];
        
        // 总奖励
        rewards.TotalReward = rewards.BaseReward + rewards.TimeReward + rewards.PositionReward;
        
        // 触发奖励计算事件
        OnRewardsCalculated?.Invoke(rewards);
        
        return rewards;
    }

    /// <summary>
    /// 应用奖励
    /// </summary>
    private void ApplyRewards(RaceRewards rewards)
    {
        // 调用经济系统添加金币
        if (EconomySystem.Instance != null)
        {
            EconomySystem.Instance.AddMoney(rewards.TotalReward);
            Debug.Log($"获得奖励：{rewards.TotalReward} 金币");
        }
    }

    /// <summary>
    /// 开始比赛计时器
    /// </summary>
    private void StartRaceTimer()
    {
        if (m_RaceTimerCoroutine != null)
        {
            StopCoroutine(m_RaceTimerCoroutine);
        }
        
        m_IsTimerActive = true;
        m_RaceTimerCoroutine = StartCoroutine(RaceTimerCoroutine());
    }

    /// <summary>
    /// 暂停比赛计时器
    /// </summary>
    private void PauseRaceTimer()
    {
        m_IsTimerActive = false;
    }

    /// <summary>
    /// 恢复比赛计时器
    /// </summary>
    private void ResumeRaceTimer()
    {
        m_IsTimerActive = true;
    }

    /// <summary>
    /// 停止比赛计时器
    /// </summary>
    private void StopRaceTimer()
    {
        m_IsTimerActive = false;
        
        if (m_RaceTimerCoroutine != null)
        {
            StopCoroutine(m_RaceTimerCoroutine);
            m_RaceTimerCoroutine = null;
        }
    }

    /// <summary>
    /// 比赛倒计时协程
    /// </summary>
    private IEnumerator CountdownCoroutine()
    {
        for (int i = m_CountdownTime; i > 0; i--)
        {
            // 触发倒计时事件
            OnRaceCountdown?.Invoke(i);
            Debug.Log($"倒计时：{i}");
            
            yield return new WaitForSeconds(1f);
        }
        
        // 倒计时结束，开始比赛
        StartRace();
        
        m_CountdownCoroutine = null;
    }

    /// <summary>
    /// 比赛计时器协程
    /// </summary>
    private IEnumerator RaceTimerCoroutine()
    {
        while (m_IsTimerActive && CurrentRaceTime < m_MaxRaceTime)
        {
            yield return null;
            
            if (m_IsTimerActive && !GameManager.Instance.IsPaused)
            {
                CurrentRaceTime += Time.deltaTime;
            }
        }
        
        // 如果达到最大时间且比赛仍在进行，自动结束比赛
        if (CurrentRaceTime >= m_MaxRaceTime && CurrentRaceState == RaceState.Racing)
        {
            Debug.Log("比赛时间已到");
            PlayerFinishedRace();
        }
        
        m_RaceTimerCoroutine = null;
    }
    #endregion
}

/// <summary>
/// 比赛状态枚举
/// </summary>
public enum RaceState
{
    NotStarted,  // 未开始
    Preparing,   // 准备中
    Countdown,   // 倒计时
    Racing,      // 比赛中
    Finished,    // 已完成
    Aborted      // 已中止
}

/// <summary>
/// 比赛奖励数据结构
/// </summary>
[System.Serializable]
public class RaceRewards
{
    public int BaseReward;     // 基础奖励
    public int TimeReward;     // 时间奖励
    public int PositionReward; // 名次奖励
    public int TotalReward;    // 总奖励
} 