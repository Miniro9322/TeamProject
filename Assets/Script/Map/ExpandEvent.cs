using System;
using System.Collections.Generic;
using UnityEngine;

 
 
// 흐름:  GameManager.ExpandMap  →  ShowChoices()  →  ChoicesReady 이벤트  →  UI가 버튼 표시
//                                                 →  UI가 SelectById()   →  그 지역 해금
public class ExpandEvent : MonoBehaviour
{
    [SerializeField] private MapRegistry registry;

    [SerializeField] private int choiceCount = 2;

    // 지금 올라간 선택지들. 선택하거나 밤이 되면 비워진다.
    private readonly List<ModuleLogic> _choices = new();

    public event Action<IReadOnlyList<ModuleLogic>> ChoicesReady;

    // 선택지 사라짐 ui가 받아서 버튼을 지운다.
    public event Action ChoicesGone;

    public IReadOnlyList<ModuleLogic> Choices => _choices;

    [ContextMenu("선택지 열기")]
    public void ShowChoices()
    {
        CollectLocked();
        if (_choices.Count == 0)
        {
            return;
        }

        ChoicesReady?.Invoke(_choices);
    }

    // 선택지 중 하나를 골랐을 때 처리.
    public void SelectModule(ModuleLogic module)
    {
        // 선택지에 없는 지역은 무시한다(이벤트 밖에서 임의로 열리는 걸 막는다).
        if (!_choices.Contains(module))
        {
            return;
        }

        _choices.Clear();
        module.Unlock();
        ChoicesGone?.Invoke();
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

    // 밤이 되면 선택을 불가. 확장은 낮에만 일어나야 하기 때문.
    // 버린 선택지는 다음 확장 이벤트 때 다시 출현.
    public void CancelChoices()
    {
        if (_choices.Count == 0)
        {
            return;
        }

        _choices.Clear();
        ChoicesGone?.Invoke();
    }

    // 아직 안 열린 지역을 등록 순서대로 choiceCount개까지 모은다.
    // 남은 게 그보다 적으면 있는 만큼만 올라간다.
    private void CollectLocked()
    {
        _choices.Clear();
        foreach (ModuleLogic module in registry.AllModules.Values)
        {
            if (module.IsUnlocked)
            {
                continue;
            }

            _choices.Add(module);
            if (_choices.Count >= choiceCount)
            {
                return;
            }
        }
    }
}
