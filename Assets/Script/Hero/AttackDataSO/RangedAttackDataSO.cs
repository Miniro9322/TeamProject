using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

[CreateAssetMenu(menuName = "HeroAttack/AttackDataSO/RangedAttack")]
public class RangedAttackDataSO : AttackDataSO
{
    public override async UniTask Execute(AttackContext ctx, CancellationToken ct)
    {
        ctx.anim.SetTrigger(HeroAnimHash.attack);
        ctx.bowAnim.SetTrigger(HeroAnimHash.attack);
        ctx.arrowAnim.SetTrigger(HeroAnimHash.attack);
        await ctx.WaitForAnimEvent("Attack", ct);
        Debug.Log("Ranged Attack");
    }
}
