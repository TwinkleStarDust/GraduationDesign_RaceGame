using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Reflection;
using Ricimi;

/// <summary>
/// 场景管理器：根据选中的关卡加载对应的场景
/// </summary>
public class StageSceneManager : MonoBehaviour
{
    [Tooltip("场景过渡组件")]
    public SceneTransition sceneTransition;
    
    [Tooltip("对应关卡的场景名称")]
    public List<string> stageSceneNames = new List<string>();
    
    [Tooltip("默认场景名称，当没有选中任何关卡时使用")]
    public string defaultSceneName = "Game";
    
    [Tooltip("Root对象名称，默认为'Root'")]
    public string rootObjectName = "Root";
    
    [Tooltip("SelectionSlider组件引用")]
    public SelectionSlider selectionSlider;
    
    [Tooltip("每个Item-Group中的关卡数量，默认为3")]
    public int itemsPerGroup = 3;
    
    // 用于缓存SelectionSlider中的selectedOption字段信息
    private FieldInfo selectedOptionField;
    
    private void Awake()
    {
        // 确保场景过渡组件存在
        if (sceneTransition == null)
        {
            sceneTransition = GetComponent<SceneTransition>();
            if (sceneTransition == null)
            {
                sceneTransition = gameObject.AddComponent<SceneTransition>();
                sceneTransition.scene = defaultSceneName;
            }
        }
        
        // 查找SelectionSlider组件
        if (selectionSlider == null)
        {
            selectionSlider = GetComponentInChildren<SelectionSlider>();
        }
        
        // 通过反射获取SelectionSlider中的selectedOption私有字段
        if (selectionSlider != null)
        {
            selectedOptionField = typeof(SelectionSlider).GetField("selectedOption", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (selectedOptionField == null)
            {
                Debug.LogWarning("无法通过反射获取SelectionSlider.selectedOption字段，将使用备选方法");
            }
        }
    }
    
    /// <summary>
    /// 在点击Select按钮时调用此方法
    /// </summary>
    public void LoadSelectedStageScene()
    {
        // 获取当前选中的关卡索引
        int selectedIndex = FindSelectedStageIndex();
        
        // 设置要加载的场景名称
        if (selectedIndex >= 0 && selectedIndex < stageSceneNames.Count)
        {
            sceneTransition.scene = stageSceneNames[selectedIndex];
            Debug.Log($"选择关卡：{selectedIndex+1}，将加载场景：{stageSceneNames[selectedIndex]}");
        }
        else
        {
            sceneTransition.scene = defaultSceneName;
            Debug.Log($"未找到选中的关卡，将加载默认场景：{defaultSceneName}");
        }
        
        // 执行场景切换
        sceneTransition.PerformTransition();
    }
    
    /// <summary>
    /// 查找当前选中的关卡索引
    /// </summary>
    private int FindSelectedStageIndex()
    {
        // 首先查找Root对象
        Transform root = FindRootTransform();
        if (root == null || root.childCount == 0)
        {
            Debug.LogWarning("未找到Root对象或Root下没有子对象");
            return -1;
        }
        
        // 查找当前激活的Item-Group
        Transform itemGroup = root.GetChild(0);
        if (itemGroup == null)
        {
            Debug.LogWarning("Root下没有Item-Group");
            return -1;
        }
        
        // 计算当前SelectionSlider的选项索引
        int groupIndex = GetCurrentGroupIndex();
        
        // 查找Stage-Selection-Item中激活的Toggle
        int toggleIndex = FindSelectedToggleIndex(itemGroup);
        if (toggleIndex == -1)
        {
            Debug.LogWarning("未找到选中的Toggle");
            return -1;
        }
        
        // 根据当前组和组内索引计算全局索引
        int globalIndex = CalculateGlobalIndex(groupIndex, toggleIndex);
        
        Debug.Log($"选中的关卡: 组{groupIndex+1}, 索引{toggleIndex+1}, 全局索引{globalIndex+1}");
        
        return globalIndex;
    }
    
    /// <summary>
    /// 根据组索引和组内索引计算全局索引
    /// </summary>
    private int CalculateGlobalIndex(int groupIndex, int toggleIndex)
    {
        // 方法1：固定大小计算 - 使用固定的每组大小
        return groupIndex * itemsPerGroup + toggleIndex;
        
        // 如果您需要更复杂的映射，可以实现自定义逻辑
        // 例如：第一组有3个关卡，第二组有2个关卡
        /*
        if (groupIndex == 0)
        {
            return toggleIndex; // 第一组：0, 1, 2
        }
        else if (groupIndex == 1)
        {
            return 3 + toggleIndex; // 第二组：3, 4
        }
        return toggleIndex;
        */
    }
    
    /// <summary>
    /// 在给定的Item-Group中查找选中的Toggle索引
    /// </summary>
    private int FindSelectedToggleIndex(Transform itemGroup)
    {
        // 首先尝试查找直接子物体中的Toggle
        for (int i = 0; i < itemGroup.childCount; i++)
        {
            Transform item = itemGroup.GetChild(i);
            
            // 尝试获取当前物体的Toggle
            Toggle toggle = item.GetComponent<Toggle>();
            if (toggle != null && toggle.isOn)
            {
                return i;
            }
            
            // 如果当前物体没有Toggle，尝试在其子物体中查找
            if (toggle == null)
            {
                toggle = item.GetComponentInChildren<Toggle>();
                if (toggle != null && toggle.isOn)
                {
                    return i;
                }
            }
        }
        
        // 如果未找到选中的Toggle，尝试查找任何Toggle以获取默认索引
        for (int i = 0; i < itemGroup.childCount; i++)
        {
            if (itemGroup.GetChild(i).GetComponentInChildren<Toggle>() != null)
            {
                Debug.LogWarning("未找到选中的Toggle，使用第一个Toggle的索引");
                return i;
            }
        }
        
        return -1; // 没有找到选中的Toggle
    }
    
    /// <summary>
    /// 获取当前SelectionSlider显示的组索引
    /// </summary>
    private int GetCurrentGroupIndex()
    {
        if (selectionSlider == null)
            return 0;
            
        // 尝试通过反射获取SelectionSlider中的selectedOption私有字段
        if (selectedOptionField != null)
        {
            try
            {
                int selectedOption = (int)selectedOptionField.GetValue(selectionSlider);
                Debug.Log($"通过反射获取到的SelectionSlider当前索引: {selectedOption}");
                return selectedOption;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"通过反射获取SelectionSlider.selectedOption失败: {e.Message}");
            }
        }
        
        // 备选方法：在Root中查找当前实例化的预制体名称，通过名称推断索引
        Transform root = FindRootTransform();
        if (root != null && root.childCount > 0)
        {
            Transform currentGroup = root.GetChild(0);
            
            if (currentGroup != null)
            {
                string groupName = currentGroup.name;
                
                // 尝试从组名称中提取索引
                // 假设Item-Group的命名规则是"Item-Group-X"或"Item-Group-X(Clone)"
                if (groupName.Contains("Item-Group-"))
                {
                    string indexPart = groupName.Replace("Item-Group-", "").Replace("(Clone)", "");
                    int index;
                    if (int.TryParse(indexPart, out index))
                    {
                        // 调整为0-based索引
                        return index - 1;
                    }
                }
                
                // 如果无法从名称推断，尝试遍历所有Options，查找名称匹配的预制体
                if (selectionSlider.Options != null)
                {
                    for (int i = 0; i < selectionSlider.Options.Count; i++)
                    {
                        GameObject option = selectionSlider.Options[i];
                        if (option != null && (groupName.StartsWith(option.name) || 
                                              groupName.StartsWith(option.name.Replace("(Clone)", ""))))
                        {
                            return i;
                        }
                    }
                }
            }
        }
        
        // 如果所有方法都失败，记录警告并返回默认值
        Debug.LogWarning("无法确定当前SelectionSlider的索引，使用默认值0");
        return 0;
    }
    
    /// <summary>
    /// 查找Root变换
    /// </summary>
    private Transform FindRootTransform()
    {
        // 如果SelectionSlider存在，尝试从中查找Root
        if (selectionSlider != null)
        {
            foreach (Transform child in selectionSlider.transform)
            {
                if (child.name == rootObjectName)
                {
                    return child;
                }
            }
        }
        
        // 全局查找Root对象
        Transform[] allTransforms = FindObjectsOfType<Transform>();
        foreach (Transform t in allTransforms)
        {
            if (t.name == rootObjectName && t.parent != null && 
                t.parent.GetComponent<SelectionSlider>() != null)
            {
                return t;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// 设置指定关卡的场景名称
    /// </summary>
    public void SetStageSceneName(int stageIndex, string sceneName)
    {
        if (stageIndex >= 0)
        {
            // 确保列表有足够的元素
            while (stageSceneNames.Count <= stageIndex)
            {
                stageSceneNames.Add(defaultSceneName);
            }
            
            stageSceneNames[stageIndex] = sceneName;
        }
    }
}