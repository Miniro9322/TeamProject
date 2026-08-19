using System;
using System.Collections.Generic;
using UnityEngine;

// 게임 상태를 읽어 저장용 데이터 모음을 만든다 (Query만, 게임 상태를 바꾸지 않음)
public class SaveCapture
{
    private readonly GameManager gameManager;
    private readonly ResourcesManager resourcesManager;
    private readonly CitizenManager citizenManager;
    private readonly RegionOverviewPanel regionPanel;
    private readonly MapRegistry mapRegistry;
    private readonly SpawnerManager spawnerManager;
    private readonly HeroRoster heroRoster;
    private readonly HeroTierUpgradeState tierState;
    private readonly MapGame mapGame;

    public SaveCapture(
        GameManager gameManager,
        ResourcesManager resourcesManager,
        CitizenManager citizenManager,
        RegionOverviewPanel regionPanel,
        MapRegistry mapRegistry,
        SpawnerManager spawnerManager,
        HeroRoster heroRoster,
        HeroTierUpgradeState tierState,
        MapGame mapGame)
    {
        this.gameManager = gameManager;
        this.resourcesManager = resourcesManager;
        this.citizenManager = citizenManager;
        this.regionPanel = regionPanel;
        this.mapRegistry = mapRegistry;
        this.spawnerManager = spawnerManager;
        this.heroRoster = heroRoster;
        this.tierState = tierState;
        this.mapGame = mapGame;
    }

    // 저장 단계에 맞는 전체 세이브 데이터를 한 번에 만든다
    public SaveData CaptureSaveData(SavePhase phase, float playTime)
    {
        SaveData data = new SaveData();
        data.savePhase = phase;
        data.saveTime = DateTime.Now.ToString("O");
        data.playTime = playTime;
        data.dayCount = gameManager.DayCount;
        data.baseHp = gameManager.Hp;
        data.heroUnlock = gameManager.UnlockHero;
        data.perfectDefensePending = gameManager.perfactDefence;
        data.woodAmount = resourcesManager.Wood;
        data.foodAmount = resourcesManager.Food;
        data.goldAmount = resourcesManager.Gold;
        data.ironAmount = resourcesManager.Iron;
        data.stoneAmount = resourcesManager.Stone;
        data.specialAmount = resourcesManager.Special;
        data.currentCitizen = citizenManager.CurrentCitizen;
        data.regionList = CaptureRegions();
        data.buildList = CaptureBuilds();
        data.heroList = CaptureHeroes();
        data.placeList = CapturePlacements();
        data.tierList = CaptureTiers();
        data.portalList = CapturePortals(phase);
        data.archiveList = CaptureArchive();
        return data;
    }

    // 지역들의 상태·오프셋을 읽는다
    private RegionSave[] CaptureRegions()
    {
        IReadOnlyDictionary<int, ModuleLogic> modules = mapRegistry.AllModules;
        RegionSave[] result = new RegionSave[modules.Count];
        int index = 0;
        foreach (KeyValuePair<int, ModuleLogic> pair in modules)
        {
            RegionSave save = new RegionSave();
            save.moduleId = pair.Key;
            save.moduleState = pair.Value.CurrentState;
            save.stageOffset = spawnerManager.GetOffset(pair.Key);
            result[index] = save;
            index++;
        }
        return result;
    }

    // 지어진 기반시설들을 읽는다 (빈 슬롯은 담지 않음)
    private BuildSave[] CaptureBuilds()
    {
        List<BuildSave> result = new List<BuildSave>();
        IReadOnlyList<RegionFacilitySlots> regions = regionPanel.Regions;
        for (int r = 0; r < regions.Count; r++)
        {
            RegionFacilitySlots region = regions[r];
            IReadOnlyList<RegionFacilitySlot> slots = region.Slots;
            for (int s = 0; s < slots.Count; s++)
            {
                RegionFacilitySlot slot = slots[s];
                if (slot.IsEmpty) continue;

                BuildSave save = CaptureBuild(region.ModuleId, s, slot.Occupant);
                if (save != null) result.Add(save);
            }
        }
        return result.ToArray();
    }

    // 슬롯 점유자 하나(생산시설 또는 주택)를 저장 데이터 한 줄로 바꾼다
    private static BuildSave CaptureBuild(int moduleId, int slotIndex, object occupant)
    {
        BuildSave save = new BuildSave();
        save.moduleId = moduleId;
        save.slotIndex = slotIndex;

        if (occupant is ProductionFacility facility)
        {
            save.buildKind = OccupantKind.Resource;
            save.buildKey = facility.BasicValue.FacilityName;
            save.upgradeCount = facility.UpgradeCount;
            save.workerAmount = facility.WorkerAmount;
            save.constructPaid = ToCostSave(facility.ConstructCostPaid);
            save.upgradePaid = ToCostSave(facility.TotalUpgradeSpent);
            return save;
        }

        if (occupant is House house)
        {
            save.buildKind = OccupantKind.Building;
            save.buildKey = house.Config.HouseName;
            save.upgradeCount = house.UpgradeCount;
            save.workerAmount = 0;
            save.constructPaid = ToCostSave(house.ConstructCostPaid);
            save.upgradePaid = ToCostSave(house.TotalUpgradeSpent);
            return save;
        }

        return null;
    }

    // 보유 영웅 로스터를 읽는다
    private HeroSave[] CaptureHeroes()
    {
        IReadOnlyList<HeroRosterEntry> entries = heroRoster.Entries;
        HeroSave[] result = new HeroSave[entries.Count];
        for (int i = 0; i < entries.Count; i++)
        {
            HeroRosterEntry entry = entries[i];
            HeroSave save = new HeroSave();
            save.rosterId = entry.Id.ToString();
            save.unitId = entry.Data.UnitId;
            save.citizenCost = entry.CitizenCost;
            save.rosterState = entry.State;
            result[i] = save;
        }
        return result;
    }

    // 맵에 배치된 영웅들을 읽는다
    private PlaceSave[] CapturePlacements()
    {
        PlacedUnitData units = mapGame.Units;
        List<PlaceSave> result = new List<PlaceSave>(units.Count);
        for (int i = 0; i < units.Count; i++)
        {
            GameObject unit = units.UnitAt(i);
            if (!unit.TryGetComponent(out HeroRosterLink link) || link.Entry == null) continue;

            PlacementArea area = units.AreaAt(i);
            PlaceSave save = new PlaceSave();
            save.rosterId = link.Entry.Id.ToString();
            save.moduleId = area.Board.Module.ModuleId;
            save.cellOrigin = area.Origin;
            save.cellSize = area.Size;
            result.Add(save);
        }
        return result.ToArray();
    }

    // 티어별 강화 레벨을 읽는다 (강화된 티어만)
    private TierSave[] CaptureTiers()
    {
        IReadOnlyDictionary<int, int> levels = tierState.Levels;
        TierSave[] result = new TierSave[levels.Count];
        int index = 0;
        foreach (KeyValuePair<int, int> pair in levels)
        {
            TierSave save = new TierSave();
            save.heroTier = pair.Key;
            save.tierLevel = pair.Value;
            result[index] = save;
            index++;
        }
        return result;
    }

    // NightReady일 때만 활성 포탈을 읽는다
    private PortalSave[] CapturePortals(SavePhase phase)
    {
        if (phase != SavePhase.NightReady) return Array.Empty<PortalSave>();

        List<int> unlocked = spawnerManager.UnlockedRegions();
        List<PortalSave> result = new List<PortalSave>(unlocked.Count);
        for (int i = 0; i < unlocked.Count; i++)
        {
            int region = unlocked[i];
            if (!spawnerManager.TryGetActiveSpawnCoords(region, out Vector2Int[] coords, out bool isFallback)) continue;

            PortalSave save = new PortalSave();
            save.regionId = region;
            save.isFallback = isFallback;
            save.spawnCells = coords;
            result.Add(save);
        }
        return result.ToArray();
    }

    // 발견한 적 도감 ID 목록을 읽는다
    private static string[] CaptureArchive()
    {
        IReadOnlyCollection<string> all = EnemyArchiveData.All;
        string[] result = new string[all.Count];
        int index = 0;
        foreach (string key in all)
        {
            result[index] = key;
            index++;
        }
        return result;
    }

    // 자원 튜플 배열을 저장용 CostSave 배열로 바꾼다
    private static CostSave[] ToCostSave((ProductionType Type, int Amount)[] pairs)
    {
        CostSave[] result = new CostSave[pairs.Length];
        for (int i = 0; i < pairs.Length; i++)
        {
            CostSave save = new CostSave();
            save.costType = pairs[i].Type;
            save.costAmount = pairs[i].Amount;
            result[i] = save;
        }
        return result;
    }
}
