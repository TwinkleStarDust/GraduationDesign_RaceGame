using UnityEngine;
using System;
using System.Collections.Generic;
using Vehicle;

/// <summary>
/// 零部件升级系统 - 管理零部件解锁与装备
/// </summary>
public class PartUpgradeSystem : MonoBehaviour
{
    #region 单例实现
    private static PartUpgradeSystem s_Instance;
    public static PartUpgradeSystem Instance
    {
        get
        {
            if (s_Instance == null)
            {
                GameObject managerObj = GameObject.Find("PartUpgradeSystem");
                if (managerObj == null)
                {
                    managerObj = new GameObject("PartUpgradeSystem");
                    s_Instance = managerObj.AddComponent<PartUpgradeSystem>();
                }
                else
                {
                    s_Instance = managerObj.GetComponent<PartUpgradeSystem>();
                    if (s_Instance == null)
                    {
                        s_Instance = managerObj.AddComponent<PartUpgradeSystem>();
                    }
                }
                DontDestroyOnLoad(managerObj);
            }
            return s_Instance;
        }
    }
    #endregion

    #region 事件定义
    // 零部件解锁事件
    public event Action<PartDataSO> OnPartUnlocked;
    // 零部件装备事件
    public event Action<PartCategory, PartDataSO> OnPartEquipped;
    #endregion

    #region 序列化字段
    [Header("零部件设置")]
    [Tooltip("所有可用零部件")]
    [SerializeField] private List<PartDataSO> m_AllParts = new List<PartDataSO>();

    [Header("持久化设置")]
    [Tooltip("已解锁零部件的PlayerPrefs键")]
    [SerializeField] private string m_UnlockedPartsKey = "UnlockedParts";

    [Tooltip("已装备零部件的PlayerPrefs键")]
    [SerializeField] private string m_EquippedPartsKey = "EquippedParts";

    [Tooltip("是否自动保存")]
    [SerializeField] private bool m_AutoSave = true;
    #endregion

    #region 私有变量
    // 已解锁的零部件ID集合
    private HashSet<string> m_UnlockedPartIDs = new HashSet<string>();

    // 当前装备的零部件
    private Dictionary<PartCategory, PartDataSO> m_EquippedParts = new Dictionary<PartCategory, PartDataSO>();

    // 缓存零部件查找
    private Dictionary<string, PartDataSO> m_PartIDToData = new Dictionary<string, PartDataSO>();
    private Dictionary<PartCategory, List<PartDataSO>> m_PartsByCategory = new Dictionary<PartCategory, List<PartDataSO>>();
    #endregion

    #region Unity生命周期
    private void Awake()
    {
        // 单例实现检查
        if (s_Instance != null && s_Instance != this)
        {
            Destroy(this);
            return;
        }

        s_Instance = this;
        DontDestroyOnLoad(gameObject);

        // 初始化缓存
        InitializeCache();

        // 加载已解锁和已装备的零部件
        LoadData();
    }

    private void OnApplicationQuit()
    {
        // 应用退出时保存数据
        if (m_AutoSave)
        {
            SaveData();
        }
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 解锁零部件
    /// </summary>
    public bool UnlockPart(string partID)
    {
        if (string.IsNullOrEmpty(partID))
        {
            Debug.LogError("解锁失败: 无效的零部件ID");
            return false;
        }

        // 检查零部件是否存在
        if (!m_PartIDToData.TryGetValue(partID, out PartDataSO partData))
        {
            Debug.LogError($"解锁失败: 找不到ID为 {partID} 的零部件");
            return false;
        }

        // 检查是否已经解锁
        if (m_UnlockedPartIDs.Contains(partID))
        {
            Debug.LogWarning($"零部件 {partID} 已经解锁");
            return false;
        }

        // 如果需要支付解锁费用
        if (partData.UnlockPrice > 0)
        {
            if (EconomySystem.Instance == null || !EconomySystem.Instance.SpendMoney(partData.UnlockPrice, $"解锁零部件 {partData.PartName}"))
            {
                Debug.LogWarning($"解锁零部件 {partID} 失败: 金钱不足");
                return false;
            }
        }

        // 添加到已解锁集合
        m_UnlockedPartIDs.Add(partID);

        // 触发解锁事件
        OnPartUnlocked?.Invoke(partData);

        // 自动保存
        if (m_AutoSave)
        {
            SaveData();
        }

        Debug.Log($"成功解锁零部件: {partData.PartName}");
        return true;
    }

    /// <summary>
    /// 装备零部件
    /// </summary>
    public bool EquipPart(string partID)
    {
        if (string.IsNullOrEmpty(partID))
        {
            Debug.LogError("装备失败: 无效的零部件ID");
            return false;
        }

        // 检查零部件是否存在
        if (!m_PartIDToData.TryGetValue(partID, out PartDataSO partData))
        {
            Debug.LogError($"装备失败: 找不到ID为 {partID} 的零部件");
            return false;
        }

        // 检查是否已解锁
        if (!IsPartUnlocked(partID))
        {
            Debug.LogWarning($"装备失败: 零部件 {partID} 尚未解锁");
            return false;
        }

        // 获取零部件类型
        PartCategory category = partData.PartCategory;

        // 装备零部件
        m_EquippedParts[category] = partData;

        // 触发装备事件
        OnPartEquipped?.Invoke(category, partData);

        // 自动保存
        if (m_AutoSave)
        {
            SaveData();
        }

        Debug.Log($"成功装备零部件: {partData.PartName}");
        return true;
    }

    /// <summary>
    /// 检查零部件是否已解锁
    /// </summary>
    public bool IsPartUnlocked(string partID)
    {
        // 检查是否默认解锁
        if (m_PartIDToData.TryGetValue(partID, out PartDataSO partData) && partData.IsDefaultUnlocked)
        {
            return true;
        }

        return m_UnlockedPartIDs.Contains(partID);
    }

    /// <summary>
    /// 获取指定类型的当前装备零部件
    /// </summary>
    public PartDataSO GetEquippedPart(PartCategory category)
    {
        if (m_EquippedParts.TryGetValue(category, out PartDataSO partData))
        {
            return partData;
        }

        // 如果没有装备，返回默认零部件
        return GetDefaultPart(category);
    }

    /// <summary>
    /// 获取指定类型的默认零部件
    /// </summary>
    public PartDataSO GetDefaultPart(PartCategory category)
    {
        if (m_PartsByCategory.TryGetValue(category, out List<PartDataSO> parts))
        {
            // 查找该类型的默认零部件
            foreach (PartDataSO part in parts)
            {
                if (part.IsDefaultUnlocked)
                {
                    return part;
                }
            }

            // 如果没有默认解锁的，返回第一个
            if (parts.Count > 0)
            {
                return parts[0];
            }
        }

        Debug.LogError($"没有找到类型为 {category} 的默认零部件");
        return null;
    }

    /// <summary>
    /// 获取指定类型的所有已解锁零部件
    /// </summary>
    public List<PartDataSO> GetUnlockedPartsByCategory(PartCategory category)
    {
        List<PartDataSO> unlockedParts = new List<PartDataSO>();

        if (m_PartsByCategory.TryGetValue(category, out List<PartDataSO> allPartsInCategory))
        {
            foreach (PartDataSO part in allPartsInCategory)
            {
                if (IsPartUnlocked(part.PartID))
                {
                    unlockedParts.Add(part);
                }
            }
        }

        return unlockedParts;
    }

    /// <summary>
    /// 获取指定类型的所有未解锁零部件
    /// </summary>
    public List<PartDataSO> GetLockedPartsByCategory(PartCategory category)
    {
        List<PartDataSO> lockedParts = new List<PartDataSO>();

        if (m_PartsByCategory.TryGetValue(category, out List<PartDataSO> allPartsInCategory))
        {
            foreach (PartDataSO part in allPartsInCategory)
            {
                if (!IsPartUnlocked(part.PartID))
                {
                    lockedParts.Add(part);
                }
            }
        }

        return lockedParts;
    }

    /// <summary>
    /// 获取所有零部件
    /// </summary>
    public List<PartDataSO> GetAllParts()
    {
        return new List<PartDataSO>(m_AllParts);
    }

    /// <summary>
    /// 应用当前装备的零部件性能到车辆
    /// </summary>
    public void ApplyEquippedPartsToVehicle(Vehicle.VehicleDriveSystem driveSystem, Vehicle.VehiclePhysics physics)
    {
        if (driveSystem == null || physics == null)
        {
            Debug.LogError("应用零部件性能失败: 车辆组件为空");
            return;
        }

        // 创建性能修改器
        PerformanceModifiers modifiers = new PerformanceModifiers();

        // 应用每种类型的装备零部件性能
        foreach (PartCategory category in Enum.GetValues(typeof(PartCategory)))
        {
            PartDataSO equippedPart = GetEquippedPart(category);
            if (equippedPart != null)
            {
                ApplyPartModifiers(equippedPart, ref modifiers);
            }
        }

        // 将修改应用到车辆
        ApplyModifiersToVehicle(modifiers, driveSystem, physics);

        Debug.Log("已应用所有装备零部件性能到车辆");
    }

    /// <summary>
    /// 重置所有解锁与装备数据（调试用）
    /// </summary>
    public void ResetAllData()
    {
        m_UnlockedPartIDs.Clear();
        m_EquippedParts.Clear();

        // 初始化默认解锁和装备
        InitializeDefaultData();

        // 保存数据
        SaveData();

        Debug.Log("已重置所有零部件数据");
    }
    #endregion

    #region 私有方法
    /// <summary>
    /// 初始化缓存
    /// </summary>
    private void InitializeCache()
    {
        m_PartIDToData.Clear();
        m_PartsByCategory.Clear();

        // 初始化各类型的列表
        foreach (PartCategory category in Enum.GetValues(typeof(PartCategory)))
        {
            m_PartsByCategory[category] = new List<PartDataSO>();
        }

        // 缓存零部件数据
        foreach (PartDataSO part in m_AllParts)
        {
            if (part != null)
            {
                // 按ID索引
                if (!string.IsNullOrEmpty(part.PartID))
                {
                    m_PartIDToData[part.PartID] = part;
                }
                else
                {
                    Debug.LogWarning($"零部件 {part.name} 没有设置ID");
                }

                // 按类型分组
                if (m_PartsByCategory.TryGetValue(part.PartCategory, out List<PartDataSO> categoryList))
                {
                    categoryList.Add(part);
                }
            }
        }
    }

    /// <summary>
    /// 初始化默认数据
    /// </summary>
    private void InitializeDefaultData()
    {
        // 解锁所有默认零部件
        foreach (PartDataSO part in m_AllParts)
        {
            if (part != null && part.IsDefaultUnlocked)
            {
                m_UnlockedPartIDs.Add(part.PartID);
            }
        }

        // 为每种类型装备默认零部件
        foreach (PartCategory category in Enum.GetValues(typeof(PartCategory)))
        {
            PartDataSO defaultPart = GetDefaultPart(category);
            if (defaultPart != null)
            {
                m_EquippedParts[category] = defaultPart;
            }
        }
    }

    /// <summary>
    /// 应用零部件性能修改器
    /// </summary>
    private void ApplyPartModifiers(PartDataSO part, ref PerformanceModifiers modifiers)
    {
        // 应用通用修改器
        modifiers.SpeedModifier += part.SpeedModifier;
        modifiers.AccelerationModifier += part.AccelerationModifier;
        modifiers.HandlingModifier += part.HandlingModifier;
        modifiers.BrakeForceModifier += part.BrakeForceModifier;

        // 根据零部件类型应用特定修改器
        switch (part.PartCategory)
        {
            case PartCategory.Tire:
                modifiers.TireFrictionModifier += part.TireFrictionModifier;
                modifiers.WetPerformanceModifier += part.WetPerformanceModifier;
                break;

            case PartCategory.Engine:
                modifiers.HasCustomEngineCurve = part.EngineTorqueCurve != null;
                modifiers.EngineTorqueCurve = part.EngineTorqueCurve;
                modifiers.EngineSound = part.EngineSound;
                break;

            case PartCategory.Nitro:
                modifiers.NitroCapacityModifier += part.NitroCapacityModifier;
                modifiers.NitroEfficiencyModifier += part.NitroEfficiencyModifier;
                modifiers.NitroRecoveryModifier += part.NitroRecoveryModifier;
                break;
        }
    }

    /// <summary>
    /// 将性能修改器应用到车辆
    /// </summary>
    private void ApplyModifiersToVehicle(PerformanceModifiers modifiers, Vehicle.VehicleDriveSystem driveSystem, Vehicle.VehiclePhysics physics)
    {
        if (driveSystem == null || physics == null)
        {
            Debug.LogError("无法应用性能修改：驱动系统或物理系统为空");
            return;
        }

        // 添加必要的公共方法到VehicleDriveSystem和VehiclePhysics类中

        // 速度修改
        float maxSpeedModifier = 1f + modifiers.SpeedModifier / 100f;
        // 需要在VehicleDriveSystem中添加以下方法:
        // public void SetMaxSpeedModifier(float modifier)
        // {
        //     maxSpeed *= modifier;
        // }

        // 加速度修改
        float accelerationModifier = 1f + modifiers.AccelerationModifier / 100f;
        // 需要在VehicleDriveSystem中添加以下方法:
        // public void SetAccelerationModifier(float modifier)
        // {
        //     acceleration *= modifier;
        // }

        // 操控性修改
        float handlingModifier = 1f + modifiers.HandlingModifier / 100f;
        // 需要在VehicleDriveSystem中添加以下方法:
        // public void SetHandlingModifier(float modifier)
        // {
        //     steeringSpeed *= modifier;
        //     maxSteeringAngle *= Mathf.Lerp(1f, 1.2f, Mathf.Clamp01((modifier - 1f) * 2f));
        // }

        // 制动力修改
        float brakeForceModifier = 1f + modifiers.BrakeForceModifier / 100f;
        // 需要在VehicleDriveSystem中添加以下方法:
        // public void SetBrakeForceModifier(float modifier)
        // {
        //     brakeForce *= modifier;
        //     handbrakeTorque *= modifier;
        // }

        // 轮胎摩擦力修改
        // 需要在VehiclePhysics中添加以下方法:
        // public void SetTireFrictionModifier(float modifier)
        // {
        //     // 应用到所有车轮
        //     if (frontLeftWheel != null) {
        //         WheelFrictionCurve fwdFriction = frontLeftWheel.forwardFriction;
        //         fwdFriction.stiffness *= (1f + modifier / 10f);
        //         frontLeftWheel.forwardFriction = fwdFriction;
        //
        //         WheelFrictionCurve sideFriction = frontLeftWheel.sidewaysFriction;
        //         sideFriction.stiffness *= (1f + modifier / 10f);
        //         frontLeftWheel.sidewaysFriction = sideFriction;
        //     }
        //     // 对其他车轮执行相同操作...
        // }

        // 引擎扭矩曲线
        if (modifiers.HasCustomEngineCurve && modifiers.EngineTorqueCurve != null)
        {
            // 需要在VehicleDriveSystem中添加以下方法:
            // public void SetEngineTorqueCurve(AnimationCurve curve)
            // {
            //     engineTorqueCurve = curve;
            // }
        }

        // 引擎声音
        if (modifiers.EngineSound != null)
        {
            // 需要在VehiclePhysics中添加以下方法:
            // public void SetEngineSound(AudioClip sound)
            // {
            //     if (engineAudioSource != null && sound != null) {
            //         engineAudioSource.clip = sound;
            //         if (!engineAudioSource.isPlaying) {
            //             engineAudioSource.Play();
            //         }
            //     }
            // }
        }

        // 氮气系统修改
        float nitroCapacityModifier = 1f + modifiers.NitroCapacityModifier / 100f;
        float nitroEfficiencyModifier = 1f + modifiers.NitroEfficiencyModifier / 100f;
        float nitroRecoveryModifier = 1f + modifiers.NitroRecoveryModifier / 100f;

        // 需要在VehicleDriveSystem中添加以下方法:
        // public void SetNitroModifiers(float capacityMod, float efficiencyMod, float recoveryMod)
        // {
        //     nitroCapacity *= capacityMod;
        //     nitroBoostFactor *= efficiencyMod;
        //     nitroRecoveryRate *= recoveryMod;
        //
        //     // 确保当前氮气量不超过新的容量
        //     currentNitroAmount = Mathf.Min(currentNitroAmount, nitroCapacity);
        // }

        Debug.Log("已准备好零部件升级系统与车辆系统的集成方法。请在VehicleDriveSystem和VehiclePhysics中实现相应的方法。");
    }

    /// <summary>
    /// 保存数据
    /// </summary>
    private void SaveData()
    {
        // 保存已解锁零部件
        SaveUnlockedParts();

        // 保存已装备零部件
        SaveEquippedParts();
    }

    /// <summary>
    /// 保存已解锁零部件
    /// </summary>
    private void SaveUnlockedParts()
    {
        string unlockedPartsData = string.Join(";", m_UnlockedPartIDs);
        PlayerPrefs.SetString(m_UnlockedPartsKey, unlockedPartsData);
    }

    /// <summary>
    /// 保存已装备零部件
    /// </summary>
    private void SaveEquippedParts()
    {
        List<string> equippedData = new List<string>();

        foreach (var pair in m_EquippedParts)
        {
            if (pair.Value != null)
            {
                string entry = $"{(int)pair.Key}:{pair.Value.PartID}";
                equippedData.Add(entry);
            }
        }

        string equippedPartsData = string.Join(";", equippedData);
        PlayerPrefs.SetString(m_EquippedPartsKey, equippedPartsData);
    }

    /// <summary>
    /// 加载数据
    /// </summary>
    private void LoadData()
    {
        // 加载已解锁零部件
        LoadUnlockedParts();

        // 加载已装备零部件
        LoadEquippedParts();

        // 如果没有任何数据，初始化默认数据
        if (m_UnlockedPartIDs.Count == 0 && m_EquippedParts.Count == 0)
        {
            InitializeDefaultData();
        }
    }

    /// <summary>
    /// 加载已解锁零部件
    /// </summary>
    private void LoadUnlockedParts()
    {
        m_UnlockedPartIDs.Clear();

        if (PlayerPrefs.HasKey(m_UnlockedPartsKey))
        {
            string unlockedPartsData = PlayerPrefs.GetString(m_UnlockedPartsKey);
            string[] parts = unlockedPartsData.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string partID in parts)
            {
                m_UnlockedPartIDs.Add(partID);
            }
        }
    }

    /// <summary>
    /// 加载已装备零部件
    /// </summary>
    private void LoadEquippedParts()
    {
        m_EquippedParts.Clear();

        if (PlayerPrefs.HasKey(m_EquippedPartsKey))
        {
            string equippedPartsData = PlayerPrefs.GetString(m_EquippedPartsKey);
            string[] entries = equippedPartsData.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string entry in entries)
            {
                string[] parts = entry.Split(':');
                if (parts.Length == 2)
                {
                    if (int.TryParse(parts[0], out int categoryInt) &&
                        Enum.IsDefined(typeof(PartCategory), categoryInt) &&
                        m_PartIDToData.TryGetValue(parts[1], out PartDataSO partData))
                    {
                        PartCategory category = (PartCategory)categoryInt;
                        m_EquippedParts[category] = partData;
                    }
                }
            }
        }
    }
    #endregion
}

/// <summary>
/// 性能修改器结构体
/// </summary>
public struct PerformanceModifiers
{
    // 通用修改器
    public float SpeedModifier;         // 速度修正 (%)
    public float AccelerationModifier;  // 加速度修正 (%)
    public float HandlingModifier;      // 操控性修正 (%)
    public float BrakeForceModifier;    // 制动力修正 (%)

    // 轮胎特有修改器
    public float TireFrictionModifier;     // 轮胎抓地力修正 (绝对值)
    public float WetPerformanceModifier;   // 轮胎湿滑路面表现 (绝对值)

    // 引擎特有修改器
    public bool HasCustomEngineCurve;      // 是否有自定义引擎曲线
    public AnimationCurve EngineTorqueCurve; // 引擎扭矩曲线
    public AudioClip EngineSound;          // 引擎声音

    // 氮气特有修改器
    public float NitroCapacityModifier;    // 氮气容量修正 (%)
    public float NitroEfficiencyModifier;  // 氮气效率修正 (绝对值)
    public float NitroRecoveryModifier;    // 氮气回复速度修正 (%)
}