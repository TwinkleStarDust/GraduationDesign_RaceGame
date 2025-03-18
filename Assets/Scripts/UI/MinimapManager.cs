using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Vehicle;

/// <summary>
/// 小地图管理器
/// 负责控制小地图的显示和功能
/// </summary>
public class MinimapManager : MonoBehaviour
{
    [Header("小地图设置")]
    [Tooltip("小地图摄像机")]
    [SerializeField] private Camera minimapCamera;

    [Tooltip("小地图Raw Image")]
    [SerializeField] private RawImage minimapImage;

    [Tooltip("小地图边框")]
    [SerializeField] private RectTransform minimapBorder;

    [Tooltip("小地图遮罩")]
    [SerializeField] private RectTransform minimapMask;

    [Header("小地图图标设置")]
    [Tooltip("玩家图标")]
    [SerializeField] private RectTransform playerMarker;

    [Tooltip("其他玩家图标预制体")]
    [SerializeField] private GameObject otherPlayerMarkerPrefab;

    [Tooltip("检查点图标预制体")]
    [SerializeField] private GameObject checkpointMarkerPrefab;

    [Header("小地图样式设置")]
    [Tooltip("小地图旋转模式")]
    [SerializeField] private MinimapRotationMode rotationMode = MinimapRotationMode.RotateWithPlayer;

    [Tooltip("小地图缩放级别")]
    [Range(5f, 200f)]
    [SerializeField] private float minimapZoom = 50f;

    [Tooltip("小地图最大缩放级别")]
    [SerializeField] private float maxZoom = 150f;

    [Tooltip("小地图最小缩放级别")]
    [SerializeField] private float minZoom = 20f;

    [Tooltip("小地图尺寸")]
    [SerializeField] private Vector2 minimapSize = new Vector2(200f, 200f);

    [Tooltip("小地图相机高度")]
    [SerializeField] private float cameraHeight = 150f;

    // 当前跟踪的车辆
    private Transform targetVehicle;

    // 其他玩家标记
    private Dictionary<Transform, RectTransform> otherPlayerMarkers = new Dictionary<Transform, RectTransform>();

    // 检查点标记
    private List<RectTransform> checkpointMarkers = new List<RectTransform>();

    // 渲染纹理
    private RenderTexture minimapRenderTexture;

    // 小地图旋转模式枚举
    public enum MinimapRotationMode
    {
        Fixed,              // 小地图固定不旋转
        RotateWithPlayer    // 小地图跟随玩家旋转
    }

    private void Awake()
    {
        // 初始化目标车辆
        FindTargetVehicle();

        // 初始化小地图
        InitializeMinimapCamera();

        // 初始化小地图UI
        InitializeMinimapUI();
    }

    private void Start()
    {
        // 获取检查点
        FindCheckpoints();

        // 立即更新一次相机设置
        UpdateCameraSettings();
    }

    private void Update()
    {
        if (targetVehicle == null)
        {
            FindTargetVehicle();
            return;
        }

        // 更新小地图相机位置
        UpdateMinimapCamera();

        // 更新小地图标记
        UpdateMinimapMarkers();
    }

    /// <summary>
    /// 初始化小地图相机
    /// </summary>
    private void InitializeMinimapCamera()
    {
        if (minimapCamera == null)
        {
            Debug.LogError("MinimapManager: 未指定小地图相机!");
            // 尝试在场景中查找小地图相机
            minimapCamera = GameObject.Find("MinimapCamera")?.GetComponent<Camera>();
            if (minimapCamera == null)
            {
                Debug.LogError("MinimapManager: 无法在场景中找到MinimapCamera!");
                return;
            }
        }

        // 创建渲染纹理
        minimapRenderTexture = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32);
        minimapRenderTexture.Create();

        // 设置相机属性
        minimapCamera.targetTexture = minimapRenderTexture;
        minimapCamera.orthographic = true;
        minimapCamera.orthographicSize = minimapZoom;
        minimapCamera.cullingMask = LayerMask.GetMask("Default", "Vehicle", "Checkpoint"); // 根据实际层设置

        // 设置相机位置高度
        Vector3 cameraPos = minimapCamera.transform.position;
        minimapCamera.transform.position = new Vector3(cameraPos.x, cameraHeight, cameraPos.z);

        // 正交相机俯视场景
        minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        Debug.Log($"MinimapManager: 相机设置完成，正交大小: {minimapCamera.orthographicSize}, 高度: {cameraHeight}");
    }

    /// <summary>
    /// 初始化小地图UI
    /// </summary>
    private void InitializeMinimapUI()
    {
        if (minimapImage == null)
        {
            Debug.LogError("MinimapManager: 未指定小地图图像!");
            return;
        }

        // 设置渲染纹理
        minimapImage.texture = minimapRenderTexture;

        // 设置小地图尺寸
        if (minimapBorder != null)
        {
            minimapBorder.sizeDelta = minimapSize;
        }

        if (minimapMask != null)
        {
            minimapMask.sizeDelta = minimapSize;
        }

        // 设置小地图图像尺寸
        minimapImage.rectTransform.sizeDelta = minimapSize;
    }

    /// <summary>
    /// 寻找目标车辆
    /// </summary>
    private void FindTargetVehicle()
    {
        // 查找玩家控制的车辆
        VehicleController vehicleController = FindObjectOfType<VehicleController>();
        if (vehicleController != null)
        {
            targetVehicle = vehicleController.transform;
        }
        else
        {
            Debug.LogWarning("MinimapManager: 未找到玩家车辆!");
        }
    }

    /// <summary>
    /// 寻找检查点
    /// </summary>
    private void FindCheckpoints()
    {
        // 查找场景中的所有检查点
        // 假设检查点使用了Checkpoint脚本或特定标签
        GameObject[] checkpoints = GameObject.FindGameObjectsWithTag("Checkpoint");

        foreach (GameObject checkpoint in checkpoints)
        {
            CreateCheckpointMarker(checkpoint.transform);
        }
    }

    /// <summary>
    /// 创建检查点标记
    /// </summary>
    private void CreateCheckpointMarker(Transform checkpoint)
    {
        if (checkpointMarkerPrefab == null) return;

        GameObject markerObj = Instantiate(checkpointMarkerPrefab, minimapImage.transform);
        RectTransform marker = markerObj.GetComponent<RectTransform>();

        if (marker != null)
        {
            checkpointMarkers.Add(marker);
        }
    }

    /// <summary>
    /// 添加其他玩家标记
    /// </summary>
    public void AddOtherPlayerMarker(Transform otherPlayer)
    {
        if (otherPlayerMarkerPrefab == null || otherPlayer == null) return;

        if (!otherPlayerMarkers.ContainsKey(otherPlayer))
        {
            GameObject markerObj = Instantiate(otherPlayerMarkerPrefab, minimapImage.transform);
            RectTransform marker = markerObj.GetComponent<RectTransform>();

            if (marker != null)
            {
                otherPlayerMarkers.Add(otherPlayer, marker);
            }
        }
    }

    /// <summary>
    /// 移除其他玩家标记
    /// </summary>
    public void RemoveOtherPlayerMarker(Transform otherPlayer)
    {
        if (otherPlayerMarkers.ContainsKey(otherPlayer))
        {
            Destroy(otherPlayerMarkers[otherPlayer].gameObject);
            otherPlayerMarkers.Remove(otherPlayer);
        }
    }

    /// <summary>
    /// 更新相机设置
    /// </summary>
    private void UpdateCameraSettings()
    {
        if (minimapCamera == null) return;

        // 更新相机正交大小
        minimapCamera.orthographicSize = minimapZoom;

        // 更新相机高度
        Vector3 cameraPos = minimapCamera.transform.position;
        if (targetVehicle != null)
        {
            cameraPos.x = targetVehicle.position.x;
            cameraPos.z = targetVehicle.position.z;
        }
        minimapCamera.transform.position = new Vector3(cameraPos.x, cameraHeight, cameraPos.z);

        Debug.Log($"MinimapManager: 更新相机设置，正交大小: {minimapCamera.orthographicSize}, 高度: {cameraHeight}");
    }

    /// <summary>
    /// 更新小地图相机
    /// </summary>
    private void UpdateMinimapCamera()
    {
        if (minimapCamera == null || targetVehicle == null) return;

        // 更新相机位置，跟随目标
        Vector3 newPos = targetVehicle.position;
        newPos.y = cameraHeight; // 使用设置的相机高度
        minimapCamera.transform.position = newPos;

        // 根据旋转模式设置相机旋转
        if (rotationMode == MinimapRotationMode.RotateWithPlayer)
        {
            float rotationY = targetVehicle.eulerAngles.y;
            minimapCamera.transform.rotation = Quaternion.Euler(90f, rotationY, 0f);

            // 保持玩家标记不旋转（始终朝上）
            if (playerMarker != null)
            {
                playerMarker.rotation = Quaternion.identity;
            }
        }
        else
        {
            minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            // 玩家标记根据车辆方向旋转
            if (playerMarker != null)
            {
                float rotationZ = -targetVehicle.eulerAngles.y;
                playerMarker.rotation = Quaternion.Euler(0f, 0f, rotationZ);
            }
        }
    }

    /// <summary>
    /// 更新小地图标记
    /// </summary>
    private void UpdateMinimapMarkers()
    {
        // 更新玩家标记位置（始终在中心）
        if (playerMarker != null)
        {
            playerMarker.anchoredPosition = Vector2.zero;
        }

        // 更新其他玩家标记
        foreach (var pair in otherPlayerMarkers)
        {
            Transform otherPlayer = pair.Key;
            RectTransform marker = pair.Value;

            if (otherPlayer == null)
            {
                // 移除无效标记
                Destroy(marker.gameObject);
                continue;
            }

            // 计算其他玩家相对于目标的位置
            Vector2 viewportPosition = WorldToMinimapPosition(otherPlayer.position);
            marker.anchoredPosition = viewportPosition;

            // 如果不是旋转模式，更新标记旋转
            if (rotationMode == MinimapRotationMode.Fixed)
            {
                float rotationZ = -otherPlayer.eulerAngles.y;
                marker.rotation = Quaternion.Euler(0f, 0f, rotationZ);
            }
        }

        // 更新检查点标记
        foreach (RectTransform marker in checkpointMarkers)
        {
            // 检查点暂不处理，等待赛道系统实现
        }
    }

    /// <summary>
    /// 世界坐标转换为小地图坐标
    /// </summary>
    private Vector2 WorldToMinimapPosition(Vector3 worldPosition)
    {
        if (minimapCamera == null || targetVehicle == null) return Vector2.zero;

        // 计算相对于相机的视口坐标
        Vector3 viewportPoint = minimapCamera.WorldToViewportPoint(worldPosition);

        // 将视口坐标转换为小地图RectTransform坐标
        float minimapHalfWidth = minimapSize.x * 0.5f;
        float minimapHalfHeight = minimapSize.y * 0.5f;

        // 视口坐标在[0,1]范围内，转换为UI坐标
        float x = (viewportPoint.x - 0.5f) * minimapSize.x;
        float y = (viewportPoint.z - 0.5f) * minimapSize.y;

        // 如果在小地图旋转模式下，需要考虑小地图的旋转
        if (rotationMode == MinimapRotationMode.RotateWithPlayer)
        {
            float angle = -targetVehicle.eulerAngles.y * Mathf.Deg2Rad;
            float cosAngle = Mathf.Cos(angle);
            float sinAngle = Mathf.Sin(angle);

            float rotatedX = x * cosAngle - y * sinAngle;
            float rotatedY = x * sinAngle + y * cosAngle;

            x = rotatedX;
            y = rotatedY;
        }

        // 限制坐标范围在小地图内
        x = Mathf.Clamp(x, -minimapHalfWidth, minimapHalfWidth);
        y = Mathf.Clamp(y, -minimapHalfHeight, minimapHalfHeight);

        return new Vector2(x, y);
    }

    /// <summary>
    /// 设置小地图缩放
    /// </summary>
    public void SetMinimapZoom(float zoom)
    {
        minimapZoom = Mathf.Clamp(zoom, minZoom, maxZoom);

        if (minimapCamera != null)
        {
            minimapCamera.orthographicSize = minimapZoom;
            Debug.Log($"MinimapManager: 设置小地图缩放为 {minimapZoom}");
        }
    }

    /// <summary>
    /// 放大小地图
    /// </summary>
    public void ZoomIn()
    {
        SetMinimapZoom(minimapZoom - 10f);
    }

    /// <summary>
    /// 缩小小地图
    /// </summary>
    public void ZoomOut()
    {
        SetMinimapZoom(minimapZoom + 10f);
    }

    /// <summary>
    /// 切换小地图旋转模式
    /// </summary>
    public void ToggleRotationMode()
    {
        if (rotationMode == MinimapRotationMode.Fixed)
            rotationMode = MinimapRotationMode.RotateWithPlayer;
        else
            rotationMode = MinimapRotationMode.Fixed;
    }

    /// <summary>
    /// 设置相机高度
    /// </summary>
    public void SetCameraHeight(float height)
    {
        cameraHeight = Mathf.Max(10f, height);
        UpdateCameraSettings();
    }

    /// <summary>
    /// 重置小地图设置
    /// </summary>
    public void ResetMinimapSettings()
    {
        minimapZoom = 50f;
        cameraHeight = 150f;
        UpdateCameraSettings();
    }
}