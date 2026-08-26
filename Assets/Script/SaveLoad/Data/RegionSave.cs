using System;

// 지역(모듈) ID · 상태 · 로컬 스테이지 오프셋
[Serializable]
public class RegionSave
{
    public int moduleId;              // 지역 고유 번호
    public ModuleState moduleState;   // 잠김/해금 상태
    public int stageOffset;
    public int unlockDay;
}
