using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class HeroInventory : MonoBehaviour
{
    [SerializeField] private MapView view;
    [SerializeField] private MapGame game;
    [SerializeField] private HeroRosterIcon iconPrefab;
    [SerializeField] private Transform container;
    [SerializeField] private HeroTabButton tabButtonPrefab;
    [SerializeField] private HeroCombineManager combineManager;
    [SerializeField] private Transform tabBarContainer;
    [SerializeField] private CanvasGroup canvasGroup;
    private readonly Dictionary<HeroRosterEntry, HeroRosterIcon> icons = new();
    private ObjectPool<HeroRosterIcon> iconPool;

    private enum FilterKind { All, Tier, Placed, Available }
    private readonly List<HeroTabButton> tabs = new();
    private FilterKind currentFilter = FilterKind.All;
    private int currentTierValue = -1;

    private void Awake()
    {
        iconPool = new ObjectPool<HeroRosterIcon>(
            createFunc: () => Instantiate(iconPrefab, container),
            actionOnGet: icon => icon.gameObject.SetActive(true),
            actionOnRelease: icon => { icon.PrepareForReuse(); icon.gameObject.SetActive(false); },
            actionOnDestroy: icon => Destroy(icon.gameObject),
            collectionCheck: true,
            defaultCapacity: 16);

        BuildTabs();
        game.HeroRoster.Changed += Refresh;
        Refresh();
        view.OnOffMode += ShowContent;
        //ShowContent();
    }

    //private void OnEnable()
    //{
    //    game.HeroRoster.Changed += Refresh;
    //    Refresh();
    //}

    //private void OnDisable()
    //{
    //    game.HeroRoster.Changed -= Refresh;
    //}

    private void OnDestroy()
    {
        game.HeroRoster.Changed -= Refresh;
        view.OnOffMode -= ShowContent;
    }

    private void BuildTabs()
    {
        AddTab("전체", FilterKind.All, -1);
        for (int tier = 1; tier <= 4; tier++)
            AddTab($"{tier}티어", FilterKind.Tier, tier);
        AddTab("배치됨", FilterKind.Placed, -1);
        AddTab("미배치", FilterKind.Available, -1);

        if (tabs.Count > 0) SelectTab(tabs[0], FilterKind.All, -1);
    }
    private void AddTab(string label, FilterKind kind, int tierValue)
    {
        HeroTabButton tab = Instantiate(tabButtonPrefab, tabBarContainer);
        tab.Set(label, () => SelectTab(tab, kind, tierValue));
        tabs.Add(tab);
    }
    private void SelectTab(HeroTabButton tab, FilterKind kind, int tierValue)
    {
        currentFilter = kind;
        currentTierValue = tierValue;
        foreach (HeroTabButton t in tabs) t.SetSelected(t == tab);
        Refresh();
    }

    private bool Matches(HeroRosterEntry entry)
    {
        switch (currentFilter)
        {
            case FilterKind.Tier: return entry.Tier == currentTierValue;
            case FilterKind.Placed: return entry.State == HeroRosterState.Placed;
            case FilterKind.Available: return entry.State == HeroRosterState.Available;
            default: return true;
        }
    }
    private IEnumerable<HeroRosterEntry> FilteredEntries()
    {
        foreach (HeroRosterEntry entry in game.HeroRoster.Entries)
            if (Matches(entry))
                yield return entry;
    }
    private void Refresh()
    {
        var current = new HashSet<HeroRosterEntry>(FilteredEntries());

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

        foreach (HeroRosterEntry entry in FilteredEntries())
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
            //cameraRig.focus = entry.PlacedUnit.transform.position;
            //cameraRig.ApplyNow();
            HeroSelectionService.Select(entry.PlacedUnit.GetComponent<Hero>());
        }
        else
        {
            HideContentForPlacement();
            view.SetHero(entry);
        }
    }
    private void OnIconDoubleClicked(HeroRosterEntry entry)
    {
        combineManager.TryCombine(entry);
    }

    private void HideContentForPlacement()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void ShowContent()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }
}
