using UnityEngine;

// 독립된 빔 비주얼 오브젝트 — ContinuousBeamStrategy/HeroAttackRunner/Projectile을 전혀 건드리지 않고,
// Hero가 이미 공개하는 상태(Target/LastUsedAttackData/AttackState.IsBusy)를 매 프레임 관찰해서 지금
// 이 빔 공격이 실제로 채널링 중인지 스스로 판정한다. 종료 신호를 따로 받지 않아도 다음 프레임에
// active가 false가 되는 즉시 사라진다.
public class BeamVisualEffect : MonoBehaviour
{
    [SerializeField] private Hero hero;
    [SerializeField] private Transform origin;
    [SerializeField] private AttackDataSO beamAttackData;
    [SerializeField] private LineRenderer lineRenderer;

    private void Update()
    {
        bool active = hero != null && hero.Target != null
            && hero.LastUsedAttackData == beamAttackData
            && hero.AttackState != null && hero.AttackState.IsBusy;

        if (lineRenderer.enabled != active) lineRenderer.enabled = active;
        if (!active) return;

        lineRenderer.SetPosition(0, origin.position);
        lineRenderer.SetPosition(1, hero.Target.transform.position);
    }
}
