using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "PlayerInventory", menuName = "RaceGame/Player Inventory", order = 1)]
public class PlayerInventorySO : ScriptableObject
{
    #region 公共字段
    [Header("当前装备")]
    public VehicleData m_CurrentVehicle;
    public PartDataSO m_EquippedEngine;
    public PartDataSO m_EquippedTires;
    public PartDataSO m_EquippedNOS;

    [Header("拥有列表")]
    public List<VehicleData> m_OwnedVehicles = new List<VehicleData>();
    public List<PartDataSO> m_OwnedParts = new List<PartDataSO>();
    #endregion

    #region 公共方法
    // 你可以在这里添加管理库存的方法，例如：
    // public void AddVehicle(VehicleData _vehicle)
    // {
    //     if (!m_OwnedVehicles.Contains(_vehicle))
    //     {
    //         m_OwnedVehicles.Add(_vehicle);
    //     }
    // }

    // public void AddPart(PartDataSO _part)
    // {
    //     if (!m_OwnedParts.Contains(_part))
    //     {
    //         m_OwnedParts.Add(_part);
    //     }
    // }

    public void EquipPart(PartDataSO _partToEquip)
    {
        if (_partToEquip == null || !m_OwnedParts.Contains(_partToEquip))
        {
            Debug.LogWarning($"尝试装备一个未拥有或为null的零件: {(_partToEquip != null ? _partToEquip.PartName : "NULL")}");
            return;
        }

        switch (_partToEquip.PartCategoryProperty)
        {
            case PartCategory.Engine:
                m_EquippedEngine = _partToEquip;
                Debug.Log($"已装备引擎: {_partToEquip.PartName}");
                break;
            case PartCategory.Tire:
                m_EquippedTires = _partToEquip;
                Debug.Log($"已装备轮胎: {_partToEquip.PartName}");
                break;
            case PartCategory.Nitro:
                m_EquippedNOS = _partToEquip;
                Debug.Log($"已装备氮气: {_partToEquip.PartName}");
                break;
            default:
                Debug.LogWarning($"尝试装备一个未知类型的零件: {_partToEquip.PartName}, 类型: {_partToEquip.PartCategoryProperty}");
                break;
        }
    }

    public void UnequipPart(PartCategory _categoryToUnequip)
    {
        switch (_categoryToUnequip)
        {
            case PartCategory.Engine:
                if (m_EquippedEngine != null) Debug.Log($"已卸载引擎: {m_EquippedEngine.PartName}");
                m_EquippedEngine = null;
                break;
            case PartCategory.Tire:
                if (m_EquippedTires != null) Debug.Log($"已卸载轮胎: {m_EquippedTires.PartName}");
                m_EquippedTires = null;
                break;
            case PartCategory.Nitro:
                if (m_EquippedNOS != null) Debug.Log($"已卸载氮气: {m_EquippedNOS.PartName}");
                m_EquippedNOS = null;
                break;
            default:
                Debug.LogWarning($"尝试卸载一个未知类型的零件: {_categoryToUnequip}");
                break;
        }
    }
    #endregion
} 