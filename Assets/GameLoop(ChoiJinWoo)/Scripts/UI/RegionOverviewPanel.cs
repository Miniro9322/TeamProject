using System.Collections.Generic;
using UnityEngine;

// 기반시설 UI 최상단 화면 - 지역 다이아몬드 오버뷰(§허브 + 6지역).
// 각 노드는 ModuleLogic.IsUnlocked를 그대로 반영한다 - 해금 자체는 기존 ExpandEvent 흐름을 그대로 탄다.
public class RegionOverviewPanel : MonoBehaviour
{
    [SerializeField] private MapRegistry registry;
    [SerializeField] private RegionDetailPanel detailPanel;
    [SerializeField] private List<RegionNodeView> nodes;

    private void OnEnable()
    {
        BindModules();
        Refresh();
    }

    private void OnDisable()
    {
        UnbindModules();
    }

    private void BindModules()
    {
        foreach (var module in registry.AllModules.Values)
        {
            module.OnStateChanged += OnModuleState;
        }
    }

    private void UnbindModules()
    {
        foreach (var module in registry.AllModules.Values)
        {
            module.OnStateChanged -= OnModuleState;
        }
    }

    private void OnModuleState(ModuleState state)
    {
        Refresh();
    }

    private void Refresh()
    {
        foreach (var node in nodes)
        {
            if (!registry.TryGetModuleLogic(node.ModuleId, out var module)) continue;

            node.SetLocked(!module.IsUnlocked);

            var region = module.GetComponent<RegionFacilitySlots>();
            if (region != null) node.SetLabel(region.RegionName);
        }
    }

    public void OnNodeClicked(RegionNodeView node)
    {
        if (!registry.TryGetModuleLogic(node.ModuleId, out var module) || !module.IsUnlocked) return;

        var region = module.GetComponent<RegionFacilitySlots>();
        if (region == null) return;

        detailPanel.Open(region);
    }
}
