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
    public System.Func<GameObject, Vector3, Quaternion, float, GameObject> spawnEffect;
    public System.Func<GameObject, Vector3, Quaternion, GameObject> spawnPersistentEffect;
    public System.Action<GameObject, GameObject> despawnEffect;
    public System.Func<Vector3, int, RangeShape, RangeQueryAffinity, List<GameObject>> getObjectsInRange;
    public System.Action<float> healSelf;
    public System.Func<Vector3, Vector3, int, List<IDamageAble>> getEnemiesInLine;
    public System.Func<Vector3, Vector3, Vector2Int> getCardinalDirection;
    public System.Func<Vector3, Vector2Int, int, Vector3> getLineEndPoint;
    public BuffManager buffManager;
    public StatContainer sc;
    public IUnit selfUnit;

    // --- 신규 2개: Trait 훅 / GroundZoneEffect 스폰 ---
    // 공격자(Hero)의 NotifyHit으로 연결된다 — 내부에서 킬 감지 후 OnKill까지 자동으로 이어서 호출하므로
    // onKill을 별도로 둘 필요가 없다.
    public System.Action<GameObject, int, bool> onHit;
    public System.Action<GameObject, Vector3> spawnGroundZone;

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
