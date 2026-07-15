using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

[CreateAssetMenu(menuName = "Data/Skill/SummonSkill")]
public class SummonSkillDataSO : UtilitySkillDataSO
{
    public GameObject summonPrefab;   // 소환할 프리팹
    public string summonStateName = "Summon";  //애니메이션 스테이트 이름이 Summon으로 일치해야함
    public float animTimeout = 5f;

    public override async UniTask Execute(EnemyBase owner, CancellationToken token)
    {
        if (summonPrefab == null)
        {
            Debug.LogWarning($"SummonSkill '{skillName}': summonPrefab 미지정");
            return;
        }

        if (owner.Board == null)
        {
            Debug.LogWarning($"SummonSkill '{skillName}': 소환자에 MapBoard가 없어 소환수가 이동할 수 없습니다.");
            return;
        }

        owner.MovementSuspended = true; // 소환 모션 동안 제자리 정지 (경로 이동이 위치를 안 덮어씀)
        try
        {
            // 소환 애니메이션 재생 → 모션이 끝날 때까지 대기.
            if (owner.animator != null)
            {
                owner.animator.SetTrigger("Summon");
                
                await WaitForAnimationEnd(owner, summonStateName, animTimeout, token);
            }

            if (owner == null || owner.IsDie) return; // 대기 중 죽었으면 소환 안 함

            int count = Mathf.Max(1, Mathf.RoundToInt(value));   // value = 소환 수
            Vector3 center = owner.transform.position;
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = center + new Vector3((i - (count - 1) * 0.5f) * 0.6f, 0f, 0f);
                var go = PoolManager.Instance.Spawn(summonPrefab, pos, Quaternion.identity);
                if (go.TryGetComponent(out EnemyBase enemy))
                    enemy.EnterMap(owner.Board, owner.Path, snapToStart: false);
                await UniTask.Delay(TimeSpan.FromSeconds(0.05f));
            }
        }
        finally
        {
            if (owner != null) owner.MovementSuspended = false; // 모션 끝 → 다시 이동
        }
    }

    // 트리거로 진입한 소환 스테이트가 끝날 때까지 대기.
    // 스테이트를 못 찾으면 timeout 초 후 탈출해 적이 영영 멈추는 것을 방지.
    private static async UniTask WaitForAnimationEnd(EnemyBase owner, string stateName, float timeout, CancellationToken token)
    {
        var anim = owner.animator;
        const int layer = 0;

        // 1) 트리거 → 소환 스테이트로 실제 전이될 때까지(전이 프레임) 대기.
        float elapsed = 0f;
        while (owner != null && !anim.GetCurrentAnimatorStateInfo(layer).IsName(stateName))
        {
            if (owner.IsDie) return;
            elapsed += Time.deltaTime;
            if (elapsed >= timeout)
            {
                Debug.LogWarning($"SummonSkill: Animator에서 '{stateName}' 스테이트를 찾지 못함 — 이름/전이 확인.", owner);
                return;
            }
            await UniTask.Yield(token); // 파괴/비활성 시 취소돼 파괴된 오브젝트 접근 방지
        }
        if (owner == null) return;

        // 2) 진입 시점의 클립 길이만큼 대기(애니메이터 speed 반영).
        var info = anim.GetCurrentAnimatorStateInfo(layer);
        float wait = info.length / Mathf.Max(0.01f, anim.speed);
        await UniTask.Delay(TimeSpan.FromSeconds(wait), cancellationToken: token);
    }
}
