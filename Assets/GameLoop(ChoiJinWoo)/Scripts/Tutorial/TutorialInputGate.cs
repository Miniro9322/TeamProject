// ESC로 패널을 닫는 여러 곳(RegionOverviewPanel, AddCitizen, UiManager 등)이 튜토리얼 진행 중인지
// 확인하는 데 쓴다. 이벤트 버스가 없는 프로젝트라 HeroSelectionService와 같은 static 서비스 패턴을
// 따른다 - 굳이 각 패널에 TutorialManager를 주입하는 역방향 의존을 만들지 않기 위함이다.
public static class TutorialInputGate
{
    public static bool BlockEscapeClose { get; set; }

    // SaveManager가 0일차 연습 상태를 세이브 파일에 남기지 않으려고 확인한다 - 0일차는 다음 날이
    // 되는 순간 전부 초기 상태로 되돌아가는 임시 데이터라, 그 사이에 저장되면 안 된다.
    // TutorialManager가 켜져 있는 동안(0일차 리셋까지 끝날 때까지) true.
    public static bool BlockSave { get; set; }
}
