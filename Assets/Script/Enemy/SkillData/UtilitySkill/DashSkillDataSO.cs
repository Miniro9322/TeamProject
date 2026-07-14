using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

[CreateAssetMenu(menuName = "Data/Skill/DashSkill")]
public class DashSkillDataSO : UtilitySkillDataSO
{
    public float distance = 10f;   // 경로를 따라 앞으로 이동할 거리(월드). 칸 단위로 쓰려면 board.CellSize를 곱해 넘길 것.

    public override async UniTask Execute(EnemyBase owner, CancellationToken token)
    {
        if (owner == null || !owner.HasPath || owner.Board == null) return;

        Vector3 start = owner.transform.position;
        Vector3 end   = owner.PointAhead(distance, out int landIndex); // 경로 기준 앞선 지점

        owner.MovementSuspended = true; // 대시 동안 일반 경로 이동이 위치를 덮어쓰지 않게 정지
        float prevSpeed = owner.animator != null ? owner.animator.speed : 1f; // 대시 후 원래 속도로 복원(슬로우/헤이스트 등 보존)
        try
        {
            if (owner.animator != null) owner.animator.speed = 4f;
            float speed = duration > 0f ? 1f / duration : 1f;
            bool blocked = false;
            for (float t = 0f; t < 1f && owner != null && !owner.IsDie; )
            {
                // 저지당하면(대시 시작 시 이미 저지 or 대시 중 적을 만남) 그 자리에서 대시 중단.
                if (owner.Board.IsBlocked(owner.gameObject)) { blocked = true; break; }

                t += Time.deltaTime * speed;
                Vector3 next = Vector3.Lerp(start, end, Mathf.Clamp01(t));
                owner.transform.position = next;
                owner.Board.MoveEnemy(owner.gameObject, next); // 칸 보고(여기서 저지 상태가 갱신됨)
                await UniTask.Yield(token); // 파괴/비활성 시 취소돼 파괴된 오브젝트 접근 방지
            }

            if (owner != null && !owner.IsDie)
            {
                if (!blocked)
                {
                    owner.transform.position = end;                 // 완주했을 때만 착지 지점으로 스냅
                    if (landIndex >= 0) owner.ResumeFrom(landIndex); // 정상 이동을 착지 지점부터 이어받기
                }
                else
                {
                    owner.ResumeFromNearest(); // 저지로 멈춤 — 현재 위치에서 경로 이어가기(멈춘 자리 유지)
                }
            }
        }
        finally
        {
            if (owner != null)
            {
                owner.MovementSuspended = false;
                if (owner.animator != null) owner.animator.speed = prevSpeed;
            }
        }
    }
}
