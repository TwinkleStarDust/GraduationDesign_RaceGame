using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

namespace RaceGame.UI
{
    /// <summary>
    /// UI按钮控制器，提供按钮悬停、点击等效果
    /// </summary>
    public class UIButtonController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        #region 私有字段
        [Header("交互设置")]
        [SerializeField] private float m_HoverScaleMultiplier = 1.1f;
        [SerializeField] private float m_PressedScaleMultiplier = 0.95f;
        [SerializeField] private float m_ScaleTransitionSpeed = 10f;

        [Header("颜色设置")]
        [SerializeField] private bool m_UseColorTransition = true;
        [SerializeField] private Color m_NormalColor = Color.white;
        [SerializeField] private Color m_HoverColor = new Color(0.9f, 0.9f, 1f);
        [SerializeField] private Color m_PressedColor = new Color(0.8f, 0.8f, 0.9f);
        [SerializeField] private float m_ColorTransitionSpeed = 10f;

        // 私有变量
        private Vector3 m_OriginalScale;
        private Vector3 m_TargetScale;
        private Image m_ButtonImage;
        private TextMeshProUGUI m_ButtonText;
        private Color m_TargetColor;
        private bool m_IsTransitioning = false;
        private Coroutine m_ScaleCoroutine;
        private Coroutine m_ColorCoroutine;
        #endregion

        #region Unity生命周期
        private void Awake()
        {
            // 获取组件引用
            m_ButtonImage = GetComponent<Image>();
            m_ButtonText = GetComponentInChildren<TextMeshProUGUI>();
            
            // 保存原始缩放
            m_OriginalScale = transform.localScale;
            m_TargetScale = m_OriginalScale;
            
            // 设置初始颜色
            if (m_UseColorTransition)
            {
                if (m_ButtonImage != null)
                {
                    m_ButtonImage.color = m_NormalColor;
                }
                m_TargetColor = m_NormalColor;
            }
        }

        private void OnDisable()
        {
            // 停止所有协程
            if (m_ScaleCoroutine != null)
            {
                StopCoroutine(m_ScaleCoroutine);
            }
            if (m_ColorCoroutine != null)
            {
                StopCoroutine(m_ColorCoroutine);
            }

            // 重置缩放和颜色
            transform.localScale = m_OriginalScale;
            if (m_UseColorTransition && m_ButtonImage != null)
            {
                m_ButtonImage.color = m_NormalColor;
            }
        }
        #endregion

        #region 接口实现
        public void OnPointerEnter(PointerEventData eventData)
        {
            m_TargetScale = m_OriginalScale * m_HoverScaleMultiplier;
            m_ScaleCoroutine = StartCoroutine(ScaleTransition());

            if (m_UseColorTransition && m_ButtonImage != null)
            {
                m_TargetColor = m_HoverColor;
                m_ColorCoroutine = StartCoroutine(ColorTransition());
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            m_TargetScale = m_OriginalScale;
            m_ScaleCoroutine = StartCoroutine(ScaleTransition());

            if (m_UseColorTransition && m_ButtonImage != null)
            {
                m_TargetColor = m_NormalColor;
                m_ColorCoroutine = StartCoroutine(ColorTransition());
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            m_TargetScale = m_OriginalScale * m_PressedScaleMultiplier;
            m_ScaleCoroutine = StartCoroutine(ScaleTransition());

            if (m_UseColorTransition && m_ButtonImage != null)
            {
                m_TargetColor = m_PressedColor;
                m_ColorCoroutine = StartCoroutine(ColorTransition());
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            m_TargetScale = m_OriginalScale * m_HoverScaleMultiplier;
            m_ScaleCoroutine = StartCoroutine(ScaleTransition());

            if (m_UseColorTransition && m_ButtonImage != null)
            {
                m_TargetColor = m_HoverColor;
                m_ColorCoroutine = StartCoroutine(ColorTransition());
            }
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 缩放过渡协程
        /// </summary>
        private IEnumerator ScaleTransition()
        {
            while (Vector3.Distance(transform.localScale, m_TargetScale) > 0.01f)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, m_TargetScale, Time.deltaTime * m_ScaleTransitionSpeed);
                yield return null;
            }
            transform.localScale = m_TargetScale;
        }

        /// <summary>
        /// 颜色过渡协程
        /// </summary>
        private IEnumerator ColorTransition()
        {
            if (m_ButtonImage != null)
            {
                while (Vector4.Distance(m_ButtonImage.color, m_TargetColor) > 0.01f)
                {
                    m_ButtonImage.color = Color.Lerp(m_ButtonImage.color, m_TargetColor, Time.deltaTime * m_ColorTransitionSpeed);
                    yield return null;
                }
                m_ButtonImage.color = m_TargetColor;
            }
        }
        #endregion
    }
} 