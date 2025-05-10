using UnityEngine;

public enum PartType
{
    Engine,
    Tires,
    NOS
}

[CreateAssetMenu(fileName = "NewPartData", menuName = "RaceGame/Part Data", order = 53)]
public class PartData : ScriptableObject
{
    #region 公共字段
    [Header("零件信息")]
    public string m_PartName = "新零件";
    public PartType m_PartType;
    public Sprite m_PartIcon; // 零件的图标
    [TextArea(3, 5)]
    public string m_PartDescription = "零件描述...";

    // 根据零件类型，你可以在这里添加具体的属性加成
    // 例如：如果是引擎，可以有 public float m_PowerBonus;
    // 例如：如果是轮胎，可以有 public float m_GripBonus;
    // 例如：如果是氮气，可以有 public float m_BoostDurationBonus;
    // public float m_StatBonusValue; // 一个通用的数值加成示例
    #endregion
} 