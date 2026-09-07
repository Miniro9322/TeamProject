using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Animator))]
public class SkillRadialToggle : MonoBehaviour
{
    [SerializeField] private Button icon;

    private static readonly int SpreadHash = Animator.StringToHash("SkillSpread");
    private static readonly int CollapseHash = Animator.StringToHash("SkillCollapse");
    private Animator anim;
    private bool spread;
    private bool suppressNextCollapsedEvent;

    public bool IsSpread => spread;

    public event Action Collapsed;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        icon.onClick.AddListener(Toggle);
    }

    private void Toggle()
    {
        if (spread)
        {
            Collapse();
        }
        else
        {
            Spread();
        }
    }

    public void Spread()
    {
        anim.Play(SpreadHash, 0, 0f);
        spread = true;
    }

    public void Collapse()
    {
        anim.Play(CollapseHash, 0, 0f);
        spread = false;
    }

    public void ResetCollapsedNow()
    {
        suppressNextCollapsedEvent = true;
        anim.Play(CollapseHash, 0, 1f);
        spread = false;
    }

    public void NotifyCollapsed()
    {
        if (suppressNextCollapsedEvent)
        {
            return;
        }
    }
}
