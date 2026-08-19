using System;

// 기반시설 슬롯 하나 — 모듈ID·슬롯번호·점유종류·건물ID·강화횟수·일꾼수·지불액 (빈 슬롯은 목록에 담지 않음)
[Serializable]
public class BuildSave
{
    public int moduleId;               // 어느 지역인지
    public int slotIndex;              // 그 지역 안에서 몇 번 슬롯인지
    public OccupantKind buildKind;     // 빈칸/생산시설/주택 중 무엇인지
    public int buildId;                // 정확히 몇 번 건물인지
    public int upgradeCount;           // 몇 번 강화했는지
    public int workerAmount;           // 배치된 일꾼 수 (시설만 사용)
    public CostSave[] constructPaid;   // 지을 때 실제로 낸 자원
    public CostSave[] upgradePaid;     // 강화하며 지금까지 낸 자원 총합
}
