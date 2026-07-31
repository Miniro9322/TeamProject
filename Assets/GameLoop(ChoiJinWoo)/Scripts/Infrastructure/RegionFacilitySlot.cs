using UnityEngine;

// 지역 한 칸의 상태(비어있음/건물 하나)만 담는다. 위치·좌표 개념은 없다.
public class RegionFacilitySlot
{
    public GameObject Occupant { get; private set; }
    public Sprite Icon { get; private set; }
    public string Label { get; private set; }
    public bool IsEmpty => Occupant == null;

    public void Assign(GameObject occupant, Sprite icon, string label)
    {
        Occupant = occupant;
        Icon = icon;
        Label = label;
    }

    public void Clear()
    {
        Occupant = null;
        Icon = null;
        Label = null;
    }
}
