using UnityEngine;
using System.Collections.Generic;


//책임: 모듈 등록/해제 + 동번호로 조회. 
public class MapRegistry : MonoBehaviour
{

    // 동번호(int) → 모듈(ModuleLogic)
    private readonly Dictionary<int, ModuleLogic> _ModuleLogicId = new();
     // 등록된 모든 모듈 
    public IReadOnlyDictionary<int, ModuleLogic> AllModules => _ModuleLogicId;

    public int ModuleLogicCount => _ModuleLogicId.Count;

    public void RegisterModuleLogic(ModuleLogic logic)
    {
        _ModuleLogicId[logic.ModuleId] = logic;
    }
    
    public void UnregisterModuleLogic(ModuleLogic logic)
    {
        _ModuleLogicId.Remove(logic.ModuleId);
    }
     // 동번호로 모듈 찾기. 없으면 false.
    public bool TryGetModuleLogic(int moduleId, out ModuleLogic logic)
    {
        return _ModuleLogicId.TryGetValue(moduleId, out logic);
    }
 
}

 