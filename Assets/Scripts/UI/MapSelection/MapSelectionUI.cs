using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro; // 确保你已经导入了TextMeshPro

public class MapSelectionUI : MonoBehaviour
{
    #region 私有字段
    [Header("地图数据列表")]
    [SerializeField] private List<MapData> m_AvailableMaps = new List<MapData>();

    [Header("UI元素引用")]
    [SerializeField] private Image m_MapPreviewImage; // 用于显示地图预览图
    [SerializeField] private TextMeshProUGUI m_MapNameText;    // 用于显示地图名称
    [SerializeField] private TextMeshProUGUI m_MapDescriptionText; // 用于显示地图描述
    [SerializeField] private Button m_NextMapButton;
    [SerializeField] private Button m_PreviousMapButton;
    [SerializeField] private Button m_SelectMapButton;
    [SerializeField] private Button m_BackButton;

    private int m_CurrentMapIndex = 0;
    #endregion

    #region Unity生命周期
    private void Start()
    {
        InitializeUI();
        AssignButtonListeners();
        UpdateMapDisplay();
    }
    #endregion

    #region 私有方法
    private void InitializeUI()
    {
        if (m_AvailableMaps.Count == 0)
        {
            Debug.LogError("没有可用的地图数据！请在Inspector中分配地图数据。");
            if (m_SelectMapButton != null) m_SelectMapButton.interactable = false;
            if (m_NextMapButton != null) m_NextMapButton.interactable = false;
            if (m_PreviousMapButton != null) m_PreviousMapButton.interactable = false;
            // 可以考虑禁用整个面板或显示提示信息
            return;
        }
    }

    private void AssignButtonListeners()
    {
        if (m_NextMapButton != null) m_NextMapButton.onClick.AddListener(ShowNextMap);
        if (m_PreviousMapButton != null) m_PreviousMapButton.onClick.AddListener(ShowPreviousMap);
        if (m_SelectMapButton != null) m_SelectMapButton.onClick.AddListener(OnSelectMapPressed);
        if (m_BackButton != null) m_BackButton.onClick.AddListener(OnBackButtonPressed);
    }

    private void UpdateMapDisplay()
    {
        if (m_AvailableMaps.Count == 0) return;

        MapData currentMap = m_AvailableMaps[m_CurrentMapIndex];

        if (m_MapPreviewImage != null && currentMap.m_MapPreviewImage != null)
        {
            m_MapPreviewImage.sprite = currentMap.m_MapPreviewImage;
            m_MapPreviewImage.gameObject.SetActive(true);
        }
        else if (m_MapPreviewImage != null)
        {
            m_MapPreviewImage.gameObject.SetActive(false); // 如果没有预览图则隐藏Image组件
        }

        if (m_MapNameText != null)
        {
            m_MapNameText.text = currentMap.m_MapName;
        }

        if (m_MapDescriptionText != null)
        {
            m_MapDescriptionText.text = currentMap.m_MapDescription;
        }

        // 更新按钮状态
        if (m_PreviousMapButton != null) m_PreviousMapButton.interactable = (m_CurrentMapIndex > 0);
        if (m_NextMapButton != null) m_NextMapButton.interactable = (m_CurrentMapIndex < m_AvailableMaps.Count - 1);
    }

    private void ShowNextMap()
    {
        if (m_CurrentMapIndex < m_AvailableMaps.Count - 1)
        {
            m_CurrentMapIndex++;
            UpdateMapDisplay();
        }
    }

    private void ShowPreviousMap()
    {
        if (m_CurrentMapIndex > 0)
        {
            m_CurrentMapIndex--;
            UpdateMapDisplay();
        }
    }

    private void OnSelectMapPressed()
    {
        if (m_AvailableMaps.Count == 0) return;

        MapData selectedMap = m_AvailableMaps[m_CurrentMapIndex];
        Debug.Log($"选择地图: {selectedMap.m_MapName}, 准备加载场景: {selectedMap.m_SceneToLoad}");

        // 在这里你可以存储所选地图的信息，例如到一个GameManager或PlayerData中
        // PlayerPrefs.SetString("SelectedMapScene", selectedMap.m_SceneToLoad);

        // 加载所选地图的场景
        if (!string.IsNullOrEmpty(selectedMap.m_SceneToLoad))
        {
            SceneManager.LoadScene(selectedMap.m_SceneToLoad);
        }
        else
        {
            Debug.LogError($"地图 {selectedMap.m_MapName} 没有配置要加载的场景名!");
        }
    }

    private void OnBackButtonPressed()
    {
        Debug.Log("返回主菜单");
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowMainMenuPanel();
        }
        else
        {
            Debug.LogError("UIManager 实例未找到！");
        }
    }
    #endregion
} 