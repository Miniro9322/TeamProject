using System;
using UnityEngine;


// <summary>
/// 모듈의 활성화 생명주기 상태만 담당
/// 책임: 상태 보유 + 전이 + 변경 알림.  
/// </summary>
public class ModuleLogic : MonoBehaviour
{

    //런타임 부여 시 SetModuleId.
    [SerializeField] private int moduleId;

    [SerializeField] private ModuleState _currentState = ModuleState.Locked;
    public int ModuleId => moduleId;

    public ModuleState CurrentState => _currentState;

    public event Action<ModuleState> OnStateChanged;

    public void SetModuleId(int ModuleId)
    {
        moduleId = ModuleId;
    }

    public void SetState(ModuleState newState)
    {
        if (_currentState == newState) return;

        _currentState = newState;
        OnStateChanged?.Invoke(newState);
    }
 
}

public enum ModuleState
{
    Locked,     // 데이터만/실루엣. 배치·전투 불가
    Revealed,   // 보상·위험 확인 가능
    Preparing,  // 배치 가능, 적 미등장
    Active,     // 생산·전투후보 등록 완료
    Battle      // 현재 웨이브 전투 진행
}
