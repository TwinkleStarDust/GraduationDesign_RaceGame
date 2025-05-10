using UnityEngine;

[CreateAssetMenu(fileName = "NewVehicleData", menuName = "RaceGame/Vehicle Data", order = 52)]
public class VehicleData : ScriptableObject
{
    #region 公共字段
    [Header("车辆信息")]
    public string m_VehicleName = "新车辆";
    public GameObject m_VehiclePrefab; // 车辆的模型预制件，用于在车库中展示
    public Sprite m_VehicleIcon; // 车辆的图标，可选
    [TextArea(3, 5)]
    public string m_VehicleDescription = "车辆描述...";

    [Header("基础物理属性")]
    [Tooltip("车辆基础质量")]
    public float m_BaseMass = 1500f;
    [Tooltip("车辆基础空气阻力")]
    public float m_BaseDrag = 0.08f;
    [Tooltip("车辆基础角阻力")]
    public float m_BaseAngularDrag = 0.08f;
    [Tooltip("重心偏移量 (相对于车辆模型原点)")]
    public Vector3 m_CenterOfMassOffset = new Vector3(0, -0.5f, 0);

    [Header("Steering Behavior")]
    [Tooltip("基础最大转向角度 (度)")]
    public float m_BaseMaxSteeringAngle = 30f;
    [Tooltip("转向灵敏度曲线。X轴: 速度 (公里/小时), Y轴: 转向角度乘数 (0-1)。定义转向角度如何随速度缩放。")]
    public AnimationCurve m_SteeringSensitivityCurve = AnimationCurve.Linear(0f, 1f, 180f, 0.3f);

    [Header("基础引擎参数")]
    [Tooltip("基础最大马力扭矩")]
    public float m_BaseMaxMotorTorque = 1500f;
    [Tooltip("基础引擎动力衰减起始因子 (0.8-1.0)")]
    [Range(0.5f, 1f)]
    public float m_BaseEnginePowerFalloffStartFactor = 0.9f;
    [Tooltip("基础引擎在绝对最大速度时的马力百分比 (0.0-0.3)")]
    [Range(0f, 0.5f)]
    public float m_BaseEnginePowerAtAbsoluteMaxFactor = 0.1f;
    [Tooltip("引擎音高计算用的最大速度参考值 (公里/小时)。CarController内部会转换为m/s进行计算。")]
    public float m_MaxSpeedForPitchCalc = 180f;
    [Tooltip("引擎最小音高")]
    public float m_EngineMinPitch = 0.5f;
    [Tooltip("引擎最大音高")]
    public float m_EngineMaxPitch = 2.0f;
    [Tooltip("油门输入对引擎音高的额外影响因子")]
    public float m_EngineInputPitchFactor = 0.3f;
    [Tooltip("（可选）车辆特定的引擎声音，如果部件未提供，则使用此声音")]
    public AudioClip m_DefaultEngineSound;
    [Tooltip("（可选）车辆特定的漂移声音，如果部件未提供或CarController中未设置备用音效，则使用此声音")]
    public AudioClip m_DefaultDriftSound;
    [Tooltip("（可选）车辆特定的引擎扭矩曲线，如果部件未提供，则使用此曲线")]
    public AnimationCurve m_DefaultEngineTorqueCurve = AnimationCurve.Linear(0, 0.8f, 1, 0.6f);

    [Header("基础操控参数")]
    [Tooltip("基础刹车扭矩")]
    public float m_BaseBrakeTorque = 3000f;
    // public VehicleSteeringConfig m_SteeringConfig;

    [Header("基础漂移参数")]
    [Tooltip("漂移时后轮刹车因子 (0-1)")]
    [Range(0f, 1f)]
    public float m_DriftActivationBrakeFactor = 0.5f;
    [Tooltip("播放漂移音效所需的最小车速 (公里/小时)。CarController内部会转换为m/s进行计算。")]
    public float m_DriftMinSpeedToPlaySound = 20f;
    [Tooltip("播放漂移音效所需的最小轮胎侧向滑动绝对值")]
    public float m_MinSidewaysSlipForDriftSound = 0.3f;

    [Header("基础速度限制")]
    [Tooltip("目标引擎在正常情况下达到的最大速度 (公里/小时)")]
    public float m_TargetEngineSpeedKPH = 160f;
    [Tooltip("车辆在任何情况下能达到的绝对最大速度 (公里/小时)")]
    public float m_AbsoluteMaxSpeedKPH = 200f;

    [Header("基础氮气系统参数 (如果车辆自带氮气系统的话)")]
    [Tooltip("是否默认启用氮气系统 (即使没有装备氮气部件)")]
    public bool m_EnableNitroSystemBaseline = false;
    [Tooltip("基础最大氮气容量")]
    public float m_BaseMaxNitroCapacity = 100f;
    [Tooltip("基础每秒消耗的氮气量")]
    public float m_BaseNitroConsumptionRate = 20f;
    [Tooltip("基础氮气提供的额外推力大小")]
    public float m_BaseNitroForceMagnitude = 5000f;
    [Tooltip("基础每秒恢复的氮气量")]
    public float m_BaseNitroRegenerationRate = 5f;
    [Tooltip("基础停止使用氮气后，开始恢复的延迟时间（秒）")]
    public float m_BaseNitroRegenerationDelay = 3f;

    [Header("轮胎默认摩擦力 - 前轮前向 (Forward Friction)")]
    public WheelFrictionPreset m_FL_ForwardFrictionPreset = WheelFrictionPreset.DefaultForwardFriction;
    [Header("轮胎默认摩擦力 - 后轮前向 (Forward Friction)")]
    public WheelFrictionPreset m_RL_ForwardFrictionPreset = WheelFrictionPreset.DefaultForwardFriction;
    [Header("轮胎默认摩擦力 - 前轮侧向 (Sideways Friction - Normal)")]
    public WheelFrictionPreset m_FL_SidewaysFrictionNormalPreset = WheelFrictionPreset.DefaultNormalSideways;
    [Header("轮胎默认摩擦力 - 后轮侧向 (Sideways Friction - Normal)")]
    public WheelFrictionPreset m_RL_SidewaysFrictionNormalPreset = WheelFrictionPreset.DefaultNormalSideways;
    [Header("轮胎默认摩擦力 - 前轮侧向 (Sideways Friction - Drifting)")]
    public WheelFrictionPreset m_FL_SidewaysFrictionDriftingPreset = WheelFrictionPreset.DefaultDriftingSideways;
    [Header("轮胎默认摩擦力 - 后轮侧向 (Sideways Friction - Drifting)")]
    public WheelFrictionPreset m_RL_SidewaysFrictionDriftingPreset = WheelFrictionPreset.DefaultDriftingSideways;
    [Header("轮胎默认摩擦力 - 右轮侧向 (Sideways Friction - Drifting)")]
    public WheelFrictionPreset m_RR_SidewaysFrictionDriftingPreset = WheelFrictionPreset.DefaultDriftingSideways;

    // 你可以在这里添加更多车辆相关的基础属性，例如：
    // public float m_BaseSpeed;
    // public float m_BaseAcceleration;
    // public float m_BaseHandling;
    #endregion
}

/// <summary>
/// 用于预设 WheelFrictionCurve 值的结构体，方便在 VehicleData 中配置。
/// </summary>
[System.Serializable]
public struct WheelFrictionPreset
{
    [Tooltip("Extremum Slip 值")]
    public float extremumSlip;
    [Tooltip("Extremum Value 值")]
    public float extremumValue;
    [Tooltip("Asymptote Slip 值")]
    public float asymptoteSlip;
    [Tooltip("Asymptote Value 值")]
    public float asymptoteValue;
    [Tooltip("Stiffness 值")]
    public float stiffness;

    // 构造函数，方便快速创建预设
    public WheelFrictionPreset(float es, float ev, float aslip, float av, float stiff)
    {
        extremumSlip = es;
        extremumValue = ev;
        asymptoteSlip = aslip;
        asymptoteValue = av;
        stiffness = stiff;
    }

    // 转换为 Unity 的 WheelFrictionCurve
    public WheelFrictionCurve ToWheelFrictionCurve()
    {
        return new WheelFrictionCurve
        {
            extremumSlip = this.extremumSlip,
            extremumValue = this.extremumValue,
            asymptoteSlip = this.asymptoteSlip,
            asymptoteValue = this.asymptoteValue,
            stiffness = this.stiffness
        };
    }

    // 默认值，例如可以创建一个静态的 DefaultPreset
    public static WheelFrictionPreset DefaultNormalSideways => 
        new WheelFrictionPreset(0.2f, 1f, 0.5f, 0.75f, 1f);
    public static WheelFrictionPreset DefaultDriftingSideways => 
        new WheelFrictionPreset(0.1f, 0.7f, 0.2f, 0.5f, 0.5f);
    public static WheelFrictionPreset DefaultForwardFriction => 
        new WheelFrictionPreset(0.4f, 1f, 0.8f, 0.5f, 1f);
} 