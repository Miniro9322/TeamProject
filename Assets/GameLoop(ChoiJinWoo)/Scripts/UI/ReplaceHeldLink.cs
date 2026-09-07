using UnityEngine;

public class ReplaceHeldLink : HeldLinkBase
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
        return view.IsReplacing;
    }
}
