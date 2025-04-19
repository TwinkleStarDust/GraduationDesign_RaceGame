using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 车辆零部件数据 - 定义车辆零部件的类型和性能属性
/// </summary>
[CreateAssetMenu(fileName = "PartData", menuName = "Racing Game/Part Data", order = 2)]
public class PartDataSO : ScriptableObject
{
    #region 序列化字段
    [Header("基本信息")]
    [Tooltip("零部件ID")]
    [SerializeField] private string m_PartID;
    
    [Tooltip("零部件名称")]
    [SerializeField] private string m_PartName;
    
    [Tooltip("零部件描述")]
    [SerializeField] [TextArea(2, 5)] private string m_Description;
    
    [Tooltip("零部件图标")]
    [SerializeField] private Sprite m_Icon;
    
    [Tooltip("零部件模型预制体")]
    [SerializeField] private GameObject m_ModelPrefab;
    
    [Header("分类")]
    [Tooltip("零部件类型")]
    [SerializeField] private PartCategory m_PartCategory;
    
    [Tooltip("零部件稀有度")]
    [SerializeField] private PartRarity m_Rarity;
    
    [Header("经济属性")]
    [Tooltip("解锁价格")]
    [SerializeField] private int m_UnlockPrice;
    
    [Tooltip("是否默认解锁")]
    [SerializeField] private bool m_IsDefaultUnlocked;
    
    [Header("车辆性能参数")]
    [Tooltip("速度修正")]
    [Range(-50f, 100f)]
    [SerializeField] private float m_SpeedModifier;
    
    [Tooltip("加速度修正")]
    [Range(-50f, 100f)]
    [SerializeField] private float m_AccelerationModifier;
    
    [Tooltip("操控性修正")]
    [Range(-50f, 100f)]
    [SerializeField] private float m_HandlingModifier;
    
    [Tooltip("制动力修正")]
    [Range(-50f, 100f)]
    [SerializeField] private float m_BrakeForceModifier;
    
    [Header("特定类型参数")]
    // 轮胎特有参数
    [Tooltip("轮胎抓地力修正")]
    [Range(-0.5f, 0.5f)]
    [SerializeField] private float m_TireFrictionModifier;
    
    [Tooltip("轮胎湿滑路面表现")]
    [Range(-0.5f, 0.5f)]
    [SerializeField] private float m_WetPerformanceModifier;
    
    // 引擎特有参数
    [Tooltip("引擎扭矩曲线")]
    [SerializeField] private AnimationCurve m_EngineTorqueCurve;
    
    [Tooltip("引擎声音")]
    [SerializeField] private AudioClip m_EngineSound;
    
    // 氮气特有参数
    [Tooltip("氮气容量修正")]
    [Range(-50f, 100f)]
    [SerializeField] private float m_NitroCapacityModifier;
    
    [Tooltip("氮气效率修正")]
    [Range(-0.5f, 0.5f)]
    [SerializeField] private float m_NitroEfficiencyModifier;
    
    [Tooltip("氮气回复速度修正")]
    [Range(-50f, 100f)]
    [SerializeField] private float m_NitroRecoveryModifier;
    
    [Header("视觉效果")]
    [Tooltip("是否有自定义外观")]
    [SerializeField] private bool m_HasCustomAppearance;
    
    [Tooltip("零部件材质")]
    [SerializeField] private Material m_PartMaterial;
    
    [Tooltip("粒子效果预制体")]
    [SerializeField] private GameObject m_ParticleEffectPrefab;
    #endregion
    
    #region 公共属性
    /// <summary>
    /// 零部件ID
    /// </summary>
    public string PartID => m_PartID;
    
    /// <summary>
    /// 零部件名称
    /// </summary>
    public string PartName => m_PartName;
    
    /// <summary>
    /// 零部件描述
    /// </summary>
    public string Description => m_Description;
    
    /// <summary>
    /// 零部件图标
    /// </summary>
    public Sprite Icon => m_Icon;
    
    /// <summary>
    /// 零部件模型预制体
    /// </summary>
    public GameObject ModelPrefab => m_ModelPrefab;
    
    /// <summary>
    /// 零部件类型
    /// </summary>
    public PartCategory PartCategory => m_PartCategory;
    
    /// <summary>
    /// 零部件稀有度
    /// </summary>
    public PartRarity Rarity => m_Rarity;
    
    /// <summary>
    /// 解锁价格
    /// </summary>
    public int UnlockPrice => m_UnlockPrice;
    
    /// <summary>
    /// 是否默认解锁
    /// </summary>
    public bool IsDefaultUnlocked => m_IsDefaultUnlocked;
    
    /// <summary>
    /// 速度修正
    /// </summary>
    public float SpeedModifier => m_SpeedModifier;
    
    /// <summary>
    /// 加速度修正
    /// </summary>
    public float AccelerationModifier => m_AccelerationModifier;
    
    /// <summary>
    /// 操控性修正
    /// </summary>
    public float HandlingModifier => m_HandlingModifier;
    
    /// <summary>
    /// 制动力修正
    /// </summary>
    public float BrakeForceModifier => m_BrakeForceModifier;
    
    /// <summary>
    /// 轮胎抓地力修正
    /// </summary>
    public float TireFrictionModifier => m_TireFrictionModifier;
    
    /// <summary>
    /// 轮胎湿滑路面表现
    /// </summary>
    public float WetPerformanceModifier => m_WetPerformanceModifier;
    
    /// <summary>
    /// 引擎扭矩曲线
    /// </summary>
    public AnimationCurve EngineTorqueCurve => m_EngineTorqueCurve;
    
    /// <summary>
    /// 引擎声音
    /// </summary>
    public AudioClip EngineSound => m_EngineSound;
    
    /// <summary>
    /// 氮气容量修正
    /// </summary>
    public float NitroCapacityModifier => m_NitroCapacityModifier;
    
    /// <summary>
    /// 氮气效率修正
    /// </summary>
    public float NitroEfficiencyModifier => m_NitroEfficiencyModifier;
    
    /// <summary>
    /// 氮气回复速度修正
    /// </summary>
    public float NitroRecoveryModifier => m_NitroRecoveryModifier;
    
    /// <summary>
    /// 是否有自定义外观
    /// </summary>
    public bool HasCustomAppearance => m_HasCustomAppearance;
    
    /// <summary>
    /// 零部件材质
    /// </summary>
    public Material PartMaterial => m_PartMaterial;
    
    /// <summary>
    /// 粒子效果预制体
    /// </summary>
    public GameObject ParticleEffectPrefab => m_ParticleEffectPrefab;
    #endregion
    
    #region 公共方法
    /// <summary>
    /// 获取零部件总评分
    /// </summary>
    public int GetOverallRating()
    {
        // 基础评分
        float baseRating = 0;
        
        // 加权计算各项性能参数
        baseRating += m_SpeedModifier * 0.3f;
        baseRating += m_AccelerationModifier * 0.25f;
        baseRating += m_HandlingModifier * 0.2f;
        baseRating += m_BrakeForceModifier * 0.1f;
        
        // 根据零部件类型添加特定评分
        switch (m_PartCategory)
        {
            case PartCategory.Tire:
                baseRating += m_TireFrictionModifier * 100f * 0.1f;
                baseRating += m_WetPerformanceModifier * 100f * 0.05f;
                break;
                
            case PartCategory.Engine:
                // 引擎扭矩曲线的评分可以基于曲线的平均值或某个特定点
                if (m_EngineTorqueCurve != null && m_EngineTorqueCurve.keys.Length > 0)
                {
                    float torqueSum = 0;
                    for (int i = 0; i < m_EngineTorqueCurve.keys.Length; i++)
                    {
                        torqueSum += m_EngineTorqueCurve.keys[i].value;
                    }
                    float avgTorque = torqueSum / m_EngineTorqueCurve.keys.Length;
                    baseRating += avgTorque * 30f; // 调整权重
                }
                break;
                
            case PartCategory.Nitro:
                baseRating += m_NitroCapacityModifier * 0.1f;
                baseRating += m_NitroEfficiencyModifier * 100f * 0.1f;
                baseRating += m_NitroRecoveryModifier * 0.05f;
                break;
        }
        
        // 根据稀有度增加额外评分
        float rarityMultiplier = 1f;
        switch (m_Rarity)
        {
            case PartRarity.Common:
                rarityMultiplier = 1.0f;
                break;
            case PartRarity.Uncommon:
                rarityMultiplier = 1.1f;
                break;
            case PartRarity.Rare:
                rarityMultiplier = 1.2f;
                break;
            case PartRarity.Epic:
                rarityMultiplier = 1.3f;
                break;
            case PartRarity.Legendary:
                rarityMultiplier = 1.5f;
                break;
        }
        
        // 应用稀有度乘数
        baseRating *= rarityMultiplier;
        
        // 四舍五入到整数
        return Mathf.RoundToInt(baseRating);
    }
    
    /// <summary>
    /// 获取零部件稀有度颜色
    /// </summary>
    public Color GetRarityColor()
    {
        switch (m_Rarity)
        {
            case PartRarity.Common:
                return new Color(0.7f, 0.7f, 0.7f); // 灰色
            case PartRarity.Uncommon:
                return new Color(0.0f, 0.8f, 0.0f); // 绿色
            case PartRarity.Rare:
                return new Color(0.0f, 0.5f, 1.0f); // 蓝色
            case PartRarity.Epic:
                return new Color(0.8f, 0.0f, 1.0f); // 紫色
            case PartRarity.Legendary:
                return new Color(1.0f, 0.8f, 0.0f); // 金色
            default:
                return Color.white;
        }
    }
    
    #if UNITY_EDITOR
    /// <summary>
    /// 为编辑器创建默认零部件数据
    /// </summary>
    public static void CreateDefaultPartData()
    {
        // 创建基础轮胎
        PartDataSO baseTire = ScriptableObject.CreateInstance<PartDataSO>();
        baseTire.m_PartID = "tire_base";
        baseTire.m_PartName = "基础轮胎";
        baseTire.m_Description = "标准轮胎，适合一般道路。";
        baseTire.m_PartCategory = PartCategory.Tire;
        baseTire.m_Rarity = PartRarity.Common;
        baseTire.m_UnlockPrice = 0;
        baseTire.m_IsDefaultUnlocked = true;
        baseTire.m_SpeedModifier = 0;
        baseTire.m_AccelerationModifier = 0;
        baseTire.m_HandlingModifier = 0;
        baseTire.m_BrakeForceModifier = 0;
        baseTire.m_TireFrictionModifier = 0.0f;
        baseTire.m_WetPerformanceModifier = 0.0f;
        
        UnityEditor.AssetDatabase.CreateAsset(baseTire, "Assets/ScriptableObjects/Parts/Tire_Base.asset");
        
        // 创建基础引擎
        PartDataSO baseEngine = ScriptableObject.CreateInstance<PartDataSO>();
        baseEngine.m_PartID = "engine_base";
        baseEngine.m_PartName = "基础引擎";
        baseEngine.m_Description = "标准引擎，性能一般。";
        baseEngine.m_PartCategory = PartCategory.Engine;
        baseEngine.m_Rarity = PartRarity.Common;
        baseEngine.m_UnlockPrice = 0;
        baseEngine.m_IsDefaultUnlocked = true;
        baseEngine.m_SpeedModifier = 0;
        baseEngine.m_AccelerationModifier = 0;
        baseEngine.m_HandlingModifier = 0;
        baseEngine.m_BrakeForceModifier = 0;
        
        // 创建一个基础的引擎扭矩曲线
        baseEngine.m_EngineTorqueCurve = new AnimationCurve(
            new Keyframe(0f, 0.8f),
            new Keyframe(0.3f, 1.0f),
            new Keyframe(0.7f, 0.9f),
            new Keyframe(1.0f, 0.7f)
        );
        
        UnityEditor.AssetDatabase.CreateAsset(baseEngine, "Assets/ScriptableObjects/Parts/Engine_Base.asset");
        
        // 创建基础氮气
        PartDataSO baseNitro = ScriptableObject.CreateInstance<PartDataSO>();
        baseNitro.m_PartID = "nitro_base";
        baseNitro.m_PartName = "基础氮气";
        baseNitro.m_Description = "标准氮气系统，提供基础的速度提升。";
        baseNitro.m_PartCategory = PartCategory.Nitro;
        baseNitro.m_Rarity = PartRarity.Common;
        baseNitro.m_UnlockPrice = 0;
        baseNitro.m_IsDefaultUnlocked = true;
        baseNitro.m_SpeedModifier = 0;
        baseNitro.m_AccelerationModifier = 0;
        baseNitro.m_HandlingModifier = 0;
        baseNitro.m_BrakeForceModifier = 0;
        baseNitro.m_NitroCapacityModifier = 0;
        baseNitro.m_NitroEfficiencyModifier = 0.0f;
        baseNitro.m_NitroRecoveryModifier = 0;
        
        UnityEditor.AssetDatabase.CreateAsset(baseNitro, "Assets/ScriptableObjects/Parts/Nitro_Base.asset");
        
        // 保存资源
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
        
        Debug.Log("创建了默认零部件数据");
    }
    
    [UnityEditor.MenuItem("Tools/Racing Game/Create Default Part Data")]
    private static void CreateDefaultPartDataMenuItem()
    {
        CreateDefaultPartData();
    }
    #endif
    #endregion
}

/// <summary>
/// 零部件类型枚举
/// </summary>
public enum PartCategory
{
    Tire,    // 轮胎
    Engine,  // 引擎
    Nitro    // 氮气
}

/// <summary>
/// 零部件稀有度枚举
/// </summary>
public enum PartRarity
{
    Common,     // 普通
    Uncommon,   // 非凡
    Rare,       // 稀有
    Epic,       // 史诗
    Legendary   // 传奇
} 