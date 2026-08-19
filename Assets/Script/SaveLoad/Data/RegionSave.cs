using System;

// 지역(모듈) ID · 상태 · 로컬 스테이지 오프셋
[Serializable]
public class RegionSave
{
    public int moduleId;              // 지역 고유 번호
    public ModuleState moduleState;   // 잠김/해금 상태
    public int stageOffset;           // 이 지역이 해금된 날 기준으로 일차를 당겨 세는 값
}
