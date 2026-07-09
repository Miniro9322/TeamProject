using UnityEngine;

public class Archer : Hero
{
    [SerializeField] private Animator bowAnim;
    [SerializeField] private Animator arrowAnim;
    public Animator BowAnim => bowAnim;
    public Animator ArrowAnim => arrowAnim;
    

    protected override void Awake()
    {
        base.Awake();
        attackState = new ArcherAttackState(this, stateMachine);
    }
}
