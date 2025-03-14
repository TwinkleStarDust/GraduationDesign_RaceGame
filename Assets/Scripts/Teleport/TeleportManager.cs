using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 传送管理器
/// 管理所有传送点和处理传送逻辑
/// </summary>
public class TeleportManager : MonoBehaviour
{
    [Header("传送系统设置")]
    [Tooltip("传送点按钮预制体")]
    [SerializeField] private GameObject teleportButtonPrefab;

    [Tooltip("传送点按钮容器")]
    [SerializeField] private Transform buttonContainer;

    [Tooltip("传送点UI面板")]
    [SerializeField] private GameObject teleportPanel;

    [Tooltip("传送点UI切换按钮")]
    [SerializeField] private Button toggleButton;

    [Tooltip("传送点UI切换按钮文本")]
    [SerializeField] private TextMeshProUGUI toggleButtonText;

    [Header("传送效果设置")]
    [Tooltip("传送时是否使用淡入淡出效果")]
    [SerializeField] private bool useFadeEffect = true;

    [Tooltip("淡入淡出效果面板")]
    [SerializeField] private CanvasGroup fadePanel;

    [Tooltip("淡入淡出持续时间")]
    [SerializeField] private float fadeDuration = 0.5f;

    // 场景中的所有传送点
    private List<TeleportPoint> teleportPoints = new List<TeleportPoint>();

    // 当前车辆控制器
    private VehicleController currentVehicle;

    // 是否正在传送
    private bool isTeleporting = false;

    // 传送面板是否显示
    private bool isPanelVisible = false;

    private void Awake()
    {
        // 初始化传送面板
        if (teleportPanel != null)
        {
            teleportPanel.SetActive(false);
        }

        // 初始化淡入淡出面板
        if (fadePanel != null)
        {
            fadePanel.alpha = 0f;
            fadePanel.gameObject.SetActive(false);
        }

        // 设置切换按钮事件
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(ToggleTeleportPanel);
        }
    }

    private void Start()
    {
        // 查找场景中的所有传送点
        FindAllTeleportPoints();

        // 查找当前车辆
        FindCurrentVehicle();

        // 创建传送点按钮
        CreateTeleportButtons();
    }

    /// <summary>
    /// 查找场景中的所有传送点
    /// </summary>
    private void FindAllTeleportPoints()
    {
        teleportPoints.Clear();
        TeleportPoint[] points = FindObjectsOfType<TeleportPoint>();

        foreach (TeleportPoint point in points)
        {
            teleportPoints.Add(point);
        }

        Debug.Log($"找到 {teleportPoints.Count} 个传送点");
    }

    /// <summary>
    /// 查找当前车辆
    /// </summary>
    private void FindCurrentVehicle()
    {
        currentVehicle = FindObjectOfType<VehicleController>();

        if (currentVehicle == null)
        {
            Debug.LogWarning("未找到车辆控制器，传送功能可能无法正常工作");
        }
    }

    /// <summary>
    /// 创建传送点按钮
    /// </summary>
    private void CreateTeleportButtons()
    {
        if (buttonContainer == null || teleportButtonPrefab == null) return;

        // 清除现有按钮
        foreach (Transform child in buttonContainer)
        {
            Destroy(child.gameObject);
        }

        // 为每个传送点创建按钮
        foreach (TeleportPoint point in teleportPoints)
        {
            GameObject buttonObj = Instantiate(teleportButtonPrefab, buttonContainer);
            TeleportButton button = buttonObj.GetComponent<TeleportButton>();

            if (button != null)
            {
                button.Initialize(point);

                // 添加点击事件
                Button uiButton = buttonObj.GetComponent<Button>();
                if (uiButton != null)
                {
                    uiButton.onClick.AddListener(() => TeleportToPoint(point));
                }
            }
        }
    }

    /// <summary>
    /// 切换传送面板显示状态
    /// </summary>
    public void ToggleTeleportPanel()
    {
        isPanelVisible = !isPanelVisible;

        if (teleportPanel != null)
        {
            teleportPanel.SetActive(isPanelVisible);
        }

        // 更新按钮文本
        if (toggleButtonText != null)
        {
            toggleButtonText.text = isPanelVisible ? "关闭传送" : "打开传送";
        }
    }

    /// <summary>
    /// 传送到指定传送点
    /// </summary>
    public void TeleportToPoint(TeleportPoint point)
    {
        if (isTeleporting || currentVehicle == null || point == null) return;

        Debug.Log($"传送到: {point.PointName}");

        if (useFadeEffect && fadePanel != null)
        {
            // 使用淡入淡出效果
            StartCoroutine(TeleportWithFadeEffect(point));
        }
        else
        {
            // 直接传送
            PerformTeleport(point);
        }

        // 关闭传送面板
        if (teleportPanel != null)
        {
            isPanelVisible = false;
            teleportPanel.SetActive(false);

            if (toggleButtonText != null)
            {
                toggleButtonText.text = "打开传送";
            }
        }
    }

    /// <summary>
    /// 使用淡入淡出效果传送
    /// </summary>
    private System.Collections.IEnumerator TeleportWithFadeEffect(TeleportPoint point)
    {
        isTeleporting = true;

        // 激活淡入淡出面板
        fadePanel.gameObject.SetActive(true);

        // 淡入
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            fadePanel.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        fadePanel.alpha = 1f;

        // 执行传送
        PerformTeleport(point);

        // 等待一帧，确保传送完成
        yield return null;

        // 淡出
        elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            fadePanel.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        fadePanel.alpha = 0f;

        // 关闭淡入淡出面板
        fadePanel.gameObject.SetActive(false);

        isTeleporting = false;
    }

    /// <summary>
    /// 执行传送
    /// </summary>
    private void PerformTeleport(TeleportPoint point)
    {
        if (currentVehicle == null || point == null) return;

        // 准备传送
        currentVehicle.PrepareForTeleport();

        // 设置车辆位置和旋转
        currentVehicle.transform.position = point.transform.position + Vector3.up * 0.5f; // 稍微抬高，防止陷入地面
        currentVehicle.transform.rotation = point.transform.rotation;

        // 完成传送
        currentVehicle.FinishTeleport();
    }

    /// <summary>
    /// 刷新传送点列表
    /// </summary>
    public void RefreshTeleportPoints()
    {
        FindAllTeleportPoints();
        CreateTeleportButtons();
    }
}