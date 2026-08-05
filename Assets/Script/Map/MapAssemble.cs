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

    private List<PathTrail> pathTrails;
    private List<EnemyLanes> laneModules;
    private PlaceGhost ghost;
    private HeroSkillCastController skillCast;

    private void Start()
    {
        List<MapBoard> boards = ModuleBoards();

        PointerPick pointerPick = new PointerPick(boards);
        PlaceFinder finder = new PlaceFinder(pointerPick, palette, placeYOffset);
        UnitReplace replace = new UnitReplace(mapGame.Units);

        DayNightBuildRule dayNightRule = new DayNightBuildRule();
        dayNightRule.rule = mapGame.Rule;

        skillCast = new HeroSkillCastController { dayNightRule = dayNightRule };

        RangeInfo rangeInfo = new RangeInfo();
        RangeTileData rangeStore = new RangeTileData();
        RangeCalc rangeCalc = new RangeCalc(rangeInfo);

        if (tilePaintView != null)
        {
            tilePaintView.skillCast = skillCast;
            tilePaintView.finder = finder;
            tilePaintView.rangeStore = rangeStore;
        }

        rangeInput.pointerPick = pointerPick;
        rangeInput.rangeCalc = rangeCalc;
        rangeInput.rangeStore = rangeStore;

        view.pointerPick = pointerPick;
        view.replace = replace;
        view.rangeInfo = rangeInfo;
        view.citizenManager = mapGame.CitizenManager;
        view.resourcesManager = mapGame.ResourcesManager;

        PlaceAction action = new PlaceAction();
        action.palette = palette;
        action.finder = finder;
        action.placer = mapGame.Placer;
        action.remover = new UnitRemover(mapGame.Units, mapGame.HeroRoster);
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
        command.dispatch = new Dictionary<PlaceMode, Action<Tile>>
        {
            { PlaceMode.Off, action.SelectTile },
            { PlaceMode.Place, action.PlaceUnit },
            { PlaceMode.Remove, action.RemoveUnit },
        };
        pathTrails = ModuleTrails();
        foreach(PathTrail trail in pathTrails)
        {
            mapGame.Rule.ChangeToDay += trail.PlayLoop;
            mapGame.Rule.ChangeToNight += trail.PlayOnce;
        }

        laneModules = ModuleLanes();
        mapGame.Rule.ChangeToDay += OnDayChanged;
        OnDayChanged(); // 첫 날짜도 시작하자마자 바로 맞춘다 — 이벤트가 처음 울릴 때까지 기다리지 않는다


        mapGame.Rule.ChangeToNight += view.ClearMode;
        mapGame.Rule.ChangeToNight += skillCast.ClearSelection;

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

    // 레지스트리에 등록된 모듈들의 보드 목록. 모듈 루트에 ModuleLogic과 MapBoard가 함께 산다.
    private List<MapBoard> ModuleBoards()
    {
        List<MapBoard> boards = new();
        foreach (ModuleLogic logic in registry.AllModules.Values)
        {
            boards.Add(logic.GetComponent<MapBoard>());
        }
        return boards;
    }
    private List<PathTrail> ModuleTrails()
    {
        List<PathTrail> trails = new();
        foreach (ModuleLogic logic in registry.AllModules.Values)
        {
            PathTrail trail = logic.GetComponent<PathTrail>();
            if (trail != null)
            {
                trails.Add(trail);
            }
        }
        return trails;
    }

    // 레지스트리에 등록된 모듈들의 EnemyLanes 목록.
    private List<EnemyLanes> ModuleLanes()
    {
        List<EnemyLanes> lanes = new();
        foreach (ModuleLogic logic in registry.AllModules.Values)
        {
            EnemyLanes found = logic.GetComponent<EnemyLanes>();
            if (found != null)
            {
                lanes.Add(found);
            }
        }
        return lanes;
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
}
