using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

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
    [SerializeField] private Button combineAllButton;
    private readonly Dictionary<HeroRosterEntry, HeroRosterIcon> icons = new();
    private ObjectPool<HeroRosterIcon> iconPool;

    private enum FilterKind { All, Tier, Placed, Available }
    private readonly List<HeroTabButton> tabs = new();
    private readonly List<(FilterKind kind, int tierValue)> tabMeta = new();
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
        combineAllButton?.onClick.AddListener(OnCombineAllClicked);
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

    private void OnEnable()
    {
        LocalizeTextManager.OnLanguageChanged += RelocalizeTabs;
        RelocalizeTabs();
    }

    private void OnDisable()
    {
        LocalizeTextManager.OnLanguageChanged -= RelocalizeTabs;
    }

    private void OnDestroy()
    {
        game.HeroRoster.Changed -= Refresh;
        view.OnOffMode -= ShowContent;
    }

    private void BuildTabs()
    {
        AddTab(FilterKind.All, -1);
        for (int tier = 1; tier <= 4; tier++)
            AddTab(FilterKind.Tier, tier);
        AddTab(FilterKind.Placed, -1);
        AddTab(FilterKind.Available, -1);

        if (tabs.Count > 0) SelectTab(tabs[0], FilterKind.All, -1);
    }
    private void AddTab(FilterKind kind, int tierValue)
    {
        HeroTabButton tab = Instantiate(tabButtonPrefab, tabBarContainer);
        tab.Set(GetTabLabel(kind, tierValue), () => SelectTab(tab, kind, tierValue));
        tabs.Add(tab);
        tabMeta.Add((kind, tierValue));
    }
    private string GetTabLabel(FilterKind kind, int tierValue)
    {
        switch (kind)
        {
            case FilterKind.Tier: return string.Format(DataTableManager.StringTable.Get("Hero_Inventory_TierFormat"), tierValue);
            case FilterKind.Placed: return DataTableManager.StringTable.Get("Hero_Inventory_Placed");
            case FilterKind.Available: return DataTableManager.StringTable.Get("Hero_Inventory_Unplaced");
            default: return DataTableManager.StringTable.Get("Hero_Inventory_All");
        }
    }
    private void RelocalizeTabs()
    {
        for (int i = 0; i < tabs.Count; i++)
            tabs[i].SetLabel(GetTabLabel(tabMeta[i].kind, tabMeta[i].tierValue));
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
    private bool CanBuildNow() => game.Rule == null || game.Rule.CanBuild;

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

        // 밤에는 회수 불가 — 콜백 자체를 넘기지 않아 아이콘 쪽에서 회수 버튼을 숨기게 한다.
        Action<HeroRosterEntry> retrieveCallback = CanBuildNow() ? OnRetrieveClicked : null;

        foreach (HeroRosterEntry entry in FilteredEntries())
        {
            if (!icons.TryGetValue(entry, out HeroRosterIcon icon))
            {
                icon = iconPool.Get();
                icons[entry] = icon;
            }
            icon.Set(entry, OnIconClicked, OnIconDoubleClicked, retrieveCallback);
            icon.transform.SetAsLastSibling();
        }

        if (combineAllButton != null)
            combineAllButton.interactable = CanBuildNow() && GroupFilteredByMergeKey().Values.Any(g => g.Count >= 3);
    }

    // 현재 필터(FilteredEntries)에 걸린 엔트리만 MergeKey로 묶는다 — 일괄합성이 필터 밖의 동일 영웅까지
    // 건드리지 않도록 후보를 여기서부터 필터링된 목록으로만 만든다.
    private Dictionary<MergeKey, List<HeroRosterEntry>> GroupFilteredByMergeKey()
    {
        var groups = new Dictionary<MergeKey, List<HeroRosterEntry>>();
        foreach (HeroRosterEntry entry in FilteredEntries())
        {
            if (!entry.TryGetMergeKey(out MergeKey key)) continue;
            if (!groups.TryGetValue(key, out List<HeroRosterEntry> list))
                groups[key] = list = new List<HeroRosterEntry>();
            list.Add(entry);
        }
        return groups;
    }

    private void OnCombineAllClicked()
    {
        bool combinedAny = true;
        while (combinedAny)
        {
            combinedAny = false;
            foreach (List<HeroRosterEntry> group in GroupFilteredByMergeKey().Values)
            {
                if (group.Count < 3) continue;
                group.Sort((a, b) => (a.PlacedUnit != null ? 1 : 0).CompareTo(b.PlacedUnit != null ? 1 : 0)); // 미배치 우선
                if (combineManager.TryCombine(group.GetRange(0, 3)))
                {
                    combinedAny = true;
                    break; // 로스터가 바뀌었으니 그룹을 다시 계산
                }
            }
        }
    }

    // 배치된 영웅을 필드에서 치우고 로스터로 되돌린다(UnitRemover.DestroyOrReturnToPool과 같은 절차,
    // 다만 엔트리를 로스터에서 빼지 않고 MarkAvailable로만 되돌린다는 점이 다르다).
    private void OnRetrieveClicked(HeroRosterEntry entry)
    {
        if (!CanBuildNow()) return; // 밤에는 회수 불가
        if (entry == null || entry.State != HeroRosterState.Placed) return;

        GameObject unit = entry.PlacedUnit;
        if (unit == null) return;

        unit.TryGetComponent(out Hero hero);

        if (game.Units.TryGetArea(unit, out PlacementArea area))
        {
            AreaPlace.Remove(area);
            game.Units.Remove(unit);
        }

        entry.MarkAvailable();
        game.HeroRoster.NotifyStateChanged();

        if (hero != null)
        {
            HeroSelectionService.ClearIfSelected(hero);
            hero.PrepareForDespawn();
            PoolManager.Instance.Despawn(unit);
        }
        else
        {
            Destroy(unit);
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
