using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

namespace RaceGame.UI
{
    /// <summary>
    /// UI面板基类，提供基本的UI面板功能
    /// </summary>
    public class UIPanel : MonoBehaviour
    {
        #region 私有字段
        [Header("过渡动画设置")]
        [SerializeField] private bool m_UseAnimation = true;
        [SerializeField] private float m_FadeInDuration = 0.3f;
        [SerializeField] private float m_FadeOutDuration = 0.2f;
        [SerializeField] private AnimationCurve m_FadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve m_FadeOutCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("背景设置")]
        [SerializeField] private CanvasGroup m_CanvasGroup;
        [SerializeField] private RectTransform m_PanelTransform;
        
        // 事件
        [Header("事件")]
        [SerializeField] private UnityEvent m_OnPanelOpened;
        [SerializeField] private UnityEvent m_OnPanelClosed;

        // 私有变量
        private Coroutine m_CurrentAnimation;
        private bool m_IsAnimating = false;
        private Vector2 m_InitialPosition;
        private Vector2 m_InitialSize;
        #endregion

        #region Unity生命周期
        protected virtual void Awake()
        {
            // 获取或添加CanvasGroup组件
            if (m_CanvasGroup == null)
            {
                m_CanvasGroup = GetComponent<CanvasGroup>();
                if (m_CanvasGroup == null)
                {
                    m_CanvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }
            
            // 获取RectTransform
            if (m_PanelTransform == null)
            {
                m_PanelTransform = GetComponent<RectTransform>();
            }
            
            // 保存初始值
            if (m_PanelTransform != null)
            {
                m_InitialPosition = m_PanelTransform.anchoredPosition;
                m_InitialSize = m_PanelTransform.sizeDelta;
            }
            
            // 默认隐藏面板
            gameObject.SetActive(false);
            if (m_CanvasGroup != null)
            {
                m_CanvasGroup.alpha = 0;
                m_CanvasGroup.interactable = false;
                m_CanvasGroup.blocksRaycasts = false;
            }
        }

        private void OnEnable()
        {
            // 当面板启用时，播放打开动画
            if (m_UseAnimation)
            {
                OpenWithAnimation();
            }
            else
            {
                InstantOpen();
            }
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 打开面板
        /// </summary>
        public virtual void Open()
        {
            if (gameObject.activeSelf)
            {
                return;
            }
            
            gameObject.SetActive(true);
            
            if (m_UseAnimation)
            {
                OpenWithAnimation();
            }
            else
            {
                InstantOpen();
            }
        }

        /// <summary>
        /// 关闭面板
        /// </summary>
        public virtual void Close()
        {
            if (!gameObject.activeSelf)
            {
                return;
            }
            
            if (m_UseAnimation)
            {
                CloseWithAnimation();
            }
            else
            {
                InstantClose();
            }
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 立即打开面板，不使用动画
        /// </summary>
        private void InstantOpen()
        {
            if (m_CanvasGroup != null)
            {
                m_CanvasGroup.alpha = 1;
                m_CanvasGroup.interactable = true;
                m_CanvasGroup.blocksRaycasts = true;
            }
            m_OnPanelOpened?.Invoke();
        }

        /// <summary>
        /// 立即关闭面板，不使用动画
        /// </summary>
        private void InstantClose()
        {
            if (m_CanvasGroup != null)
            {
                m_CanvasGroup.alpha = 0;
                m_CanvasGroup.interactable = false;
                m_CanvasGroup.blocksRaycasts = false;
            }
            gameObject.SetActive(false);
            m_OnPanelClosed?.Invoke();
        }

        /// <summary>
        /// 使用动画打开面板
        /// </summary>
        private void OpenWithAnimation()
        {
            // 停止当前动画
            if (m_CurrentAnimation != null)
            {
                StopCoroutine(m_CurrentAnimation);
            }
            
            // 设置初始状态
            if (m_CanvasGroup != null)
            {
                m_CanvasGroup.alpha = 0;
                m_CanvasGroup.interactable = false;
                m_CanvasGroup.blocksRaycasts = false;
            }
            
            // 开始新动画
            m_CurrentAnimation = StartCoroutine(OpenAnimationCoroutine());
        }

        /// <summary>
        /// 使用动画关闭面板
        /// </summary>
        private void CloseWithAnimation()
        {
            // 停止当前动画
            if (m_CurrentAnimation != null)
            {
                StopCoroutine(m_CurrentAnimation);
            }
            
            // 开始新动画
            m_CurrentAnimation = StartCoroutine(CloseAnimationCoroutine());
        }

        /// <summary>
        /// 打开动画协程
        /// </summary>
        private IEnumerator OpenAnimationCoroutine()
        {
            m_IsAnimating = true;
            
            float elapsed = 0;
            while (elapsed < m_FadeInDuration)
            {
                float normalizedTime = elapsed / m_FadeInDuration;
                float curveValue = m_FadeInCurve.Evaluate(normalizedTime);
                
                // 更新透明度
                if (m_CanvasGroup != null)
                {
                    m_CanvasGroup.alpha = curveValue;
                }
                
                // 可以在这里添加更多动画效果，如位置、缩放等
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // 确保最终状态
            if (m_CanvasGroup != null)
            {
                m_CanvasGroup.alpha = 1;
                m_CanvasGroup.interactable = true;
                m_CanvasGroup.blocksRaycasts = true;
            }
            
            m_IsAnimating = false;
            m_OnPanelOpened?.Invoke();
        }

        /// <summary>
        /// 关闭动画协程
        /// </summary>
        private IEnumerator CloseAnimationCoroutine()
        {
            m_IsAnimating = true;
            
            if (m_CanvasGroup != null)
            {
                m_CanvasGroup.interactable = false;
                m_CanvasGroup.blocksRaycasts = false;
            }
            
            float elapsed = 0;
            while (elapsed < m_FadeOutDuration)
            {
                float normalizedTime = elapsed / m_FadeOutDuration;
                float curveValue = m_FadeOutCurve.Evaluate(normalizedTime);
                
                // 更新透明度
                if (m_CanvasGroup != null)
                {
                    m_CanvasGroup.alpha = 1 - curveValue;
                }
                
                // 可以在这里添加更多动画效果，如位置、缩放等
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // 确保最终状态
            if (m_CanvasGroup != null)
            {
                m_CanvasGroup.alpha = 0;
            }
            
            m_IsAnimating = false;
            gameObject.SetActive(false);
            m_OnPanelClosed?.Invoke();
        }
        #endregion

        #region 保护方法
        /// <summary>
        /// 重置面板状态
        /// </summary>
        protected virtual void ResetPanel()
        {
            // 可以在子类中重写此方法以添加特定的重置逻辑
        }
        #endregion
    }
} 