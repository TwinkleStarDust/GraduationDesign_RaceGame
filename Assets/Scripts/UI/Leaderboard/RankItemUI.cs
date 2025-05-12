using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class RankItemUI : MonoBehaviour
{
    #region UI元素引用
    [Header("UI元素")]
    [Tooltip("显示名次的TextMeshProUGUI组件")]
    [SerializeField] private TextMeshProUGUI m_RankText;
    [Tooltip("显示玩家名称的TextMeshProUGUI组件")]
    [SerializeField] private TextMeshProUGUI m_PlayerNameText;
    [Tooltip("显示总用时的TextMeshProUGUI组件")]
    [SerializeField] private TextMeshProUGUI m_TotalTimeText;

    [Header("布局设置")]
    [Tooltip("排行榜项目的高度")]
    [SerializeField] private float m_ItemHeight = 60f;
    #endregion

    #region Unity生命周期
    private void Start()
    {
        // 确保LayoutElement存在并设置首选高度
        LayoutElement layoutElement = GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = gameObject.AddComponent<LayoutElement>();
        }
        layoutElement.preferredHeight = m_ItemHeight;
        // 宽度将完全由父级的布局系统（如VerticalLayoutGroup的childForceExpandWidth）
        // 和自身的LayoutElement（如果设置了preferredWidth或flexibleWidth）控制。
        // 此处不进行任何宽度设置。

        // 确保有背景Image组件（如果需要默认背景）
        Image backgroundImage = GetComponent<Image>();
        if (backgroundImage == null)
        {
            // 可以选择是否在代码中添加默认Image，或者要求用户必须在Prefab中设置好
            // Debug.LogWarning("RankItemUI: 背景Image组件未找到，如果需要背景请在Prefab中添加。", this.gameObject);
            // backgroundImage = gameObject.AddComponent<Image>();
            // backgroundImage.color = new Color(0.2f, 0.2f, 0.2f, 0.7f); // 示例默认颜色
        }
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 设置排行榜条目的显示内容。
    /// </summary>
    /// <param name="_rank">名次。</param>
    /// <param name="_playerName">玩家名称。</param>
    /// <param name="_totalTime">总用时（秒）。</param>
    public void Setup(int _rank, string _playerName, float _totalTime)
    {
        if (m_RankText != null)
        {
            m_RankText.text = _rank.ToString();
        }
        if (m_PlayerNameText != null)
        {
            m_PlayerNameText.text = _playerName;
        }
        if (m_TotalTimeText != null)
        {
            // 将秒格式化为 MM:SS.FF (分钟:秒钟.百分秒)
            int minutes = Mathf.FloorToInt(_totalTime / 60F);
            int seconds = Mathf.FloorToInt(_totalTime % 60F);
            int milliseconds = Mathf.FloorToInt((_totalTime * 100F) % 100F);
            m_TotalTimeText.text = string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, milliseconds);
        }
    }
    #endregion
} 