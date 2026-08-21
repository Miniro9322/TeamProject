// ESC로 패널을 닫는 여러 곳(RegionOverviewPanel, AddCitizen, UiManager 등)이 튜토리얼 진행 중인지
// 확인하는 데 쓴다. 이벤트 버스가 없는 프로젝트라 HeroSelectionService와 같은 static 서비스 패턴을
// 따른다 - 굳이 각 패널에 TutorialManager를 주입하는 역방향 의존을 만들지 않기 위함이다.
public static class TutorialInputGate
{
    public static bool BlockEscapeClose { get; set; }
}
