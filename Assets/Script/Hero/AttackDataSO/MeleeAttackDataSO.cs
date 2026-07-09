using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

[CreateAssetMenu(menuName = "HeroAttack/AttackDataSO/MeleeAttack")]
public class MeleeAttackDataSO : AttackDataSO
{
    public override UniTask Execute(AttackContext ctx, CancellationToken ct)
    {
        throw new System.NotImplementedException();
    }
}
