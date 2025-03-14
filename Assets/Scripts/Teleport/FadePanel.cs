using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 淡入淡出效果面板
/// 用于传送时的过渡效果
/// </summary>
public class FadePanel : MonoBehaviour
{
    [Tooltip("淡入淡出效果的图像")]
    [SerializeField] private Image fadeImage;

    [Tooltip("淡入淡出效果的Canvas Group")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Tooltip("默认淡入淡出颜色")]
    [SerializeField] private Color fadeColor = Color.black;

    private void Awake()
    {
        // 确保组件存在
        if (fadeImage == null)
        {
            fadeImage = GetComponent<Image>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        // 初始化
        if (fadeImage != null)
        {
            fadeImage.color = fadeColor;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = false;
        }

        // 默认隐藏
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 设置淡入淡出颜色
    /// </summary>
    public void SetFadeColor(Color color)
    {
        fadeColor = color;
        if (fadeImage != null)
        {
            fadeImage.color = fadeColor;
        }
    }

    /// <summary>
    /// 设置透明度
    /// </summary>
    public void SetAlpha(float alpha)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.Clamp01(alpha);
        }
    }

    /// <summary>
    /// 立即淡入
    /// </summary>
    public void FadeIn()
    {
        gameObject.SetActive(true);
        SetAlpha(1f);
    }

    /// <summary>
    /// 立即淡出
    /// </summary>
    public void FadeOut()
    {
        SetAlpha(0f);
        gameObject.SetActive(false);
    }
}