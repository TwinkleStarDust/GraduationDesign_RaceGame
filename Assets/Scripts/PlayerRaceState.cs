using UnityEngine;
using System.Collections.Generic;
using System.Linq; // 用于 LapTimes 的 Sum 等操作

/// <summary>
/// 追踪单个玩家的比赛进度、时间和状态。
/// </summary>
public class PlayerRaceState : MonoBehaviour
{
    #region 私有字段
    [Header("比赛进度")]
    [SerializeField, Tooltip("玩家当前所处的圈数。")]
    private int m_CurrentLap = 0;
    [SerializeField, Tooltip("玩家上一个正确通过的检查点ID。")]
    private int m_LastCorrectlyPassedCheckpointID = -1;
    private Transform m_LastCheckpointRespawnTransform;

    [Header("时间记录")]
    [SerializeField, Tooltip("当前圈开始的时间。")]
    private float m_CurrentLapStartTime;
    private List<float> m_LapTimes = new List<float>();

    [Header("配置")]
    [SerializeField, Tooltip("对RaceManager的引用，以获取赛道信息。")]
    private RaceManager m_RaceManager;
    [SerializeField, Tooltip("用于重生到上一个检查点的按键。")]
    private KeyCode m_RespawnKey = KeyCode.B;

    private Rigidbody m_PlayerRigidbody;
    #endregion

    #region 公共属性
    public int CurrentLap => m_CurrentLap;
    public int LastCorrectlyPassedCheckpointID => m_LastCorrectlyPassedCheckpointID;
    public IReadOnlyList<float> LapTimes => m_LapTimes.AsReadOnly();
    public Transform LastCheckpointRespawnLocation => m_LastCheckpointRespawnTransform;
    public float CurrentLapTime => (m_CurrentLap > 0 && m_RaceManager != null && m_CurrentLap <= m_RaceManager.TotalLapsToComplete) ? (Time.time - m_CurrentLapStartTime) : 0f;
    public float TotalRaceTime => m_LapTimes.Sum();
    #endregion

    #region Unity生命周期
    private void Awake()
    {
        if (m_RaceManager == null)
        {
            m_RaceManager = FindObjectOfType<RaceManager>();
            if (m_RaceManager == null)
            {
                Debug.LogError("[PlayerRaceState] 场景中未找到 RaceManager！", this);
                enabled = false; 
                return;
            }
        }
        m_PlayerRigidbody = GetComponentInParent<Rigidbody>();
        if (m_PlayerRigidbody == null)
        {
            Debug.LogWarning("[PlayerRaceState] 未能找到玩家的 Rigidbody 组件。", this);
        }
    }

    private void Start()
    {
        ResetRaceState();
    }

    private void Update()
    {
        HandleRespawnInput();
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 当玩家车辆触发一个检查点时调用。
    /// </summary>
    /// <param name="_checkpoint">被触发的检查点。</param>
    public void OnCheckpointReached(Checkpoint _checkpoint)
    {
        Debug.Log($"[PlayerRaceState ENTRY] OnCheckpointReached called for Checkpoint ID: {_checkpoint.m_CheckpointID}. Current Lap: {m_CurrentLap}. LastCorrectID: {m_LastCorrectlyPassedCheckpointID}", this.gameObject);

        if (m_RaceManager == null) 
        {
            Debug.LogError("[PlayerRaceState CRITICAL] m_RaceManager is NULL at OnCheckpointReached entry!", this.gameObject);
            return;
        }
        if (m_RaceManager.TotalUniqueCheckpoints == 0) 
        {
            Debug.LogWarning("[PlayerRaceState CRITICAL] m_RaceManager.TotalUniqueCheckpoints is 0 at OnCheckpointReached entry!", this);
            return;
        }

        int expectedCheckpointID;
        if (m_LastCorrectlyPassedCheckpointID == -1) 
        {
            expectedCheckpointID = 0;
        }
        else
        {
            expectedCheckpointID = (m_LastCorrectlyPassedCheckpointID + 1) % m_RaceManager.TotalUniqueCheckpoints;
        }

        // 当前检查点是否为预期的下一个检查点
        bool isCorrectCheckpoint = (_checkpoint.m_CheckpointID == expectedCheckpointID);
        
        if (isCorrectCheckpoint)
        {
            // 先获取上一个检查点ID，用于逻辑判断
            int previousCheckpointID = m_LastCorrectlyPassedCheckpointID;
            
            // 更新上一个正确通过的检查点ID
            m_LastCorrectlyPassedCheckpointID = _checkpoint.m_CheckpointID;
            m_LastCheckpointRespawnTransform = _checkpoint.m_RespawnTransformOverride != null ? _checkpoint.m_RespawnTransformOverride : _checkpoint.transform; 

            float currentTime = Time.time;
            
            // 计算总检查点数和最后一个检查点ID
            int totalCheckpoints = m_RaceManager.TotalUniqueCheckpoints;
            int lastCheckpointID = totalCheckpoints - 1;
            
            Debug.Log($"[PlayerRaceState DEBUG] 赛道信息: 总检查点数={totalCheckpoints}, 最后检查点ID={lastCheckpointID}");
            Debug.Log($"[PlayerRaceState DEBUG] 当前状态: 当前圈数={m_CurrentLap}, 当前检查点ID={_checkpoint.m_CheckpointID}, 上一个检查点ID={previousCheckpointID}");
            
            // 通过非终点线检查点的正常情况
            if (!_checkpoint.m_IsFinishLine)
            {
                Debug.Log($"[PlayerRaceState] 正确通过检查点: {_checkpoint.m_CheckpointID} (期望: {expectedCheckpointID}). 上一个: {previousCheckpointID}. 圈: {m_CurrentLap}. Time: {currentTime - m_CurrentLapStartTime:F2}s.", this.gameObject);
                return;
            }
            
            // 处理终点线逻辑
            Debug.Log($"[PlayerRaceState FINISH] 通过终点线! 当前圈={m_CurrentLap}, 上一个检查点={previousCheckpointID}, 最后检查点={lastCheckpointID}", this.gameObject);
            
            if (m_CurrentLap == 0)
            {
                // 第一次通过终点线，开始计时第一圈
                m_CurrentLap = 1;
                m_CurrentLapStartTime = currentTime;
                Debug.Log($"[PlayerRaceState] 比赛开始！第 {m_CurrentLap} 圈。", this);
            }
            else if (previousCheckpointID == lastCheckpointID)
            {
                // 已经通过了最后一个检查点，现在通过了终点线，完成了一圈
                float lapTime = currentTime - m_CurrentLapStartTime;
                m_LapTimes.Add(lapTime);
                
                Debug.Log($"[PlayerRaceState] ✓✓✓ 完成第 {m_CurrentLap} 圈! 圈时: {lapTime:F2}s", this.gameObject);
                
                if (m_LapTimes.Count >= m_RaceManager.TotalLapsToComplete)
                {
                    // 完成所有圈数
                    Debug.Log($"[PlayerRaceState] 🏁 比赛结束！成功完成所有 {m_RaceManager.TotalLapsToComplete} 圈。总用时: {TotalRaceTime:F2}s。", this.gameObject);
                    Debug.LogWarning("🏁🏁🏁 玩家已到达终点！ 🏁🏁🏁", this.gameObject);
                    enabled = false;
                }
                else
                {
                    // 进入下一圈
                    m_CurrentLap++;
                    m_CurrentLapStartTime = currentTime;
                    Debug.Log($"[PlayerRaceState] >>> 开始第 {m_CurrentLap} 圈！ <<<", this.gameObject);
                }
            }
            else
            {
                // 通过了终点线，但不是在通过最后一个检查点之后
                Debug.LogWarning($"[PlayerRaceState] ⚠ 通过终点线但顺序不正确。当前圈数: {m_CurrentLap}, 上一个检查点ID: {previousCheckpointID}, 应该先通过检查点ID: {lastCheckpointID}", this.gameObject);
            }
        }
        else
        {
            Debug.LogWarning($"[PlayerRaceState] 顺序错误! 尝试: {_checkpoint.m_CheckpointID}，期望: {expectedCheckpointID}. 圈: {m_CurrentLap}.", this.gameObject);
        }
    }
    #endregion

    #region 私有方法
    private void HandleRespawnInput()
    {
        if (Input.GetKeyDown(m_RespawnKey))
        {
            RespawnPlayer();
        }
    }

    private void RespawnPlayer()
    {
        if (m_LastCheckpointRespawnTransform != null)
        {
            transform.position = m_LastCheckpointRespawnTransform.position;
            transform.rotation = m_LastCheckpointRespawnTransform.rotation;

            if (m_PlayerRigidbody != null)
            {
                m_PlayerRigidbody.linearVelocity = Vector3.zero;
                m_PlayerRigidbody.angularVelocity = Vector3.zero;
            }
            Debug.Log($"[PlayerRaceState] 玩家已重生到检查点 {m_LastCorrectlyPassedCheckpointID} 的位置。", this);
        }
        else
        {
            Debug.LogWarning("[PlayerRaceState] 没有可用的重生点。", this);
        }
    }

    /// <summary>
    /// 重置玩家的比赛状态，用于比赛开始或重赛。
    /// </summary>
    public void ResetRaceState()
    {
        m_CurrentLap = 0; 
        m_LastCorrectlyPassedCheckpointID = -1; 
        m_LapTimes.Clear();
        m_CurrentLapStartTime = Time.time; 
        
        if (m_RaceManager != null && m_RaceManager.GetStartingLineTransform() != null)
        {
            Transform initialCheckpointObjectTransform = m_RaceManager.GetStartingLineTransform();
            Checkpoint startingLineCheckpoint = initialCheckpointObjectTransform.GetComponent<Checkpoint>();

            if (startingLineCheckpoint != null && startingLineCheckpoint.m_RespawnTransformOverride != null)
            {
                m_LastCheckpointRespawnTransform = startingLineCheckpoint.m_RespawnTransformOverride;
            }
            else
            {
                m_LastCheckpointRespawnTransform = initialCheckpointObjectTransform; // 备选方案，使用检查点对象自身的Transform
            }
        } else {
             Debug.LogWarning("[PlayerRaceState] RaceManager 未设置或无法获取初始重生点。", this);
        }
        Debug.Log("[PlayerRaceState] 玩家比赛状态已重置。", this);
        enabled = true; 
    }
    #endregion

    #if UNITY_EDITOR
    [ContextMenu("调试玩家状态")]
    private void DebugPlayerState()
    {
        string lapTimesStr = string.Join(", ", m_LapTimes.Select(t => t.ToString("F2")));
        Debug.Log($"[PlayerRaceState DEBUG] CurLap: {m_CurrentLap}, LastCP: {m_LastCorrectlyPassedCheckpointID}, LapTimes: [{lapTimesStr}], CurLapTime: {CurrentLapTime:F2}s", this);
        if(m_LastCheckpointRespawnTransform != null) Debug.Log($"[PlayerRaceState DEBUG] Respawn: {m_LastCheckpointRespawnTransform.name} at {m_LastCheckpointRespawnTransform.position}", this);
    }
    #endif
} 