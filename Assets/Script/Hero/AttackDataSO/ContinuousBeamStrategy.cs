using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool;

// 채널링형 지속 공격 — executor(근접/원거리/힐)를 호출하지 않고, 애니메이션 이벤트 윈도우 대신 자체
// tick 루프로 continuousDuration 동안 continuousTickInterval마다 피해를 적용한다. 매 틱 시작
// 전에 hero.Target으로 "지금도 유효한 타겟인가"를 다시 확인하고(넘겨받은 ctx는 struct 복사본이라
// Hero.CheckTargetStillInRange가 타겟을 null로 바꿔도 갱신되지 않는다), hero.Context로 매번 새
// 스냅샷을 떠서 데미지를 적용한다 — 그렇지 않으면 채널링 도중 타겟이 죽는 순간 NRE가 난다.
// data.continuousDelivery == Beam이면 잠긴 타겟에 즉시 데미지(BeamVisualEffect와 짝을 이룸),
// ProjectileVolley면 매 tick 실제 투사체를 발사하는 방식으로 갈린다(attackCount/targetCount로 매 tick
// 몇 발을 어디에 쏠지 결정 — RangedAttackExecutor.FireVolley와 동일한 타겟팅 규칙).
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
                if (data.continuousDelivery == ContinuousDelivery.ProjectileVolley)
                    await FireProjectileVolley(hero, data, ctx, ct);
                else
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

    // RangedAttackExecutor.FireVolley와 동일한 타겟팅 규칙 — DifferentEnemies는 AttackTargetSelector로
    // 최대 targetCount종의 적에게 attackCount발을 분배(라운드로빈), SameTarget은 잠긴 타겟에게
    // attackCount발 전부. 데미지는 즉시 적용되지 않고 각 투사체가 도착했을 때 Projectile.Hit()이
    // 적용한다.
    private async UniTask FireProjectileVolley(Hero hero, AttackDataSO data, AttackContext ctx, CancellationToken ct)
    {
        List<GameObject> targets;
        if (data.targetMode == TargetMode.DifferentEnemies)
        {
            List<GameObject> enemies = hero.GetObjectsInRange(ctx.self.position, data.range, data.rangeShape, RangeQueryAffinity.TargetableEnemy);
            targets = AttackTargetSelector.SelectTargets(enemies, data.attackCount, data.targetCount);
        }
        else
        {
            if (hero.Target == null) return;
            targets = new List<GameObject>(data.attackCount);
            for (int i = 0; i < data.attackCount; i++)
                targets.Add(hero.Target);
        }
        if (targets.Count == 0) return; // 사거리 내 유효 타겟 없음 — 이번 tick은 스킵

        int damage = (int)(ctx.sc[StatType.ATK] * data.attackPer);
        IObjectPool<Projectile> pool = hero.GetProjectilePool(data.projectilePrefab);

        for (int i = 0; i < targets.Count; i++)
        {
            FireOneProjectile(hero, data, ctx, pool, targets[i], damage);
            if (i < targets.Count - 1)
                await UniTask.Delay(TimeSpan.FromSeconds(data.shotInterval), cancellationToken: ct);
        }
    }

    private void FireOneProjectile(Hero hero, AttackDataSO data, AttackContext ctx, IObjectPool<Projectile> pool, GameObject target, int damage)
    {
        Projectile arrow = pool.Get();
        arrow.transform.SetPositionAndRotation(ctx.muzzle.position, ctx.muzzle.rotation);
        hero.SpawnEffect(data.attackEffect, ctx.muzzle.position, ctx.muzzle.rotation, data.attackEffectLifetime);

        arrow.Launch(target.transform, damage, pool,
            ProjectileAoEConfig.From(data, hero, ctx.sc, ctx.buffManager, ctx.self.position));
    }
}
