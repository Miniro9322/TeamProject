using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using System.Threading;

public struct AttackContext
{
    public Transform self;
    public Transform target;
    public Animator anim;
    public Animator bowAnim;
    public Animator arrowAnim;
    public HeroAnimEvents animEvents;
    public Transform muzzle;
    public System.Func<Projectile, IObjectPool<Projectile>> getProjectilePool;
    public System.Func<Vector3, int, RangeShape, List<IDamageAble>> getEnemiesInRange;
    public System.Func<Vector3, int, RangeShape, List<Transform>> getEnemyTargetsInRange;
    public System.Func<Vector3, int, RangeShape, List<GameObject>> getEnemyObjectsInRange;
    public System.Func<Vector3, int, RangeShape, List<IDamageAble>> getTargetableEnemiesInRange;
    public System.Func<Vector3, int, RangeShape, List<GameObject>> getTargetableEnemyObjectsInRange;
    public System.Func<Vector3, int, RangeShape, List<Transform>> getTargetableEnemyTargetsInRange;
    public System.Func<Vector3, Vector3, int, List<IDamageAble>> getEnemiesInLine;
    public System.Func<Vector3, Vector3, Vector2Int> getCardinalDirection;
    public System.Func<Vector3, Vector2Int, int, Vector3> getLineEndPoint;
    public BuffManager buffManager;
    public StatContainer sc;
    public IUnit selfUnit;

    public async UniTask WaitForAnimEvent(string eventName, CancellationToken ct)
    {
        bool fired = false;
        System.Action handler = () => fired = true;
        animEvents.Subscribe(eventName, handler);
        try
        {
            await UniTask.WaitUntil(() => fired, cancellationToken: ct);
        }
        finally
        {
            animEvents.Unsubscribe(eventName, handler);
        }
    }
}
