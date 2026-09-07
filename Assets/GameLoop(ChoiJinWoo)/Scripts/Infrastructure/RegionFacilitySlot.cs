using UnityEngine;

public class RegionFacilitySlot
{
    public object Occupant { get; private set; }
    public Sprite Icon { get; private set; }
    public bool IsEmpty => Occupant == null;

    public void Assign(object occupant, Sprite icon)
    {
        Occupant = occupant;
        Icon = icon;
    }

    public void Clear()
    {
        Occupant = null;
        Icon = null;
    }
}
