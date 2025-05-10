using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio; // 用于控制AudioMixer
using TMPro; // 确保已导入TextMeshPro

public class SettingsUI : MonoBehaviour
{
    #region 私有常量
    private const string c_SfxVolumeKey = "SFXVolume";
    private const string c_BgmVolumeKey = "BGMVolume";
    private const string c_SfxVolumeMixerParam = "SFXVolumeParam"; // AudioMixer中暴露的SFX音量参数名
    private const string c_BgmVolumeMixerParam = "BGMVolumeParam"; // AudioMixer中暴露的BGM音量参数名
    #endregion

    #region 私有字段
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer m_MasterMixer; // 引用你的主AudioMixer

    [Header("UI元素引用")]
    [SerializeField] private Slider m_SfxVolumeSlider;
    [SerializeField] private TextMeshProUGUI m_SfxVolumeValueText;
    [SerializeField] private Slider m_BgmVolumeSlider;
    [SerializeField] private TextMeshProUGUI m_BgmVolumeValueText;
    [SerializeField] private Button m_BackButton;
    // 可以添加恢复默认设置的按钮等
    #endregion

    #region Unity生命周期
    private void Start()
    {
        LoadSettings();
        AssignListeners();
    }
    #endregion

    #region 私有方法
    private void LoadSettings()
    {
        // 加载音效音量
        float sfxVolume = PlayerPrefs.GetFloat(c_SfxVolumeKey, 0.8f); // 默认0.8
        if (m_SfxVolumeSlider != null) m_SfxVolumeSlider.value = sfxVolume;
        SetSfxVolume(sfxVolume); // 应用到Mixer并更新文本

        // 加载BGM音量
        float bgmVolume = PlayerPrefs.GetFloat(c_BgmVolumeKey, 0.8f); // 默认0.8
        if (m_BgmVolumeSlider != null) m_BgmVolumeSlider.value = bgmVolume;
        SetBgmVolume(bgmVolume); // 应用到Mixer并更新文本
    }

    private void AssignListeners()
    {
        if (m_SfxVolumeSlider != null) m_SfxVolumeSlider.onValueChanged.AddListener(SetSfxVolume);
        if (m_BgmVolumeSlider != null) m_BgmVolumeSlider.onValueChanged.AddListener(SetBgmVolume);
        if (m_BackButton != null) m_BackButton.onClick.AddListener(OnBackButtonPressed);
    }

    private void SetSfxVolume(float _volume)
    {
        if (m_MasterMixer != null)
        {
            // AudioMixer的音量通常是对数标度 (-80dB 到 0dB 或 20dB)
            // Slider的值通常是线性的 (0 到 1)
            // 需要一个转换函数，例如 Mathf.Log10(volume) * 20
            m_MasterMixer.SetFloat(c_SfxVolumeMixerParam, ConvertLinearToDecibels(_volume));
        }
        if (m_SfxVolumeValueText != null) m_SfxVolumeValueText.text = Mathf.RoundToInt(_volume * 100).ToString();
        PlayerPrefs.SetFloat(c_SfxVolumeKey, _volume);
    }

    private void SetBgmVolume(float _volume)
    {
        if (m_MasterMixer != null)
        {
            m_MasterMixer.SetFloat(c_BgmVolumeMixerParam, ConvertLinearToDecibels(_volume));
        }
        if (m_BgmVolumeValueText != null) m_BgmVolumeValueText.text = Mathf.RoundToInt(_volume * 100).ToString();
        PlayerPrefs.SetFloat(c_BgmVolumeKey, _volume);
    }

    // 将线性值 (0-1) 转换为分贝值 (通常-80 到 0)
    private float ConvertLinearToDecibels(float _linearValue)
    {
        // 避免 log(0) 的情况
        return Mathf.Log10(Mathf.Max(_linearValue, 0.0001f)) * 20f;
    }

    private void OnBackButtonPressed()
    {
        PlayerPrefs.Save(); // 确保设置被保存
        Debug.Log("从设置返回主菜单");
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowMainMenuPanel();
        }
        else
        {
            Debug.LogError("UIManager 实例未找到！");
        }
    }
    #endregion
} 