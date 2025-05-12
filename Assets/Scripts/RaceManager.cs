using UnityEngine;
using System.Collections.Generic;
using System.Linq; // 用于 OrderBy 和 Where

/// <summary>
/// 管理比赛的整体状态、检查点和规则。
/// </summary>
public class RaceManager : MonoBehaviour
{
    #region 公开字段
    [Header("比赛配置")]
    [Tooltip("完成比赛所需的总圈数。")]
    public int m_TotalLapsToComplete = 3;

    [Header("检查点设置")]
    [Tooltip("包含所有检查点GameObject的父Transform。如果为空，则会通过标签查找。")]
    public Transform m_CheckpointsParent;
    [Tooltip("用于查找检查点GameObject的标签（如果m_CheckpointsParent为空）。")]
    public string m_CheckpointTag = "Checkpoint";

    [Header("UI 管理器引用")]
    [SerializeField, Tooltip("对InGameUIManager的引用，用于显示胜利面板")]
    private InGameUIManager m_InGameUIManager;
    #endregion

    #region 私有字段
    private List<Checkpoint> m_OrderedCheckpoints = new List<Checkpoint>();
    private bool m_IsRaceFinished = false;
    #endregion

    #region 公共属性
    /// <summary>
    /// 赛道上唯一检查点的总数。
    /// </summary>
    public int TotalUniqueCheckpoints => m_OrderedCheckpoints.Count;
    /// <summary>
    /// 比赛需要完成的总圈数。
    /// </summary>
    public int TotalLapsToComplete => m_TotalLapsToComplete;
    #endregion

    #region Unity生命周期
    private void Awake()
    {
        InitializeCheckpoints();

        if (m_InGameUIManager == null)
        {
            m_InGameUIManager = FindObjectOfType<InGameUIManager>();
            if (m_InGameUIManager == null)
            {
                Debug.LogError("[RaceManager] 场景中未找到 InGameUIManager！无法显示胜利面板。", this);
            }
        }
        m_IsRaceFinished = false;
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 当有玩家完成比赛时由PlayerRaceState调用。
    /// </summary>
    /// <param name="winningPlayer">完成比赛的玩家状态。</param>
    public void NotifyRaceFinished(PlayerRaceState winningPlayer)
    {
        if (m_IsRaceFinished) return;
        m_IsRaceFinished = true;

        Debug.LogWarning($"🏁🏁🏁 RaceManager: 玩家 {winningPlayer.name} 已完成比赛! 🏁🏁🏁", winningPlayer.gameObject);

        CarController carController = winningPlayer.GetComponentInParent<CarController>();
        if (carController != null)
        {
            carController.SetInputDisabled(true);
            Debug.Log("[RaceManager] 已禁用获胜玩家车辆输入。", carController.gameObject);
        }
        else
        {
            Debug.LogWarning("[RaceManager] 未能找到获胜玩家的CarController来禁用输入。", winningPlayer.gameObject);
        }

        if (m_InGameUIManager != null)
        {
            m_InGameUIManager.ShowWinPanel();
            Debug.Log("[RaceManager] 已通知InGameUIManager显示胜利面板。", m_InGameUIManager);
        }
        else
        {
            Debug.LogError("[RaceManager] InGameUIManager 引用为空，无法显示胜利面板！", this);
        }
    }

    /// <summary>
    /// 根据ID获取检查点。
    /// </summary>
    /// <param name="_id">检查点ID。</param>
    /// <returns>找到的检查点，如果未找到则返回null。</returns>
    public Checkpoint GetCheckpointByID(int _id)
    {
        if (_id >= 0 && _id < m_OrderedCheckpoints.Count && m_OrderedCheckpoints[_id].m_CheckpointID == _id)
        {
            return m_OrderedCheckpoints[_id];
        }
        var checkpoint = m_OrderedCheckpoints.FirstOrDefault(cp => cp.m_CheckpointID == _id);
        if (checkpoint == null)
        {
            Debug.LogWarning($"未能找到ID为 {_id} 的检查点。", this);
        }
        return checkpoint;
    }

    /// <summary>
    /// 获取起点/终点线的Transform，用于初始重生或参考。
    /// </summary>
    /// <returns>起点线的Transform，如果未找到则返回null或RaceManager自身的Transform。</returns>
    public Transform GetStartingLineTransform()
    {
        if (m_OrderedCheckpoints.Count > 0 && m_OrderedCheckpoints[0].m_IsFinishLine && m_OrderedCheckpoints[0].m_CheckpointID == 0)
        {
            return m_OrderedCheckpoints[0].transform;
        }
        Debug.LogWarning("未能找到有效的起点线检查点 (ID 0 且 IsFinishLine 为 true)。请检查检查点配置。", this);
        return (m_OrderedCheckpoints.Count > 0) ? m_OrderedCheckpoints[0].transform : transform;
    }
    #endregion

    #region 私有方法
    /// <summary>
    /// 初始化检查点列表，对其进行排序和验证。
    /// </summary>
    private void InitializeCheckpoints()
    {
        m_OrderedCheckpoints.Clear();
        Checkpoint[] foundCheckpoints;

        if (m_CheckpointsParent != null)
        {
            foundCheckpoints = m_CheckpointsParent.GetComponentsInChildren<Checkpoint>(true);
        }
        else
        {
            GameObject[] checkpointGOs = GameObject.FindGameObjectsWithTag(m_CheckpointTag);
            foundCheckpoints = new Checkpoint[checkpointGOs.Length];
            for (int i = 0; i < checkpointGOs.Length; i++)
            {
                foundCheckpoints[i] = checkpointGOs[i].GetComponent<Checkpoint>();
            }
        }

        if (foundCheckpoints.Length == 0)
        {
            Debug.LogError("没有找到任何检查点！请确保检查点已正确标记或分配给 m_CheckpointsParent，并且挂载了 Checkpoint 脚本。", this);
            return;
        }

        m_OrderedCheckpoints = foundCheckpoints
            .Where(cp => cp != null)
            .OrderBy(cp => cp.m_CheckpointID)
            .ToList();

        if (m_OrderedCheckpoints.Count == 0) {
            Debug.LogError("过滤和排序后没有有效的检查点！", this);
            return;
        }

        bool hasFinishLineWithID0 = false;
        for (int i = 0; i < m_OrderedCheckpoints.Count; i++)
        {
            if (m_OrderedCheckpoints[i].m_CheckpointID != i)
            {
                Debug.LogWarning($"检查点 '{m_OrderedCheckpoints[i].name}' (ID: {m_OrderedCheckpoints[i].m_CheckpointID}) 在排序后其ID与期望的索引 {i} 不匹配。请检查检查点ID是否有重复或间断。", m_OrderedCheckpoints[i]);
            }
            if (m_OrderedCheckpoints[i].m_IsFinishLine)
            {
                if (m_OrderedCheckpoints[i].m_CheckpointID == 0)
                {
                    hasFinishLineWithID0 = true;
                }
                else
                {
                    Debug.LogWarning($"检查点 '{m_OrderedCheckpoints[i].name}' 被标记为终点线，但其ID ({m_OrderedCheckpoints[i].m_CheckpointID}) 不是0。终点线ID必须为0。", m_OrderedCheckpoints[i]);
                }
            }
        }

        if (!hasFinishLineWithID0 && m_OrderedCheckpoints.Count > 0)
        {
            Debug.LogError("没有找到ID为0且被标记为IsFinishLine的检查点。赛道必须有一个ID为0的起点/终点线。", this);
        }
        else if (m_OrderedCheckpoints.Count > 0)
        {
             Debug.Log($"RaceManager 初始化完成，找到 {m_OrderedCheckpoints.Count} 个检查点。起点/终点线: '{m_OrderedCheckpoints[0].name}'.", this);
        }
    }
    #endregion

    #if UNITY_EDITOR
    [ContextMenu("重新初始化并验证检查点 (编辑器)")]
    private void EditorForceReinitializeCheckpoints()
    {
        InitializeCheckpoints();
        if (m_OrderedCheckpoints.Count > 0) {
            UnityEditor.Selection.objects = m_OrderedCheckpoints.Select(cp => cp.gameObject).ToArray();
            Debug.Log($"已在编辑器中重新初始化并选中了 {m_OrderedCheckpoints.Count} 个检查点。", this);
        }
    }
    #endif
} 