# Unity C# 开发规范

## 核心原则

- 编写清晰、技术性强的代码，提供精确的C#和Unity示例
- 尽可能使用Unity内置功能和工具，充分利用其能力
- 优先考虑代码的可读性和可维护性
- 使用描述性的变量和函数名称（如公共成员使用PascalCase，私有成员使用camelCase）
- 使用Unity基于组件的架构构建模块化项目，促进代码重用和关注点分离

## C#/Unity 最佳实践

### 组件设计
- 使用MonoBehaviour作为附加到GameObject的脚本组件
- 优先使用ScriptableObjects作为数据容器和共享资源
- 严格遵循组件模式，确保关注点分离和模块化

### 功能实现
- 利用Unity的物理引擎和碰撞检测系统实现游戏机制和交互
- 使用Unity的Input System处理跨平台的玩家输入
- 使用Unity的UI系统（Canvas, UI元素）创建用户界面
- 使用协程(Coroutines)处理基于时间的操作和Unity单线程环境中的异步任务

## 错误处理与调试

- 在适当情况下使用try-catch块实现错误处理，尤其是文件I/O和网络操作
- 使用Unity的Debug类进行日志记录和调试（Debug.Log, Debug.LogWarning, Debug.LogError）
- 利用Unity的profiler和frame debugger识别和解决性能问题
- 实现自定义错误消息和调试可视化，改善开发体验
- 使用Unity的断言系统（Debug.Assert）捕获开发过程中的逻辑错误

## 依赖项

- Unity6引擎
- 兼容Unity版本的.NET Framework
- Unity Asset Store包（根据特定功能需求）
- 第三方插件Mirror（经过兼容性和性能的仔细验证）

## Unity特定指南

### 资源管理
- 使用预制体(Prefabs)实现可重用的游戏对象和UI元素
- 在脚本中保留游戏逻辑；使用Unity编辑器进行场景组合和初始设置
- 利用Unity的资源包系统(Asset Bundle)进行高效的资源管理和加载
- 使用Unity的标签和层系统对对象进行分类和碰撞过滤

### 视觉效果
- 使用Unity的动画系统（Animator, Animation Clips）实现角色和对象动画
- 应用Unity内置的照明和后期处理效果增强视觉效果

### 测试
- 使用Unity的内置测试框架进行单元测试和集成测试

## 性能优化

- 对频繁实例化和销毁的对象使用对象池(Object Pooling)
- 通过批处理材质和使用精灵集和UI元素的图集优化绘制调用
- 为复杂的3D模型实现细节层次(LOD)系统以提高渲染性能
- 使用Unity的Job System和Burst Compiler处理CPU密集型操作
- 通过使用简化的碰撞网格和调整固定时间步长优化物理性能

## 代码风格和约定

### 命名规范
- 变量: `m_VariableName`
- 常量: `c_ConstantName`
- 静态变量: `s_StaticName`
- 类/结构体: `ClassName`
- 属性: `PropertyName`
- 方法: `MethodName()`
- 参数: `_argumentName`
- 临时变量: `temporaryVariable`

### 代码组织
- 使用#regions组织代码部分
- 使用#if UNITY_EDITOR包装仅编辑器代码
- 使用[SerializeField]在检视面板中公开私有字段
- 适当时为float字段实现Range特性

### 最佳实践
- 使用TryGetComponent避免空引用异常
- 优先使用直接引用或GetComponent()而不是GameObject.Find()或Transform.Find()
- 始终使用TextMeshPro进行文本渲染
- 为频繁实例化的对象实现对象池
- 使用ScriptableObjects进行数据驱动设计和共享资源
- 利用协程处理基于时间的操作，使用Job System处理CPU密集型任务
- 通过批处理和图集优化绘制调用
- 为复杂的3D模型实现LOD系统

## 示例代码结构

```csharp
public class ExampleClass : MonoBehaviour
{
    #region 常量
    private const int c_MaxItems = 100;
    #endregion

    #region 私有字段
    [SerializeField] private int m_ItemCount;
    [SerializeField, Range(0f, 1f)] private float m_SpawnChance;
    #endregion

    #region 公共属性
    public int ItemCount => m_ItemCount;
    #endregion

    #region Unity生命周期
    private void Awake()
    {
        InitializeComponents();
    }

    private void Update()
    {
        UpdateGameLogic();
    }
    #endregion

    #region 私有方法
    private void InitializeComponents()
    {
        // 初始化逻辑
    }

    private void UpdateGameLogic()
    {
        // 更新逻辑
    }
    #endregion

    #region 公共方法
    public void AddItem(int _amount)
    {
        m_ItemCount = Mathf.Min(m_ItemCount + _amount, c_MaxItems);
    }
    #endregion

    #if UNITY_EDITOR
    [ContextMenu("Debug Info")]
    private void DebugInfo()
    {
        Debug.Log($"当前物品数量: {m_ItemCount}");
    }
    #endif
}
```

## 关键约定
1. 遵循Unity的基于组件的架构，创建模块化和可重用的游戏元素
2. 在开发的每个阶段优先考虑性能优化和内存管理
3. 维护清晰合理的项目结构，提高可读性和资产管理效率

请参考Unity文档和C#编程指南了解脚本编写、游戏架构和性能优化的最佳实践。在提供解决方案时，始终考虑特定上下文、目标平台和性能要求。适当时提供多种方法，解释每种方法的优缺点。 