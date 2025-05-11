using UnityEngine;

/// <summary>
/// 控制车辆行为，包括引擎、转向、刹车和漂移。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    #region 引用和配置
    [Header("核心数据引用")]
    [Tooltip("当前车辆的基础数据。在实际游戏中，这应该在车辆实例化时被设置。")]
    public VehicleData currentVehicleData; // 这个应该由外部逻辑（如车辆选择系统）设置
    [Tooltip("玩家库存数据，用于获取已装备的零件。在实际游戏中，这应该被正确引用。")]
    public PlayerInventorySO playerInventory; // 这个也应该由外部逻辑设置或找到

    [Header("轮子碰撞器")]
    [Tooltip("前左轮碰撞器")]
    public WheelCollider wheelFL;
    [Tooltip("前右轮碰撞器")]
    public WheelCollider wheelFR;
    [Tooltip("后左轮碰撞器")]
    public WheelCollider wheelRL;
    [Tooltip("后右轮碰撞器")]
    public WheelCollider wheelRR;

    [Header("轮子模型")]
    [Tooltip("前左轮的Transform")]
    public Transform wheelFLTransform;
    [Tooltip("前右轮的Transform")]
    public Transform wheelFRTransform;
    [Tooltip("后左轮的Transform")]
    public Transform wheelRLTransform;
    [Tooltip("后右轮的Transform")]
    public Transform wheelRRTransform;
    
    [Header("音效组件")]
    [Tooltip("用于播放引擎声音的AudioSource")]
    public AudioSource engineAudioSource;
    [Tooltip("用于播放漂移声音的AudioSource")]
    public AudioSource driftAudioSource;
    [Tooltip("如果车辆和部件都没有定义漂移音效，则使用这个")]
    public AudioClip fallbackDriftSoundClip;

    [Header("轮胎痕迹 Trail Renderers")]
    [Tooltip("后左轮的轮胎痕迹 Trail Renderer")]
    public TrailRenderer tireTrailRL;
    [Tooltip("后右轮的轮胎痕迹 Trail Renderer")]
    public TrailRenderer tireTrailRR;
    [Tooltip("产生轮胎痕迹所需的最小侧向滑动绝对值")]
    public float minSlipMagnitudeForTireMarks = 0.4f;
    
    [Header("音效基础设置 (AudioSource)")]
    [Tooltip("引擎声音的默认音量")]
    [Range(0f, 1f)] public float defaultEngineVolume = 0.7f;
    [Tooltip("漂移声音的默认音量")]
    [Range(0f, 1f)] public float defaultDriftVolume = 0.6f;
    [Tooltip("AudioMixer中的音效组 (例如 SFX组)")]
    public UnityEngine.Audio.AudioMixerGroup sfxAudioMixerGroup;
    [Tooltip("AudioSource的3D空间混合 (0=2D, 1=3D)")]
    [Range(0f, 1f)] public float spatialBlend = 1.0f;

    [Header("（可选）重心覆盖")]
    [Tooltip("（可选）指定一个子Transform对象作为重心参考点。如果设置，将使用此对象的localPosition覆盖centerOfMassOffset。")]
    public Transform comOverrideTarget = null;
    #endregion

    #region 内部计算后的最终生效属性
    private float m_FinalMaxMotorTorque;
    private float m_FinalMaxSteeringAngle;
    private float m_FinalBrakeTorque;
    private float m_FinalDriftActivationBrakeFactor;
    private float m_FinalTargetEngineSpeedKPH;
    private float m_FinalAbsoluteMaxSpeedKPH;
    private float m_FinalEnginePowerFalloffStartFactor;
    private float m_FinalEnginePowerAtAbsoluteMaxFactor;
    private bool m_FinalEnableNitroSystem;
    private float m_FinalMaxNitroCapacity;
    private float m_FinalNitroConsumptionRate;
    private float m_FinalNitroForceMagnitude;
    private float m_FinalNitroRegenerationRate;
    private float m_FinalNitroRegenerationDelay;
    private float m_FinalMass;
    private float m_FinalDrag;
    private float m_FinalAngularDrag;

    private WheelFrictionCurve m_FL_FwdFriction, m_FR_FwdFriction, m_RL_FwdFriction, m_RR_FwdFriction;
    private WheelFrictionCurve m_FL_SideNormalFriction, m_FR_SideNormalFriction, m_RL_SideNormalFriction, m_RR_SideNormalFriction;
    private WheelFrictionCurve m_FL_SideDriftFriction, m_FR_SideDriftFriction, m_RL_SideDriftFriction, m_RR_SideDriftFriction;
    
    private AudioClip m_FinalEngineSoundClip;
    private AudioClip m_FinalDriftSoundClip;
    private AnimationCurve m_FinalEnginePitchCurve; 
    private float m_FinalMaxSpeedForPitchCalc;
    private float m_FinalEngineMinPitch;
    private float m_FinalEngineMaxPitch;
    private float m_FinalEngineInputPitchFactor;
    private float m_FinalDriftMinSpeedToPlaySound;
    private float m_FinalMinSidewaysSlipForDriftSound;
    private float m_FinalMinSlipMagnitudeForTireMarks;

    // 新增：用于触摸输入的状态
    private bool m_TouchThrottleActive = false;
    private bool m_TouchBrakeActive = false;
    private bool m_TouchSteerLeftActive = false;
    private bool m_TouchSteerRightActive = false;
    private bool m_TouchNitroActive = false;
    #endregion

    [Header("控制参数")]
    [Tooltip("转向输入的平滑速度")]
    public float steerSmoothingSpeed = 5f;
    [Tooltip("激活氮气的按键")]
    public KeyCode nitroKey = KeyCode.LeftShift;
    [Tooltip("氮气恢复时是否允许车辆移动")]
    public bool allowRegenWhileMovingNitro = true;
    [Tooltip("如果禁止行驶时恢复氮气，车辆速度低于此值(m/s)才开始恢复")]
    public float minSpeedForNoRegenNitroConsideredStop = 0.5f;

    private float m_MotorInput;
    private float m_TargetSteerInput;
    private float m_CurrentSteerInput;
    private Rigidbody m_Rigidbody;
    private bool m_IsNitroActive = false;
    private float m_TimeSinceNitroLastUsed = 0f;
    private float currentNitroAmount;
    private bool m_IsInputDisabled = false; // 新增：用于禁用输入

    #region 公共属性 - 用于外部访问车辆状态
    public bool IsNitroSystemEnabled => m_FinalEnableNitroSystem;
    public bool IsNitroActiveAndEnabled => m_FinalEnableNitroSystem && m_IsNitroActive; // 重命名 IsNitroActivePublic 并确保逻辑正确
    public float TargetEngineSpeedKPH => m_FinalTargetEngineSpeedKPH;
    #endregion

    private void Awake()
    {
        if (!TryGetComponent<Rigidbody>(out m_Rigidbody))
        {
            Debug.LogError("CarController 未在 GameObject 上找到 Rigidbody 组件！请添加一个。", this);
            enabled = false;
            return;
        }

        LoadAndCalculateVehicleProperties();
        EnsureAudioSources();
        InitializeAudioSystemFromCalculated();

        if (m_FinalEnableNitroSystem)
        {
            currentNitroAmount = m_FinalMaxNitroCapacity;
            m_TimeSinceNitroLastUsed = m_FinalNitroRegenerationDelay; 
        }

        if (comOverrideTarget != null)
        {
            m_Rigidbody.centerOfMass = comOverrideTarget.localPosition;
        }
        else if (currentVehicleData != null) 
        {
            m_Rigidbody.centerOfMass = currentVehicleData.m_CenterOfMassOffset; 
        }

        InitializeWheelFrictionsFromCalculated(); 
        InitializeTireMarkSystemFromCalculated(); 
    }

    private void LoadAndCalculateVehicleProperties()
    {
        if (currentVehicleData == null)
        {
            Debug.LogError("CarController: currentVehicleData 未分配！车辆属性将无法正确加载。", this);
            m_FinalMaxMotorTorque = 1000f; m_FinalMaxSteeringAngle = 25f; m_FinalBrakeTorque = 2000f;
            m_FinalMass = 1400f; m_FinalDrag = 0.1f; m_FinalAngularDrag = 0.1f;
            m_FinalEnginePitchCurve = AnimationCurve.Linear(0,0.5f,1,1.5f); // 基本备用
            WheelFrictionCurve safeFriction = new WheelFrictionCurve { extremumSlip = 0.4f, extremumValue = 1f, asymptoteSlip = 0.8f, asymptoteValue = 0.5f, stiffness = 1f };
            m_FL_FwdFriction = m_FR_FwdFriction = m_RL_FwdFriction = m_RR_FwdFriction = safeFriction;
            m_FL_SideNormalFriction = m_FR_SideNormalFriction = m_RL_SideNormalFriction = m_RR_SideNormalFriction = safeFriction;
            m_FL_SideDriftFriction = m_FR_SideDriftFriction = m_RL_SideDriftFriction = m_RR_SideDriftFriction = safeFriction;
            enabled = false; 
            return;
        }

        m_FinalMaxMotorTorque = currentVehicleData.m_BaseMaxMotorTorque;
        m_FinalMaxSteeringAngle = currentVehicleData.m_BaseMaxSteeringAngle;
        m_FinalBrakeTorque = currentVehicleData.m_BaseBrakeTorque;
        m_FinalDriftActivationBrakeFactor = currentVehicleData.m_DriftActivationBrakeFactor;
        m_FinalTargetEngineSpeedKPH = currentVehicleData.m_TargetEngineSpeedKPH;
        m_FinalAbsoluteMaxSpeedKPH = currentVehicleData.m_AbsoluteMaxSpeedKPH;
        m_FinalEnginePowerFalloffStartFactor = currentVehicleData.m_BaseEnginePowerFalloffStartFactor;
        m_FinalEnginePowerAtAbsoluteMaxFactor = currentVehicleData.m_BaseEnginePowerAtAbsoluteMaxFactor;
        m_FinalMass = currentVehicleData.m_BaseMass;
        m_FinalDrag = currentVehicleData.m_BaseDrag;
        m_FinalAngularDrag = currentVehicleData.m_BaseAngularDrag;

        m_FinalEngineSoundClip = currentVehicleData.m_DefaultEngineSound;
        m_FinalDriftSoundClip = currentVehicleData.m_DefaultDriftSound;
        m_FinalEnginePitchCurve = currentVehicleData.m_DefaultEngineTorqueCurve; 
        if(m_FinalEnginePitchCurve == null || m_FinalEnginePitchCurve.length == 0) 
            m_FinalEnginePitchCurve = AnimationCurve.Linear(0, 0.5f, 1, 1.5f); 
        m_FinalMaxSpeedForPitchCalc = currentVehicleData.m_MaxSpeedForPitchCalc;
        m_FinalEngineMinPitch = currentVehicleData.m_EngineMinPitch;
        m_FinalEngineMaxPitch = currentVehicleData.m_EngineMaxPitch;
        m_FinalEngineInputPitchFactor = currentVehicleData.m_EngineInputPitchFactor;
        m_FinalDriftMinSpeedToPlaySound = currentVehicleData.m_DriftMinSpeedToPlaySound;
        m_FinalMinSidewaysSlipForDriftSound = currentVehicleData.m_MinSidewaysSlipForDriftSound;
        m_FinalMinSlipMagnitudeForTireMarks = currentVehicleData.m_MinSidewaysSlipForDriftSound * 1.2f;
        if (m_FinalMinSlipMagnitudeForTireMarks == 0) m_FinalMinSlipMagnitudeForTireMarks = 0.4f;

        m_FinalEnableNitroSystem = currentVehicleData.m_EnableNitroSystemBaseline;
        m_FinalMaxNitroCapacity = currentVehicleData.m_BaseMaxNitroCapacity;
        m_FinalNitroConsumptionRate = currentVehicleData.m_BaseNitroConsumptionRate;
        m_FinalNitroForceMagnitude = currentVehicleData.m_BaseNitroForceMagnitude;
        m_FinalNitroRegenerationRate = currentVehicleData.m_BaseNitroRegenerationRate;
        m_FinalNitroRegenerationDelay = currentVehicleData.m_BaseNitroRegenerationDelay;

        m_FL_FwdFriction = currentVehicleData.m_FL_ForwardFrictionPreset.ToWheelFrictionCurve();
        m_FR_FwdFriction = currentVehicleData.m_FL_ForwardFrictionPreset.ToWheelFrictionCurve(); 
        m_RL_FwdFriction = currentVehicleData.m_RL_ForwardFrictionPreset.ToWheelFrictionCurve();
        m_RR_FwdFriction = currentVehicleData.m_RL_ForwardFrictionPreset.ToWheelFrictionCurve(); 

        m_FL_SideNormalFriction = currentVehicleData.m_FL_SidewaysFrictionNormalPreset.ToWheelFrictionCurve();
        m_FR_SideNormalFriction = currentVehicleData.m_FL_SidewaysFrictionNormalPreset.ToWheelFrictionCurve();
        m_RL_SideNormalFriction = currentVehicleData.m_RL_SidewaysFrictionNormalPreset.ToWheelFrictionCurve();
        m_RR_SideNormalFriction = currentVehicleData.m_RL_SidewaysFrictionNormalPreset.ToWheelFrictionCurve();

        m_FL_SideDriftFriction = currentVehicleData.m_FL_SidewaysFrictionDriftingPreset.ToWheelFrictionCurve();
        m_FR_SideDriftFriction = currentVehicleData.m_FL_SidewaysFrictionDriftingPreset.ToWheelFrictionCurve();
        m_RL_SideDriftFriction = currentVehicleData.m_RL_SidewaysFrictionDriftingPreset.ToWheelFrictionCurve();
        m_RR_SideDriftFriction = currentVehicleData.m_RL_SidewaysFrictionDriftingPreset.ToWheelFrictionCurve();

        if (m_Rigidbody != null)
        {
            m_Rigidbody.mass = m_FinalMass;
            m_Rigidbody.linearDamping = m_FinalDrag;
            m_Rigidbody.angularDamping = m_FinalAngularDrag;
        }

        if (playerInventory != null)
        {
            ApplyPartModifier(playerInventory.m_EquippedEngine);
            ApplyPartModifier(playerInventory.m_EquippedTires);
            ApplyPartModifier(playerInventory.m_EquippedNOS);
        }
        
        if (m_Rigidbody != null)
        {
            m_Rigidbody.mass = m_FinalMass;
            m_Rigidbody.linearDamping = m_FinalDrag;
            m_Rigidbody.angularDamping = m_FinalAngularDrag;
        }
        
        Debug.Log($"Vehicle properties loaded. Torque: {m_FinalMaxMotorTorque}, Brake: {m_FinalBrakeTorque}, Mass: {m_FinalMass}");
    }

    private void ApplyPartModifier(PartDataSO part)
    {
        if (part == null) return;

        m_FinalMaxMotorTorque += part.MaxMotorTorqueBonus; 
        m_FinalMaxSteeringAngle += part.MaxSteeringAngleBonus;
        m_FinalBrakeTorque += part.BrakeTorqueBonus;
        m_FinalEnginePowerFalloffStartFactor += part.EnginePowerFalloffStartFactorBonus;
        m_FinalEnginePowerAtAbsoluteMaxFactor += part.EnginePowerAtAbsoluteMaxFactorBonus;
        m_FinalMass += part.MassModifier;
        m_FinalDrag += part.DragModifier;
        m_FinalAngularDrag += part.AngularDragModifier;

        m_FinalMaxMotorTorque = Mathf.Max(0, m_FinalMaxMotorTorque);
        m_FinalBrakeTorque = Mathf.Max(0, m_FinalBrakeTorque);
        m_FinalMaxSteeringAngle = Mathf.Max(5, m_FinalMaxSteeringAngle); 
        m_FinalMass = Mathf.Max(100, m_FinalMass); 
        m_FinalDrag = Mathf.Max(0.01f, m_FinalDrag);
        m_FinalAngularDrag = Mathf.Max(0.01f, m_FinalAngularDrag);
        m_FinalEnginePowerFalloffStartFactor = Mathf.Clamp(m_FinalEnginePowerFalloffStartFactor, 0.1f, 1f);
        m_FinalEnginePowerAtAbsoluteMaxFactor = Mathf.Clamp(m_FinalEnginePowerAtAbsoluteMaxFactor, 0f, 0.5f);

        switch (part.PartCategoryProperty)
        {
            case PartCategory.Engine:
                if (part.EngineSound != null) m_FinalEngineSoundClip = part.EngineSound;
                if (part.EngineTorqueCurve != null && part.EngineTorqueCurve.length > 0) m_FinalEnginePitchCurve = part.EngineTorqueCurve;
                break;

            case PartCategory.Tire:
                if (part.DriftSound != null) m_FinalDriftSoundClip = part.DriftSound;
                ModifyFrictionCurve(ref m_FL_FwdFriction, part.FL_Fwd_ExtremumSlipModifier, part.FL_Fwd_ExtremumValueModifier, part.FL_Fwd_AsymptoteSlipModifier, part.FL_Fwd_AsymptoteValueModifier, part.FL_Fwd_StiffnessModifier);
                ModifyFrictionCurve(ref m_FR_FwdFriction, part.FL_Fwd_ExtremumSlipModifier, part.FL_Fwd_ExtremumValueModifier, part.FL_Fwd_AsymptoteSlipModifier, part.FL_Fwd_AsymptoteValueModifier, part.FL_Fwd_StiffnessModifier); 
                ModifyFrictionCurve(ref m_RL_FwdFriction, part.RL_Fwd_ExtremumSlipModifier, part.RL_Fwd_ExtremumValueModifier, part.RL_Fwd_AsymptoteSlipModifier, part.RL_Fwd_AsymptoteValueModifier, part.RL_Fwd_StiffnessModifier);
                ModifyFrictionCurve(ref m_RR_FwdFriction, part.RL_Fwd_ExtremumSlipModifier, part.RL_Fwd_ExtremumValueModifier, part.RL_Fwd_AsymptoteSlipModifier, part.RL_Fwd_AsymptoteValueModifier, part.RL_Fwd_StiffnessModifier); 
                
                ModifyFrictionCurve(ref m_FL_SideNormalFriction, part.FL_Side_Normal_ExtremumSlipModifier, part.FL_Side_Normal_ExtremumValueModifier, part.FL_Side_Normal_AsymptoteSlipModifier, part.FL_Side_Normal_AsymptoteValueModifier, part.FL_Side_Normal_StiffnessModifier);
                ModifyFrictionCurve(ref m_FR_SideNormalFriction, part.FL_Side_Normal_ExtremumSlipModifier, part.FL_Side_Normal_ExtremumValueModifier, part.FL_Side_Normal_AsymptoteSlipModifier, part.FL_Side_Normal_AsymptoteValueModifier, part.FL_Side_Normal_StiffnessModifier);
                ModifyFrictionCurve(ref m_RL_SideNormalFriction, part.RL_Side_Normal_ExtremumSlipModifier, part.RL_Side_Normal_ExtremumValueModifier, part.RL_Side_Normal_AsymptoteSlipModifier, part.RL_Side_Normal_AsymptoteValueModifier, part.RL_Side_Normal_StiffnessModifier);
                ModifyFrictionCurve(ref m_RR_SideNormalFriction, part.RL_Side_Normal_ExtremumSlipModifier, part.RL_Side_Normal_ExtremumValueModifier, part.RL_Side_Normal_AsymptoteSlipModifier, part.RL_Side_Normal_AsymptoteValueModifier, part.RL_Side_Normal_StiffnessModifier);

                ModifyFrictionCurve(ref m_FL_SideDriftFriction, part.FL_Side_Drift_ExtremumSlipModifier, part.FL_Side_Drift_ExtremumValueModifier, part.FL_Side_Drift_AsymptoteSlipModifier, part.FL_Side_Drift_AsymptoteValueModifier, part.FL_Side_Drift_StiffnessModifier);
                ModifyFrictionCurve(ref m_FR_SideDriftFriction, part.FL_Side_Drift_ExtremumSlipModifier, part.FL_Side_Drift_ExtremumValueModifier, part.FL_Side_Drift_AsymptoteSlipModifier, part.FL_Side_Drift_AsymptoteValueModifier, part.FL_Side_Drift_StiffnessModifier);
                ModifyFrictionCurve(ref m_RL_SideDriftFriction, part.RL_Side_Drift_ExtremumSlipModifier, part.RL_Side_Drift_ExtremumValueModifier, part.RL_Side_Drift_AsymptoteSlipModifier, part.RL_Side_Drift_AsymptoteValueModifier, part.RL_Side_Drift_StiffnessModifier);
                ModifyFrictionCurve(ref m_RR_SideDriftFriction, part.RL_Side_Drift_ExtremumSlipModifier, part.RL_Side_Drift_ExtremumValueModifier, part.RL_Side_Drift_AsymptoteSlipModifier, part.RL_Side_Drift_AsymptoteValueModifier, part.RL_Side_Drift_StiffnessModifier);
                break;

            case PartCategory.Nitro:
                m_FinalEnableNitroSystem = true; 
                m_FinalMaxNitroCapacity += part.MaxNitroCapacityBonus;
                m_FinalNitroConsumptionRate -= part.NitroConsumptionRateReductionBonus; 
                m_FinalNitroForceMagnitude += part.NitroForceMagnitudeBonus;
                m_FinalNitroRegenerationRate += part.NitroRegenerationRateBonus;
                m_FinalNitroRegenerationDelay -= part.NitroRegenerationDelayReductionBonus; 
                
                m_FinalMaxNitroCapacity = Mathf.Max(10, m_FinalMaxNitroCapacity);
                m_FinalNitroConsumptionRate = Mathf.Max(0.1f, m_FinalNitroConsumptionRate);
                m_FinalNitroForceMagnitude = Mathf.Max(0, m_FinalNitroForceMagnitude);
                m_FinalNitroRegenerationRate = Mathf.Max(0, m_FinalNitroRegenerationRate);
                m_FinalNitroRegenerationDelay = Mathf.Max(0.05f, m_FinalNitroRegenerationDelay);
                break;
        }
    }

    private void ModifyFrictionCurve(ref WheelFrictionCurve curve, float esMod, float evMod, float asMod, float avMod, float stiffMod)
    {
        curve.extremumSlip = Mathf.Clamp(curve.extremumSlip + esMod, 0.01f, 1f);
        curve.extremumValue = Mathf.Clamp(curve.extremumValue + evMod, 0.01f, 2f); 
        curve.asymptoteSlip = Mathf.Clamp(curve.asymptoteSlip + asMod, 0.02f, 2f); 
        curve.asymptoteValue = Mathf.Clamp(curve.asymptoteValue + avMod, 0.01f, 2f); 
        curve.stiffness = Mathf.Clamp(curve.stiffness + stiffMod, 0.01f, 5f); 
    }

    private void InitializeWheelFrictionsFromCalculated()
    {
        if (wheelFL != null) { wheelFL.forwardFriction = m_FL_FwdFriction; wheelFL.sidewaysFriction = m_FL_SideNormalFriction; }
        if (wheelFR != null) { wheelFR.forwardFriction = m_FR_FwdFriction; wheelFR.sidewaysFriction = m_FR_SideNormalFriction; }
        if (wheelRL != null) { wheelRL.forwardFriction = m_RL_FwdFriction; wheelRL.sidewaysFriction = m_RL_SideNormalFriction; }
        if (wheelRR != null) { wheelRR.forwardFriction = m_RR_FwdFriction; wheelRR.sidewaysFriction = m_RR_SideNormalFriction; }
    }

    private void EnsureAudioSources()
    {
        engineAudioSource = EnsureAudioSourceComponent(engineAudioSource, "EngineAudio", defaultEngineVolume, true, m_FinalEngineSoundClip, true);
        driftAudioSource = EnsureAudioSourceComponent(driftAudioSource, "DriftAudio", defaultDriftVolume, false, m_FinalDriftSoundClip, false);
    }

    private AudioSource EnsureAudioSourceComponent(AudioSource _source, string _gameObjectNameSuffix, float _defaultVolume, bool _loop, AudioClip _defaultClipFromDataOrPart, bool _playOnAwakeIfAvailable)
    {
        bool isNewlyCreated = false;
        AudioSource sourceToUse = _source; // 使用传入的 Inspector 引用 (如果有)

        if (sourceToUse == null)
        {
            GameObject audioGO = new GameObject(gameObject.name + "_" + _gameObjectNameSuffix);
            audioGO.transform.SetParent(transform);
            audioGO.transform.localPosition = Vector3.zero;
            sourceToUse = audioGO.AddComponent<AudioSource>();
            isNewlyCreated = true;
            Debug.Log($"AudioSource for {_gameObjectNameSuffix} was not assigned in Inspector. Auto-created on {audioGO.name}.", this);
        }

        if (isNewlyCreated)
        {
            sourceToUse.volume = _defaultVolume;
            sourceToUse.spatialBlend = spatialBlend;
            sourceToUse.loop = _loop;
            sourceToUse.playOnAwake = false; 
        }
        
        if (sourceToUse.outputAudioMixerGroup == null && sfxAudioMixerGroup != null)
        {
            sourceToUse.outputAudioMixerGroup = sfxAudioMixerGroup;
        }

        // 设置 Clip 的逻辑
        AudioClip clipToAssign = _defaultClipFromDataOrPart; // 来自 VehicleData 或 Part

        if (_gameObjectNameSuffix == "DriftAudio" && clipToAssign == null) // 特殊处理漂移音效的 fallback
        {
            if (fallbackDriftSoundClip != null)
            {
                clipToAssign = fallbackDriftSoundClip;
                Debug.Log("Drift sound for " + gameObject.name + ": Using fallbackDriftSoundClip.", sourceToUse.gameObject);
            }
        }

        if (sourceToUse.clip == null || sourceToUse.clip != clipToAssign) // 仅在需要时更新clip
        {
            sourceToUse.clip = clipToAssign;
        }
        
        if (sourceToUse.clip != null)
        {
            if (_playOnAwakeIfAvailable && Application.isPlaying && !sourceToUse.isPlaying)
            {
                if(isNewlyCreated || _source.playOnAwake) 
                {
                    sourceToUse.Play();
                }
            }
        }
        else
        {
            Debug.LogWarning($"AudioClip for {_gameObjectNameSuffix} on {gameObject.name} is not set (VehicleData, Part, or Fallback). AudioSource will be silent.", sourceToUse.gameObject);
        }
        return sourceToUse;
    }

    private void InitializeAudioSystemFromCalculated()
    {
        if (engineAudioSource != null && engineAudioSource.clip != null && !engineAudioSource.isPlaying && Application.isPlaying)
        {
            engineAudioSource.Play(); // 如果引擎音效源有clip但没在播放，则播放它
        }
        // 漂移音效由UpdateAudioSystem按需播放，这里通常不需要特别处理
    }
    
    private void InitializeTireMarkSystemFromCalculated()
    {
        SetTrailEmitting(tireTrailRL, false);
        SetTrailEmitting(tireTrailRR, false);
    }

    private void Update()
    {
        if (currentVehicleData == null || m_Rigidbody == null) return;
        GetInput();
        ApplyMotorAndSteering();
        ApplyBrakesAndDrift();
        
        if (m_FinalEnableNitroSystem)
        {
            HandleNitroSystem();
        }

        UpdateWheelTransforms();
        UpdateAudioSystem();
        UpdateTireMarksSystem();
    }

    private void FixedUpdate()
    {
        if (m_Rigidbody == null || currentVehicleData == null) return;

        if (m_FinalEnableNitroSystem && m_IsNitroActive)
        {
            Vector3 nitroDirection = transform.forward; 
            m_Rigidbody.AddForce(nitroDirection * m_FinalNitroForceMagnitude, ForceMode.Force);
        }

        float absoluteMaxSpeedMS = m_FinalAbsoluteMaxSpeedKPH / 3.6f;
        if (m_Rigidbody.linearVelocity.sqrMagnitude > absoluteMaxSpeedMS * absoluteMaxSpeedMS)
        {
            m_Rigidbody.linearVelocity = m_Rigidbody.linearVelocity.normalized * absoluteMaxSpeedMS;
        }
    }
    
    private void GetInput()
    {
        if (m_IsInputDisabled) // 新增：检查输入是否被禁用
        {
            m_MotorInput = 0f;
            m_TargetSteerInput = 0f;
            // 如果有触摸状态，也在这里重置它们是个好主意，以防菜单打开时按钮仍然被"按住"
            m_TouchThrottleActive = false;
            m_TouchBrakeActive = false;
            m_TouchSteerLeftActive = false;
            m_TouchSteerRightActive = false;
            m_TouchNitroActive = false;
            return;
        }

        float keyboardVerticalInput = Input.GetAxis("Vertical");
        float keyboardHorizontalInput = Input.GetAxis("Horizontal");

        // 优先处理触摸油门/刹车输入
        if (m_TouchThrottleActive && !m_TouchBrakeActive)
        {
            m_MotorInput = 1.0f;
        }
        else if (m_TouchBrakeActive && !m_TouchThrottleActive)
        {
            m_MotorInput = -1.0f;
        }
        else if (m_TouchThrottleActive && m_TouchBrakeActive) // 同时按油门和刹车
        {
            m_MotorInput = 0.0f;
        }
        else
        {
            // 没有活动的触摸油门/刹车输入，使用键盘输入
            m_MotorInput = keyboardVerticalInput;
        }

        // 处理触摸转向输入
        if (m_TouchSteerLeftActive && !m_TouchSteerRightActive)
        {
            m_TargetSteerInput = -1.0f;
        }
        else if (m_TouchSteerRightActive && !m_TouchSteerLeftActive)
        {
            m_TargetSteerInput = 1.0f;
        }
        else if (m_TouchSteerLeftActive && m_TouchSteerRightActive) // 同时按左右转向
        {
            m_TargetSteerInput = 0.0f;
        }
        else
        {
            // 没有活动的触摸转向输入，使用键盘输入
            m_TargetSteerInput = keyboardHorizontalInput;
        }
    }

    private void ApplyMotorAndSteering()
    {
        float currentSpeedMS = GetCurrentForwardSpeedMS(); // 这个方法获取的是 m/s
        float currentSpeedKPH = currentSpeedMS * 3.6f;    // 转换为 km/h
        float targetEngineSpeedMS = m_FinalTargetEngineSpeedKPH / 3.6f;
        float absoluteLimitSpeedMS = m_FinalAbsoluteMaxSpeedKPH / 3.6f;

        m_CurrentSteerInput = Mathf.Lerp(m_CurrentSteerInput, m_TargetSteerInput, Time.deltaTime * steerSmoothingSpeed);

        float steerFactor = 1.0f;
        if (currentVehicleData != null && currentVehicleData.m_SteeringSensitivityCurve != null && currentVehicleData.m_SteeringSensitivityCurve.length > 0)
        {
            // 使用转换后的 km/h 速度来评估曲线
            steerFactor = currentVehicleData.m_SteeringSensitivityCurve.Evaluate(currentSpeedKPH);
        }
        
        float finalSteerAngle = m_CurrentSteerInput * m_FinalMaxSteeringAngle * steerFactor;

        float actualAppliedMotorTorque;
        if (m_MotorInput > 0 && currentSpeedMS > targetEngineSpeedMS * m_FinalEnginePowerFalloffStartFactor)
        {
            float falloffRangeStartSpeed = targetEngineSpeedMS * m_FinalEnginePowerFalloffStartFactor;
            float range = absoluteLimitSpeedMS - falloffRangeStartSpeed;
            float speedIntoFalloffRange = currentSpeedMS - falloffRangeStartSpeed;
            float falloffRatio = 0f;
            if (range > 0.01f) falloffRatio = Mathf.Clamp01(speedIntoFalloffRange / range);
            else if (currentSpeedMS >= absoluteLimitSpeedMS) falloffRatio = 1f;
            
            float torqueMultiplier = Mathf.Lerp(1.0f, m_FinalEnginePowerAtAbsoluteMaxFactor, falloffRatio);
            actualAppliedMotorTorque = m_MotorInput * m_FinalMaxMotorTorque * torqueMultiplier;
        }
        else
        {
            actualAppliedMotorTorque = m_MotorInput * m_FinalMaxMotorTorque;
        }

        if(wheelRL != null) wheelRL.motorTorque = actualAppliedMotorTorque;
        if(wheelRR != null) wheelRR.motorTorque = actualAppliedMotorTorque;
        if(wheelFL != null) wheelFL.motorTorque = 0; 
        if(wheelFR != null) wheelFR.motorTorque = 0;

        if(wheelFL != null) wheelFL.steerAngle = finalSteerAngle;
        if(wheelFR != null) wheelFR.steerAngle = finalSteerAngle;
    }

    private void ApplyBrakesAndDrift()
    {
        bool isKeyboardDriftIntent = Input.GetKey(KeyCode.Space);
        bool isTouchBrakeButtonPressed = m_TouchBrakeActive; // 玩家是否按下了触摸刹车按钮

        Vector3 localVelocity = transform.InverseTransformDirection(m_Rigidbody.linearVelocity);
        float forwardSpeed = localVelocity.z;
        bool applyStandardBrakes = false;

        // 标准刹车逻辑：当输入方向与当前速度方向相反时，或者漂移意图激活且有一定速度时
        if (Mathf.Abs(forwardSpeed) > 0.1f && Mathf.Sign(m_MotorInput) == -Mathf.Sign(forwardSpeed))
        {
            applyStandardBrakes = true;
        }

        // 玩家是否明确按下了用于漂移/手刹的按钮 (键盘或触摸)
        bool playerWantsToDriftOrHardBrake = isKeyboardDriftIntent || isTouchBrakeButtonPressed;

        if (m_MotorInput < -0.01f) // 优先处理明确的倒车马达输入
        {
            ApplyDriftFrictionSettings(false); // 倒车时使用正常摩擦力
            // 当m_MotorInput为负时，GetInput已经因为isTouchBrakeButtonPressed=true而设置了它
            // 此时，马达扭矩已经是负的，负责倒车。
            // 我们只需要施加正常的刹车（主要是在从前进切换到倒车时，或者倒车时微调）
            // 如果isTouchBrakeButtonPressed为true，并且m_MotorInput为负，我们不希望后轮有额外的漂移刹车。
            float brakeValue = applyStandardBrakes ? m_FinalBrakeTorque : 0f;
            if(wheelFL != null) wheelFL.brakeTorque = brakeValue;
            if(wheelFR != null) wheelFR.brakeTorque = brakeValue;
            if(wheelRL != null) wheelRL.brakeTorque = brakeValue; // 后轮也使用标准刹车，而不是漂移刹车
            if(wheelRR != null) wheelRR.brakeTorque = brakeValue;
        }
        else if (playerWantsToDriftOrHardBrake) // 玩家按下了漂移/手刹键，并且没有负的马达输入 (即不是主要想倒车)
        {
            ApplyDriftFrictionSettings(true); // 激活漂移摩擦力设置
            // 后轮施加漂移/手刹制动力
            if(wheelRL != null) wheelRL.brakeTorque = m_FinalBrakeTorque * m_FinalDriftActivationBrakeFactor;
            if(wheelRR != null) wheelRR.brakeTorque = m_FinalBrakeTorque * m_FinalDriftActivationBrakeFactor;
            // 前轮的刹车可以基于标准刹车逻辑，在漂移时通常较轻或根据需求调整
            float frontBrakeFactor = (applyStandardBrakes && m_MotorInput >=0) ? 0.2f : 0f; // 如果同时踩油门或空档按刹车，前轮轻刹
            if(wheelFL != null) wheelFL.brakeTorque = m_FinalBrakeTorque * frontBrakeFactor;
            if(wheelFR != null) wheelFR.brakeTorque = m_FinalBrakeTorque * frontBrakeFactor;
        }
        else // 没有倒车马达输入，也没有按下漂移/手刹键
        {
            ApplyDriftFrictionSettings(false);
            float currentBrake = applyStandardBrakes ? m_FinalBrakeTorque : 0f;
            if(wheelFL != null) wheelFL.brakeTorque = currentBrake;
            if(wheelFR != null) wheelFR.brakeTorque = currentBrake;
            if(wheelRL != null) wheelRL.brakeTorque = currentBrake;
            if(wheelRR != null) wheelRR.brakeTorque = currentBrake;
        }
    }

    private void ApplyDriftFrictionSettings(bool _isDrifting)
    {
        if (_isDrifting)
        {
            if (wheelFL != null) wheelFL.sidewaysFriction = m_FL_SideDriftFriction;
            if (wheelFR != null) wheelFR.sidewaysFriction = m_FR_SideDriftFriction;
            if (wheelRL != null) wheelRL.sidewaysFriction = m_RL_SideDriftFriction;
            if (wheelRR != null) wheelRR.sidewaysFriction = m_RR_SideDriftFriction;
        }
        else
        {
            if (wheelFL != null) wheelFL.sidewaysFriction = m_FL_SideNormalFriction;
            if (wheelFR != null) wheelFR.sidewaysFriction = m_FR_SideNormalFriction;
            if (wheelRL != null) wheelRL.sidewaysFriction = m_RL_SideNormalFriction;
            if (wheelRR != null) wheelRR.sidewaysFriction = m_RR_SideNormalFriction;
        }
    }

    private void UpdateWheelTransforms()
    {
        UpdateSingleWheelAndTrail(wheelFL, wheelFLTransform, null);     
        UpdateSingleWheelAndTrail(wheelFR, wheelFRTransform, null);     
        UpdateSingleWheelAndTrail(wheelRL, wheelRLTransform, tireTrailRL);
        UpdateSingleWheelAndTrail(wheelRR, wheelRRTransform, tireTrailRR);
    }

    private void UpdateSingleWheelAndTrail(WheelCollider wheelCollider, Transform wheelVisualTransform, TrailRenderer tireTrail)
    {
        if (wheelCollider == null) return;
        Vector3 position;
        Quaternion rotation;
        wheelCollider.GetWorldPose(out position, out rotation); 
        if (wheelVisualTransform != null)
        {
            wheelVisualTransform.position = position;
            wheelVisualTransform.rotation = rotation;
        }
        if (tireTrail != null && tireTrail.gameObject != null) 
        {
            WheelHit hit;
            if (wheelCollider.GetGroundHit(out hit) && wheelCollider.isGrounded)
            {
                tireTrail.transform.position = hit.point + hit.normal * 0.01f; 
                Vector3 forwardOnGround = Vector3.ProjectOnPlane(transform.forward, hit.normal).normalized;
                if (forwardOnGround == Vector3.zero) 
                {
                    forwardOnGround = Vector3.ProjectOnPlane(transform.right, hit.normal).normalized;
                    if (forwardOnGround == Vector3.zero) 
                    {
                        forwardOnGround = Vector3.ProjectOnPlane(Vector3.forward, hit.normal).normalized;
                    }
                }
                if (forwardOnGround != Vector3.zero) 
                {
                    tireTrail.transform.rotation = Quaternion.LookRotation(forwardOnGround, hit.normal);
                }
                else
                {
                    tireTrail.transform.up = hit.normal;
                }
            }
        }
    }

    private void UpdateAudioSystem()
    {
        if (engineAudioSource != null && engineAudioSource.isPlaying && m_FinalEnginePitchCurve != null && m_FinalEnginePitchCurve.length > 0)
        {
            float maxSpeedForPitchCalcMS = m_FinalMaxSpeedForPitchCalc / 3.6f; // 将km/h转换为m/s
            float normalizedSpeed = 0f;
            if (maxSpeedForPitchCalcMS > 0.01f) // 避免除以零
            {
                normalizedSpeed = Mathf.Clamp01(m_Rigidbody.linearVelocity.magnitude / maxSpeedForPitchCalcMS);
            }
            else if (m_Rigidbody.linearVelocity.magnitude > 0) // 如果参考速度为0但车在动，则认为是最大
            {
                normalizedSpeed = 1f;
            }
            
            float targetPitch = m_FinalEnginePitchCurve.Evaluate(normalizedSpeed); 
            float actualMotorInput = Mathf.Abs(m_MotorInput);
            targetPitch += actualMotorInput * m_FinalEngineInputPitchFactor; 
            targetPitch = Mathf.Clamp(targetPitch, m_FinalEngineMinPitch, m_FinalEngineMaxPitch);
            engineAudioSource.pitch = Mathf.Lerp(engineAudioSource.pitch, targetPitch, Time.deltaTime * 5f); 
        }

        float driftMinSpeedToPlaySoundMS = m_FinalDriftMinSpeedToPlaySound / 3.6f; // 将km/h转换为m/s
        bool vehicleIsMovingFastEnough = m_Rigidbody.linearVelocity.magnitude > driftMinSpeedToPlaySoundMS;

        if (driftAudioSource != null && m_FinalDriftSoundClip != null) 
        {
            bool shouldPlayDriftSound = false;
            float combinedSidewaysSlip = 0f;
            int groundedWheelsContributingToSlip = 0;
            WheelHit hit;
            if (wheelRL != null && wheelRL.GetGroundHit(out hit) && wheelRL.isGrounded) { combinedSidewaysSlip += Mathf.Abs(hit.sidewaysSlip); groundedWheelsContributingToSlip++; }
            if (wheelRR != null && wheelRR.GetGroundHit(out hit) && wheelRR.isGrounded) { combinedSidewaysSlip += Mathf.Abs(hit.sidewaysSlip); groundedWheelsContributingToSlip++; }
            float averageSidewaysSlip = (groundedWheelsContributingToSlip > 0) ? combinedSidewaysSlip / groundedWheelsContributingToSlip : 0;

            if (vehicleIsMovingFastEnough)
            {
                float slipThresholdForSound = m_FinalMinSidewaysSlipForDriftSound;
                bool playerWantsToDrift = Input.GetKey(KeyCode.Space) || m_TouchBrakeActive; // 检查键盘或触摸漂移意图
                if (playerWantsToDrift)
                {
                    slipThresholdForSound *= 0.15f;
                }
                if (averageSidewaysSlip > slipThresholdForSound)
                {
                    shouldPlayDriftSound = true;
                }
            }

            if (shouldPlayDriftSound)
            {
                if (!driftAudioSource.isPlaying) driftAudioSource.Play();
            }
            else
            {
                if (driftAudioSource.isPlaying) driftAudioSource.Stop();
            }
        }
    }

    private void UpdateTireMarksSystem()
    {
        bool isDriftingActiveIntentByKeyboard = Input.GetKey(KeyCode.Space);
        bool isDriftingActiveIntentByTouch = m_TouchBrakeActive; // 假设触摸刹车也触发漂移轮胎痕迹
        bool isDriftingActiveIntent = isDriftingActiveIntentByKeyboard || isDriftingActiveIntentByTouch;

        HandleSingleTireTrail(wheelRL, tireTrailRL, isDriftingActiveIntent);
        HandleSingleTireTrail(wheelRR, tireTrailRR, isDriftingActiveIntent);
    }

    private void HandleSingleTireTrail(WheelCollider wheel, TrailRenderer trail, bool playerWantsToDrift)
    {
        if (trail == null || wheel == null) return;

        bool shouldBeEmitting = false; 
        if (wheel.GetGroundHit(out WheelHit hit) && wheel.isGrounded) 
        {
            float actualSidewaysSlip = Mathf.Abs(hit.sidewaysSlip);
            if (playerWantsToDrift)
            {
                float playerDriftSlipThreshold = m_FinalMinSlipMagnitudeForTireMarks * 0.1f; 
                if (actualSidewaysSlip > playerDriftSlipThreshold && m_Rigidbody.linearVelocity.magnitude > 0.5f) 
                {
                    shouldBeEmitting = true;
                }
            }
            else 
            {
                bool significantSidewaysSlipForNaturalDrift = actualSidewaysSlip > m_FinalMinSlipMagnitudeForTireMarks;
                bool strongSteering = Mathf.Abs(Input.GetAxis("Horizontal")) > 0.7f; 
                if ((strongSteering && significantSidewaysSlipForNaturalDrift) || actualSidewaysSlip > m_FinalMinSlipMagnitudeForTireMarks * 1.5f) 
                {
                    shouldBeEmitting = true;
                }
            }
            if (shouldBeEmitting != trail.emitting) trail.emitting = shouldBeEmitting;
        }
        else 
        {
            if (trail.emitting) trail.emitting = false;
        }
    }

    private void SetTrailEmitting(TrailRenderer trail, bool isEmitting)
    {
        if (trail != null)
        {
            trail.emitting = isEmitting;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (Application.isPlaying && m_Rigidbody != null)
        {
            Gizmos.color = Color.red; 
            Gizmos.DrawSphere(m_Rigidbody.worldCenterOfMass, 0.1f);
        }
        else if (currentVehicleData != null) 
        {
            Vector3 previewLocalCom;
            Color previewColor;
            if (comOverrideTarget != null)
            {
                previewLocalCom = comOverrideTarget.localPosition;
                previewColor = Color.cyan; 
            }
            else
            {
                previewLocalCom = currentVehicleData.m_CenterOfMassOffset;
                previewColor = Color.yellow; 
            }
            if (transform != null) 
            {
                Gizmos.color = previewColor;
                Gizmos.DrawSphere(transform.TransformPoint(previewLocalCom), 0.1f);
            }
        }
    }
#endif

    public float GetCurrentForwardSpeedMS()
    {
        if (m_Rigidbody == null) 
        { 
            Debug.LogWarning("CarController: Rigidbody is not available to get current speed.", this);
            return 0f; 
        }
        return Vector3.Dot(m_Rigidbody.linearVelocity, transform.forward);
    }

    public float GetCurrentNitroNormalized()
    {
        if (!m_FinalEnableNitroSystem || m_FinalMaxNitroCapacity <= 0) return 0f;
        return currentNitroAmount / m_FinalMaxNitroCapacity;
    }

    public float GetCurrentNitroAbsolute()
    {
        if (!m_FinalEnableNitroSystem) return 0f;
        return currentNitroAmount;
    }

    public float GetMaxNitroCapacity()
    {
        if (!m_FinalEnableNitroSystem) return 0f; 
        return m_FinalMaxNitroCapacity;
    }

    public void AddNitro(float amount)
    {
        if (!m_FinalEnableNitroSystem) return;
        currentNitroAmount += amount;
        currentNitroAmount = Mathf.Clamp(currentNitroAmount, 0, m_FinalMaxNitroCapacity);
    }

    private void HandleNitroSystem()
    {
        bool keyboardNitroPressed = Input.GetKey(nitroKey);
        bool touchNitroPressed = m_TouchNitroActive;

        if ((keyboardNitroPressed || touchNitroPressed) && currentNitroAmount > 0)
        {
            m_IsNitroActive = true;
            currentNitroAmount -= m_FinalNitroConsumptionRate * Time.deltaTime;
            currentNitroAmount = Mathf.Max(0, currentNitroAmount);
            m_TimeSinceNitroLastUsed = 0f;
        }
        else
        {
            m_IsNitroActive = false;
        }

        if (currentNitroAmount <= 0) m_IsNitroActive = false;

        if (!m_IsNitroActive && currentNitroAmount < m_FinalMaxNitroCapacity)
        {
            m_TimeSinceNitroLastUsed += Time.deltaTime;
            if (m_TimeSinceNitroLastUsed >= m_FinalNitroRegenerationDelay)
            {
                bool canRegenerateNow = false;
                if (allowRegenWhileMovingNitro) canRegenerateNow = true;
                else if (m_Rigidbody != null && m_Rigidbody.linearVelocity.magnitude < minSpeedForNoRegenNitroConsideredStop) canRegenerateNow = true;

                if (canRegenerateNow)
                {
                    currentNitroAmount += m_FinalNitroRegenerationRate * Time.deltaTime;
                    currentNitroAmount = Mathf.Min(currentNitroAmount, m_FinalMaxNitroCapacity);
                }
            }
        }
    }

    #region 公共方法 - 用于外部控制车辆状态
    /// <summary>
    /// 设置触摸油门状态。
    /// </summary>
    /// <param name="active">是否激活油门。</param>
    public void SetTouchThrottle(bool active)
    {
        m_TouchThrottleActive = active;
    }

    /// <summary>
    /// 设置触摸刹车/倒车状态。
    /// </summary>
    /// <param name="active">是否激活刹车/倒车。</param>
    public void SetTouchBrake(bool active)
    {
        m_TouchBrakeActive = active;
    }

    /// <summary>
    /// 设置触摸左转状态。
    /// </summary>
    /// <param name="active">是否激活左转。</param>
    public void SetTouchSteerLeft(bool active)
    {
        m_TouchSteerLeftActive = active;
    }

    /// <summary>
    /// 设置触摸右转状态。
    /// </summary>
    /// <param name="active">是否激活右转。</param>
    public void SetTouchSteerRight(bool active)
    {
        m_TouchSteerRightActive = active;
    }

    /// <summary>
    /// 设置触摸氮气状态。
    /// </summary>
    /// <param name="active">是否激活氮气。</param>
    public void SetTouchNitro(bool active)
    {
        m_TouchNitroActive = active;
    }

    /// <summary>
    /// 设置是否禁用玩家控制输入。
    /// </summary>
    /// <param name="isDisabled">如果为true，则禁用输入；否则启用。</param>
    public void SetInputDisabled(bool isDisabled)
    {
        m_IsInputDisabled = isDisabled;
    }
    #endregion
}
