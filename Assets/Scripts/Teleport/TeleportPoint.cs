using UnityEngine;

/// <summary>
/// 传送点
/// 定义传送点的位置和属性
/// </summary>
public class TeleportPoint : MonoBehaviour
{
    [Tooltip("传送点名称")]
    [SerializeField] private string pointName = "传送点";

    [Tooltip("传送点描述")]
    [SerializeField] private string description = "";

    [Tooltip("传送点图标")]
    [SerializeField] private Sprite icon;

    [Tooltip("传送点颜色")]
    [SerializeField] private Color pointColor = Color.blue;

    [Tooltip("显示传送点可视化效果")]
    [SerializeField] private bool showVisualizer = true;

    [Tooltip("可视化效果大小")]
    [SerializeField] private float visualizerSize = 3f;

    /// <summary>
    /// 获取传送点名称
    /// </summary>
    public string PointName => pointName;

    /// <summary>
    /// 获取传送点描述
    /// </summary>
    public string Description => description;

    /// <summary>
    /// 获取传送点图标
    /// </summary>
    public Sprite Icon => icon;

    /// <summary>
    /// 获取传送点颜色
    /// </summary>
    public Color PointColor => pointColor;

    /// <summary>
    /// 在编辑器中绘制可视化效果
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!showVisualizer) return;

        // 设置Gizmos颜色
        Gizmos.color = pointColor;

        // 绘制传送点位置
        Gizmos.DrawSphere(transform.position, 0.5f);

        // 绘制方向指示器
        Gizmos.DrawRay(transform.position, transform.forward * 2f);

        // 绘制平台
        Vector3 platformCenter = transform.position + new Vector3(0, -0.25f, 0);
        Gizmos.DrawCube(platformCenter, new Vector3(visualizerSize, 0.1f, visualizerSize));

        // 绘制边界
        Gizmos.DrawWireCube(platformCenter, new Vector3(visualizerSize, 0.1f, visualizerSize));
    }

#if UNITY_EDITOR
    /// <summary>
    /// 在编辑器中重命名游戏对象以匹配传送点名称
    /// </summary>
    private void OnValidate()
    {
        if (!string.IsNullOrEmpty(pointName) && gameObject.name != pointName)
        {
            gameObject.name = "TeleportPoint_" + pointName;
        }
    }
#endif
}