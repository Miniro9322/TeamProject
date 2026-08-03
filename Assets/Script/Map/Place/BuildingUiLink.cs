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
    // 생산 시설은 기반시설 UI로 옮겨가 더 이상 타일에 존재하지 않으니 그 분기는 제거했다.
    public bool OpenIfBuilding(Tile tile, PlaceMode mode)
    {
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
