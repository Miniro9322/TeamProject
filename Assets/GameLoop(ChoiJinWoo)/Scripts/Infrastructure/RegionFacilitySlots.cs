using System;
using System.Collections.Generic;
using UnityEngine;

// 지역(맵 모듈)이 보유한 건설 슬롯들. ModuleLogic과 같은 오브젝트에 붙여 모듈ID로 묶는다.
// 슬롯 좌표는 없다 - 기반시설 UI에서만 관리되는 추상 슬롯(§2단계: 지역 클릭 -> 슬롯 그리드).
[RequireComponent(typeof(ModuleLogic))]
public class RegionFacilitySlots : MonoBehaviour
{
    [Tooltip("이 지역의 건설 슬롯 개수. 밸런스 테스트하며 자유롭게 조정.")]
    [SerializeField, Min(1)] private int slotCount = 4;

    [Tooltip("지역 이름(패널 헤더에 표시).")]
    [SerializeField] private string regionName = "지역";

    private readonly List<RegionFacilitySlot> slots = new();
    private ModuleLogic module;

    public ModuleLogic Module => module;
    public string RegionName => regionName;
    public IReadOnlyList<RegionFacilitySlot> Slots => slots;

    public event Action OnSlotsChanged;

    private void Awake()
    {
        module = GetComponent<ModuleLogic>();
        for (int i = 0; i < slotCount; i++)
        {
            slots.Add(new RegionFacilitySlot());
        }
    }

    public bool TryAssign(int index, GameObject occupant, Sprite icon, string label)
    {
        if (index < 0 || index >= slots.Count || !slots[index].IsEmpty) return false;

        slots[index].Assign(occupant, icon, label);
        OnSlotsChanged?.Invoke();
        return true;
    }

    public bool TryClear(int index)
    {
        if (index < 0 || index >= slots.Count || slots[index].IsEmpty) return false;

        slots[index].Clear();
        OnSlotsChanged?.Invoke();
        return true;
    }

    // 인구 로우가 읽을 이 지역의 총 배치 인력(생산 시설에만 있는 개념 - House는 셈하지 않는다).
    public int TotalWorkers()
    {
        int total = 0;
        foreach (var slot in slots)
        {
            if (slot.Occupant == null) continue;

            var facility = slot.Occupant.GetComponent<ProductionFacility>();
            if (facility != null) total += facility.WorkerAmount;
        }
        return total;
    }
}
