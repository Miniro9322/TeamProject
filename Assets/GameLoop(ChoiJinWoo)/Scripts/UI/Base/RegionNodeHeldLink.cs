using UnityEngine;

// 지역 노드 버튼을 그 지역의 DetailPanel이 열려있는 동안 눌린 모양으로 유지한다.
public class RegionNodeHeldLink : HeldLinkBase
{
    [SerializeField] private RegionDetailPanel detailPanel;
    [SerializeField] private RegionNodeView nodeView;

    // 지금 열린 지역이 이 노드가 대표하는 지역과 같은지로 눌림 여부를 판단한다
    protected override bool CheckHeld()
    {
        var region = detailPanel.CurrentRegion;
        return region != null && region.ModuleId == nodeView.ModuleId;
    }
}
