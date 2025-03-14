using UnityEngine;

/// <summary>
/// 传送输入处理器
/// 处理传送系统的键盘输入
/// </summary>
public class TeleportInputHandler : MonoBehaviour
{
    [Tooltip("传送管理器引用")]
    [SerializeField] private TeleportManager teleportManager;

    [Tooltip("打开/关闭传送面板的按键")]
    [SerializeField] private KeyCode togglePanelKey = KeyCode.T;

    [Tooltip("刷新传送点列表的按键")]
    [SerializeField] private KeyCode refreshKey = KeyCode.F5;

    private void Awake()
    {
        // 如果没有指定传送管理器，尝试查找
        if (teleportManager == null)
        {
            teleportManager = FindObjectOfType<TeleportManager>();
            if (teleportManager == null)
            {
                Debug.LogWarning("未找到传送管理器！");
            }
        }
    }

    private void Update()
    {
        // 处理打开/关闭传送面板的输入
        if (Input.GetKeyDown(togglePanelKey))
        {
            if (teleportManager != null)
            {
                teleportManager.ToggleTeleportPanel();
            }
        }

        // 处理刷新传送点列表的输入
        if (Input.GetKeyDown(refreshKey))
        {
            if (teleportManager != null)
            {
                teleportManager.RefreshTeleportPoints();
                Debug.Log("已刷新传送点列表");
            }
        }
    }
}