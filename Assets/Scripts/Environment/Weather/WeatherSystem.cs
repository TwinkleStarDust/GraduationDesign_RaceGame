using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 天气系统 - 管理赛道天气和路面摩擦力
/// </summary>
public class WeatherSystem : MonoBehaviour
{
    #region 单例实现
    private static WeatherSystem s_Instance;
    public static WeatherSystem Instance 
    { 
        get 
        {
            if (s_Instance == null)
            {
                GameObject managerObj = GameObject.Find("WeatherSystem");
                if (managerObj == null)
                {
                    managerObj = new GameObject("WeatherSystem");
                    s_Instance = managerObj.AddComponent<WeatherSystem>();
                }
                else
                {
                    s_Instance = managerObj.GetComponent<WeatherSystem>();
                    if (s_Instance == null)
                    {
                        s_Instance = managerObj.AddComponent<WeatherSystem>();
                    }
                }
            }
            return s_Instance;
        }
    }
    #endregion

    #region 事件定义
    // 天气变化事件
    public event Action<WeatherType, WeatherDataSO> OnWeatherChanged;
    #endregion

    #region 序列化字段
    [Header("天气数据")]
    [Tooltip("干燥天气数据")]
    [SerializeField] private WeatherDataSO m_DryWeatherData;

    [Tooltip("湿滑天气数据")]
    [SerializeField] private WeatherDataSO m_WetWeatherData;

    [Tooltip("冰雪天气数据")]
    [SerializeField] private WeatherDataSO m_IceWeatherData;

    [Header("天气设置")]
    [Tooltip("是否随机天气")]
    [SerializeField] private bool m_UseRandomWeather = false;

    [Tooltip("天气变化间隔（秒）")]
    [SerializeField] private float m_WeatherChangeInterval = 300f;

    [Tooltip("默认天气类型")]
    [SerializeField] private WeatherType m_DefaultWeatherType = WeatherType.Dry;

    [Tooltip("天气变化概率（干燥/湿滑/冰雪）")]
    [SerializeField] private Vector3 m_WeatherChangeProbability = new Vector3(0.6f, 0.3f, 0.1f);

    [Header("视觉效果")]
    [Tooltip("雨滴粒子系统")]
    [SerializeField] private ParticleSystem m_RainParticles;

    [Tooltip("雪花粒子系统")]
    [SerializeField] private ParticleSystem m_SnowParticles;
    #endregion

    #region 公共属性
    /// <summary>
    /// 当前天气类型
    /// </summary>
    public WeatherType CurrentWeather { get; private set; }

    /// <summary>
    /// 当前天气数据
    /// </summary>
    public WeatherDataSO CurrentWeatherData { get; private set; }
    #endregion

    #region 私有变量
    // 计时器
    private float m_WeatherTimer = 0f;

    // 注册的路面段落列表
    private List<RoadSegment> m_RegisteredRoadSegments = new List<RoadSegment>();
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

        // 初始化默认天气
        SetWeather(m_DefaultWeatherType);
    }

    private void Start()
    {
        // 注册比赛状态改变事件
        if (RaceManager.Instance != null)
        {
            RaceManager.Instance.OnRaceStateChanged += OnRaceStateChanged;
        }
    }

    private void Update()
    {
        // 只在随机天气模式下更新天气计时器
        if (m_UseRandomWeather && RaceManager.Instance != null && RaceManager.Instance.IsRaceActive)
        {
            m_WeatherTimer += Time.deltaTime;

            // 如果达到天气变化间隔，则随机改变天气
            if (m_WeatherTimer >= m_WeatherChangeInterval)
            {
                m_WeatherTimer = 0f;
                RandomizeWeather();
            }
        }
    }

    private void OnDestroy()
    {
        // 取消注册事件
        if (RaceManager.Instance != null)
        {
            RaceManager.Instance.OnRaceStateChanged -= OnRaceStateChanged;
        }
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 设置天气类型
    /// </summary>
    public void SetWeather(WeatherType weatherType)
    {
        // 如果天气类型相同，直接返回
        if (weatherType == CurrentWeather) return;

        // 记录当前天气类型
        CurrentWeather = weatherType;

        // 获取对应的天气数据
        WeatherDataSO weatherData = GetWeatherData(weatherType);
        CurrentWeatherData = weatherData;

        // 更新天气视觉效果
        UpdateWeatherVisuals(weatherType);

        // 向所有注册的路面段落广播天气变化事件
        ApplyWeatherToRoadSegments(weatherData);

        // 触发天气变化事件
        OnWeatherChanged?.Invoke(weatherType, weatherData);

        Debug.Log($"天气已变更为: {weatherType}");
    }

    /// <summary>
    /// 注册路面段落
    /// </summary>
    public void RegisterRoadSegment(RoadSegment roadSegment)
    {
        if (!m_RegisteredRoadSegments.Contains(roadSegment))
        {
            m_RegisteredRoadSegments.Add(roadSegment);

            // 立即应用当前天气效果到新注册的路面段落
            if (CurrentWeatherData != null)
            {
                roadSegment.UpdateFriction(CurrentWeatherData);
            }
        }
    }

    /// <summary>
    /// 注销路面段落
    /// </summary>
    public void UnregisterRoadSegment(RoadSegment roadSegment)
    {
        if (m_RegisteredRoadSegments.Contains(roadSegment))
        {
            m_RegisteredRoadSegments.Remove(roadSegment);
        }
    }
    #endregion

    #region 私有方法
    /// <summary>
    /// 比赛状态改变回调
    /// </summary>
    private void OnRaceStateChanged(RaceState oldState, RaceState newState)
    {
        // 当比赛开始时
        if (newState == RaceState.Racing)
        {
            // 重置天气计时器
            m_WeatherTimer = 0f;

            // 如果使用随机天气，则随机选择天气
            if (m_UseRandomWeather)
            {
                RandomizeWeather();
            }
            else
            {
                // 否则使用默认天气
                SetWeather(m_DefaultWeatherType);
            }
        }
    }

    /// <summary>
    /// 随机选择天气
    /// </summary>
    private void RandomizeWeather()
    {
        // 归一化概率向量
        Vector3 probabilities = m_WeatherChangeProbability.normalized;

        // 随机值
        float randomValue = UnityEngine.Random.value;

        // 根据概率选择天气
        WeatherType newWeather;

        if (randomValue < probabilities.x)
        {
            newWeather = WeatherType.Dry;
        }
        else if (randomValue < probabilities.x + probabilities.y)
        {
            newWeather = WeatherType.Wet;
        }
        else
        {
            newWeather = WeatherType.Ice;
        }

        // 设置新天气（如果与当前不同）
        if (newWeather != CurrentWeather)
        {
            SetWeather(newWeather);
        }
    }

    /// <summary>
    /// 获取天气数据
    /// </summary>
    private WeatherDataSO GetWeatherData(WeatherType weatherType)
    {
        switch (weatherType)
        {
            case WeatherType.Dry:
                return m_DryWeatherData;
            case WeatherType.Wet:
                return m_WetWeatherData;
            case WeatherType.Ice:
                return m_IceWeatherData;
            default:
                return m_DryWeatherData;
        }
    }

    /// <summary>
    /// 更新天气视觉效果
    /// </summary>
    private void UpdateWeatherVisuals(WeatherType weatherType)
    {
        // 停止所有粒子系统
        if (m_RainParticles != null)
        {
            m_RainParticles.Stop();
        }

        if (m_SnowParticles != null)
        {
            m_SnowParticles.Stop();
        }

        // 根据天气类型播放相应的粒子效果
        switch (weatherType)
        {
            case WeatherType.Wet:
                if (m_RainParticles != null)
                {
                    m_RainParticles.Play();
                }
                break;
            case WeatherType.Ice:
                if (m_SnowParticles != null)
                {
                    m_SnowParticles.Play();
                }
                break;
        }
    }

    /// <summary>
    /// 应用天气效果到所有注册的路面段落
    /// </summary>
    private void ApplyWeatherToRoadSegments(WeatherDataSO weatherData)
    {
        foreach (RoadSegment roadSegment in m_RegisteredRoadSegments)
        {
            if (roadSegment != null)
            {
                roadSegment.UpdateFriction(weatherData);
            }
        }
    }
    #endregion
}

/// <summary>
/// 天气类型枚举
/// </summary>
public enum WeatherType
{
    Dry,    // 干燥
    Wet,    // 湿滑
    Ice     // 冰雪
} 