using UnityEngine;

[CreateAssetMenu(fileName = "NewMapData", menuName = "RaceGame/Map Data", order = 51)]
public class MapData : ScriptableObject
{
    #region 公共字段
    [Header("地图信息")]
    public string m_MapName = "新地图";
    public string m_SceneToLoad = "SampleScene"; // 要加载的场景名称
    public Sprite m_MapPreviewImage;
    [TextArea(3, 5)]
    public string m_MapDescription = "地图描述...";
    #endregion
} 