using UnityEngine;
using UnityEngine.UI; // 需要引入UI命名空间
// 如果 CarController 在特定命名空间下，也需要引入，例如：
// using Vehicle; // 假设 CarController 在 Vehicle 命名空间

public class NitroDisplayUI : MonoBehaviour
{
    [Header("UI 引用")]
    [Tooltip("用于显示氮气量的Slider UI元素")]
    [SerializeField] private Slider nitroSlider;

    [Header("目标车辆控制器引用")]
    [Tooltip("场景中的CarController脚本实例")]
    [SerializeField] private CarController targetCarController;

    void Awake()
    {
        // 尝试自动查找CarController，如果未在检视面板中指定
        if (targetCarController == null)
        {
            targetCarController = FindObjectOfType<CarController>();
        }

        if (targetCarController == null)
        {
            Debug.LogError("NitroDisplayUI: 未能找到场景中的CarController实例！请在检视面板中指定一个。", this);
            enabled = false; // 禁用此脚本以避免Update中出错
            return;
        }

        if (nitroSlider == null)
        {
            Debug.LogError("NitroDisplayUI: Nitro Slider UI元素未在检视面板中指定！", this);
            enabled = false;
            return;
        }

        // 初始化Slider的范围 (确保是0到1，因为我们将使用归一化值)
        nitroSlider.minValue = 0f;
        nitroSlider.maxValue = 1f;
    }

    void Update()
    {
        if (targetCarController == null || nitroSlider == null)
        {
            return; // 如果引用丢失，则不执行任何操作
        }

        // 从CarController获取归一化的氮气量 (0到1)
        if (targetCarController.IsNitroSystemEnabled) // 使用 IsNitroSystemEnabled 属性
        {
            nitroSlider.gameObject.SetActive(true); // 如果氮气系统启用，显示Slider
            nitroSlider.value = targetCarController.GetCurrentNitroNormalized();
        }
        else
        {
            nitroSlider.gameObject.SetActive(false); // 如果氮气系统禁用，隐藏Slider
        }
    }
} 