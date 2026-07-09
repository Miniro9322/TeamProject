using UnityEngine;

/// <summary>
/// [테스트 전용 · 삭제 예정] Hero(실물 아군 유닛)를 흉내 낸 최소 공격자.
///
/// <see cref="Hero"/>와 <b>똑같은 구조</b>로 동작한다:
///   트리거 콜라이더 = 사거리 → 범위에 들어온 태그 "Enemy"를 <c>OnTriggerEnter</c>로 타겟팅
///   → 공격 간격마다 타겟의 <see cref="IDamageAble.TakeDamage"/> 호출.
///
/// 차이는 딱 하나 — Hero가 아직 비워 둔 "실제 데미지"까지 채웠다는 것.
/// 즉 이건 Hero의 스탠드인이자, 유닛팀이 <c>Hero.Attack()</c>에 넣을 코드의 참조 틀이다.
/// Hero.Attack()이 TakeDamage를 호출하게 완성되면 이 컴포넌트는 삭제한다.
///
/// 전제(적 쪽): 태그 "Enemy" + 콜라이더 + Rigidbody(트리거 이벤트 발생 조건).
/// → <see cref="DummyDamageable"/>가 이 3종을 자동 보장하므로 그대로 맞물린다.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class DummyMeleeAttacker : MonoBehaviour
{
    [Tooltip("사거리 = 트리거 반경(월드 단위). Hero는 자기 콜라이더가 이 역할을 한다.")]
    public float range = 2f;
    [Tooltip("공격 1회 데미지.")]
    public int power = 5;
    [Tooltip("공격 간격(초). Hero의 attackSpeed와 같은 의미.")]
    public float attackInterval = 0.5f;
    [Tooltip("타겟으로 삼을 태그. Hero.cs와 동일하게 \"Enemy\".")]
    public string targetTag = "Enemy";
    public bool logAttack = true;

    private GameObject _target;
    private float _timer;
    private SphereCollider _rangeCollider;

    private void Awake() => Apply();
    private void OnValidate() => Apply();

    /// <summary>트리거 콜라이더를 사거리에 맞춘다. 런타임에 range를 바꾼 뒤에도 호출.</summary>
    public void Apply()
    {
        if (_rangeCollider == null) _rangeCollider = GetComponent<SphereCollider>();
        if (_rangeCollider == null) return;
        _rangeCollider.isTrigger = true;

        // 트리거 반경은 오브젝트 스케일의 영향을 받는다. 더미 유닛이 축소돼 있어도(예: 0.4배,
        // 타일 큐브 스케일까지 곱)  월드 반경이 range와 같도록 lossyScale로 보정한다.
        Vector3 ls = transform.lossyScale;
        float s = Mathf.Max(Mathf.Abs(ls.x), Mathf.Abs(ls.y), Mathf.Abs(ls.z));
        _rangeCollider.radius = s > 1e-4f ? range / s : range;
    }

    // ── Hero와 동일한 타겟팅 ──
    private void OnTriggerEnter(Collider other)
    {
        if (_target == null && other.CompareTag(targetTag))
        {
            _target = other.gameObject;
            if (logAttack) Debug.Log($"[DummyMeleeAttacker] {name} 사거리 진입 감지 → 타겟 {_target.name}", this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (_target == other.gameObject) _target = null;
    }

    private void Update()
    {
        if (_target == null) { _timer = 0f; return; } // Idle: 타겟 없음

        _timer += Time.deltaTime;
        if (_timer < attackInterval) return;
        _timer = 0f;
        Attack();
    }

    /// <summary>Hero.Attack()이 채워야 할 부분 = 이 로직(타겟의 IDamageAble에 데미지).</summary>
    private void Attack()
    {
        if (_target.GetComponentInParent<IDamageAble>() is not IDamageAble d) return;
        d.TakeDamage(power);
        if (logAttack) Debug.Log($"[DummyMeleeAttacker] {name} → {_target.name} 공격 (-{power}, 남은 HP {d.Hp:0.#})", this);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
