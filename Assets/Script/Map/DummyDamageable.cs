using UnityEngine;

/// <summary>
/// [테스트 전용 · 삭제 예정] 적의 "피격측(被擊側)" 스탠드인.
///
/// 유닛 담당이 실물 적 오브젝트를 확정하기 전에, 아군(Hero)과의 상호작용을
/// 지금 검증하기 위한 더미다. 스탯은 인스펙터 값만 쓴다(데이터테이블 미사용).
///
/// <b>Hero 방식에 맞춤</b>: Hero는 자기 트리거 콜라이더 범위에 들어온 태그 "Enemy"
/// 오브젝트를 <c>OnTriggerEnter</c>로 타겟팅한다. 그래서 이 더미는 스스로
/// (1) 태그 "Enemy" 설정, (2) 활성 콜라이더 보장, (3) kinematic Rigidbody 부착
/// (트리거 이벤트는 한쪽에 Rigidbody가 있어야 발생) 을 해서 Hero에 감지되게 한다.
/// 레이어는 MapBoard의 광역질의(건물·원거리용)에 걸리도록 옵션으로 둔다.
///
/// 공격 로직은 만들지 않는다 — 그건 유닛(Hero) 몫. 이 컴포넌트는 "맞고 죽는" 쪽만 담당한다.
/// 실물 적(IDamageAble 구현 MonoBehaviour)이 붙는 순간 이 컴포넌트는 통째로 지우면 된다.
/// </summary>
public class DummyDamageable : MonoBehaviour, IDamageAble
{
    [Header("더미 스탯 (인스펙터 값)")]
    public float maxHp = 20f;
    public int defense = 0;

    [Header("Hero 감지용 노출")]
    [Tooltip("Hero의 OnTriggerEnter가 잡도록 이 태그로 설정한다(Hero.cs는 \"Enemy\"를 본다). 비우면 태그 변경 안 함.")]
    public string enemyTag = "Enemy";
    [Tooltip("콜라이더가 없을 때 자동으로 붙일 트리거 SphereCollider 반지름.")]
    public float autoColliderRadius = 0.5f;
    [Tooltip("0~31이면 이 레이어로 강제(MapBoard 광역질의 mask용, 선택). 음수면 현재 레이어 유지.")]
    public int forceLayer = -1;

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
        // (1) Hero가 태그로 타겟팅 → 태그 설정.
        if (!string.IsNullOrEmpty(enemyTag)) gameObject.tag = enemyTag;

        // (2) 레이어(광역질의용, 선택).
        if (forceLayer >= 0 && forceLayer <= 31) gameObject.layer = forceLayer;

        // (3) 활성 콜라이더 보장.
        Collider col = GetComponentInChildren<Collider>();
        if (col == null)
        {
            var sphere = gameObject.AddComponent<SphereCollider>();
            sphere.radius = autoColliderRadius;
            sphere.isTrigger = true; // 물리·타일 피킹은 방해 않도록
            col = sphere;
        }
        col.enabled = true;

        // (4) 트리거 이벤트(Hero.OnTriggerEnter)는 한쪽에 Rigidbody가 있어야 발생.
        //     적은 transform으로 이동(EnemyUnit)하니 물리 영향 없는 kinematic으로 붙인다.
        if (!TryGetComponent(out Rigidbody rb)) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
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
