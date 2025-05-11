using UnityEngine;

// PartRarity 枚举 (来自 Assets/Scripts/Data/ScriptableObjects/PartDataSO.cs)
public enum PartRarity
{
    Common,     // 普通
    Uncommon,   // 非凡
    Rare,       // 稀有
    Epic,       // 史诗
    Legendary   // 传奇
}

// PartCategory 枚举 (保持只有三种核心类型)
public enum PartCategory
{
    None,   // 无类型或未指定
    Engine, // 引擎
    Tire,   // 轮胎
    Nitro   // 氮气
}

[CreateAssetMenu(fileName = "NewPartDataSO", menuName = "RaceGame/Part Data SO", order = 53)]
public class PartDataSO : ScriptableObject
{
    #region 基础信息 (合并并应用新风格)
    [Header("零件基础信息")]
    [Tooltip("零件名称")]
    [SerializeField] private string m_PartName = "新零件";
    public string PartName => m_PartName;

    [Tooltip("零件类型")]
    [SerializeField] private PartCategory m_PartCategory;
    public PartCategory PartCategoryProperty => m_PartCategory; // 属性名调整以避免与枚举类型名冲突

    [Tooltip("零件图标 (UI使用)")]
    [SerializeField] private Sprite m_Icon;
    public Sprite Icon => m_Icon;

    [Tooltip("零件描述")]
    [SerializeField] [TextArea(3, 5)] private string m_Description = "零件描述...";
    public string Description => m_Description;

    [Header("稀有度与解锁 (合并)")]
    [Tooltip("零件稀有度")]
    [SerializeField] private PartRarity m_Rarity;
    public PartRarity Rarity => m_Rarity;

    [Tooltip("解锁价格")]
    [SerializeField] private int m_UnlockPrice;
    public int UnlockPrice => m_UnlockPrice;

    [Tooltip("是否默认解锁")]
    [SerializeField] private bool m_IsDefaultUnlocked;
    public bool IsDefaultUnlocked => m_IsDefaultUnlocked;
    #endregion

    #region 通用属性加成 (保留并应用新风格)
    [Header("通用属性加成")]
    [Tooltip("最大马力扭矩加成")]
    [SerializeField] private float m_MaxMotorTorqueBonus = 0f;
    public float MaxMotorTorqueBonus => m_MaxMotorTorqueBonus;

    [Tooltip("最大转向角加成")]
    [SerializeField] private float m_MaxSteeringAngleBonus = 0f;
    public float MaxSteeringAngleBonus => m_MaxSteeringAngleBonus;

    [Tooltip("刹车扭矩加成")]
    [SerializeField] private float m_BrakeTorqueBonus = 0f;
    public float BrakeTorqueBonus => m_BrakeTorqueBonus;

    [Tooltip("引擎动力衰减起始系数加成 (百分比，例如0.1表示10%)")]
    [SerializeField] private float m_EnginePowerFalloffStartFactorBonus = 0f;
    public float EnginePowerFalloffStartFactorBonus => m_EnginePowerFalloffStartFactorBonus;

    [Tooltip("达到绝对最大速度时引擎动力系数加成 (百分比)")]
    [SerializeField] private float m_EnginePowerAtAbsoluteMaxFactorBonus = 0f;
    public float EnginePowerAtAbsoluteMaxFactorBonus => m_EnginePowerAtAbsoluteMaxFactorBonus;

    [Tooltip("质量修改值 (可以是负数以减轻重量)")]
    [SerializeField] private float m_MassModifier = 0f;
    public float MassModifier => m_MassModifier;

    [Tooltip("线性阻力修改值")]
    [SerializeField] private float m_DragModifier = 0f;
    public float DragModifier => m_DragModifier;

    [Tooltip("角阻力修改值")]
    [SerializeField] private float m_AngularDragModifier = 0f;
    public float AngularDragModifier => m_AngularDragModifier;
    #endregion

    #region 引擎特定属性 (保留并应用新风格)
    [Header("引擎特定属性")]
    [Tooltip("引擎声音片段 (覆盖车辆默认)")]
    [SerializeField] private AudioClip m_EngineSound;
    public AudioClip EngineSound => m_EngineSound;

    [Tooltip("引擎扭矩/音高曲线 (覆盖车辆默认)")]
    [SerializeField] private AnimationCurve m_EngineTorqueCurve;
    public AnimationCurve EngineTorqueCurve => m_EngineTorqueCurve;
    #endregion

    #region 轮胎特定属性 (保留并应用新风格)
    [Header("轮胎特定属性")]
    [Tooltip("漂移声音片段 (覆盖车辆或默认)")]
    [SerializeField] private AudioClip m_DriftSound;
    public AudioClip DriftSound => m_DriftSound;

    [Header("轮胎摩擦力调整 - 前轮前进方向")]
    [Tooltip("前轮前进方向 - Extremum Slip 修改值")] [SerializeField] private float m_FL_Fwd_ExtremumSlipModifier = 0f;
    public float FL_Fwd_ExtremumSlipModifier => m_FL_Fwd_ExtremumSlipModifier;
    [Tooltip("前轮前进方向 - Extremum Value 修改值")] [SerializeField] private float m_FL_Fwd_ExtremumValueModifier = 0f;
    public float FL_Fwd_ExtremumValueModifier => m_FL_Fwd_ExtremumValueModifier;
    [Tooltip("前轮前进方向 - Asymptote Slip 修改值")] [SerializeField] private float m_FL_Fwd_AsymptoteSlipModifier = 0f;
    public float FL_Fwd_AsymptoteSlipModifier => m_FL_Fwd_AsymptoteSlipModifier;
    [Tooltip("前轮前进方向 - Asymptote Value 修改值")] [SerializeField] private float m_FL_Fwd_AsymptoteValueModifier = 0f;
    public float FL_Fwd_AsymptoteValueModifier => m_FL_Fwd_AsymptoteValueModifier;
    [Tooltip("前轮前进方向 - Stiffness 修改值")] [SerializeField] private float m_FL_Fwd_StiffnessModifier = 0f;
    public float FL_Fwd_StiffnessModifier => m_FL_Fwd_StiffnessModifier;

    [Header("轮胎摩擦力调整 - 后轮前进方向")]
    [Tooltip("后轮前进方向 - Extremum Slip 修改值")] [SerializeField] private float m_RL_Fwd_ExtremumSlipModifier = 0f;
    public float RL_Fwd_ExtremumSlipModifier => m_RL_Fwd_ExtremumSlipModifier;
    [Tooltip("后轮前进方向 - Extremum Value 修改值")] [SerializeField] private float m_RL_Fwd_ExtremumValueModifier = 0f;
    public float RL_Fwd_ExtremumValueModifier => m_RL_Fwd_ExtremumValueModifier;
    [Tooltip("后轮前进方向 - Asymptote Slip 修改值")] [SerializeField] private float m_RL_Fwd_AsymptoteSlipModifier = 0f;
    public float RL_Fwd_AsymptoteSlipModifier => m_RL_Fwd_AsymptoteSlipModifier;
    [Tooltip("后轮前进方向 - Asymptote Value 修改值")] [SerializeField] private float m_RL_Fwd_AsymptoteValueModifier = 0f;
    public float RL_Fwd_AsymptoteValueModifier => m_RL_Fwd_AsymptoteValueModifier;
    [Tooltip("后轮前进方向 - Stiffness 修改值")] [SerializeField] private float m_RL_Fwd_StiffnessModifier = 0f;
    public float RL_Fwd_StiffnessModifier => m_RL_Fwd_StiffnessModifier;
    
    [Header("轮胎摩擦力调整 - 前轮侧向 (正常)")]
    [Tooltip("前轮侧向 (正常) - Extremum Slip 修改值")] [SerializeField] private float m_FL_Side_Normal_ExtremumSlipModifier = 0f;
    public float FL_Side_Normal_ExtremumSlipModifier => m_FL_Side_Normal_ExtremumSlipModifier;
    [Tooltip("前轮侧向 (正常) - Extremum Value 修改值")] [SerializeField] private float m_FL_Side_Normal_ExtremumValueModifier = 0f;
    public float FL_Side_Normal_ExtremumValueModifier => m_FL_Side_Normal_ExtremumValueModifier;
    [Tooltip("前轮侧向 (正常) - Asymptote Slip 修改值")] [SerializeField] private float m_FL_Side_Normal_AsymptoteSlipModifier = 0f;
    public float FL_Side_Normal_AsymptoteSlipModifier => m_FL_Side_Normal_AsymptoteSlipModifier;
    [Tooltip("前轮侧向 (正常) - Asymptote Value 修改值")] [SerializeField] private float m_FL_Side_Normal_AsymptoteValueModifier = 0f;
    public float FL_Side_Normal_AsymptoteValueModifier => m_FL_Side_Normal_AsymptoteValueModifier;
    [Tooltip("前轮侧向 (正常) - Stiffness 修改值")] [SerializeField] private float m_FL_Side_Normal_StiffnessModifier = 0f;
    public float FL_Side_Normal_StiffnessModifier => m_FL_Side_Normal_StiffnessModifier;

    [Header("轮胎摩擦力调整 - 后轮侧向 (正常)")]
    [Tooltip("后轮侧向 (正常) - Extremum Slip 修改值")] [SerializeField] private float m_RL_Side_Normal_ExtremumSlipModifier = 0f;
    public float RL_Side_Normal_ExtremumSlipModifier => m_RL_Side_Normal_ExtremumSlipModifier;
    [Tooltip("后轮侧向 (正常) - Extremum Value 修改值")] [SerializeField] private float m_RL_Side_Normal_ExtremumValueModifier = 0f;
    public float RL_Side_Normal_ExtremumValueModifier => m_RL_Side_Normal_ExtremumValueModifier;
    [Tooltip("后轮侧向 (正常) - Asymptote Slip 修改值")] [SerializeField] private float m_RL_Side_Normal_AsymptoteSlipModifier = 0f;
    public float RL_Side_Normal_AsymptoteSlipModifier => m_RL_Side_Normal_AsymptoteSlipModifier;
    [Tooltip("后轮侧向 (正常) - Asymptote Value 修改值")] [SerializeField] private float m_RL_Side_Normal_AsymptoteValueModifier = 0f;
    public float RL_Side_Normal_AsymptoteValueModifier => m_RL_Side_Normal_AsymptoteValueModifier;
    [Tooltip("后轮侧向 (正常) - Stiffness 修改值")] [SerializeField] private float m_RL_Side_Normal_StiffnessModifier = 0f;
    public float RL_Side_Normal_StiffnessModifier => m_RL_Side_Normal_StiffnessModifier;

    [Header("轮胎摩擦力调整 - 前轮侧向 (漂移)")]
    [Tooltip("前轮侧向 (漂移) - Extremum Slip 修改值")] [SerializeField] private float m_FL_Side_Drift_ExtremumSlipModifier = 0f;
    public float FL_Side_Drift_ExtremumSlipModifier => m_FL_Side_Drift_ExtremumSlipModifier;
    [Tooltip("前轮侧向 (漂移) - Extremum Value 修改值")] [SerializeField] private float m_FL_Side_Drift_ExtremumValueModifier = 0f;
    public float FL_Side_Drift_ExtremumValueModifier => m_FL_Side_Drift_ExtremumValueModifier;
    [Tooltip("前轮侧向 (漂移) - Asymptote Slip 修改值")] [SerializeField] private float m_FL_Side_Drift_AsymptoteSlipModifier = 0f;
    public float FL_Side_Drift_AsymptoteSlipModifier => m_FL_Side_Drift_AsymptoteSlipModifier;
    [Tooltip("前轮侧向 (漂移) - Asymptote Value 修改值")] [SerializeField] private float m_FL_Side_Drift_AsymptoteValueModifier = 0f;
    public float FL_Side_Drift_AsymptoteValueModifier => m_FL_Side_Drift_AsymptoteValueModifier;
    [Tooltip("前轮侧向 (漂移) - Stiffness 修改值")] [SerializeField] private float m_FL_Side_Drift_StiffnessModifier = 0f;
    public float FL_Side_Drift_StiffnessModifier => m_FL_Side_Drift_StiffnessModifier;

    [Header("轮胎摩擦力调整 - 后轮侧向 (漂移)")]
    [Tooltip("后轮侧向 (漂移) - Extremum Slip 修改值")] [SerializeField] private float m_RL_Side_Drift_ExtremumSlipModifier = 0f;
    public float RL_Side_Drift_ExtremumSlipModifier => m_RL_Side_Drift_ExtremumSlipModifier;
    [Tooltip("后轮侧向 (漂移) - Extremum Value 修改值")] [SerializeField] private float m_RL_Side_Drift_ExtremumValueModifier = 0f;
    public float RL_Side_Drift_ExtremumValueModifier => m_RL_Side_Drift_ExtremumValueModifier;
    [Tooltip("后轮侧向 (漂移) - Asymptote Slip 修改值")] [SerializeField] private float m_RL_Side_Drift_AsymptoteSlipModifier = 0f;
    public float RL_Side_Drift_AsymptoteSlipModifier => m_RL_Side_Drift_AsymptoteSlipModifier;
    [Tooltip("后轮侧向 (漂移) - Asymptote Value 修改值")] [SerializeField] private float m_RL_Side_Drift_AsymptoteValueModifier = 0f;
    public float RL_Side_Drift_AsymptoteValueModifier => m_RL_Side_Drift_AsymptoteValueModifier;
    [Tooltip("后轮侧向 (漂移) - Stiffness 修改值")] [SerializeField] private float m_RL_Side_Drift_StiffnessModifier = 0f;
    public float RL_Side_Drift_StiffnessModifier => m_RL_Side_Drift_StiffnessModifier;
    #endregion

    #region 氮气特定属性 (保留并应用新风格)
    [Header("氮气特定属性")]
    [Tooltip("最大氮气容量加成")]
    [SerializeField] private float m_MaxNitroCapacityBonus = 0f;
    public float MaxNitroCapacityBonus => m_MaxNitroCapacityBonus;

    [Tooltip("氮气消耗速率降低值 (正值表示消耗减慢)")]
    [SerializeField] private float m_NitroConsumptionRateReductionBonus = 0f;
    public float NitroConsumptionRateReductionBonus => m_NitroConsumptionRateReductionBonus;

    [Tooltip("氮气推进力大小加成")]
    [SerializeField] private float m_NitroForceMagnitudeBonus = 0f;
    public float NitroForceMagnitudeBonus => m_NitroForceMagnitudeBonus;

    [Tooltip("氮气恢复速率加成")]
    [SerializeField] private float m_NitroRegenerationRateBonus = 0f;
    public float NitroRegenerationRateBonus => m_NitroRegenerationRateBonus;

    [Tooltip("氮气恢复延迟缩短值 (正值表示延迟缩短)")]
    [SerializeField] private float m_NitroRegenerationDelayReductionBonus = 0f;
    public float NitroRegenerationDelayReductionBonus => m_NitroRegenerationDelayReductionBonus;
    #endregion

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/Race Game/Create Default PartDataSO Assets")]
    private static void CreateDefaultPartDataAssets()
    {
        // 确保目标目录存在
        string dirPath = "Assets/ScriptableObjects/Parts";
        if (!UnityEditor.AssetDatabase.IsValidFolder(dirPath))
        {
            UnityEditor.AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Parts");
        }

        CreateSinglePartData("Engine_BaseSO", "基础引擎", "标准引擎，性能一般。", PartCategory.Engine, PartRarity.Common, 0, true, dirPath);
        CreateSinglePartData("Tire_BaseSO", "基础轮胎", "标准轮胎，适合一般道路。", PartCategory.Tire, PartRarity.Common, 0, true, dirPath);
        CreateSinglePartData("Nitro_BaseSO", "基础氮气", "标准氮气系统。", PartCategory.Nitro, PartRarity.Common, 0, true, dirPath);
        
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
        Debug.Log($"创建了默认的 PartDataSO 资源文件到 {dirPath} 目录下。");
    }

    private static void CreateSinglePartData(string assetName, string partName, string description, PartCategory category, PartRarity rarity, int unlockPrice, bool isDefaultUnlocked, string directoryPath)
    {
        PartDataSO partData = ScriptableObject.CreateInstance<PartDataSO>();
        partData.m_PartName = partName;
        partData.m_Description = description;
        partData.m_PartCategory = category;
        partData.m_Rarity = rarity;
        partData.m_UnlockPrice = unlockPrice;
        partData.m_IsDefaultUnlocked = isDefaultUnlocked;
        
        // 为新字段设置默认值，或者根据类型设置特定基础值
        if(category == PartCategory.Engine) {
            partData.m_EngineTorqueCurve = new AnimationCurve(new Keyframe(0, 0.8f), new Keyframe(1, 1.2f)); // 示例曲线
        }
        else if (category == PartCategory.Nitro)
        {
            partData.m_NitroConsumptionRateReductionBonus = 0f;
            partData.m_NitroRegenerationDelayReductionBonus = 0f;
        }
        // ...可以为其他详细参数设置合理的默认基础值

        string path = $"{directoryPath}/{assetName}.asset";
        UnityEditor.AssetDatabase.CreateAsset(partData, path);
    }
#endif
} 