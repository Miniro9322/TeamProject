using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [테스트 전용 · 삭제 예정] 아군 유닛의 "공격측(攻擊側)" 스탠드인.
///
/// 목적은 단 하나 — <b>적이 사거리 안에 들어오면 공격 판정이 뜨는지</b> 확인.
/// attackInterval마다 <see cref="MapBoard.DamageablesInRange"/>로
/// "내 사거리(타일 칸) 안에 적(IDamageAble)이 있나?"를 질의하고,
/// 있으면 가장 가까운 대상에게 공격 판정을 낸다(콘솔 로그 + TakeDamage).
///
/// 데이터테이블은 쓰지 않는다 — 사거리/데미지/간격은 전부 인스펙터 값.
/// 실물 유닛이 나오면 이 컴포넌트는 통째로 지운다.
/// </summary>
[DisallowMultipleComponent]
public class DummyAttacker : MonoBehaviour
{
    [Tooltip("공간 질의를 맡길 보드. 비우면 씬에서 자동으로 찾는다.")]
    public MapBoard board;

    [Header("공격 파라미터 (인스펙터 값만)")]
    [Tooltip("사거리 — 타일 칸 수.")]
    public int range = 2;
    [Tooltip("공격 판정 1회의 데미지.")]
    public int power = 5;
    [Tooltip("공격 판정 간격(초). 0 이하면 매 프레임.")]
    public float attackInterval = 0.5f;
    [Tooltip("적을 찾을 레이어. 기본 전체(적 더미는 Default 레이어).")]
    public LayerMask targetMask = ~0;
    [Tooltip("사거리 모양: 켜면 사각(체비셰프), 끄면 다이아몬드(맨해튼).")]
    public bool squareRange = false;
    public bool logJudgement = true;

    private float _cooldown;

    private void Awake()
    {
        if (board == null) board = FindFirstObjectByType<MapBoard>();
    }

    private void Update()
    {
        if (board == null) return;

        _cooldown -= Time.deltaTime;
        if (_cooldown > 0f) return;
        _cooldown = Mathf.Max(0f, attackInterval);

        IDamageAble target = FindNearestInRange();
        if (target == null) return;

        // ── 공격 판정 ──
        if (logJudgement)
            Debug.Log($"[DummyAttacker] {name} 사거리 {range}칸 안에 적 진입 → 공격 판정! (대상 HP {target.Hp:0.#} → -{Mathf.Max(1, power - target.Defense)})", this);
        target.TakeDamage(power);
    }

    /// <summary>사거리 안 적 후보 중 가장 가까운 하나. 없으면 null.</summary>
    private IDamageAble FindNearestInRange()
    {
        List<IDamageAble> inRange = board.DamageablesInRange(transform.position, range, targetMask, squareRange);
        if (inRange.Count == 0) return null;

        Vector2Int origin = board.WorldToCell(transform.position);
        IDamageAble best = null;
        int bestDist = int.MaxValue;
        foreach (IDamageAble d in inRange)
        {
            if (d is MonoBehaviour mb && mb != null)
            {
                int dist = MapBoard.TileDistance(origin, board.WorldToCell(mb.transform.position), squareRange);
                if (dist < bestDist) { bestDist = dist; best = d; }
            }
            else best ??= d;
        }
        return best;
    }

    // 씬 뷰에서 사거리 확인용(선택 시).
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.35f);
        float r = board != null ? (range + 0.5f) * board.CellSize : range + 0.5f;
        Gizmos.DrawWireSphere(transform.position, r);
    }
}
