using UnityEngine;

/// <summary>
/// 路面段落 - 接收天气变化并更新摩擦力
/// </summary>
public class RoadSegment : MonoBehaviour
{
    #region 序列化字段
    [Tooltip("路面类型")]
    [SerializeField] private RoadType m_RoadType = RoadType.Asphalt;
    
    [Tooltip("是否使用物理材质（如果为false则使用WheelCollider）")]
    [SerializeField] private bool m_UsePhysicMaterial = true;
    
    [Tooltip("路面碰撞器")]
    [SerializeField] private Collider m_RoadCollider;
    
    [Tooltip("默认物理材质")]
    [SerializeField] private PhysicsMaterial m_DefaultPhysicMaterial;
    
    [Header("摩擦力修正")]
    [Tooltip("路面类型额外摩擦力系数")]
    [SerializeField] private float m_FrictionMultiplier = 1.0f;
    
    [Tooltip("最小摩擦力")]
    [SerializeField] private float m_MinFriction = 0.1f;
    #endregion
    
    #region 私有变量
    // 记录路面原始摩擦力
    private float m_OriginalStaticFriction;
    private float m_OriginalDynamicFriction;
    
    // 当前天气数据
    private WeatherDataSO m_CurrentWeatherData;
    #endregion
    
    #region Unity生命周期
    private void Awake()
    {
        // 获取碰撞器或添加一个
        if (m_RoadCollider == null)
        {
            m_RoadCollider = GetComponent<Collider>();
        }
        
        if (m_RoadCollider == null)
        {
            Debug.LogError("路面段落缺少碰撞器！", this);
            return;
        }
        
        // 如果使用物理材质模式但没有设置材质，则创建一个
        if (m_UsePhysicMaterial && m_RoadCollider.sharedMaterial == null)
        {
            if (m_DefaultPhysicMaterial != null)
            {
                m_RoadCollider.sharedMaterial = m_DefaultPhysicMaterial;
            }
            else
            {
                PhysicsMaterial newMaterial = new PhysicsMaterial("RoadMaterial");
                newMaterial.staticFriction = 0.6f;
                newMaterial.dynamicFriction = 0.6f;
                newMaterial.frictionCombine = PhysicsMaterialCombine.Average;
                m_RoadCollider.sharedMaterial = newMaterial;
                m_DefaultPhysicMaterial = newMaterial;
            }
        }
        
        // 记录初始摩擦力
        if (m_UsePhysicMaterial && m_RoadCollider.sharedMaterial != null)
        {
            m_OriginalStaticFriction = m_RoadCollider.sharedMaterial.staticFriction;
            m_OriginalDynamicFriction = m_RoadCollider.sharedMaterial.dynamicFriction;
        }
    }
    
    private void OnEnable()
    {
        // 注册到天气系统
        if (WeatherSystem.Instance != null)
        {
            WeatherSystem.Instance.RegisterRoadSegment(this);
        }
    }
    
    private void OnDisable()
    {
        // 从天气系统注销
        if (WeatherSystem.Instance != null)
        {
            WeatherSystem.Instance.UnregisterRoadSegment(this);
        }
    }
    #endregion
    
    #region 公共方法
    /// <summary>
    /// 更新摩擦力
    /// </summary>
    public void UpdateFriction(WeatherDataSO weatherData)
    {
        if (weatherData == null) return;
        
        // 保存当前天气数据
        m_CurrentWeatherData = weatherData;
        
        // 计算修正后的摩擦力
        float staticFriction = Mathf.Max(m_MinFriction, weatherData.StaticFriction * m_FrictionMultiplier);
        float dynamicFriction = Mathf.Max(m_MinFriction, weatherData.DynamicFriction * m_FrictionMultiplier);
        
        // 如果使用物理材质
        if (m_UsePhysicMaterial && m_RoadCollider != null && m_RoadCollider.sharedMaterial != null)
        {
            // 创建一个新的物理材质副本以避免修改共享材质
            PhysicsMaterial updatedMaterial = new PhysicsMaterial(m_RoadCollider.sharedMaterial.name);
            updatedMaterial.staticFriction = staticFriction;
            updatedMaterial.dynamicFriction = dynamicFriction;
            updatedMaterial.frictionCombine = m_RoadCollider.sharedMaterial.frictionCombine;
            updatedMaterial.bounceCombine = m_RoadCollider.sharedMaterial.bounceCombine;
            
            // 应用新材质
            m_RoadCollider.sharedMaterial = updatedMaterial;
        }
    }
    
    /// <summary>
    /// 重置摩擦力到初始值
    /// </summary>
    public void ResetFriction()
    {
        if (m_UsePhysicMaterial && m_RoadCollider != null && m_DefaultPhysicMaterial != null)
        {
            m_RoadCollider.sharedMaterial = m_DefaultPhysicMaterial;
        }
    }
    
    /// <summary>
    /// 获取当前路面类型
    /// </summary>
    public RoadType GetRoadType()
    {
        return m_RoadType;
    }
    
    /// <summary>
    /// 获取摩擦力修正系数
    /// </summary>
    public float GetFrictionMultiplier()
    {
        return m_FrictionMultiplier;
    }
    #endregion
}

/// <summary>
/// 路面类型枚举
/// </summary>
public enum RoadType
{
    Asphalt,    // 沥青路面
    Dirt,       // 泥土路面
    Grass,      // 草地
    Gravel,     // 砂砾路面
    Ice         // 冰面
} 