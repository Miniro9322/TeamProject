using System;
using System.Collections.Generic;
using UnityEngine;

// 담당들을 만들어 MapCommand와 MapView에 넣어준다. 조립만 하고 게임 로직은 갖지 않는다.
// Start에서 하는 이유: MapGame의 [Inject] Construct가 Awake 단계에 끝나므로 그 뒤라야 Placer가 채워져 있다.
public class MapAssemble : MonoBehaviour
{
    [SerializeField] private MapGame mapGame;
    [SerializeField] private MapRegistry registry;
    [SerializeField] private MapCommand command;
    [SerializeField] private MapView view;
    [SerializeField] private PlacePalette palette;
    [SerializeField] private ExpandEvent expand;
    [SerializeField] private float dragPixels = 8f;
    [SerializeField] private float placeYOffset = 0f;
    [Range(0f, 1f)]
    [Tooltip("배치 미리보기의 진하기. 낮출수록 투명해진다.")]
    [SerializeField] private float ghostAlpha = 0.45f;
    [SerializeField] private TilePaintView tilePaintView;
    [SerializeField] private RangeInput rangeInput;
    [SerializeField] private HeroCombineManager combineManager;
    [SerializeField] private DesertZone desertZone;

    private List<PathTrail> pathTrails;
    private List<EnemyLanes> laneModules;
    private PlaceGhost ghost;
    private HeroSkillCastController skillCast;
    private ZoneEffectApplier zoneEffectApplier;
    private CampfireLightController campfireLights;
    private MapBoard desertBoard;

    private void Start()
    {
        List<MapBoard> boards;
        CollectModuleComponents(out boards, out pathTrails, out laneModules);
        BuildCampfires(boards);

        palette.Bind(mapGame.HeroRoster);

        HoveredTileData hoverData = new HoveredTileData();
        PointerPick pointerPick = new PointerPick(boards, hoverData);
        PlaceFinder finder = new PlaceFinder(pointerPick, palette, placeYOffset);

        desertBoard = desertZone.GetComponent<MapBoard>();
        WindShelterData shelterData = new WindShelterCalc().BuildData(desertBoard.Cells);
        WindwallData windwallData = new WindwallCalc().BuildData(desertBoard.Cells, desertZone.WindwallReach);
        desertZone.SetWindwall(windwallData);
        WindPreview windPreview = new WindPreview(
            desertBoard,
            desertZone.transform,
            desertZone.ArrowSize,
            desertZone.ArrowHeight,
            desertZone.ArrowColor);
        DesertLineEffect lineEffect = new DesertLineEffect(
            desertBoard,
            Resources.Load<GameObject>("ZoneEffectPrefab/DesertStrongVFX"),
            Resources.Load<GameObject>("ZoneEffectPrefab/DesertWeakVFX"));
        zoneEffectApplier = new ZoneEffectApplier(
            desertZone,
            desertBoard,
            shelterData,
            windwallData,
            mapGame.Units,
            windPreview,
            lineEffect);

        DayNightBuildRule dayNightRule = new DayNightBuildRule();
        dayNightRule.rule = mapGame.Rule;
        palette.dayNightRule = dayNightRule;

        mapGame.Placer.zoneEffectApplier = zoneEffectApplier;
        UnitReplace replace = new UnitReplace(mapGame.Units, zoneEffectApplier);

        skillCast = new HeroSkillCastController { dayNightRule = dayNightRule };

        RangeInfo rangeInfo = new RangeInfo();
        RangeTileData rangeStore = new RangeTileData();
        RangeCalc rangeCalc = new RangeCalc(rangeInfo);
        HoverPlaceData hoverPlace = new HoverPlaceData();

        if (tilePaintView != null)
        {
            PlaceHoverFinder hoverFinder = new PlaceHoverFinder(view, finder, hoverPlace);
            SkillTargetFinder skillFinder = new SkillTargetFinder(skillCast, view);
            tilePaintView.SetupEdges(boards);
            tilePaintView.sync = new TilePaintSync(hoverFinder, skillFinder, rangeCalc, rangeStore, tilePaintView.Painter);
        }

        rangeInput.pointerPick = pointerPick;
        rangeInput.rangeCalc = rangeCalc;
        rangeInput.rangeStore = rangeStore;

        view.pointerPick = pointerPick;
        view.replace = replace;
        view.citizenManager = mapGame.CitizenManager;
        view.resourcesManager = mapGame.ResourcesManager;

        PlaceAction action = new PlaceAction();
        action.palette = palette;
        action.finder = finder;
        action.placer = mapGame.Placer;
        action.remover = new UnitRemover(mapGame.Units, mapGame.HeroRoster, zoneEffectApplier);
        action.replace = replace;
        action.dayNightRule = dayNightRule;
        action.view = view;
        action.heroRoster = mapGame.HeroRoster;
        action.skillCast = skillCast;
        action.combineManager = combineManager;

        command.pointerPick = pointerPick;
        command.dragDetect = new DragDetect(dragPixels);
        command.rightDragDetect = new DragDetect(dragPixels);
        command.replace = replace;
        command.action = action;
        ghost = new PlaceGhost(ghostAlpha);
        command.ghost = ghost;
        command.finder = finder;
        command.hoverPlace = hoverPlace;
        command.dispatch = new Dictionary<PlaceMode, Action<Tile>>
        {
            { PlaceMode.Off, action.SelectTile },
            { PlaceMode.Place, action.PlaceUnit },
            { PlaceMode.Remove, action.RemoveUnit },
        };
        foreach(PathTrail trail in pathTrails)
        {
            mapGame.Rule.ChangeToDay += trail.PlayLoop;
            mapGame.Rule.ChangeToNight += trail.PlayOnce;
        }

        mapGame.Rule.ChangeToDay += OnDayChanged;
        OnDayChanged(); // 첫 날짜도 시작하자마자 바로 맞춘다 — 이벤트가 처음 울릴 때까지 기다리지 않는다

        mapGame.Rule.ChangeToDay += zoneEffectApplier.OnDayChanged;
        zoneEffectApplier.OnDayChanged();

        mapGame.Rule.ChangeToDay += campfireLights.TurnOff;
        campfireLights.TurnOff(); // 첫 날도 낮이니 꺼진 채로 시작

        mapGame.Rule.ChangeToDay += OnFireDayChanged;
        OnFireDayChanged(); // 첫 날도 낮이니 꺼진 채로 시작
        mapGame.Rule.ChangeToNight += OnFireNightChanged;
        FireReceiver.SetGameManager(mapGame.Rule);

        mapGame.Rule.ChangeToNight += view.ClearMode;
        mapGame.Rule.ChangeToNight += OnDesertNightChanged;
        mapGame.Rule.ChangeToNight += skillCast.ClearSelection;
        mapGame.Rule.ChangeToNight += campfireLights.TurnOn;

        // 확장 이벤트: 5일마다 GameManager가 쏘고, 밤이 되면 선택을 무른다.
        // 미배선이면 확장만 꺼지고 나머지 조립은 그대로 돈다.
        if (expand != null)
        {
            mapGame.Rule.ChangeToNight += expand.CancelChoices;
        }
    }

    private void OnDestroy()
    {
        ghost.ClearGhosts();
        mapGame.Rule.ChangeToNight -= view.ClearMode;
        mapGame.Rule.ChangeToNight -= OnDesertNightChanged;
        mapGame.Rule.ChangeToDay -= zoneEffectApplier.OnDayChanged;
        zoneEffectApplier.Dispose();
        if (campfireLights != null)
        {
            mapGame.Rule.ChangeToNight -= campfireLights.TurnOn;
            mapGame.Rule.ChangeToDay -= campfireLights.TurnOff;
        }
        mapGame.Rule.ChangeToDay -= OnFireDayChanged;
        mapGame.Rule.ChangeToNight -= OnFireNightChanged;
        if (laneModules != null)
        {
            mapGame.Rule.ChangeToDay -= OnDayChanged;
        }
        if (skillCast != null)
        {
            mapGame.Rule.ChangeToNight -= skillCast.ClearSelection;
        }
        if (expand != null)
        {
            mapGame.Rule.ChangeToNight -= expand.CancelChoices;
        }
        if (pathTrails != null)
        {
            foreach (PathTrail trail in pathTrails)
            {
                mapGame.Rule.ChangeToDay -= trail.PlayLoop;
                mapGame.Rule.ChangeToNight -= trail.PlayOnce;
            }
        }
    }

    // 레지스트리에 등록된 모듈들의 보드·트레일·레인 목록을 한 번의 순회로 모은다. 모듈 루트에 ModuleLogic과 MapBoard가 함께 산다.
    private void CollectModuleComponents(
        out List<MapBoard> boards,
        out List<PathTrail> trails,
        out List<EnemyLanes> lanes)
    {
        boards = new List<MapBoard>();
        trails = new List<PathTrail>();
        lanes = new List<EnemyLanes>();

        foreach (ModuleLogic logic in registry.AllModules.Values)
        {
            boards.Add(logic.GetComponent<MapBoard>());

            PathTrail trail = logic.GetComponent<PathTrail>();
            if (trail != null)
            {
                trails.Add(trail);
            }

            EnemyLanes lane = logic.GetComponent<EnemyLanes>();
            if (lane != null)
            {
                lanes.Add(lane);
            }
        }
    }

    // 모든 얼음 보드의 고정 모닥불 보호 영역과 그 자리에 놓인 불빛을 시작할 때 한 번 만듭니다.
    private void BuildCampfires(List<MapBoard> boards)
    {
        CampfireCalc calc = new();
        campfireLights = new CampfireLightController();
        for (int index = 0; index < boards.Count; index++)
        {
            MapBoard board = boards[index];
            IceZone iceZone = board.GetComponent<IceZone>();
            if (iceZone != null)
            {
                CampfireData data = calc.BuildData(board.Cells, iceZone.CampfireRange);
                iceZone.SetCampfire(data);
                campfireLights.Collect(board, iceZone.CampfireRange);
            }
        }
    }

    // 날짜가 바뀔 때마다 모든 모듈의 레인을 그 날짜로 다시 계산한다.
    private void OnDayChanged()
    {
        int day = mapGame.Rule.DayCount;
        for (int i = 0; i < laneModules.Count; i++)
        {
            laneModules[i].RefreshForDay(day);
        }
    }

    // 사막 모듈이 아직 잠겨있으면 밤이 되어도 지대 효과 적용을 건너뛴다.
    private void OnDesertNightChanged()
    {
        if (!desertBoard.IsUnlocked)
        {
            return;
        }

        zoneEffectApplier.OnNightChanged();
    }

    // 낮이 되면 불 칸이 대미지를 끊도록 알린다.
    private void OnFireDayChanged()
    {
        FireReceiver.SetNight(false);
    }

    // 밤이 되면 불 칸이 대미지를 넣도록 알린다.
    private void OnFireNightChanged()
    {
        FireReceiver.SetNight(true);
        IgniteStandingUnits();
    }

    // 낮에 배치돼 진입 신호를 놓친 유닛도 밤이 오면 불 칸이면 점화한다.
    private void IgniteStandingUnits()
    {
        for (int i = 0; i < mapGame.Units.Count; i++)
        {
            GameObject unit = mapGame.Units.UnitAt(i);
            PlacementArea area = mapGame.Units.AreaAt(i);
            for (int j = 0; j < area.Cells.Count; j++)
            {
                Tile tile = area.Board.Cells[area.Cells[j]];
                FireReceiver.ReceiveEntry(tile, unit.transform);
            }
        }
    }
}
