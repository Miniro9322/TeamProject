using UnityEngine;

/// <summary>
/// 스턴 이펙트만 담당한다. EnemyBase가 소유하고 매 프레임 Tick으로 굴린다(EnemyCloak/EnemyBurrow와 같은 구조).
/// 프리팹이나 앵커가 없으면 Setup/Tick/Reset이 전부 조용히 no-op.
///
/// 소환/반납을 Stun() 호출이 아니라 "지금 스턴 상태인가"로 굴리는 게 핵심이다.
/// 그래서 스턴이 중복으로 들어와도 이펙트는 늘 하나이고(이미 떠 있으면 새로 안 만든다),
/// 만료 시각이 뒤로 밀리면 이펙트도 자동으로 그만큼 더 유지된다 —
/// Despawn(go, duration)으로 미리 예약해두면 나중에 들어온 스턴과 시각이 어긋난다.
///
/// 위치는 부모로 붙이지 않고 매 프레임 앵커를 따라간다(EnemyBurrow의 지면 마커와 같은 방식).
/// 자식으로 붙이면 유닛 스케일이 이펙트에 곱해져서, 보스나 분열체(SetScaleMul)에서 크기가 달라진다.
/// </summary>
public class EnemyStunEffect
{
    private GameObject _prefab;
    private Transform _anchor;
    private GameObject _effect;

    public bool IsSetup => _prefab != null && _anchor != null;

    /// <summary>EnemyBase가 Awake에서 1회 호출. anchor엔 보통 머리 위에 만든 빈 오브젝트를 넘긴다.</summary>
    public void Setup(GameObject prefab, Transform anchor)
    {
        _prefab = prefab;
        _anchor = anchor;
    }

    /// <summary>stunned가 true인 동안만 이펙트를 유지한다. 상승 엣지에 소환, 하강 엣지에 반납.</summary>
    public void Tick(bool stunned)
    {
        if (!IsSetup) return;

        if (!stunned) { Reset(); return; }

        // 이미 떠 있으면 새로 소환하지 않는다 — 스턴이 겹쳐 들어와도 이펙트는 하나다.
        if (_effect == null)
            _effect = PoolManager.Instance.Spawn(_prefab, _anchor.position, _prefab.transform.rotation);
        else
            _effect.transform.position = _anchor.position;   // 넉백·잠행 등으로 유닛이 움직여도 머리 위에 붙어 있게
    }

    /// <summary>
    /// 스턴 해제·사망·풀 반납에서 호출. 이펙트를 풀에 되돌린다.
    /// 안 되돌리면 적이 풀로 돌아갈 때 이펙트가 떠 있는 채로 남는다(EnemyBurrow 지면 마커와 같은 함정).
    /// </summary>
    public void Reset()
    {
        if (_effect == null) return;

        PoolManager.Instance.Despawn(_effect);
        _effect = null;
    }
}
