using System.Collections.Generic;
using UnityEngine;

public class ExpandEvent : MonoBehaviour
{
    [SerializeField] private MapRegistry registry;
    // 지금 올라간 선택지들. 선택하거나 밤이 되면 비워진다.
    private readonly List<ModuleLogic> _choices = new();
    // 선택지로 올라간 지역을 읽기 전용으로 제공. UI가 켜질 때 읽는다.
    public IReadOnlyList<ModuleLogic> Choices => _choices;

    // 선택지는 버튼 수만큼만 올린다.



     
    // 선택지로 올라간 지역을 UI가 읽고, 버튼 클릭 시 선택한다.
    public void ShowChoices(int count) // 선택지로 올라간 지역을 UI가 읽고, 버튼 클릭 시 선택한다.
    {
        CollectLocked(count);
    }

    // 선택지 중 하나를 골랐을 때 처리.
    //실제 호출부 메서드. expand.ShowChoices()로 선택지 뽑고, 
    // UI에서 버튼 클릭 시 호출.
    public void SelectModule(ModuleLogic module) // 선택지 중 하나를 골랐을 때 처리. (UI에서 버튼 클릭 시 호출)
    {
        // 선택지에 없는 지역은 무시한다(이벤트 밖에서 임의로 열리는 걸 막는다).
        if (!_choices.Contains(module))
        {
            return;
        }

        _choices.Clear();
        module.Unlock(); //모듈 상태를 Preparing으로 전환. (UI에서만 호출)
    }

    // 지역 번호로 고르는 통로.  
    public void SelectById(int moduleId)
    {
        if (!registry.TryGetModuleLogic(moduleId, out ModuleLogic module))
        {
            return;
        }
        SelectModule(module);
    }

    // 밤이 되면 선택을 불가.
    // 버린 선택지는 다음 확장 이벤트 때 다시 출현.
    public void CancelChoices()
    {
        _choices.Clear();
    }

    private void CollectLocked(int count)
    {
        _choices.Clear();
        foreach (ModuleLogic module in registry.AllModules.Values)
        {
            if (module.IsUnlocked)
            {
                continue;
            }

            _choices.Add(module);
            if (_choices.Count >= count)
            {
                return;
            }
        }
    }
  
}
