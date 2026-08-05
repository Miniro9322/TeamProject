/// <summary>
/// 디버프 장부를 들고 있는 대상(적/영웅 공용). IStunAble과 같은 취지 — 호출자는 이 계약만 안다.
///
/// EnemyBase가 구현한다. Hero는 아직 구현하지 않으므로(팀원 소유 파일) 영웅에게 디버프를 걸면
/// 효과는 들어가되 장부에는 안 남는다 — DebuffSO가 null 장부를 건너뛴다.
/// Hero에 이 두 줄이 생기는 날 SO는 한 줄도 안 고치고 영웅 쪽 조회가 켜진다.
/// </summary>
public interface IDebuffCarrier
{
    DebuffTracker Debuffs { get; }

    /// <summary>걸리지 않는 디버프 종류들(OR 조합). 없으면 DebuffType.None.</summary>
    DebuffType ImmuneDebuffs { get; }
}
