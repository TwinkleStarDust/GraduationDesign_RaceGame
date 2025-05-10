using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro; // 确保已导入TextMeshPro

public class GarageUI : MonoBehaviour
{
    #region 私有字段
    [Header("玩家库存数据")]
    [SerializeField] private PlayerInventorySO m_PlayerInventory; // 引用玩家库存ScriptableObject

    [Header("当前车辆展示")]
    [SerializeField] private TextMeshProUGUI m_CurrentVehicleNameText;
    [SerializeField] private Image m_CurrentVehicleImage; // 或者使用RawImage + RenderTexture来展示3D模型
    [SerializeField] private Transform m_VehicleDisplayPoint; // 用于实例化和展示车辆3D模型的挂点
    private GameObject m_CurrentVehicleInstance;

    [Header("已装备零件显示")]
    [SerializeField] private Image m_EquippedEngineIcon;
    [SerializeField] private TextMeshProUGUI m_EquippedEngineNameText;
    [SerializeField] private Image m_EquippedTiresIcon;
    [SerializeField] private TextMeshProUGUI m_EquippedTiresNameText;
    [SerializeField] private Image m_EquippedNOSIcon;
    [SerializeField] private TextMeshProUGUI m_EquippedNOSNameText;

    [Header("拥有零件列表UI (可选的动态列表)")]
    // 你可以使用ScrollView + GridLayoutGroup + 预制件来动态生成拥有的零件列表
    [SerializeField] private Transform m_OwnedPartsContainer; // <--- 确保这个在Inspector中被赋值
    [SerializeField] private GameObject m_PartItemPrefab;    // <--- 确保这个预制件存在并被赋值

    [Header("导航按钮")]
    [SerializeField] private Button m_BackButton;
    // 可以添加切换车辆、更换零件的按钮等
    #endregion

    #region Unity生命周期
    private void OnEnable() // 当面板激活时更新显示
    {
        if (m_PlayerInventory == null)
        {
            Debug.LogError("玩家库存 PlayerInventorySO 未在GarageUI中分配!");
            return;
        }
        UpdateGarageDisplay();
    }

    private void Start()
    {
        if (m_BackButton != null) m_BackButton.onClick.AddListener(OnBackButtonPressed);
    }
    #endregion

    #region 私有方法
    private void UpdateGarageDisplay()
    {
        if (m_PlayerInventory == null) return;

        // 更新当前车辆信息
        if (m_PlayerInventory.m_CurrentVehicle != null)
        {
            if (m_CurrentVehicleNameText != null) m_CurrentVehicleNameText.text = m_PlayerInventory.m_CurrentVehicle.m_VehicleName;
            if (m_CurrentVehicleImage != null && m_PlayerInventory.m_CurrentVehicle.m_VehicleIcon != null) // 如果使用图片展示
            {
                m_CurrentVehicleImage.sprite = m_PlayerInventory.m_CurrentVehicle.m_VehicleIcon;
                m_CurrentVehicleImage.gameObject.SetActive(true);
            }
            else if (m_CurrentVehicleImage != null)
            {
                m_CurrentVehicleImage.gameObject.SetActive(false);
            }

            // 如果要展示3D模型
            if (m_VehicleDisplayPoint != null && m_PlayerInventory.m_CurrentVehicle.m_VehiclePrefab != null)
            {
                if (m_CurrentVehicleInstance != null) Destroy(m_CurrentVehicleInstance);
                m_CurrentVehicleInstance = Instantiate(m_PlayerInventory.m_CurrentVehicle.m_VehiclePrefab, m_VehicleDisplayPoint.position, m_VehicleDisplayPoint.rotation, m_VehicleDisplayPoint);
                // 你可能需要调整模型的缩放和旋转以适应UI
            }
        }
        else
        {
            if (m_CurrentVehicleNameText != null) m_CurrentVehicleNameText.text = "未选择车辆";
            if (m_CurrentVehicleImage != null) m_CurrentVehicleImage.gameObject.SetActive(false);
            if (m_CurrentVehicleInstance != null) Destroy(m_CurrentVehicleInstance);
        }

        // 更新已装备零件信息
        UpdateEquippedPartUI(m_PlayerInventory.m_EquippedEngine, m_EquippedEngineIcon, m_EquippedEngineNameText, "引擎");
        UpdateEquippedPartUI(m_PlayerInventory.m_EquippedTires, m_EquippedTiresIcon, m_EquippedTiresNameText, "轮胎");
        UpdateEquippedPartUI(m_PlayerInventory.m_EquippedNOS, m_EquippedNOSIcon, m_EquippedNOSNameText, "氮气");

        // TODO: 更新拥有的零件列表 (如果实现了动态列表)
        PopulateOwnedPartsList(); // <--- 取消注释，并调用
    }

    private void UpdateEquippedPartUI(PartDataSO _partData, Image _iconImage, TextMeshProUGUI _nameText, string _defaultNamePrefix)
    {
        if (_iconImage != null)
        {
            if (_partData != null && _partData.Icon != null)
            {
                _iconImage.sprite = _partData.Icon;
                _iconImage.gameObject.SetActive(true);
            }
            else
            {
                _iconImage.gameObject.SetActive(false); // 没有零件或没有图标则隐藏
            }
        }

        if (_nameText != null)
        {
            _nameText.text = (_partData != null) ? _partData.PartName : $"未装备{_defaultNamePrefix}";
        }
    }

    // 示例：填充拥有零件列表的方法 (需要UI预制件和容器)
    private void PopulateOwnedPartsList()
    {
        if (m_OwnedPartsContainer == null || m_PartItemPrefab == null || m_PlayerInventory == null) return;

        // 清空旧列表
        foreach (Transform child in m_OwnedPartsContainer)
        {
            Destroy(child.gameObject);
        }

        // 为每个拥有的零件创建UI项
        foreach (PartDataSO part in m_PlayerInventory.m_OwnedParts)
        {
            GameObject partItemGO = Instantiate(m_PartItemPrefab, m_OwnedPartsContainer);
            // 假设PartItemPrefab上有一个脚本 PartItemUI 来设置其内容
            PartItemUI partItemUI = partItemGO.GetComponent<PartItemUI>(); 
            if (partItemUI != null)
            {
                partItemUI.Setup(part, this); // 传入GarageUI实例以便处理点击事件
            }
            else
            {
                Debug.LogWarning($"PartItemPrefab ({m_PartItemPrefab.name}) 上缺少 PartItemUI 组件！");
            }
        }
    }

    // 当玩家从列表中选择一个零件来装备时调用此方法 (需要 PartItemUI 脚本配合)
    public void OnPartSelectedForEquipping(PartDataSO _selectedPart) // <--- 参数类型已更新为 PartDataSO
    {
        if (m_PlayerInventory == null || _selectedPart == null) return;

        // 调用PlayerInventory中的装备方法
        m_PlayerInventory.EquipPart(_selectedPart);
        Debug.Log($"已调用装备零件: {_selectedPart.PartName}");

        // 刷新车库显示，以反映新装备的零件
        UpdateGarageDisplay();
    }

    private void OnBackButtonPressed()
    {
        Debug.Log("从车库返回主菜单");
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

// 如果你创建了 PartItemUI.cs 用于动态列表中的零件项，它可能看起来像这样：
/*
public class PartItemUI : MonoBehaviour
{
    public Image m_PartIconImage;
    public TextMeshProUGUI m_PartNameText;
    public Button m_SelectButton;

    private PartDataSO m_PartData;
    private GarageUI m_GarageUIInstance;

    public void Setup(PartDataSO _data, GarageUI _garageUI)
    {
        m_PartData = _data;
        m_GarageUIInstance = _garageUI;

        if (m_PartIconImage != null && _data.m_PartIcon != null) m_PartIconImage.sprite = _data.m_PartIcon;
        if (m_PartNameText != null) m_PartNameText.text = _data.m_PartName;
        if (m_SelectButton != null) m_SelectButton.onClick.AddListener(OnSelect);
    }

    private void OnSelect()
    {
        if (m_GarageUIInstance != null && m_PartData != null)
        {
            // m_GarageUIInstance.OnPartSelectedForEquipping(m_PartData);
        }
    }
}
*/ 