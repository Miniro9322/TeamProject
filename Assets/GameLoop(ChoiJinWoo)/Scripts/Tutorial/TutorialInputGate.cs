// ESC로 패널을 닫는 여러 곳(RegionOverviewPanel, AddCitizen, UiManager 등)이 튜토리얼 진행 중인지
// 확인하는 데 쓴다. 이벤트 버스가 없는 프로젝트라 HeroSelectionService와 같은 static 서비스 패턴을
// 따른다 - 굳이 각 패널에 TutorialManager를 주입하는 역방향 의존을 만들지 않기 위함이다.
public static class TutorialInputGate
{
    public static bool BlockEscapeClose { get; set; }

    // ESC 외의 단축키(B/N/O/G/P/U/R/E/I 등)를 튜토리얼 진행 중에 막는 데 쓴다. 스포트라이트가
    // 짚어주는 순서를 벗어나 임의로 다른 패널을 열면 튜토리얼 스텝이 꼬인다. BlockEscapeClose와
    // 항상 같은 구간(TutorialManager가 켜져 있는 동안)에서 함께 토글된다.
    public static bool BlockHotkeys { get; set; }

    // SaveManager가 0일차 연습 상태를 세이브 파일에 남기지 않으려고 확인한다 - 0일차는 다음 날이
    // 되는 순간 전부 초기 상태로 되돌아가는 임시 데이터라, 그 사이에 저장되면 안 된다.
    // TutorialManager가 켜져 있는 동안(0일차 리셋까지 끝날 때까지) true.
    public static bool BlockSave { get; set; }
}
