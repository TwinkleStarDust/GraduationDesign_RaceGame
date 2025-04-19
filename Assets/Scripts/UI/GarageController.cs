using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

/// <summary>
/// 车库控制器 - 管理车辆零部件装配界面
/// </summary>
public class GarageController : MonoBehaviour
{
    // 零部件类别枚举
    public enum TempPartCategory
    {
        Tire,    // 轮胎
        Engine,  // 引擎
        Nitro    // 氮气
    }
    
    [Header("面板引用")]
    [Tooltip("轮胎面板")]
    [SerializeField] private GameObject m_TiresPanel;
    
    [Tooltip("引擎面板")]
    [SerializeField] private GameObject m_EnginePanel;
    
    [Tooltip("氮气面板")]
    [SerializeField] private GameObject m_NitroPanel;
    
    [Header("标签页引用")]
    [Tooltip("轮胎标签页")]
    [SerializeField] private Toggle m_TiresToggle;
    
    [Tooltip("引擎标签页")]
    [SerializeField] private Toggle m_EngineToggle;
    
    [Tooltip("氮气标签页")]
    [SerializeField] private Toggle m_NitroToggle;
    
    [Header("零部件预制体")]
    [Tooltip("零部件项目预制体")]
    [SerializeField] private GameObject m_PartItemPrefab;
    
    [Header("按钮引用")]
    [Tooltip("返回按钮")]
    [SerializeField] private Button m_BackButton;
    
    [Header("信息显示")]
    [Tooltip("车辆性能统计文本")]
    [SerializeField] private TextMeshProUGUI m_VehicleStatsText;
    
    // 临时数据存储
    private List<TempPartData> m_TempParts = new List<TempPartData>();
    
    private void Start()
    {
        // 初始化测试数据
        InitializeTestData();
        
        // 注册事件
        if (m_TiresToggle != null)
            m_TiresToggle.onValueChanged.AddListener((isOn) => { if(isOn) ShowPanel(TempPartCategory.Tire); });
            
        if (m_EngineToggle != null)
            m_EngineToggle.onValueChanged.AddListener((isOn) => { if(isOn) ShowPanel(TempPartCategory.Engine); });
            
        if (m_NitroToggle != null)
            m_NitroToggle.onValueChanged.AddListener((isOn) => { if(isOn) ShowPanel(TempPartCategory.Nitro); });
        
        if (m_BackButton != null)
            m_BackButton.onClick.AddListener(OnBackClicked);
        
        // 默认选择轮胎面板
        if (m_TiresToggle != null)
            m_TiresToggle.isOn = true;
        else
            ShowPanel(TempPartCategory.Tire);
        
        // 载入零部件UI
        LoadParts();
        
        // 更新车辆状态显示
        UpdateVehicleStats();
    }
    
    private void InitializeTestData()
    {
        // 添加一些测试零部件数据
        m_TempParts.Add(new TempPartData { 
            PartID = "tire_01", 
            PartName = "标准轮胎", 
            Description = "基础性能轮胎",
            PartCategory = TempPartCategory.Tire,
            HandlingModifier = 0.1f,
            BrakeForceModifier = 0.1f
        });
        
        m_TempParts.Add(new TempPartData { 
            PartID = "tire_02", 
            PartName = "高级轮胎", 
            Description = "提升操控性能",
            PartCategory = TempPartCategory.Tire,
            HandlingModifier = 0.2f,
            BrakeForceModifier = 0.15f
        });
        
        m_TempParts.Add(new TempPartData { 
            PartID = "engine_01", 
            PartName = "标准引擎", 
            Description = "基础性能引擎",
            PartCategory = TempPartCategory.Engine,
            SpeedModifier = 0.1f,
            AccelerationModifier = 0.1f
        });
        
        m_TempParts.Add(new TempPartData { 
            PartID = "nitro_01", 
            PartName = "标准氮气", 
            Description = "基础氮气系统",
            PartCategory = TempPartCategory.Nitro,
            SpeedModifier = 0.05f,
            AccelerationModifier = 0.15f
        });
    }
    
    private void ShowPanel(TempPartCategory category)
    {
        if (m_TiresPanel != null)
            m_TiresPanel.SetActive(category == TempPartCategory.Tire);
            
        if (m_EnginePanel != null)
            m_EnginePanel.SetActive(category == TempPartCategory.Engine);
            
        if (m_NitroPanel != null)
            m_NitroPanel.SetActive(category == TempPartCategory.Nitro);
    }
    
    private void LoadParts()
    {
        // 为每个类别载入零部件
        LoadPartsForCategory(TempPartCategory.Tire, m_TiresPanel?.transform);
        LoadPartsForCategory(TempPartCategory.Engine, m_EnginePanel?.transform);
        LoadPartsForCategory(TempPartCategory.Nitro, m_NitroPanel?.transform);
    }
    
    private void LoadPartsForCategory(TempPartCategory category, Transform parent)
    {
        if (parent == null) return;
        
        // 清空现有内容
        foreach (Transform child in parent)
        {
            Destroy(child.gameObject);
        }
        
        // 获取该类别的零部件
        List<TempPartData> categoryParts = m_TempParts.FindAll(p => p.PartCategory == category);
        
        // 在UI中显示零部件
        foreach (TempPartData part in categoryParts)
        {
            if (m_PartItemPrefab == null) continue;
            
            GameObject partItem = Instantiate(m_PartItemPrefab, parent);
            // 设置零部件数据 - 这将依赖于PartItemUI.cs的修改
            // 我们将在修改PartItemUI.cs后处理这部分
        }
    }
    
    private void UpdateVehicleStats()
    {
        if (m_VehicleStatsText == null) return;
        
        // 显示默认状态
        float speed = 100f;
        float acceleration = 100f;
        float handling = 100f;
        float braking = 100f;
        
        // 模拟装备零部件影响
        speed += 20f;
        acceleration += 15f;
        handling += 10f;
        braking += 5f;
        
        // 更新UI显示
        m_VehicleStatsText.text = $"速度: {speed:F0}\n加速: {acceleration:F0}\n操控: {handling:F0}\n制动: {braking:F0}";
    }
    
    private void OnBackClicked()
    {
        // 关闭车库界面
        Debug.Log("关闭车库界面");
        // 尝试隐藏当前面板
        if (transform.parent != null)
            transform.parent.gameObject.SetActive(false);
    }
    
    // 临时零部件数据类
    [Serializable]
    public class TempPartData
    {
        public string PartID = "part_id";
        public string PartName = "默认零件名";
        public string Description = "零件描述";
        public Sprite Icon = null;
        public TempPartCategory PartCategory;
        public float SpeedModifier = 0f;
        public float AccelerationModifier = 0f;
        public float HandlingModifier = 0f;
        public float BrakeForceModifier = 0f;
        
        // 获取稀有度颜色
        public Color GetRarityColor()
        {
            return Color.white;
        }
    }
} 