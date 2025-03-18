using UnityEngine;

/// <summary>
/// 天气数据 - 定义不同天气条件下的摩擦系数
/// </summary>
[CreateAssetMenu(fileName = "WeatherData", menuName = "Racing Game/Weather Data", order = 1)]
public class WeatherDataSO : ScriptableObject
{
    [Header("基本信息")]
    [Tooltip("天气名称")]
    [SerializeField] private string m_WeatherName = "晴天";
    
    [Tooltip("天气描述")]
    [SerializeField] [TextArea(2, 5)] private string m_Description = "晴朗的天气，路面干燥，摩擦力正常。";
    
    [Tooltip("天气图标")]
    [SerializeField] private Sprite m_WeatherIcon;
    
    [Header("摩擦力参数")]
    [Tooltip("静态摩擦系数")]
    [Range(0.1f, 1.0f)]
    [SerializeField] private float m_StaticFriction = 0.8f;
    
    [Tooltip("动态摩擦系数")]
    [Range(0.1f, 1.0f)]
    [SerializeField] private float m_DynamicFriction = 0.7f;
    
    [Header("视觉效果")]
    [Tooltip("雾效强度")]
    [Range(0f, 1f)]
    [SerializeField] private float m_FogIntensity = 0f;
    
    [Tooltip("雾效颜色")]
    [SerializeField] private Color m_FogColor = Color.white;
    
    [Tooltip("天空盒材质")]
    [SerializeField] private Material m_SkyboxMaterial;
    
    [Header("光照设置")]
    [Tooltip("光强度调整")]
    [Range(0.5f, 1.5f)]
    [SerializeField] private float m_LightIntensity = 1.0f;
    
    [Tooltip("光颜色")]
    [SerializeField] private Color m_LightColor = Color.white;
    
    [Header("音效设置")]
    [Tooltip("环境音效")]
    [SerializeField] private AudioClip m_AmbientSound;
    
    [Tooltip("音量")]
    [Range(0f, 1f)]
    [SerializeField] private float m_SoundVolume = 0.5f;
    
    #region 公共属性
    /// <summary>
    /// 天气名称
    /// </summary>
    public string WeatherName => m_WeatherName;
    
    /// <summary>
    /// 天气描述
    /// </summary>
    public string Description => m_Description;
    
    /// <summary>
    /// 天气图标
    /// </summary>
    public Sprite WeatherIcon => m_WeatherIcon;
    
    /// <summary>
    /// 静态摩擦系数
    /// </summary>
    public float StaticFriction => m_StaticFriction;
    
    /// <summary>
    /// 动态摩擦系数
    /// </summary>
    public float DynamicFriction => m_DynamicFriction;
    
    /// <summary>
    /// 雾效强度
    /// </summary>
    public float FogIntensity => m_FogIntensity;
    
    /// <summary>
    /// 雾效颜色
    /// </summary>
    public Color FogColor => m_FogColor;
    
    /// <summary>
    /// 天空盒材质
    /// </summary>
    public Material SkyboxMaterial => m_SkyboxMaterial;
    
    /// <summary>
    /// 光强度调整
    /// </summary>
    public float LightIntensity => m_LightIntensity;
    
    /// <summary>
    /// 光颜色
    /// </summary>
    public Color LightColor => m_LightColor;
    
    /// <summary>
    /// 环境音效
    /// </summary>
    public AudioClip AmbientSound => m_AmbientSound;
    
    /// <summary>
    /// 音量
    /// </summary>
    public float SoundVolume => m_SoundVolume;
    #endregion
    
    /// <summary>
    /// 创建默认天气数据集合
    /// </summary>
    public static void CreateDefaultWeatherDataSet()
    {
        #if UNITY_EDITOR
        // 创建干燥天气
        WeatherDataSO dryWeather = ScriptableObject.CreateInstance<WeatherDataSO>();
        dryWeather.m_WeatherName = "晴天";
        dryWeather.m_Description = "晴朗的天气，路面干燥，摩擦力正常。";
        dryWeather.m_StaticFriction = 0.8f;
        dryWeather.m_DynamicFriction = 0.7f;
        dryWeather.m_FogIntensity = 0f;
        dryWeather.m_LightIntensity = 1.0f;
        
        UnityEditor.AssetDatabase.CreateAsset(dryWeather, "Assets/ScriptableObjects/Weather/DryWeather.asset");
        
        // 创建湿滑天气
        WeatherDataSO wetWeather = ScriptableObject.CreateInstance<WeatherDataSO>();
        wetWeather.m_WeatherName = "雨天";
        wetWeather.m_Description = "下雨天气，路面湿滑，摩擦力降低。";
        wetWeather.m_StaticFriction = 0.5f;
        wetWeather.m_DynamicFriction = 0.4f;
        wetWeather.m_FogIntensity = 0.3f;
        wetWeather.m_LightIntensity = 0.7f;
        wetWeather.m_FogColor = new Color(0.8f, 0.8f, 0.9f);
        
        UnityEditor.AssetDatabase.CreateAsset(wetWeather, "Assets/ScriptableObjects/Weather/WetWeather.asset");
        
        // 创建冰雪天气
        WeatherDataSO iceWeather = ScriptableObject.CreateInstance<WeatherDataSO>();
        iceWeather.m_WeatherName = "冰雪";
        iceWeather.m_Description = "冰雪覆盖的路面，摩擦力极低，非常滑。";
        iceWeather.m_StaticFriction = 0.2f;
        iceWeather.m_DynamicFriction = 0.1f;
        iceWeather.m_FogIntensity = 0.5f;
        iceWeather.m_LightIntensity = 0.8f;
        iceWeather.m_FogColor = Color.white;
        
        UnityEditor.AssetDatabase.CreateAsset(iceWeather, "Assets/ScriptableObjects/Weather/IceWeather.asset");
        
        // 保存所有资源
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
        
        Debug.Log("创建了默认天气数据集合");
        #endif
    }
    
    #if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/Racing Game/Create Default Weather Data")]
    private static void CreateDefaultWeatherDataMenuItem()
    {
        CreateDefaultWeatherDataSet();
    }
    #endif
} 