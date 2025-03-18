using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 小地图控制界面
/// 处理小地图的缩放、旋转等交互
/// </summary>
public class MinimapControls : MonoBehaviour
{
    [Tooltip("小地图管理器")]
    [SerializeField] private MinimapManager minimapManager;

    [Header("控制按钮")]
    [Tooltip("缩放按钮")]
    [SerializeField] private Button zoomInButton;
    [SerializeField] private Button zoomOutButton;

    [Tooltip("旋转模式切换按钮")]
    [SerializeField] private Button rotationModeButton;

    [Tooltip("全屏小地图切换按钮")]
    [SerializeField] private Button toggleFullscreenButton;

    [Tooltip("重置小地图按钮")]
    [SerializeField] private Button resetMinimapButton;

    [Header("全屏小地图设置")]
    [Tooltip("全屏小地图面板")]
    [SerializeField] private RectTransform fullscreenMinimapPanel;

    [Tooltip("正常小地图面板")]
    [SerializeField] private RectTransform normalMinimapPanel;

    [Tooltip("全屏切换动画时间")]
    [SerializeField] private float transitionTime = 0.3f;

    [Header("高级设置")]
    [Tooltip("相机高度调整步长")]
    [SerializeField] private float cameraHeightStep = 10f;

    // 是否处于全屏模式
    private bool isFullscreen = false;

    // 动画过渡计时器
    private float transitionTimer = 0f;

    // 动画起始和目标大小
    private Vector2 startSize;
    private Vector2 targetSize;

    // 是否正在过渡动画
    private bool isTransitioning = false;

    private void Awake()
    {
        // 自动获取小地图管理器（如果未指定）
        if (minimapManager == null)
        {
            minimapManager = GetComponentInParent<MinimapManager>();
            if (minimapManager == null)
            {
                // 尝试在场景中查找小地图管理器
                minimapManager = FindObjectOfType<MinimapManager>();
                if (minimapManager == null)
                {
                    Debug.LogError("MinimapControls: 无法找到MinimapManager!");
                }
            }
        }

        // 初始化全屏模式
        if (fullscreenMinimapPanel != null)
        {
            fullscreenMinimapPanel.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        // 设置按钮事件
        SetupButtons();
    }

    private void Update()
    {
        // 处理全屏模式过渡动画
        if (isTransitioning)
        {
            UpdateTransition();
        }

        // 处理键盘输入
        HandleKeyboardInput();
    }

    /// <summary>
    /// 设置按钮点击事件
    /// </summary>
    private void SetupButtons()
    {
        // 设置缩放按钮
        if (zoomInButton != null)
        {
            zoomInButton.onClick.AddListener(ZoomIn);
        }

        if (zoomOutButton != null)
        {
            zoomOutButton.onClick.AddListener(ZoomOut);
        }

        // 设置旋转模式按钮
        if (rotationModeButton != null)
        {
            rotationModeButton.onClick.AddListener(ToggleRotationMode);
        }

        // 设置全屏切换按钮
        if (toggleFullscreenButton != null)
        {
            toggleFullscreenButton.onClick.AddListener(ToggleFullscreen);
        }

        // 设置重置按钮
        if (resetMinimapButton != null)
        {
            resetMinimapButton.onClick.AddListener(ResetMinimapSettings);
        }
    }

    /// <summary>
    /// 处理键盘输入
    /// </summary>
    private void HandleKeyboardInput()
    {
        // 缩放控制
        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.Plus))
        {
            ZoomIn();
        }
        else if (Input.GetKeyDown(KeyCode.Minus))
        {
            ZoomOut();
        }

        // 旋转模式切换
        if (Input.GetKeyDown(KeyCode.R))
        {
            ToggleRotationMode();
        }

        // 全屏模式切换
        if (Input.GetKeyDown(KeyCode.M))
        {
            ToggleFullscreen();
        }

        // 调整相机高度
        if (Input.GetKeyDown(KeyCode.PageUp))
        {
            IncreaseCameraHeight();
        }
        else if (Input.GetKeyDown(KeyCode.PageDown))
        {
            DecreaseCameraHeight();
        }

        // 重置小地图设置
        if (Input.GetKeyDown(KeyCode.Home))
        {
            ResetMinimapSettings();
        }
    }

    /// <summary>
    /// 放大小地图
    /// </summary>
    public void ZoomIn()
    {
        if (minimapManager != null)
        {
            minimapManager.ZoomIn();
        }
    }

    /// <summary>
    /// 缩小小地图
    /// </summary>
    public void ZoomOut()
    {
        if (minimapManager != null)
        {
            minimapManager.ZoomOut();
        }
    }

    /// <summary>
    /// 增加相机高度
    /// </summary>
    public void IncreaseCameraHeight()
    {
        if (minimapManager != null)
        {
            // 获取当前MinimapManager的私有字段cameraHeight的值
            System.Reflection.FieldInfo field = typeof(MinimapManager).GetField("cameraHeight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                float currentHeight = (float)field.GetValue(minimapManager);
                minimapManager.SetCameraHeight(currentHeight + cameraHeightStep);
                Debug.Log($"MinimapControls: 增加相机高度到 {currentHeight + cameraHeightStep}");
            }
        }
    }

    /// <summary>
    /// 减小相机高度
    /// </summary>
    public void DecreaseCameraHeight()
    {
        if (minimapManager != null)
        {
            // 获取当前MinimapManager的私有字段cameraHeight的值
            System.Reflection.FieldInfo field = typeof(MinimapManager).GetField("cameraHeight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                float currentHeight = (float)field.GetValue(minimapManager);
                minimapManager.SetCameraHeight(currentHeight - cameraHeightStep);
                Debug.Log($"MinimapControls: 减小相机高度到 {currentHeight - cameraHeightStep}");
            }
        }
    }

    /// <summary>
    /// 重置小地图设置
    /// </summary>
    public void ResetMinimapSettings()
    {
        if (minimapManager != null)
        {
            minimapManager.ResetMinimapSettings();
            Debug.Log("MinimapControls: 重置小地图设置");
        }
    }

    /// <summary>
    /// 切换旋转模式
    /// </summary>
    public void ToggleRotationMode()
    {
        if (minimapManager != null)
        {
            minimapManager.ToggleRotationMode();
        }
    }

    /// <summary>
    /// 切换全屏小地图模式
    /// </summary>
    public void ToggleFullscreen()
    {
        if (normalMinimapPanel == null || fullscreenMinimapPanel == null) return;

        isFullscreen = !isFullscreen;

        // 开始过渡动画
        StartTransition();
    }

    /// <summary>
    /// 开始过渡动画
    /// </summary>
    private void StartTransition()
    {
        if (normalMinimapPanel == null || fullscreenMinimapPanel == null) return;

        // 设置起始和目标大小
        if (isFullscreen)
        {
            // 切换到全屏
            startSize = normalMinimapPanel.sizeDelta;
            targetSize = new Vector2(Screen.width * 0.8f, Screen.height * 0.8f);
            fullscreenMinimapPanel.gameObject.SetActive(true);
            fullscreenMinimapPanel.sizeDelta = startSize;
        }
        else
        {
            // 切换回正常大小
            startSize = fullscreenMinimapPanel.sizeDelta;
            targetSize = normalMinimapPanel.sizeDelta;
        }

        // 初始化过渡计时器
        transitionTimer = 0f;
        isTransitioning = true;
    }

    /// <summary>
    /// 更新过渡动画
    /// </summary>
    private void UpdateTransition()
    {
        // 更新计时器
        transitionTimer += Time.deltaTime;

        // 计算过渡进度 (0-1)
        float progress = Mathf.Clamp01(transitionTimer / transitionTime);

        // 平滑过渡使用Smoothstep插值
        float smoothProgress = progress * progress * (3f - 2f * progress);

        // 更新大小
        Vector2 currentSize = Vector2.Lerp(startSize, targetSize, smoothProgress);

        if (isFullscreen)
        {
            fullscreenMinimapPanel.sizeDelta = currentSize;
        }
        else
        {
            fullscreenMinimapPanel.sizeDelta = currentSize;
        }

        // 过渡完成
        if (progress >= 1f)
        {
            isTransitioning = false;

            // 切换面板显示
            if (!isFullscreen)
            {
                fullscreenMinimapPanel.gameObject.SetActive(false);
            }
        }
    }
}