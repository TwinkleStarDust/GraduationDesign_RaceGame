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
    
    [Header("通用物理参数修正")]
    [Tooltip("质量修正值 (加到车辆基础质量上)")]
    [SerializeField] private float m_MassModifier = 0f;
    [Tooltip("空气阻力修正值 (加到车辆基础空气阻力上)")]
    [Range(-0.5f, 0.5f)]
    [SerializeField] private float m_DragModifier = 0f;
    [Tooltip("角阻力修正值 (加到车辆基础角阻力上)")]
    [Range(-0.5f, 0.5f)]
    [SerializeField] private float m_AngularDragModifier = 0f;
    [Tooltip("轮胎在潮湿路面的性能表现修正（0-1，1代表不受影响，暂未完全实现）")]
    [Range(0f, 1f)]
    [SerializeField] private float m_WetPerformanceModifier = 1f;

    // 引擎特有参数
    [Header("引擎特有参数修正")]
    [Tooltip("最大马力扭矩额外加成值")]
    [SerializeField] private float m_MaxMotorTorqueBonus = 0f;
    [Tooltip("引擎动力衰减起始因子额外加成值 (正值使衰减更早，负值更晚)")]
    [Range(-0.2f, 0.2f)]
    [SerializeField] private float m_EnginePowerFalloffStartFactorBonus = 0f;
    [Tooltip("引擎在绝对最大速度时的马力百分比额外加成值")]
    [Range(-0.2f, 0.2f)]
    [SerializeField] private float m_EnginePowerAtAbsoluteMaxFactorBonus = 0f;
    [Tooltip("引擎扭矩曲线 (如果需要每个引擎部件定义自己的曲线)")]
    [SerializeField] private AnimationCurve m_EngineTorqueCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(0.5f, 1.2f), new Keyframe(1, 0.8f));
    [Tooltip("引擎声音 (如果需要每个引擎部件定义自己的声音)")]
    [SerializeField] private AudioClip m_EngineSound;

    // 轮胎特有参数
    [Header("轮胎特有参数修正")]
    [Header("前轮 - 前向摩擦力修正")]
    [Tooltip("前轮前向摩擦力 - Extremum Slip 修正值")]
    [Range(-0.2f, 0.2f)]
    [SerializeField] private float m_FL_Fwd_ExtremumSlipModifier = 0f;
    [Tooltip("前轮前向摩擦力 - Extremum Value 修正值")]
    [Range(-0.5f, 0.5f)]
    [SerializeField] private float m_FL_Fwd_ExtremumValueModifier = 0f;
    [Tooltip("前轮前向摩擦力 - Asymptote Slip 修正值")]
    [Range(-0.2f, 0.2f)]
    [SerializeField] private float m_FL_Fwd_AsymptoteSlipModifier = 0f;
    [Tooltip("前轮前向摩擦力 - Asymptote Value 修正值")]
    [Range(-0.5f, 0.5f)]
    [SerializeField] private float m_FL_Fwd_AsymptoteValueModifier = 0f;
    [Tooltip("前轮前向摩擦力 - Stiffness 修正值")]
    [Range(-0.5f, 0.5f)]
    [SerializeField] private float m_FL_Fwd_StiffnessModifier = 0f;

    [Header("后轮 - 前向摩擦力修正")]
    [Tooltip("后轮前向摩擦力 - Extremum Slip 修正值")]
    [Range(-0.2f, 0.2f)]
    [SerializeField] private float m_RL_Fwd_ExtremumSlipModifier = 0f;
    [Tooltip("后轮前向摩擦力 - Extremum Value 修正值")]
    [Range(-0.5f, 0.5f)]
    [SerializeField] private float m_RL_Fwd_ExtremumValueModifier = 0f;
    [Tooltip("后轮前向摩擦力 - Asymptote Slip 修正值")]
    [Range(-0.2f, 0.2f)]
    [SerializeField] private float m_RL_Fwd_AsymptoteSlipModifier = 0f;
    [Tooltip("后轮前向摩擦力 - Asymptote Value 修正值")]
    [Range(-0.5f, 0.5f)]
    [SerializeField] private float m_RL_Fwd_AsymptoteValueModifier = 0f;
    [Tooltip("后轮前向摩擦力 - Stiffness 修正值")]
    [Range(-0.5f, 0.5f)]
    [SerializeField] private float m_RL_Fwd_StiffnessModifier = 0f;

    [Header("前轮 - 侧向摩擦力修正 (正常行驶)")]
    [Tooltip("前轮侧向摩擦力 (正常) - Extremum Slip 修正值")]
    [Range(-0.2f, 0.2f)]
    [SerializeField] private float m_FL_Side_Normal_ExtremumSlipModifier = 0f;
    [Tooltip("前轮侧向摩擦力 (正常) - Extremum Value 修正值")]
    [Range(-0.5f, 0.5f)]
    [SerializeField] private float m_FL_Side_Normal_ExtremumValueModifier = 0f;
    [Tooltip("前轮侧向摩擦力 (正常) - Asymptote Slip 修正值")]
    [Range(-0.2f, 0.2f)]
    [SerializeField] private float m_FL_Side_Normal_AsymptoteSlipModifier = 0f;
    [Tooltip("前轮侧向摩擦力 (正常) - Asymptote Value 修正值")]
    [Range(-0.5f, 0.5f)]
    [SerializeField] private float m_FL_Side_Normal_AsymptoteValueModifier = 0f;
    [Tooltip("前轮侧向摩擦力 (正常) - Stiffness 修正值")]
    [Range(-0.5f, 0.5f)]
    [SerializeField] private float m_FL_Side_Normal_StiffnessModifier = 0f;

    [Header("后轮 - 侧向摩擦力修正 (正常行驶)")]
    [Tooltip("后轮侧向摩擦力 (正常) - Extremum Slip 修正值")]
    [Range(-0.2f, 0.2f)]
    [SerializeField] private float m_RL_Side_Normal_ExtremumSlipModifier = 0f;
    [Tooltip("后轮侧向摩擦力 (正常) - Extremum Value 修正值")]
    [Range(-0.5f, 0.5f)]
    [SerializeField] private float m_RL_Side_Normal_ExtremumValueModifier = 0f;
    [Tooltip("后轮侧向摩擦力 (正常) - Asymptote Slip 修正值")]
    [Range(-0.2f, 0.2f)]
    [SerializeField] private float m_RL_Side_Normal_AsymptoteSlipModifier = 0f;
    [Tooltip("后轮侧向摩擦力 (正常) - Asymptote Value 修正值")]
    [Range(-0.5f, 0.5f)]
    [SerializeField] private float m_RL_Side_Normal_AsymptoteValueModifier = 0f;
    [Tooltip("后轮侧向摩擦力 (正常) - Stiffness 修正值")]
    [Range(-0.5f, 0.5f)]
    [SerializeField] private float m_RL_Side_Normal_StiffnessModifier = 0f;
    
    [Header("前轮 - 侧向摩擦力修正 (漂移时)")]
    [Tooltip("前轮侧向摩擦力 (漂移) - Extremum Slip 修正值")]
    [Range(-0.1f, 0.1f)] // 漂移时侧滑更敏感，修正范围小一些
    [SerializeField] private float m_FL_Side_Drift_ExtremumSlipModifier = 0f;
    [Tooltip("前轮侧向摩擦力 (漂移) - Extremum Value 修正值")]
    [Range(-0.3f, 0.3f)]
    [SerializeField] private float m_FL_Side_Drift_ExtremumValueModifier = 0f;
    [Tooltip("前轮侧向摩擦力 (漂移) - Asymptote Slip 修正值")]
    [Range(-0.1f, 0.1f)]
    [SerializeField] private float m_FL_Side_Drift_AsymptoteSlipModifier = 0f;
    [Tooltip("前轮侧向摩擦力 (漂移) - Asymptote Value 修正值")]
    [Range(-0.3f, 0.3f)]
    [SerializeField] private float m_FL_Side_Drift_AsymptoteValueModifier = 0f;
    [Tooltip("前轮侧向摩擦力 (漂移) - Stiffness 修正值")]
    [Range(-0.3f, 0.3f)]
    [SerializeField] private float m_FL_Side_Drift_StiffnessModifier = 0f;

    [Header("后轮 - 侧向摩擦力修正 (漂移时)")]
    [Tooltip("后轮侧向摩擦力 (漂移) - Extremum Slip 修正值")]
    [Range(-0.1f, 0.1f)]
    [SerializeField] private float m_RL_Side_Drift_ExtremumSlipModifier = 0f;
    [Tooltip("后轮侧向摩擦力 (漂移) - Extremum Value 修正值")]
    [Range(-0.3f, 0.3f)]
    [SerializeField] private float m_RL_Side_Drift_ExtremumValueModifier = 0f;
    [Tooltip("后轮侧向摩擦力 (漂移) - Asymptote Slip 修正值")]
    [Range(-0.1f, 0.1f)]
    [SerializeField] private float m_RL_Side_Drift_AsymptoteSlipModifier = 0f;
    [Tooltip("后轮侧向摩擦力 (漂移) - Asymptote Value 修正值")]
    [Range(-0.3f, 0.3f)]
    [SerializeField] private float m_RL_Side_Drift_AsymptoteValueModifier = 0f;
    [Tooltip("后轮侧向摩擦力 (漂移) - Stiffness 修正值")]
    [Range(-0.3f, 0.3f)]
    [SerializeField] private float m_RL_Side_Drift_StiffnessModifier = 0f;

    [Tooltip("刹车扭矩额外加成值")]
    [SerializeField] private float m_BrakeTorqueBonus = 0f;
    [Tooltip("最大转向角度额外加成值")]
    [SerializeField] private float m_MaxSteeringAngleBonus = 0f;
    // [Tooltip("轮胎抓地力修正 (旧的，将被下面的详细参数替代，但可以保留用于简化显示或计算总评)")]
    // [Range(-0.5f, 0.5f)]
    // [SerializeField] private float m_TireFrictionModifier; 
    
    // 氮气特有参数
    [Header("氮气特有参数修正")]
    [Tooltip("最大氮气容量额外加成值")]
    [SerializeField] private float m_MaxNitroCapacityBonus = 0f;
    [Tooltip("氮气消耗速率乘数 (例如 0.9 表示消耗减慢10%，1.1 表示加快10%)")]
    [Range(0.5f, 1.5f)]
    [SerializeField] private float m_NitroConsumptionRateMultiplier = 1.0f;
    [Tooltip("氮气推力大小额外加成值")]
    [SerializeField] private float m_NitroForceMagnitudeBonus = 0f;
    [Tooltip("氮气恢复速率额外加成值")]
    [SerializeField] private float m_NitroRegenerationRateBonus = 0f;
    [Tooltip("氮气恢复延迟乘数 (例如 0.9 表示延迟缩短10%，1.1 表示延长10%)")]
    [Range(0.5f, 1.5f)]
    [SerializeField] private float m_NitroRegenerationDelayMultiplier = 1.0f;

    [Header("音效特定修正")]
    [Tooltip("此轮胎零件特定的漂移音效。如果为null，则使用车辆默认或CarController的备用音效。仅当 PartCategory 为 Tire 时有效。 ")]
    [SerializeField] private AudioClip m_DriftSound;
    // [Tooltip("氮气效率修正 (旧的，将被上面的详细参数替代)")]
    // [Range(-0.5f, 0.5f)]
    // [SerializeField] private float m_NitroEfficiencyModifier; 
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
    /// 轮胎抓地力修正 (旧)
    /// </summary>
    // public float TireFrictionModifier => m_TireFrictionModifier; // 暂时注释，使用更细致的参数
    
    /// <summary>
    /// 轮胎湿滑路面表现 (可能需要更细致的实现或与其他摩擦力参数结合)
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
    /// 氮气容量修正 (旧)
    /// </summary>
    // public float NitroCapacityModifier => m_NitroCapacityModifier; // 暂时注释
    
    /// <summary>
    /// 氮气效率修正 (旧)
    /// </summary>
    // public float NitroEfficiencyModifier => m_NitroEfficiencyModifier; // 暂时注释
    
    /// <summary>
    /// 氮气回复速度修正 (旧)
    /// </summary>
    // public float NitroRecoveryModifier => m_NitroRecoveryModifier; // 暂时注释

    #region 新增具体参数的公共属性

    // 通用物理
    public float MassModifier => m_MassModifier;
    public float DragModifier => m_DragModifier;
    public float AngularDragModifier => m_AngularDragModifier;

    // 引擎
    public float MaxMotorTorqueBonus => m_MaxMotorTorqueBonus;
    public float EnginePowerFalloffStartFactorBonus => m_EnginePowerFalloffStartFactorBonus;
    public float EnginePowerAtAbsoluteMaxFactorBonus => m_EnginePowerAtAbsoluteMaxFactorBonus;

    // 轮胎 - 前轮前向
    public float FL_Fwd_ExtremumSlipModifier => m_FL_Fwd_ExtremumSlipModifier;
    public float FL_Fwd_ExtremumValueModifier => m_FL_Fwd_ExtremumValueModifier;
    public float FL_Fwd_AsymptoteSlipModifier => m_FL_Fwd_AsymptoteSlipModifier;
    public float FL_Fwd_AsymptoteValueModifier => m_FL_Fwd_AsymptoteValueModifier;
    public float FL_Fwd_StiffnessModifier => m_FL_Fwd_StiffnessModifier;
    // 轮胎 - 后轮前向
    public float RL_Fwd_ExtremumSlipModifier => m_RL_Fwd_ExtremumSlipModifier;
    public float RL_Fwd_ExtremumValueModifier => m_RL_Fwd_ExtremumValueModifier;
    public float RL_Fwd_AsymptoteSlipModifier => m_RL_Fwd_AsymptoteSlipModifier;
    public float RL_Fwd_AsymptoteValueModifier => m_RL_Fwd_AsymptoteValueModifier;
    public float RL_Fwd_StiffnessModifier => m_RL_Fwd_StiffnessModifier;
    // 轮胎 - 前轮侧向 (正常)
    public float FL_Side_Normal_ExtremumSlipModifier => m_FL_Side_Normal_ExtremumSlipModifier;
    public float FL_Side_Normal_ExtremumValueModifier => m_FL_Side_Normal_ExtremumValueModifier;
    public float FL_Side_Normal_AsymptoteSlipModifier => m_FL_Side_Normal_AsymptoteSlipModifier;
    public float FL_Side_Normal_AsymptoteValueModifier => m_FL_Side_Normal_AsymptoteValueModifier;
    public float FL_Side_Normal_StiffnessModifier => m_FL_Side_Normal_StiffnessModifier;
    // 轮胎 - 后轮侧向 (正常)
    public float RL_Side_Normal_ExtremumSlipModifier => m_RL_Side_Normal_ExtremumSlipModifier;
    public float RL_Side_Normal_ExtremumValueModifier => m_RL_Side_Normal_ExtremumValueModifier;
    public float RL_Side_Normal_AsymptoteSlipModifier => m_RL_Side_Normal_AsymptoteSlipModifier;
    public float RL_Side_Normal_AsymptoteValueModifier => m_RL_Side_Normal_AsymptoteValueModifier;
    public float RL_Side_Normal_StiffnessModifier => m_RL_Side_Normal_StiffnessModifier;
    // 轮胎 - 前轮侧向 (漂移)
    public float FL_Side_Drift_ExtremumSlipModifier => m_FL_Side_Drift_ExtremumSlipModifier;
    public float FL_Side_Drift_ExtremumValueModifier => m_FL_Side_Drift_ExtremumValueModifier;
    public float FL_Side_Drift_AsymptoteSlipModifier => m_FL_Side_Drift_AsymptoteSlipModifier;
    public float FL_Side_Drift_AsymptoteValueModifier => m_FL_Side_Drift_AsymptoteValueModifier;
    public float FL_Side_Drift_StiffnessModifier => m_FL_Side_Drift_StiffnessModifier;
    // 轮胎 - 后轮侧向 (漂移)
    public float RL_Side_Drift_ExtremumSlipModifier => m_RL_Side_Drift_ExtremumSlipModifier;
    public float RL_Side_Drift_ExtremumValueModifier => m_RL_Side_Drift_ExtremumValueModifier;
    public float RL_Side_Drift_AsymptoteSlipModifier => m_RL_Side_Drift_AsymptoteSlipModifier;
    public float RL_Side_Drift_AsymptoteValueModifier => m_RL_Side_Drift_AsymptoteValueModifier;
    public float RL_Side_Drift_StiffnessModifier => m_RL_Side_Drift_StiffnessModifier;

    public float BrakeTorqueBonus => m_BrakeTorqueBonus;
    public float MaxSteeringAngleBonus => m_MaxSteeringAngleBonus;

    // 氮气
    public float MaxNitroCapacityBonus => m_MaxNitroCapacityBonus;
    public float NitroConsumptionRateMultiplier => m_NitroConsumptionRateMultiplier;
    public float NitroForceMagnitudeBonus => m_NitroForceMagnitudeBonus;
    public float NitroRegenerationRateBonus => m_NitroRegenerationRateBonus;
    public float NitroRegenerationDelayMultiplier => m_NitroRegenerationDelayMultiplier;

    // 音效
    public AudioClip DriftSound => m_DriftSound;

    #endregion
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
        baseRating += m_BrakeTorqueBonus / 100f * 0.1f; // 假设刹车力加成是以实际数值，这里做个转换参与评级
        
        // 新增具体参数对评分的贡献（示例）
        baseRating += m_MaxMotorTorqueBonus / 100f * 0.2f; // 假设马力加成值较大
        baseRating += m_MaxSteeringAngleBonus * 2f * 0.1f;   // 假设转向角加成值较小，权重调高
        
        // 根据零部件类型添加特定评分
        switch (m_PartCategory)
        {
            case PartCategory.Tire:
                // baseRating += m_TireFrictionModifier * 100f * 0.1f;
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
                // 也可以根据 m_MaxMotorTorqueBonus 等新参数调整引擎评分
                break;
                
            case PartCategory.Nitro:
                // baseRating += m_NitroCapacityModifier * 0.1f;
                // baseRating += m_NitroEfficiencyModifier * 100f * 0.1f;
                // baseRating += m_NitroRecoveryModifier * 0.05f;
                baseRating += m_MaxNitroCapacityBonus / 10f * 0.1f;
                baseRating += (1.5f - m_NitroConsumptionRateMultiplier) * 50f * 0.1f; // 消耗越低评分越高
                baseRating += m_NitroForceMagnitudeBonus / 100f * 0.1f;
                baseRating += m_NitroRegenerationRateBonus * 0.05f;
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
        baseTire.m_WetPerformanceModifier = 0.0f;
        baseTire.m_BrakeTorqueBonus = 0f;
        baseTire.m_MaxSteeringAngleBonus = 0f;
        
        UnityEditor.AssetDatabase.CreateAsset(baseTire, "Assets/ScriptableObjects/Parts/Tire_BaseSO.asset");
        
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
        baseEngine.m_MaxMotorTorqueBonus = 0f;
        baseEngine.m_EnginePowerFalloffStartFactorBonus = 0f;
        baseEngine.m_EnginePowerAtAbsoluteMaxFactorBonus = 0f;
        
        UnityEditor.AssetDatabase.CreateAsset(baseEngine, "Assets/ScriptableObjects/Parts/Engine_BaseSO.asset");
        
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
        baseNitro.m_MaxNitroCapacityBonus = 0f;
        baseNitro.m_NitroConsumptionRateMultiplier = 1.0f;
        baseNitro.m_NitroForceMagnitudeBonus = 0f;
        baseNitro.m_NitroRegenerationRateBonus = 0f;
        baseNitro.m_NitroRegenerationDelayMultiplier = 1.0f;
        
        UnityEditor.AssetDatabase.CreateAsset(baseNitro, "Assets/ScriptableObjects/Parts/Nitro_BaseSO.asset");
        
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
    Nitro,   // 氮气
    Spoiler, // 尾翼 (示例，如果需要)
    BodyKit  // 包围 (示例，如果需要)
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