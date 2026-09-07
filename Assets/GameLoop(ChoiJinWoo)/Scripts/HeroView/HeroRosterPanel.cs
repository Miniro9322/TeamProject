using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class HeroRosterPanel : MonoBehaviour
{
    [SerializeField] private MapView view;
    [SerializeField] private MapGame game;
    [SerializeField] private CameraRig cameraRig;
    [SerializeField] private HeroRosterIcon iconPrefab;
    [SerializeField] private Transform container;
    [SerializeField] private HeroCombineManager combineManager;

    private readonly Dictionary<HeroRosterEntry, HeroRosterIcon> icons = new();
    private ObjectPool<HeroRosterIcon> iconPool;

    private void Awake()
    {
        iconPool = new ObjectPool<HeroRosterIcon>(
            createFunc: () => Instantiate(iconPrefab, container),
            actionOnGet: icon => icon.gameObject.SetActive(true),
            actionOnRelease: icon => { icon.PrepareForReuse(); icon.gameObject.SetActive(false); },
            actionOnDestroy: icon => Destroy(icon.gameObject),
            collectionCheck: true,
            defaultCapacity: 16);
    }

    private void OnEnable()
    {
        game.HeroRoster.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        game.HeroRoster.Changed -= Refresh;
    }

    private void Refresh()
    {
        var current = new HashSet<HeroRosterEntry>(game.HeroRoster.Entries);

        List<HeroRosterEntry> stale = null;
        foreach (KeyValuePair<HeroRosterEntry, HeroRosterIcon> kv in icons)
            if (!current.Contains(kv.Key))
                (stale ??= new List<HeroRosterEntry>()).Add(kv.Key);

        if (stale != null)
            foreach (HeroRosterEntry entry in stale)
            {
                iconPool.Release(icons[entry]);
                icons.Remove(entry);
            }

        foreach (HeroRosterEntry entry in game.HeroRoster.Entries)
        {
            if (!icons.TryGetValue(entry, out HeroRosterIcon icon))
            {
                icon = iconPool.Get();
                icons[entry] = icon;
            }
            icon.Set(entry, OnIconClicked, OnIconDoubleClicked);

            icon.transform.SetAsLastSibling();
        }
    }

    private void OnIconClicked(HeroRosterEntry entry, HeroRosterIcon icon)
    {
        if (entry.State == HeroRosterState.Placed)
        {
            cameraRig.focus = entry.PlacedUnit.transform.position;
            cameraRig.ApplyNow();
            HeroSelectionService.Select(entry.PlacedUnit.GetComponent<Hero>());
        }
        else
        {
            view.SetHero(entry);
        }
    }
    private void OnIconDoubleClicked(HeroRosterEntry entry)
    {
        combineManager.TryCombine(entry);
    }
}
