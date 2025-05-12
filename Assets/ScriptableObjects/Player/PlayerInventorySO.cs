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

    [Header("货币")]
    [SerializeField, Tooltip("玩家当前拥有的金币数量")]
    private int m_PlayerCoins = 1000; // 默认给1000金币作为示例
    #endregion

    #region 公共属性
    // 添加一个公共属性来读取金币数量
    public int PlayerCoins => m_PlayerCoins;
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

    /// <summary>
    /// 增加玩家的金币数量。
    /// </summary>
    /// <param name="_amount">要增加的金币数量 (必须为正数)。</param>
    public void AddCoins(int _amount)
    {
        if (_amount <= 0)
        {
            Debug.LogWarning($"尝试增加非正数的金币: {_amount}");
            return;
        }
        m_PlayerCoins += _amount;
        Debug.Log($"增加了 {_amount} 金币，当前总计: {m_PlayerCoins}");
        // 注意：这里需要调用保存逻辑才能在关闭游戏后保留数据
    }

    /// <summary>
    /// 尝试花费指定数量的金币。
    /// </summary>
    /// <param name="_amount">要花费的金币数量 (必须为正数)。</param>
    /// <returns>如果金币足够并成功花费，返回 true；否则返回 false。</returns>
    public bool TrySpendCoins(int _amount)
    {
        if (_amount <= 0)
        {
            Debug.LogWarning($"尝试花费非正数的金币: {_amount}");
            return false;
        }

        if (m_PlayerCoins >= _amount)
        {
            m_PlayerCoins -= _amount;
            Debug.Log($"花费了 {_amount} 金币，剩余: {m_PlayerCoins}");
            // 注意：这里需要调用保存逻辑才能在关闭游戏后保留数据
            return true;
        }
        else
        {
            Debug.LogWarning($"金币不足！需要: {_amount}, 当前拥有: {m_PlayerCoins}");
            return false;
        }
    }

    /// <summary>
    /// 将一个零件添加到玩家拥有的零件列表中。
    /// 注意：目前允许在列表中存在对同一 PartDataSO 的重复引用，
    /// 这意味着玩家可以"拥有"同一类型零件的多个实例（如果UI支持这样显示）。
    /// </summary>
    /// <param name="partToAdd">要添加的零件数据。</param>
    /// <returns>如果成功添加（或零件已存在且允许重复），返回 true。</returns>
    public bool AddPart(PartDataSO partToAdd)
    {
        if (partToAdd == null)
        {
            Debug.LogWarning("尝试向库存中添加一个 null 零件。");
            return false;
        }

        m_OwnedParts.Add(partToAdd);
        Debug.Log($"零件 '{partToAdd.PartName}' 已添加到库存。");
        // TODO: 在此处或调用此方法后考虑调用数据保存逻辑
        // OnInventoryChanged?.Invoke(); // 如果有事件系统用于通知UI等更新
        return true;
    }

    /// <summary>
    /// Attempts to remove a part from the owned parts list.
    /// Will fail if the part is currently equipped.
    /// </summary>
    /// <param name="partToRemove">The part data to remove.</param>
    /// <returns>True if the part was successfully removed, false otherwise (e.g., not owned or equipped).</returns>
    public bool RemovePart(PartDataSO partToRemove)
    {
        if (partToRemove == null)
        {
            Debug.LogWarning("Attempted to remove a null part.");
            return false;
        }

        // CRITICAL: Check if the part is currently equipped
        if (m_EquippedEngine == partToRemove || m_EquippedTires == partToRemove || m_EquippedNOS == partToRemove)
        {
            Debug.LogWarning($"Cannot remove part '{partToRemove.PartName}' because it is currently equipped.");
            return false;
        }

        // Attempt to remove from the list
        bool removed = m_OwnedParts.Remove(partToRemove);
        if (removed)
        {
            Debug.Log($"Removed part '{partToRemove.PartName}' from inventory.");
        }
        else
        {
            Debug.LogWarning($"Could not remove part '{partToRemove.PartName}' from inventory (might not be owned?).");
        }
        return removed;
    }

    #endregion
} 