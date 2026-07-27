 using UnityEngine;

// 낮/밤 신호를 받아 열려있는 모든 모듈의 페이즈를 한 번에 전환한다.
public class PhaseController : MonoBehaviour
{
    [SerializeField] private MapRegistry registry;  

    // 낮: 열린 모듈 전부 배치 가능 상태로.
    public void EnterDay()
    {
        SetAll(ModuleState.Preparing);
    }

    // 밤: 열린 모듈 전부 전투 상태로.
    public void EnterNight()
    {
        //SetAll(ModuleState.Battle);
    }

    // Locked는 건드리지 않고, IsUnlocked인 모듈만 전환한다.
    private void SetAll(ModuleState state)
    {
        foreach (ModuleLogic module in registry.AllModules.Values)
        {
            if (module.IsUnlocked)
            {
                module.SetState(state);
            }
        }
    }
}