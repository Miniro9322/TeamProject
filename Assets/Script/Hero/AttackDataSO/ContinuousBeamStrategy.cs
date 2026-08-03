using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

// 채널링형 지속 공격 — executor(근접/원거리/힐)를 호출하지 않고, 애니메이션 이벤트 윈도우 대신 자체
// tick 루프로 continuousDuration 동안 continuousTickInterval마다 즉시 데미지를 적용한다. 매 틱 시작
// 전에 hero.Target으로 "지금도 유효한 타겟인가"를 다시 확인하고(넘겨받은 ctx는 struct 복사본이라
// Hero.CheckTargetStillInRange가 타겟을 null로 바꿔도 갱신되지 않는다), hero.Context로 매번 새
// 스냅샷을 떠서 데미지를 적용한다 — 그렇지 않으면 채널링 도중 타겟이 죽는 순간 NRE가 난다.
public class ContinuousBeamStrategy : IAttackDeliveryStrategy
{
    public async UniTask Deliver(Hero hero, AttackDataSO data, AttackContext ctx, IAttackExecutor executor, CancellationToken ct)
    {
        // 캐스팅 포즈는 Trigger가 아니라 Bool 파라미터로 유지된다 — animTriggers[0]을 그 파라미터
        // 이름으로 재사용한다(채널링 한 번에 이름 하나만 필요하므로 Sequential/Random 선택은 불필요).
        string animParam = (data.animTriggers != null && data.animTriggers.Length > 0) ? data.animTriggers[0] : null;
        if (string.IsNullOrEmpty(animParam))
            Debug.LogError($"[ContinuousBeamStrategy] '{data.name}' 의 animTriggers가 비어 있습니다.");
        else
            ctx.anim.SetBool(animParam, true);

        try
        {
            float interval = Mathf.Max(0.05f, data.continuousTickInterval);
            float elapsed = 0f;
            while (data.continuousDuration <= 0f || elapsed < data.continuousDuration)
            {
                if (hero.Target == null) break; // 채널링 도중 타겟이 죽거나 벗어남 — 여기서 끊는다
                await AttackDamageUtil.ApplyInstantDamage(data, hero.Context, ct);
                await UniTask.Delay(TimeSpan.FromSeconds(interval), cancellationToken: ct);
                elapsed += interval;
            }
        }
        finally
        {
            // 정상 종료/타겟 소실(break)/취소(OperationCanceledException) 어떤 경로로 빠져나가도
            // Bool이 켜진 채로 남지 않도록 반드시 꺼준다.
            if (!string.IsNullOrEmpty(animParam)) ctx.anim.SetBool(animParam, false);
        }
    }
}
