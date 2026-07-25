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
    public bool IsUnlocked => _currentState != ModuleState.Locked;
    public bool IsPreparing => _currentState == ModuleState.Preparing;

    public event Action<ModuleState> OnStateChanged;

    public void SetModuleId(int ModuleId)
    {
        moduleId = ModuleId;
    }
    //배치 가능 여부 전환(배치 가능 == 구역 해금으로 판단)
    public void SetState(ModuleState newState)
    {
        if (_currentState == newState) return;

        _currentState = newState;
        OnStateChanged?.Invoke(newState);
    }
    //구역 상태를 Locked -> Preparing으로 전환. 이미 해금 상태면 무시.
    public void Unlock()
    {
        if (IsUnlocked) return;

        SetState(ModuleState.Preparing);
    }
 
}

public enum ModuleState
{
    Locked,     // 데이터만/실루엣. 배치·전투 불가
    Preparing,  // 배치 가능, 적 미등장
    //Battle      // 현재 웨이브 전투 진행
}
