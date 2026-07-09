using UnityEngine;

/// <summary>
/// [테스트 전용 · 삭제 예정] 적의 "피격측(被擊側)" 스탠드인.
///
/// 유닛 담당이 실물 적 오브젝트를 확정하기 전에, 상호작용 루프
/// (아군 유닛 → <see cref="MapBoard.DamageablesInRange"/> → <see cref="IDamageAble.TakeDamage"/>)를
/// 지금 검증하기 위한 더미다. 스탯은 인스펙터 값만 쓴다(데이터테이블 미사용).
///
/// OverlapSphere 질의에 걸리도록 <b>콜라이더/레이어를 스스로 보장</b>한다
/// (맵의 더미 적은 콜라이더가 꺼져 있어 원래는 질의에 안 잡힌다).
///
/// 공격 로직은 만들지 않는다 — 그건 유닛 담당 몫. 이 컴포넌트는 "맞고 죽는" 쪽만 담당한다.
/// 실물 적(IDamageAble 구현 MonoBehaviour)이 붙는 순간 이 컴포넌트는 통째로 지우면 된다.
/// </summary>
public class DummyDamageable : MonoBehaviour, IDamageAble
{
    [Header("더미 스탯 (인스펙터 값)")]
    public float maxHp = 20f;
    public int defense = 0;

    [Header("질의 노출")]
    [Tooltip("0~31이면 이 레이어로 강제(아군 유닛의 DamageablesInRange mask와 맞춰야 함). 음수면 현재 레이어 유지.")]
    public int forceLayer = -1;
    [Tooltip("콜라이더가 없을 때 자동으로 붙일 트리거 SphereCollider 반지름.")]
    public float autoColliderRadius = 0.5f;

    [Header("디버그")]
    public bool logHits = true;

    private float _hp;
    private bool _initialized;

    public float Hp => _hp;
    public int Defense => defense;

    /// <summary>사망 직전 알림(테스트 하네스가 목록에서 제거하는 용도). Destroy 전에 호출된다.</summary>
    public event System.Action<DummyDamageable> Died;

    private void Start() => Initialize();

    /// <summary>
    /// HP 초기화 + 콜라이더/레이어 보장. 인스펙터 배치 시엔 Start가 부른다.
    /// 런타임에 AddComponent 후 필드를 채운 경우(하네스), 채운 직후 직접 호출하면 즉시 반영된다. 두 번 불러도 안전.
    /// </summary>
    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        _hp = maxHp;
        EnsureQueryable();
    }

    private void EnsureQueryable()
    {
        if (forceLayer >= 0 && forceLayer <= 31) gameObject.layer = forceLayer;

        Collider col = GetComponentInChildren<Collider>();
        if (col == null)
        {
            var sphere = gameObject.AddComponent<SphereCollider>();
            sphere.radius = autoColliderRadius;
            sphere.isTrigger = true; // 질의(OverlapSphere)만 걸리고 물리·타일 피킹은 방해 않도록
            col = sphere;
        }
        col.enabled = true;
    }

    public void TakeDamage(int damage)
    {
        int hit = Mathf.Max(1, damage - defense);
        _hp -= hit;
        if (logHits) Debug.Log($"[DummyDamageable] {name} -{hit} → HP {_hp:0.#}/{maxHp:0.#}", this);
        if (_hp <= 0f) Die();
    }

    public void Die()
    {
        Died?.Invoke(this);
        Destroy(gameObject);
    }
}
