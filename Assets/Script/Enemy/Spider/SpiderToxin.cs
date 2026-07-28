using UnityEngine;
using VContainer.Unity;

public class SpiderToxin : EnemyBase
{
    public float hp;
    public float speed;
    public int attack;
    public int def; //테스트용 인스펙터 확인용 스탯들

    [SerializeField] private float poisonRatio = 0.3f;
    [SerializeField] private float poisonInterval = 0.5f;
    [SerializeField] private float poisonDuration = 4f;

    protected override void OnEnable()
    {
        base.OnEnable();
    }

    private void Start()
    {
        hp = Hp;
        speed = MoveSpeed;
        attack = AttackPower;
        def = Defense; //테스트용
    }
    public override void EnemySoundAttack()
    {
        base.EnemySoundAttack();
        EnemySoundManager.Play("SpiderAttack");
    }
    // 평타가 적중한 영웅에게 독을 건다. 독 상태는 PoisonRegistry가 들고 굴리므로
    // 이 거미가 죽거나 풀에 반납돼도 남은 독은 계속 들어간다.
    public override void AnimEvent_AttackHit()
    {
        base.AnimEvent_AttackHit();
        if (IsDead) return;

        // base가 이미 한 번 찾았지만 대상을 돌려주지 않아 다시 찾는다.
        // Range=1이라 격자 조회 비용은 무시할 수준(온히트 효과를 쓰는 적이 늘면 EnemyBase에 훅을 파는 게 낫다).
        GameObject target = FindAttackTarget();
        if (target == null) return;
        if (target.GetComponentInParent<Hero>() is not Hero hero) return; // 영웅이 아닌 점유물(건물 등)은 제외

        PoisonRegistry.Apply(hero,
            Mathf.Max(1, Mathf.RoundToInt(AttackPower * poisonRatio)),
            poisonInterval,
            poisonDuration,
            GameManager);
    }
}
