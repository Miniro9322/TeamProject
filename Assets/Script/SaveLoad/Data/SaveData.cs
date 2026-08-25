using System;

// 슬롯 하나의 저장 내용을 전부 담는 최상위 데이터 모음 (아래 항목별 데이터 모음들을 한데 묶음)
[Serializable]
public class SaveData
{
    public SavePhase savePhase;   // 낮 시작에 찍었는지 밤 직전에 찍었는지
    public string saveTime;       // 저장한 시각
    public float playTime;        // 이 슬롯의 누적 플레이 시간

    public int dayCount;                 // 저장 시점의 일차
    public int baseHp;                   // 저장 시점의 기지 체력
    public byte heroUnlock;              // 해금된 영웅들을 비트로 모아둔 값 (영웅 하나가 아님)
    public bool perfectDefensePending;   // 완벽 방어 특수자원 보상을 아직 안 줬는지

    public string heroDrawSeed;    // 영웅 뽑기 시드 (슬롯 생성 시 한 번 정해진 뒤 불변)
    public int heroDrawCount;      // 지금까지 영웅을 뽑은 횟수 (뽑기 결과의 재현 순번)
    public int heroCombineCount;   // 지금까지 영웅을 합성한 횟수 (합성 결과의 재현 순번)

    public int woodAmount;      // 목재
    public int foodAmount;      // 식량
    public int goldAmount;      // 골드
    public int ironAmount;      // 철
    public int stoneAmount;     // 석재
    public int specialAmount;   // 특수자원

    public int currentCitizen;    // 현재 시민 수
    public int usedCitizen;       // 기반시설에 배치된 일꾼 수 합계
    public int heroUsedCitizen;   // 로스터 영웅들의 시민 비용 합계

    public RegionSave[] regionList = Array.Empty<RegionSave>();   // 지역들의 상태
    public BuildSave[] buildList = Array.Empty<BuildSave>();     // 지어진 기반시설들
    public HeroSave[] heroList = Array.Empty<HeroSave>();       // 보유 영웅 로스터
    public PlaceSave[] placeList = Array.Empty<PlaceSave>();     // 맵에 배치된 영웅들
    public TierSave[] tierList = Array.Empty<TierSave>();       // 티어별 강화 레벨
    public ClassSave[] classList = Array.Empty<ClassSave>();     // 클래스별 강화 레벨
    public PortalSave[] portalList = Array.Empty<PortalSave>();   // NightReady일 때만 채워지는 활성 포탈
    public string[] archiveList = Array.Empty<string>();      // 발견한 적 도감 ID 목록
    public int[] savedDayList = Array.Empty<int>();           // 지금까지 저장된 일차 번호 목록
}
