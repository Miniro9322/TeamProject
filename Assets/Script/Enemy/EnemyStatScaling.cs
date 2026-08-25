using UnityEngine;

/// <summary>
/// 적 스탯의 라운드·지역 배율 계산. 실제로 스폰되는 값(EnemyBase.ApplyData)과
/// 스테이지 정보 툴팁에 미리 보여주는 값(StageInfoView)이 반드시 같아야 하므로 한 곳에 모았다.
/// 여기 표나 식을 고치면 양쪽 다 따라온다 — 예전처럼 한쪽만 바뀌어 툴팁이 거짓말하는 일이 없다.
/// </summary>
public static class EnemyStatScaling
{
    /// 일차·해금 지역 수를 반영한 스탯. AttackSpeed·MoveSpeed는 배율이 없어 여기 없다.
    public readonly struct Stats
    {
        public readonly float Hp;
        public readonly float Attack;
        public readonly float Defense;

        public Stats(float hp, float attack, float defense)
        {
            Hp = hp;
            Attack = attack;
            Defense = defense;
        }
    }

    /// 체력은 매일, 공격력·방어력은 5일마다 한 단계 오른다(정수 나눗셈이라 1~4일차는 0단계).
    /// 체력·방어력에는 해금 지역 수 배율을 곱한다 — 지역을 더 열수록 같은 몹도 계단식으로 단단해진다.
    public static Stats Compute(EnemyTable.Data data, EnemyClass cls, int dayCount)
    {
        int fiveDayStep = dayCount / 5;
        return new Stats(
            (data.Health + (dayCount * data.UpHealthScale)) * RegionHpScale(cls),
            data.Attack + (data.UpAttackScale * fiveDayStep),
            (data.Defense + (data.UpDefenseScale * fiveDayStep)) * RegionDefenseScale(cls));
    }

    /// CSV 원본 Class 문자열(EnemyTable.Data.Class)을 그대로 넘기는 쪽.
    public static Stats Compute(EnemyTable.Data data, string cls, int dayCount)
        => Compute(data, ParseClass(cls), dayCount);

    /// 대소문자·공백을 무시하고 파싱한다. EnemyBase가 Class를 만드는 방식과 같아야
    /// CSV에 "boss"라고 적혀도 실제 적과 툴팁이 같은 배율을 쓴다.
    public static EnemyClass ParseClass(string raw)
        => !string.IsNullOrEmpty(raw) && System.Enum.TryParse(raw.Trim(), true, out EnemyClass c)
            ? c
            : EnemyClass.Normal;

    // 해금된 지역 수별 배율(인덱스 = 해금 수). 계산식이 아니라 디자이너가 정한 곡선이라 표로 둔다.
    // 게임 시작 상태가 이미 1개 해금이므로 0·1 칸은 1배로 두고 2개째 해금부터 배율이 붙는다.
    // 표 길이를 넘는 해금 수는 마지막 값으로 고정되므로, 지역이 늘면 칸을 추가하면 된다.
    // 보스 전용 표를 따로 두고, Elite·Normal은 잡몹 표를 공유한다.
    private static readonly float[] RegionHpScaleTable = { 1f, 1f, 1.25f, 1.6f, 2.15f, 3f, 4f };
    private static readonly float[] RegionBossHpScaleTable = { 1f, 1f, 1.5f, 2.5f, 3.9f, 5.7f, 8f };
    private static readonly float[] RegionDefenseScaleTable = { 1f, 1f, 1.15f, 1.3f, 1.5f, 1.75f, 2f };
    private static readonly float[] RegionBossDefenseScaleTable = { 1f, 1f, 1.3f, 1.75f, 2.4f, 3.2f, 4f };

    // 전 지역 해금이 끝나면 위 표가 마지막 칸에서 멈춘다 — 그 뒤로는 판이 더 길어져도 보스가 그 자리에 선다.
    // 그래서 해금이 다 끝난 뒤부터 10라운드마다 배율에 +2를 더해 계속 오르게 한다.
    // 기준점은 "전 지역 해금을 확인한 라운드"라 해금 직후엔 +0에서 시작한다(끊기지 않게).
    // 보스만 적용한다 — 잡몹은 DayCount × UpHealthScale로 이미 매 라운드 오르고 있다.
    private const int BossHpStepRounds = 10;
    private const float BossHpStepScale = 2f;

    public static float RegionHpScale(EnemyClass cls)
        => RegionScale(RegionHpScaleTable, RegionBossHpScaleTable, cls) + BossFullUnlockHpBonus(cls);

    public static float RegionDefenseScale(EnemyClass cls)
        => RegionScale(RegionDefenseScaleTable, RegionBossDefenseScaleTable, cls);

    // 표 길이는 자기 표 기준으로 클램프한다(표마다 칸 수가 달라져도 안전하게).
    private static float RegionScale(float[] normalTable, float[] bossTable, EnemyClass cls)
    {
        float[] table = cls == EnemyClass.Boss ? bossTable : normalTable;
        int unlocked = SpawnerManager.UnlockedRegionCount;
        if (unlocked <= 0) return table[0];
        return table[Mathf.Min(unlocked, table.Length - 1)];
    }

    private static float BossFullUnlockHpBonus(EnemyClass cls)
    {
        if (cls != EnemyClass.Boss) return 0f;
        return SpawnerManager.RoundsSinceFullUnlock / BossHpStepRounds * BossHpStepScale;
    }
}
