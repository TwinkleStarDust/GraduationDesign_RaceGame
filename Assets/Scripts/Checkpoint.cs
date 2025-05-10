using UnityEngine;

/// <summary>
/// 代表赛道上的一个检查点。
/// </summary>
public class Checkpoint : MonoBehaviour
{
    #region 公开字段
    [Tooltip("此检查点的唯一ID。ID 0 应为起点/终点线。")]
    public int m_CheckpointID = 0;

    [Tooltip("标记此检查点是否为起点/终点线。")]
    public bool m_IsFinishLine = false;
    #endregion

    #region Unity生命周期
    private void OnTriggerEnter(Collider _other)
    {
        // 玩家车辆标签已从 "Player" 修改为 "carBody"
        if (_other.CompareTag("carBody"))
        {
            PlayerRaceState playerState = _other.GetComponentInParent<PlayerRaceState>();
            if (playerState != null)
            {
                Debug.Log($"[Checkpoint {m_CheckpointID}] Triggered by {_other.name}. Handing off to PlayerRaceState.", this.gameObject);
                playerState.OnCheckpointReached(this);
            }
            else
            {
                Debug.LogWarning($"[Checkpoint {m_CheckpointID}] 名为 {_other.name} 的对象（标签为carBody）上没有找到 PlayerRaceState 组件。", _other.gameObject);
            }
        }
        // else
        // {
             // Keep this commented out unless actively debugging tag issues.
            // Debug.Log($"[Checkpoint {m_CheckpointID}] OnTriggerEnter triggered by: {_other.name} (Tag: {_other.tag}), but 'carBody' tag did NOT match.", this.gameObject);
        // }
    }
    #endregion

    #if UNITY_EDITOR
    [ContextMenu("调试检查点信息")]
    private void DebugInfo()
    {
        Debug.Log($"检查点 ID: {m_CheckpointID}, 是否为终点线: {m_IsFinishLine}, 位置: {transform.position}", this);
    }
    #endif
} 