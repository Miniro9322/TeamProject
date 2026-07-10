using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Threading;

public struct AttackContext
{
    public Transform self;
    public Transform target;
    public Animator anim;
    public HeroAnimEvents animEvents;
    // 

    public async UniTask WaitForAnimEvent(string eventName, CancellationToken ct)
    {
        bool fired = false;
        System.Action handler = () => fired = true;
        animEvents.Subscribe(eventName, handler);
        await UniTask.WaitUntil(() => fired, cancellationToken: ct);
        animEvents.Unsubscribe(eventName, handler);
    }
}
