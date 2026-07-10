
public enum TerrainType
{
    Core,   // 본진 영역. 방어 대상이며 일반 배치 불가
    Ground, // 지상. 적 경로·근접 영웅·생산 건물의 후보 타일
    High,   // 고지. 적 이동을 막는 벽 성격, 원거리 영웅 배치
    Empty   // 장식/미사용 빈 타일(외곽 경계 포함)
}

/// <summary>플레이어가 이 타일을 쓸 수 있는지의 상태(설계 §3 TerritoryState).</summary>
/// <remarks>기획상 "개활지"는 별도 지형이 아니라 Unclaimed 상태로 본다(설계 §3, decision_ans).</remarks>
public enum TerritoryState
{
    Unclaimed, // 미점령(개활지). 배치·건설 불가
    Claimed    // 점령됨. 배치·건설 후보
}

/// <summary>기존 씬 데이터 호환용 값. 새 코드는 TileState의 명시 필드를 사용한다.</summary>
public enum TileFlags
{
    None              = 0,
    EnemyLane         = 1,
    MeleePlaceable    = 2,
    RangedPlaceable   = 4,
    BuildingPlaceable = 8
    //방식 변경 고려
}
