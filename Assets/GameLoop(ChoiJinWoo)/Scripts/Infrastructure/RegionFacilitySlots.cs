using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RegionFacilitySlots
{
    [Tooltip("이 지역이 대응하는 ModuleLogic의 moduleId. 모듈 프리팹은 안 건드리고 번호만 맞춰서 엮는다.")]
    [SerializeField] private int moduleId;

    [Tooltip("이 지역의 건설 슬롯 개수. 밸런스 테스트하며 자유롭게 조정.")]
    [SerializeField, Min(1)] private int slotCount = 4;

    private List<RegionFacilitySlot> slots;

    public int ModuleId => moduleId;
    public string RegionName => string.Format(DataTableManager.StringTable.Get("Ui_RegionName"), moduleId);
    public IReadOnlyList<RegionFacilitySlot> Slots => EnsureInitialized();

    public event Action OnSlotsChanged;

    private List<RegionFacilitySlot> EnsureInitialized()
    {
        if (slots == null)
        {
            slots = new List<RegionFacilitySlot>(slotCount);
            for (int i = 0; i < slotCount; i++)
            {
                slots.Add(new RegionFacilitySlot());
            }
        }
        return slots;
    }

    public bool TryAssign(int index, object occupant, Sprite icon)
    {
        var list = EnsureInitialized();
        if (index < 0 || index >= list.Count || !list[index].IsEmpty) return false;

        list[index].Assign(occupant, icon);
        OnSlotsChanged?.Invoke();
        return true;
    }

    public bool TryClear(int index)
    {
        var list = EnsureInitialized();
        if (index < 0 || index >= list.Count || list[index].IsEmpty) return false;

        list[index].Clear();
        OnSlotsChanged?.Invoke();
        return true;
    }

    public int TotalWorkers()
    {
        int total = 0;
        foreach (var slot in EnsureInitialized())
        {
            if (slot.Occupant is ProductionFacility facility) total += facility.WorkerAmount;
        }
        return total;
    }
}
