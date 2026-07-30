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

    private List<PathTrail> pathTrails;

    private void Start()
    {
        List<MapBoard> boards = ModuleBoards();

        PointerPick pointerPick = new PointerPick(boards);
        UnitReplace replace = new UnitReplace(mapGame.Units);

        BuildingUiLink buildingUi = new BuildingUiLink();
        buildingUi.ui = mapGame.Ui;
        buildingUi.rule = mapGame.Rule;

        view.pointerPick = pointerPick;
        view.replace = replace;
        view.rangeInfo = new RangeInfo(mapGame.Units);
        view.citizenManager = mapGame.CitizenManager;
        view.resourcesManager = mapGame.ResourcesManager;

        PlaceAction action = new PlaceAction();
        action.palette = palette;
        action.pointerPick = pointerPick;
        action.placer = mapGame.Placer;
        action.remover = new UnitRemover(boards, mapGame.Units, mapGame.HeroRoster);
        action.replace = replace;
        action.buildingUi = buildingUi;
        action.view = view;
        action.heroRoster = mapGame.HeroRoster;
        action.placeYOffset = placeYOffset;

        command.pointerPick = pointerPick;
        command.dragDetect = new DragDetect(dragPixels);
        command.replace = replace;
        command.buildingUi = buildingUi;
        command.action = action;
        command.ghost = new PlaceGhost(view, placeYOffset);
        command.placeYOffset = placeYOffset;
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
        

        mapGame.Rule.ChangeToNight += view.ClearMode;

        // 확장 이벤트: 5일마다 GameManager가 쏘고, 밤이 되면 선택을 무른다.
        // 미배선이면 확장만 꺼지고 나머지 조립은 그대로 돈다.
        if (expand != null)
        {
            mapGame.Rule.ChangeToNight += expand.CancelChoices;
        }
    }

    private void OnDestroy()
    {
        mapGame.Rule.ChangeToNight -= view.ClearMode;
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
}