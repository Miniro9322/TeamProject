using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

// 분열 스킬 — 유닛이 죽을 때(TriggerOnDeath) 자신과 같은 적을 value마리 스폰한다.
// 분열체는 최대 체력의 hpPercent 비율로 시작하며, maxGeneration으로 무한 분열을 막는다.
public class SplitSkillDataSO : UtilitySkillDataSO
{
    [Range(0f, 1f)]
    [Tooltip("분열체가 가질 체력 비율(최대 체력 대비). 0.5 = 50%.")]
    public float hpPercent = 0.5f;

    [Tooltip("분열 최대 세대. 1이면 원본만 분열하고 분열체는 다시 분열하지 않음(무한 방지).")]
    public int maxGeneration = 1;

    public override bool TriggerOnDeath => true; // 쿨다운이 아니라 죽을 때 발동

    public override async UniTask Execute(EnemyBase owner, CancellationToken token)
    {
        if (owner == null) return;
        if (owner.SplitGeneration >= maxGeneration) return; // 분열체는 더 이상 분열하지 않음

        int count = Mathf.Max(1, Mathf.RoundToInt(value)); // value = 분열 수
        int nextGen = owner.SplitGeneration + 1;
        float childHp = owner.MaxHp * hpPercent;

        // 죽는 시점의 위치/보드/경로를 캡처(스폰 도중 owner가 디스폰돼도 안전).
        Vector3 center = owner.transform.position;
        var board = owner.Board;
        var path = owner.Path;
        // 풀은 넘긴 prefab을 키로 쓴다 → 인스턴스(owner.gameObject) 대신 원본 프리팹을 넘겨야 풀이 올바르게 재사용됨.
        var prefab = owner.TryGetComponent(out PooledObject po) ? po.SourcePrefab : owner.gameObject;
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = center + new Vector3((i - (count - 1) * 0.5f) * 0.6f, 0f, 0f);
            var go = PoolManager.Instance.Spawn(prefab, pos, Quaternion.identity);
            if (go.TryGetComponent(out EnemyBase enemy))
            {
                enemy.SetSplitGeneration(nextGen);            // 세대 부여(무한 분열 차단)
                enemy.EnterMap(board, path, snapToStart: false);
                enemy.SetCurrentHp(childHp);                  // OnEnable 풀피 리셋을 현재 체력으로 덮어씀
                enemy.SetScaleMul(0.6f);                      // 분열체는 0.6 크기(인스턴스에만 적용, 풀 재사용 시 원복)
            }
        }
        await UniTask.CompletedTask;
    }
}
