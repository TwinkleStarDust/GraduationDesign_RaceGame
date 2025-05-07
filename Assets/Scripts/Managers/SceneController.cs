using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace RaceGame.Managers
{
    /// <summary>
    /// 场景控制器，负责场景加载和切换
    /// </summary>
    public class SceneController : MonoBehaviour
    {
        #region 单例
        private static SceneController s_Instance;
        
        public static SceneController Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = FindObjectOfType<SceneController>();
                    
                    if (s_Instance == null)
                    {
                        GameObject obj = new GameObject("SceneController");
                        s_Instance = obj.AddComponent<SceneController>();
                    }
                }
                
                return s_Instance;
            }
        }
        #endregion

        #region 私有字段
        [Header("加载画面设置")]
        [SerializeField] private GameObject m_LoadingScreenPrefab;
        [SerializeField] private float m_MinLoadingTime = 0.5f;
        [SerializeField] private float m_FadeInDuration = 0.5f;
        [SerializeField] private float m_FadeOutDuration = 0.5f;
        
        // 私有变量
        private GameObject m_LoadingScreenInstance;
        private Slider m_LoadingBar;
        private TextMeshProUGUI m_LoadingText;
        private CanvasGroup m_CanvasGroup;
        #endregion

        #region Unity生命周期
        private void Awake()
        {
            // 单例模式
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            s_Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 加载场景
        /// </summary>
        /// <param name="_sceneName">场景名称</param>
        /// <param name="_showLoadingScreen">是否显示加载画面</param>
        public void LoadScene(string _sceneName, bool _showLoadingScreen = true)
        {
            if (_showLoadingScreen)
            {
                StartCoroutine(LoadSceneAsync(_sceneName));
            }
            else
            {
                SceneManager.LoadScene(_sceneName);
            }
        }

        /// <summary>
        /// 重新加载当前场景
        /// </summary>
        public void ReloadCurrentScene()
        {
            LoadScene(SceneManager.GetActiveScene().name);
        }

        /// <summary>
        /// 加载主菜单场景
        /// </summary>
        public void LoadMainMenu()
        {
            LoadScene("MainMenuQWQ");
        }

        /// <summary>
        /// 加载游戏场景
        /// </summary>
        /// <param name="_trackName">赛道名称</param>
        public void LoadGameScene(string _trackName)
        {
            // 可以在这里添加根据赛道名称加载不同场景的逻辑
            LoadScene(_trackName);
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 异步加载场景协程
        /// </summary>
        private IEnumerator LoadSceneAsync(string _sceneName)
        {
            // 创建加载画面
            CreateLoadingScreen();
            
            // 淡入加载画面
            yield return StartCoroutine(FadeLoadingScreen(true, m_FadeInDuration));
            
            // 记录开始时间
            float startTime = Time.time;
            
            // 开始异步加载场景
            AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(_sceneName);
            asyncOperation.allowSceneActivation = false;
            
            // 等待加载完成
            while (!asyncOperation.isDone)
            {
                // 计算加载进度
                float progress = Mathf.Clamp01(asyncOperation.progress / 0.9f);
                
                // 更新加载条
                if (m_LoadingBar != null)
                {
                    m_LoadingBar.value = progress;
                }
                
                // 更新加载文本
                if (m_LoadingText != null)
                {
                    m_LoadingText.text = $"加载中... {Mathf.Floor(progress * 100)}%";
                }
                
                // 当加载接近完成时
                if (asyncOperation.progress >= 0.9f)
                {
                    // 确保最小加载时间
                    if (Time.time - startTime >= m_MinLoadingTime)
                    {
                        // 淡出加载画面
                        yield return StartCoroutine(FadeLoadingScreen(false, m_FadeOutDuration));
                        
                        // 允许场景激活
                        asyncOperation.allowSceneActivation = true;
                    }
                }
                
                yield return null;
            }
            
            // 销毁加载画面
            DestroyLoadingScreen();
        }

        /// <summary>
        /// 创建加载画面
        /// </summary>
        private void CreateLoadingScreen()
        {
            if (m_LoadingScreenPrefab != null && m_LoadingScreenInstance == null)
            {
                m_LoadingScreenInstance = Instantiate(m_LoadingScreenPrefab);
                DontDestroyOnLoad(m_LoadingScreenInstance);
                
                // 获取组件引用
                m_LoadingBar = m_LoadingScreenInstance.GetComponentInChildren<Slider>();
                m_LoadingText = m_LoadingScreenInstance.GetComponentInChildren<TextMeshProUGUI>();
                m_CanvasGroup = m_LoadingScreenInstance.GetComponent<CanvasGroup>();
                
                // 如果没有CanvasGroup，则添加一个
                if (m_CanvasGroup == null)
                {
                    m_CanvasGroup = m_LoadingScreenInstance.AddComponent<CanvasGroup>();
                }
                
                // 初始化状态
                if (m_LoadingBar != null)
                {
                    m_LoadingBar.value = 0;
                }
                
                if (m_CanvasGroup != null)
                {
                    m_CanvasGroup.alpha = 0;
                }
            }
        }

        /// <summary>
        /// 销毁加载画面
        /// </summary>
        private void DestroyLoadingScreen()
        {
            if (m_LoadingScreenInstance != null)
            {
                Destroy(m_LoadingScreenInstance);
                m_LoadingScreenInstance = null;
                m_LoadingBar = null;
                m_LoadingText = null;
                m_CanvasGroup = null;
            }
        }

        /// <summary>
        /// 淡入淡出加载画面
        /// </summary>
        private IEnumerator FadeLoadingScreen(bool _fadeIn, float _duration)
        {
            if (m_CanvasGroup == null)
            {
                yield break;
            }
            
            float startAlpha = m_CanvasGroup.alpha;
            float targetAlpha = _fadeIn ? 1f : 0f;
            float elapsed = 0f;
            
            while (elapsed < _duration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / _duration);
                m_CanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, normalizedTime);
                yield return null;
            }
            
            m_CanvasGroup.alpha = targetAlpha;
            
            // 设置交互性
            if (_fadeIn)
            {
                m_CanvasGroup.interactable = true;
                m_CanvasGroup.blocksRaycasts = true;
            }
            else
            {
                m_CanvasGroup.interactable = false;
                m_CanvasGroup.blocksRaycasts = false;
            }
        }
        #endregion
    }
} 