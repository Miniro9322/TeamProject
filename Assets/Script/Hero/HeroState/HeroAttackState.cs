using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class HeroAttackState : HeroState
{
    protected float timer;
    protected float lastAttackTime = float.NegativeInfinity;
    protected CancellationTokenSource attackCts;

    public HeroAttackState(Hero hero, HeroStateMachine stateMachine) : base(hero, stateMachine)
    {
        
    }

    public override void Enter()
    {
        timer = 0f;
        // 씬 전환/영웅 파괴 시(게임오버 -> 타이틀 등) Exit()이 안 불려도 진행 중이던 공격 코루틴이
        // 알아서 취소되도록 UniTask의 파괴 토큰과 묶는다 - 안 그러면 딜레이 도중 씬이 넘어가서
        // 이미 파괴된 타겟의 transform에 접근해 MissingReferenceException이 난다.
        attackCts = CancellationTokenSource.CreateLinkedTokenSource(hero.GetCancellationTokenOnDestroy());
    }

    public override void Exit()
    {
        attackCts?.Cancel();
        attackCts?.Dispose();
        attackCts = null;
    }

    // 공격 애니메이션(윈도우) 진행 중인지. 진행 중이면 간격이 지나도 타이머만 계속 쌓이고
    // 새 공격은 시작하지 않는다 — 실제로 다음 공격을 시작한 순간에만 timer를 리셋해서
    // "실행 도중 만료된 타이머가 버려지고 0부터 다시 쌓이는" 낭비 사이클을 없앤다.
    public virtual bool IsBusy => false;

    public override void Update()
    {
        if (hero.Context.target == null && !IsBusy)
        {
            stateMachine.ChangeState(hero.IdleState);
            return;
        }

        if (stateMachine.CurrentState != this) return;

        // 공격이 계속 유지되는 동안(채널링 중이거나 다음 공격을 기다리는 중)에도 매 프레임
        // 타겟 방향으로 몸을 돌린다 — 공격을 새로 시작하는 순간에만 돌면 타겟이 사거리 안에서
        // 움직여도 몸이 옛 방향에 고정된 것처럼 보인다.
        if (hero.Context.target != null)
            RotateToTarget();

        // Continuous 공격은 자체 tick 간격이 이미 AS를 반영하므로, 바로 직전 공격이 Continuous였다면
        // 바깥쪽 1/AS 대기 게이트를 걸지 않는다 — 안 그러면 채널링이 continuousDuration보다 일찍
        // 끝났을 때(타겟 조기 사망 등) 남은 시간만큼 다음 공격이 불필요하게 밀린다.
        bool skipAsGate = hero.LastUsedAttackData != null
            && hero.LastUsedAttackData.timingMode == AttackTimingMode.Continuous;
        float interval = hero.SC[StatType.AS] > 0f ? 1f / hero.SC[StatType.AS] : 1f; // AS = 초당 공격 횟수
        timer += Time.deltaTime;
        if ((skipAsGate || timer >= interval) && !IsBusy)
        {
            timer = 0f;
            TryExecuteCurrentStep();
        }
    }

    private const float RotateSpeedDegPerSec = 360f;

    protected void RotateToTarget()
    {
        Vector3 aimVector = hero.Context.target.transform.position - hero.transform.position;
        aimVector.y = 0f;
        if (aimVector.sqrMagnitude > 0.00001f)
        {
            Quaternion desired = Quaternion.LookRotation(aimVector);
            hero.transform.rotation = Quaternion.RotateTowards(
                hero.transform.rotation, desired, RotateSpeedDegPerSec * Time.deltaTime);
        }
    }

    protected virtual void TryExecuteCurrentStep()
    {
        
    }
}
