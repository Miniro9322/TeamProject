using UnityEngine;

// 테스트용 Hero를 흉내 낸 최소 공격자.
 
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
            if (logAttack) Debug.Log($"[Melee] {name} 타겟 {_target.name}", this);
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
        if (logAttack) Debug.Log($"[Melee] {name} → {_target.name} 공격 (-{power}, 남은 HP {d.Hp:0.#})", this);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
