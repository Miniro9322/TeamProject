using UnityEngine.EventSystems;

// 팀원 빌딩 UI/낮밤 규칙과 이어주는 창구. 테스트용 — 나중에 삭제할 코드라 다듬지 않고 원본 로직 그대로 옮김.
public class BuildingUiLink
{
    public UiManager ui;
    public GameManager rule;

    public bool CanBuild()
    {
        return rule == null || rule.CanBuild;
    }

    // 빌딩 UI 처리 후 계속 진행하면 true, UI 위 클릭이라 멈춰야 하면 false.
    public bool OpenIfBuilding(Tile tile, PlaceMode mode)
    {
        if (ui != null && tile.State.Occupant == OccupantKind.Resource && mode != PlaceMode.Remove && CanBuild())
        {
            ui.OpenBuildingUi(tile.OccupantObject.GetComponent<ProductionFacility>());
            return true;
        }
        if (EventSystem.current.IsPointerOverGameObject()) return false;
        Close();
        return true;
    }

    public void CloseUnlessOverUi()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        Close();
    }

    private void Close()
    {
        if (ui != null && ui.BuildingUiOpen) ui.CloseBuildingUi();
    }
}
