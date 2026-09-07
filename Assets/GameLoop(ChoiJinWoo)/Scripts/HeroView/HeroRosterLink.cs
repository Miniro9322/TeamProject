using UnityEngine;

public class HeroRosterLink : MonoBehaviour
{
    public HeroRosterEntry Entry;

    public static void Attach(GameObject unit, HeroRosterEntry entry)
    {
        if (!unit.TryGetComponent(out HeroRosterLink link))
            link = unit.AddComponent<HeroRosterLink>();
        link.Entry = entry;
    }
}
