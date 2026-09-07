using UnityEngine;

public class RemoveHeldLink : HeldLinkBase
{
    [SerializeField] private MapView view;

    protected override void OnEnable()
    {
        base.OnEnable();
        view.OnStateChanged += Refresh;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        view.OnStateChanged -= Refresh;
    }

    protected override bool CheckHeld()
    {
        return view.IsRemoving;
    }
}
