using UnityEngine;
using UnityEngine.UI;


/// 小地图标记
/// 用于控制小地图上的标记点显示

public class MinimapMarker : MonoBehaviour
{
    public enum MarkerType
    {
        Player,         // 玩家
        OtherPlayer,    // 其他玩家
        Checkpoint,     // 检查点
        Start,          // 起点
        Finish,         // 终点
        Vehicle         // 其他车辆
    }

    [Header("标记设置")]
    [Tooltip("标记类型")]
    [SerializeField] private MarkerType markerType = MarkerType.Player;

    [Tooltip("标记图像")]
    [SerializeField] private Image markerImage;

    [Tooltip("标记尺寸")]
    [SerializeField] private Vector2 markerSize = new Vector2(20f, 20f);

    [Header("玩家标记颜色")]
    [SerializeField] private Color playerColor = Color.green;

    [Header("其他玩家标记颜色")]
    [SerializeField] private Color otherPlayerColor = Color.blue;

    [Header("检查点标记颜色")]
    [SerializeField] private Color checkpointColor = Color.yellow;

    [Header("起点标记颜色")]
    [SerializeField] private Color startColor = Color.green;

    [Header("终点标记颜色")]
    [SerializeField] private Color finishColor = Color.red;

    [Header("其他车辆标记颜色")]
    [SerializeField] private Color vehicleColor = Color.cyan;

    [Header("脉冲效果")]
    [Tooltip("是否启用脉冲效果")]
    [SerializeField] private bool enablePulse = false;

    [Tooltip("脉冲周期")]
    [SerializeField] private float pulsePeriod = 1.5f;

    [Tooltip("脉冲最小尺寸")]
    [SerializeField] private float pulseMinSize = 0.8f;

    [Tooltip("脉冲最大尺寸")]
    [SerializeField] private float pulseMaxSize = 1.2f;

    // 私有变量
    private RectTransform rectTransform;
    private float pulseTimer = 0f;

    private void Awake()
    {
        // 获取组件
        rectTransform = GetComponent<RectTransform>();

        if (markerImage == null)
        {
            markerImage = GetComponent<Image>();
        }

        // 设置标记尺寸
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = markerSize;
        }

        // 根据标记类型设置颜色
        SetMarkerColor();
    }

    private void Update()
    {
        // 更新脉冲效果
        if (enablePulse)
        {
            UpdatePulseEffect();
        }
    }


    /// 设置标记颜色

    private void SetMarkerColor()
    {
        if (markerImage == null) return;

        switch (markerType)
        {
            case MarkerType.Player:
                markerImage.color = playerColor;
                break;
            case MarkerType.OtherPlayer:
                markerImage.color = otherPlayerColor;
                break;
            case MarkerType.Checkpoint:
                markerImage.color = checkpointColor;
                break;
            case MarkerType.Start:
                markerImage.color = startColor;
                break;
            case MarkerType.Finish:
                markerImage.color = finishColor;
                break;
            case MarkerType.Vehicle:
                markerImage.color = vehicleColor;
                break;
        }
    }


    /// 更新脉冲效果

    private void UpdatePulseEffect()
    {
        if (rectTransform == null) return;

        // 更新计时器
        pulseTimer += Time.deltaTime;

        // 计算脉冲系数(0-1-0)
        float pulseFactor = Mathf.PingPong(pulseTimer / pulsePeriod * 2f, 1f);

        // 应用脉冲效果
        float pulseSize = Mathf.Lerp(pulseMinSize, pulseMaxSize, pulseFactor);
        rectTransform.localScale = new Vector3(pulseSize, pulseSize, 1f);
    }


    /// 设置标记类型

    public void SetMarkerType(MarkerType type)
    {
        markerType = type;
        SetMarkerColor();
    }


    /// 设置标记尺寸

    public void SetMarkerSize(Vector2 size)
    {
        markerSize = size;

        if (rectTransform != null)
        {
            rectTransform.sizeDelta = markerSize;
        }
    }


    /// 设置脉冲效果

    public void SetPulseEffect(bool enable)
    {
        enablePulse = enable;

        if (!enable && rectTransform != null)
        {
            rectTransform.localScale = Vector3.one;
        }
    }


    /// 高亮标记

    public void Highlight(bool highlight)
    {
        if (markerImage == null) return;

        if (highlight)
        {
            // 增加亮度
            markerImage.color = new Color(
                markerImage.color.r,
                markerImage.color.g,
                markerImage.color.b,
                1.0f
            );

            // 启用脉冲效果
            SetPulseEffect(true);
        }
        else
        {
            // 恢复正常颜色
            SetMarkerColor();

            // 关闭脉冲效果
            SetPulseEffect(false);
        }
    }
}