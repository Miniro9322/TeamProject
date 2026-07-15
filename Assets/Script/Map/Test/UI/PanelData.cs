using System;

public class PanelData
{
    public string Status = "";
    public string TileText = "";
    public string Mode = "";
    public string[] Units = Array.Empty<string>();
    public bool ShowPath;
    public int UnitIndex = -1;
    public bool Holding; // 유닛을 집은 이동 중 상태 → 이때 테스트 버튼 비활성화.
}
