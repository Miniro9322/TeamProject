/// <summary>
/// 맵 도메인 열거형 모음. 씬의 큐브 이름(Cube_Concrete/Grass/Wood/Stone/…)을
/// 게임 의미로 번역한 결과가 TileKind다.
/// </summary>
public enum TileKind
{
    /// <summary>회색 콘크리트. 지상 — 적이 지나가고 근접 유닛/건물을 놓을 수 있다.</summary>
    Ground,
    /// <summary>고지대. 적이 통행 불가(경로가 우회), 원거리 유닛만 배치.</summary>
    High,
    /// <summary>초록 잔디 테두리. 외곽 경계 — 통행·배치 모두 불가.</summary>
    Border,
    /// <summary>갈색 나무. 적 스폰 지점 — 경로의 시작.</summary>
    Spawn,
    /// <summary>돌. 본진 — 경로의 끝.</summary>
    Core,
    /// <summary>위 어디에도 해당하지 않는 큐브.</summary>
    Unknown
}

/// <summary>타일에 놓을 수 있는 것. 원거리 유닛은 아직 프리팹이 없고 틀만 존재.</summary>
public enum PlaceKind
{
    /// <summary>근접/지상 유닛(더미 캡슐). 지상 타일에 배치.</summary>
    Unit,
    /// <summary>원거리 유닛(더미). 고지 타일에만 배치.</summary>
    Ranged,
    /// <summary>건물(더미 박스). 지상 타일에 배치.</summary>
    Building
}
