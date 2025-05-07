using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

namespace RaceGame.UI
{
    /// <summary>
    /// 菜单音频控制器，管理菜单中的音频效果和音量控制
    /// </summary>
    public class MenuAudioController : MonoBehaviour
    {
        #region 私有字段
        [Header("音效设置")]
        [SerializeField] private AudioSource m_ButtonClickSource;
        [SerializeField] private AudioSource m_HoverSource;
        [SerializeField] private AudioSource m_BackSource;
        [SerializeField] private AudioSource m_ConfirmSource;
        [SerializeField] private AudioSource m_ErrorSource;
        [SerializeField] private AudioSource m_MenuMusicSource;

        [Header("音频混合器")]
        [SerializeField] private AudioMixer m_AudioMixer;
        [SerializeField] private string m_MasterVolumeParam = "MasterVolume";
        [SerializeField] private string m_MusicVolumeParam = "MusicVolume";
        [SerializeField] private string m_SFXVolumeParam = "SFXVolume";

        [Header("UI设置")]
        [SerializeField] private Slider m_MasterVolumeSlider;
        [SerializeField] private Slider m_MusicVolumeSlider;
        [SerializeField] private Slider m_SFXVolumeSlider;
        [SerializeField] private Toggle m_MuteToggle;

        // 私有变量
        private float m_PreviousMasterVolume = 1f;
        private const float c_MinVolume = 0.0001f; // -80dB
        #endregion

        #region Unity生命周期
        private void Awake()
        {
            InitializeAudioSettings();
            SetupUIControls();
        }

        private void OnEnable()
        {
            // 从PlayerPrefs加载保存的音量设置
            LoadVolumeSettings();
        }

        private void OnDisable()
        {
            // 保存音量设置到PlayerPrefs
            SaveVolumeSettings();
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 播放按钮点击音效
        /// </summary>
        public void PlayButtonClickSound()
        {
            if (m_ButtonClickSource != null)
            {
                m_ButtonClickSource.Play();
            }
        }

        /// <summary>
        /// 播放悬停音效
        /// </summary>
        public void PlayHoverSound()
        {
            if (m_HoverSource != null)
            {
                m_HoverSource.Play();
            }
        }

        /// <summary>
        /// 播放返回音效
        /// </summary>
        public void PlayBackSound()
        {
            if (m_BackSource != null)
            {
                m_BackSource.Play();
            }
        }

        /// <summary>
        /// 播放确认音效
        /// </summary>
        public void PlayConfirmSound()
        {
            if (m_ConfirmSource != null)
            {
                m_ConfirmSource.Play();
            }
        }

        /// <summary>
        /// 播放错误音效
        /// </summary>
        public void PlayErrorSound()
        {
            if (m_ErrorSource != null)
            {
                m_ErrorSource.Play();
            }
        }

        /// <summary>
        /// 设置主音量
        /// </summary>
        /// <param name="_volume">音量值(0-1)</param>
        public void SetMasterVolume(float _volume)
        {
            if (m_AudioMixer == null) return;
            
            // 将0-1范围的值转换为音频混合器的分贝值
            float dbValue = _volume > c_MinVolume ? Mathf.Log10(_volume) * 20 : -80f;
            m_AudioMixer.SetFloat(m_MasterVolumeParam, dbValue);
            
            // 更新UI控件
            if (m_MasterVolumeSlider != null)
            {
                m_MasterVolumeSlider.value = _volume;
            }
        }

        /// <summary>
        /// 设置音乐音量
        /// </summary>
        /// <param name="_volume">音量值(0-1)</param>
        public void SetMusicVolume(float _volume)
        {
            if (m_AudioMixer == null) return;
            
            float dbValue = _volume > c_MinVolume ? Mathf.Log10(_volume) * 20 : -80f;
            m_AudioMixer.SetFloat(m_MusicVolumeParam, dbValue);
            
            // 更新UI控件
            if (m_MusicVolumeSlider != null)
            {
                m_MusicVolumeSlider.value = _volume;
            }
        }

        /// <summary>
        /// 设置音效音量
        /// </summary>
        /// <param name="_volume">音量值(0-1)</param>
        public void SetSFXVolume(float _volume)
        {
            if (m_AudioMixer == null) return;
            
            float dbValue = _volume > c_MinVolume ? Mathf.Log10(_volume) * 20 : -80f;
            m_AudioMixer.SetFloat(m_SFXVolumeParam, dbValue);
            
            // 更新UI控件
            if (m_SFXVolumeSlider != null)
            {
                m_SFXVolumeSlider.value = _volume;
            }
        }

        /// <summary>
        /// 设置静音状态
        /// </summary>
        /// <param name="_isMuted">是否静音</param>
        public void SetMute(bool _isMuted)
        {
            if (m_AudioMixer == null) return;
            
            if (_isMuted)
            {
                // 保存当前音量并设置为静音
                m_AudioMixer.GetFloat(m_MasterVolumeParam, out float currentVolume);
                m_PreviousMasterVolume = Mathf.Pow(10, currentVolume / 20);
                SetMasterVolume(c_MinVolume);
            }
            else
            {
                // 恢复之前的音量
                SetMasterVolume(m_PreviousMasterVolume);
            }
            
            // 更新UI控件
            if (m_MuteToggle != null)
            {
                m_MuteToggle.isOn = _isMuted;
            }
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 初始化音频设置
        /// </summary>
        private void InitializeAudioSettings()
        {
            if (m_AudioMixer == null)
            {
                Debug.LogError("未设置音频混合器！");
                return;
            }
            
            // 设置默认音量
            SetMasterVolume(1f);
            SetMusicVolume(0.8f);
            SetSFXVolume(0.8f);
        }

        /// <summary>
        /// 设置UI控件
        /// </summary>
        private void SetupUIControls()
        {
            // 设置主音量滑块监听
            if (m_MasterVolumeSlider != null)
            {
                m_MasterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
            }
            
            // 设置音乐音量滑块监听
            if (m_MusicVolumeSlider != null)
            {
                m_MusicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
            }
            
            // 设置音效音量滑块监听
            if (m_SFXVolumeSlider != null)
            {
                m_SFXVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
            }
            
            // 设置静音开关监听
            if (m_MuteToggle != null)
            {
                m_MuteToggle.onValueChanged.AddListener(SetMute);
            }
        }

        /// <summary>
        /// 加载音量设置
        /// </summary>
        private void LoadVolumeSettings()
        {
            float masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            float musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
            float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
            bool isMuted = PlayerPrefs.GetInt("MuteAudio", 0) == 1;
            
            // 应用设置
            SetMusicVolume(musicVolume);
            SetSFXVolume(sfxVolume);
            
            if (isMuted)
            {
                m_PreviousMasterVolume = masterVolume;
                SetMute(true);
            }
            else
            {
                SetMasterVolume(masterVolume);
            }
        }

        /// <summary>
        /// 保存音量设置
        /// </summary>
        private void SaveVolumeSettings()
        {
            if (m_AudioMixer == null) return;
            
            // 获取当前音量设置
            m_AudioMixer.GetFloat(m_MasterVolumeParam, out float masterVolumeDB);
            m_AudioMixer.GetFloat(m_MusicVolumeParam, out float musicVolumeDB);
            m_AudioMixer.GetFloat(m_SFXVolumeParam, out float sfxVolumeDB);
            
            // 将dB值转换回0-1范围
            float masterVolume = masterVolumeDB <= -80f ? 0f : Mathf.Pow(10, masterVolumeDB / 20);
            float musicVolume = musicVolumeDB <= -80f ? 0f : Mathf.Pow(10, musicVolumeDB / 20);
            float sfxVolume = sfxVolumeDB <= -80f ? 0f : Mathf.Pow(10, sfxVolumeDB / 20);
            
            // 保存到PlayerPrefs
            PlayerPrefs.SetFloat("MasterVolume", m_MuteToggle != null && m_MuteToggle.isOn ? m_PreviousMasterVolume : masterVolume);
            PlayerPrefs.SetFloat("MusicVolume", musicVolume);
            PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
            PlayerPrefs.SetInt("MuteAudio", m_MuteToggle != null && m_MuteToggle.isOn ? 1 : 0);
            PlayerPrefs.Save();
        }
        #endregion
    }
} 