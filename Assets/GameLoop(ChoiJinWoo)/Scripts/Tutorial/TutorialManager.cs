using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] private TutorialOverlayUI overlay;
    [SerializeField] private TutorialStepDefinition[] steps;

    [Tooltip("영웅 배치 단계에서 로스터 아이콘을 고른 뒤(맵 클릭 대기 중) 보여줄 문구. " +
        "이 순간엔 스포트라이트로 짚어줄 UI가 없어 화면 전체를 막지 않고 이 문구만 띄운다.")]
    [SerializeField] private string placeHeroMapClickMessageKey;

    [Tooltip("영웅 재배치 단계에서 재배치 버튼을 누른 뒤(집기/내려놓기 대기 중) 보여줄 문구. " +
        "PlaceHero의 placeHeroMapClickMessageKey와 같은 이유로 화면 전체를 막지 않는다.")]
    [SerializeField] private string relocateHeroMapClickMessageKey;

    private CitizenManager citizenManager;
    private BaseConstructor baseConstructor;
    private HeroRoster heroRoster;
    private PlacePalette placePalette;
    private BuildModePanel buildModePanel;
    private BuildingPanel buildingPanel;
    private GameManager gameManager;
    private ResourcesManager resourcesManager;
    private RegionOverviewPanel regionOverviewPanel;
    private MapGame mapGame;
    private MapView mapView;
    private UiManager uiManager;
    private EnviromentManager enviromentManager;
    private SaveManager saveManager;
    private TutorialState state;

    private int currentIndex = -1;
    private int citizenSnapshot;
    private int usedCitizenSnapshot;
    private string lastShownMessageKey;
    private readonly HashSet<HeroRosterEntry> placedSnapshot = new();

    private TutorialWaypoint currentWaypoint;
    private bool waypointDirty = true;

    private bool sequenceFinished;
    private bool dayZeroResetDone;

    private CanvasGroup sceneHudGroup;
    private CanvasGroup uiManagerHudGroup;
    private bool hudBlocked;

    private CanvasGroup gameSpeedGroup;
    private bool gameSpeedBlocked;

    private CanvasGroup heroUpgradeGroup;
    private bool heroUpgradeBlocked;

    [Tooltip("0일차 리셋이 끝난 뒤(진짜 1일차 시작) 한 번 보여줄 완료 메시지 키.")]
    [SerializeField] private string completionMessageKey;
    private bool showingCompletionMessage;

    [Inject]
    private void Construct(CitizenManager citizenManager,
        BaseConstructor baseConstructor, HeroRoster heroRoster, PlacePalette placePalette,
        BuildModePanel buildModePanel,
        BuildingPanel buildingPanel, GameManager gameManager, ResourcesManager resourcesManager,
        RegionOverviewPanel regionOverviewPanel, MapGame mapGame, MapView mapView, UiManager uiManager,
        EnviromentManager enviromentManager,
        SaveManager saveManager, TutorialState state)
    {
        this.citizenManager = citizenManager;
        this.baseConstructor = baseConstructor;
        this.heroRoster = heroRoster;
        this.placePalette = placePalette;
        this.buildModePanel = buildModePanel;
        this.buildingPanel = buildingPanel;
        this.gameManager = gameManager;
        this.resourcesManager = resourcesManager;
        this.regionOverviewPanel = regionOverviewPanel;
        this.mapGame = mapGame;
        this.mapView = mapView;
        this.uiManager = uiManager;
        this.enviromentManager = enviromentManager;
        this.saveManager = saveManager;
        this.state = state;
    }

    private void Start()
    {
        WireRuntimeWaypoints();
        ResolveHudGroups();

        if (state.Seen || steps.Length == 0)
        {
            overlay.Hide();
            enabled = false;
            return;
        }

        BeginStep(0);
    }

    private void WireRuntimeWaypoints()
    {
        var watched = new HashSet<GameObject>();
        foreach (var step in steps)
        {
            if (step.id == TutorialStepId.GameSpeedMention && step.waypoints != null)
            {
                foreach (var waypoint in step.waypoints)
                {
                    if (waypoint != null) waypoint.target = uiManager.GameSpeedUiRect;
                }
            }

            if (step.waypoints == null) continue;
            foreach (var waypoint in step.waypoints)
            {
                if (waypoint == null) continue;
                EnsureActivityWatcher(waypoint.target != null ? waypoint.target.gameObject : null, watched);
                EnsureActivityWatcher(waypoint.activationCheck, watched);
                EnsureActivityWatcher(waypoint.blockedWhile, watched);
            }
        }
    }

    private static void EnsureActivityWatcher(GameObject go, HashSet<GameObject> watched)
    {
        if (go == null || !watched.Add(go)) return;
        if (go.GetComponent<TutorialActivityWatcher>() == null) go.AddComponent<TutorialActivityWatcher>();
    }

    private void ResolveHudGroups()
    {
        sceneHudGroup = ResolveHudGroup(regionOverviewPanel.transform);
        uiManagerHudGroup = ResolveHudGroup(uiManager.GameSpeedUiRect);

        gameSpeedGroup = GetOrAddCanvasGroup(uiManager.GameSpeedUiRect);
        heroUpgradeGroup = GetOrAddCanvasGroup(FindStepTarget(TutorialStepId.HeroUpgradeMention));
    }

    private RectTransform FindStepTarget(TutorialStepId id)
    {
        foreach (var step in steps)
        {
            if (step.id == id && step.waypoints != null && step.waypoints.Length > 0)
                return step.waypoints[0].target;
        }
        return null;
    }

    private static CanvasGroup ResolveHudGroup(Transform anchor)
    {
        if (anchor == null) return null;

        Canvas canvas = anchor.GetComponentInParent<Canvas>(true);
        if (canvas == null) return null;

        return GetOrAddCanvasGroup(canvas.transform);
    }

    private static CanvasGroup GetOrAddCanvasGroup(Transform target)
    {
        if (target == null) return null;

        CanvasGroup group = target.GetComponent<CanvasGroup>();
        return group != null ? group : target.gameObject.AddComponent<CanvasGroup>();
    }

    private void SetHudBlocked(bool blocked)
    {
        if (hudBlocked == blocked) return;
        hudBlocked = blocked;

        SetGroupBlocked(sceneHudGroup, blocked);
        SetGroupBlocked(uiManagerHudGroup, blocked);
    }

    private void SetBlocked(CanvasGroup group, ref bool current, bool blocked)
    {
        if (current == blocked) return;
        current = blocked;
        SetGroupBlocked(group, blocked);
    }

    private static void SetGroupBlocked(CanvasGroup group, bool blocked)
    {
        if (group == null) return;
        group.blocksRaycasts = !blocked;
    }

    private void OnEnable()
    {
        TutorialInputGate.BlockEscapeClose = true;
        TutorialInputGate.BlockHotkeys = true;
        TutorialInputGate.BlockSave = true;
        TutorialInputGate.BlockHeroRetrieve = true;
        TutorialActivityWatcher.Changed += OnWaypointActivityChanged;
        citizenManager.CitizenChanged += OnCitizenChanged;
        baseConstructor.Built += OnBuilt;
        heroRoster.Changed += OnHeroRosterChanged;
        buildingPanel.Upgraded += OnUpgraded;
        overlay.AcknowledgeClicked += OnAcknowledgeClicked;
        gameManager.ChangeToNight += OnChangeToNight;
        enviromentManager.OnDay += OnDayTransitionComplete;
        mapView.Replaced += OnHeroReplaced;
    }

    private void OnDisable()
    {
        TutorialInputGate.BlockEscapeClose = false;
        TutorialInputGate.BlockHotkeys = false;
        TutorialInputGate.BlockSave = false;
        TutorialInputGate.BlockHeroRetrieve = false;
        TutorialInputGate.BlockPanelOpen = false;
        TutorialInputGate.BlockHeroUpgradeOpen = false;
        TutorialInputGate.BlockHeroPlacementFromInventory = false;
        TutorialActivityWatcher.Changed -= OnWaypointActivityChanged;
        SetHudBlocked(false);
        SetBlocked(gameSpeedGroup, ref gameSpeedBlocked, false);
        SetBlocked(heroUpgradeGroup, ref heroUpgradeBlocked, false);
        TutorialInputGate.BlockPlayerSkillCast = false;
        citizenManager.CitizenChanged -= OnCitizenChanged;
        baseConstructor.Built -= OnBuilt;
        heroRoster.Changed -= OnHeroRosterChanged;
        buildingPanel.Upgraded -= OnUpgraded;
        overlay.AcknowledgeClicked -= OnAcknowledgeClicked;
        gameManager.ChangeToNight -= OnChangeToNight;
        enviromentManager.OnDay -= OnDayTransitionComplete;
        mapView.Replaced -= OnHeroReplaced;
    }

    private void Update()
    {
        if (sequenceFinished) return;

        bool awaitingPlaceClick = IsActive(TutorialStepId.PlaceHero) && placePalette.Mode == PlaceMode.Place;

        bool awaitingRelocateClick = IsActive(TutorialStepId.RelocateHero) && placePalette.Mode == PlaceMode.Replace;
        bool awaitingMapClick = awaitingPlaceClick || awaitingRelocateClick;

        TutorialInputGate.BlockPanelOpen = awaitingMapClick;
        SetHudBlocked(awaitingMapClick);
        if (awaitingMapClick)
        {
            ShowUnblockedMessage(awaitingRelocateClick ? relocateHeroMapClickMessageKey : placeHeroMapClickMessageKey);
            return;
        }

        TutorialInputGate.BlockHeroUpgradeOpen = IsActive(TutorialStepId.HeroUpgradeMention);
        SetBlocked(heroUpgradeGroup, ref heroUpgradeBlocked, IsActive(TutorialStepId.HeroUpgradeMention));
        TutorialInputGate.BlockHeroPlacementFromInventory = IsActive(TutorialStepId.HeroCombineMention);

        SetBlocked(gameSpeedGroup, ref gameSpeedBlocked, IsActive(TutorialStepId.GameSpeedMention));
        TutorialInputGate.BlockPlayerSkillCast = IsActive(TutorialStepId.PlayerSkillMention);

        var step = steps[currentIndex];
        if (waypointDirty)
        {
            currentWaypoint = ResolveWaypoint();
            waypointDirty = false;
        }
        var waypoint = currentWaypoint;

        if (waypoint == null && step.waypoints != null && step.waypoints.Length > 0)
        {
            overlay.Hide();
            SetHudBlocked(true);
            lastShownMessageKey = null;
            return;
        }

        SetHudBlocked(false);
        overlay.Show(step.completesOnAcknowledge);

        RefreshMessage(waypoint);
        overlay.SetSpotlight(waypoint?.target);
    }

    private void ShowUnblockedMessage(string messageKey)
    {
        overlay.ShowUnblocked();
        overlay.PositionMessageBoxBottomCenter();
        if (lastShownMessageKey == messageKey) return;

        lastShownMessageKey = messageKey;
        overlay.SetMessage(messageKey);
    }

    private void OnWaypointActivityChanged() => waypointDirty = true;

    private TutorialWaypoint ResolveWaypoint()
    {
        if (currentIndex < 0 || currentIndex >= steps.Length) return null;

        var waypoints = steps[currentIndex].waypoints;
        if (waypoints == null) return null;

        for (int i = waypoints.Length - 1; i >= 0; i--)
        {
            var waypoint = waypoints[i];
            if (waypoint?.target == null) continue;
            if (waypoint.blockedWhile != null && waypoint.blockedWhile.activeInHierarchy) continue;

            GameObject gate = waypoint.activationCheck != null ? waypoint.activationCheck : waypoint.target.gameObject;
            if (gate.activeInHierarchy) return waypoint;
        }
        return null;
    }

    private void RefreshMessage(TutorialWaypoint waypoint)
    {
        if (currentIndex < 0 || currentIndex >= steps.Length) return;

        string key = !string.IsNullOrEmpty(waypoint?.messageKey) ? waypoint.messageKey : steps[currentIndex].messageKey;
        if (key == lastShownMessageKey) return;

        lastShownMessageKey = key;
        overlay.SetMessage(key);
    }

    private void BeginStep(int index)
    {
        currentIndex = index;

        var step = steps[index];
        if (step.pauseTimeWhileActive) uiManager.GameSpeedUi.OnButtonClick((int)Speed.Zero);

        citizenSnapshot = citizenManager.CurrentCitizen;
        usedCitizenSnapshot = citizenManager.UsedCitizen;
        SnapshotPlacedHeroes();

        lastShownMessageKey = null;
        currentWaypoint = ResolveWaypoint();
        waypointDirty = false;
        overlay.Show(step.completesOnAcknowledge);
        RefreshMessage(currentWaypoint);
    }

    private void SnapshotPlacedHeroes()
    {
        placedSnapshot.Clear();
        foreach (var entry in heroRoster.Entries)
        {
            if (entry.State == HeroRosterState.Placed) placedSnapshot.Add(entry);
        }
    }

    private void CompleteStep()
    {
        if (steps[currentIndex].pauseTimeWhileActive) uiManager.GameSpeedUi.OnButtonClick((int)Speed.Normal);

        int next = currentIndex + 1;

        if (next >= steps.Length) FinishSequence();
        else BeginStep(next);
    }

    private void FinishSequence()
    {
        overlay.Hide();
        TutorialInputGate.BlockEscapeClose = false;
        TutorialInputGate.BlockHotkeys = false;
        TutorialInputGate.BlockPanelOpen = false;
        TutorialInputGate.BlockHeroUpgradeOpen = false;
        TutorialInputGate.BlockHeroPlacementFromInventory = false;
        SetHudBlocked(false);
        SetBlocked(gameSpeedGroup, ref gameSpeedBlocked, false);
        SetBlocked(heroUpgradeGroup, ref heroUpgradeBlocked, false);
        TutorialInputGate.BlockPlayerSkillCast = false;
        sequenceFinished = true;
        TryFullyDisable();
    }

    private void TryFullyDisable()
    {
        if (sequenceFinished && dayZeroResetDone && !showingCompletionMessage) enabled = false;
    }

    [ContextMenu("튜토리얼 초기화 후 재시작")]
    public void DebugRestart()
    {
        if (steps.Length == 0) return;

        state.Reset();
        gameManager.ResetDayCountForTutorialReplay();
        currentIndex = -1;
        sequenceFinished = false;
        dayZeroResetDone = false;
        showingCompletionMessage = false;
        uiManager.GameSpeedUi.OnButtonClick((int)Speed.Normal);
        enabled = true;
        BeginStep(0);
    }

    private bool IsActive(TutorialStepId id) => currentIndex >= 0 && currentIndex < steps.Length && steps[currentIndex].id == id;

    private void OnAcknowledgeClicked()
    {
        if (showingCompletionMessage)
        {
            showingCompletionMessage = false;
            overlay.Hide();
            TutorialInputGate.BlockEscapeClose = false;
            TutorialInputGate.BlockHotkeys = false;
            uiManager.GameSpeedUi.OnButtonClick((int)Speed.Normal);
            TryFullyDisable();
            return;
        }

        if (sequenceFinished) return;
        if (currentIndex < 0 || currentIndex >= steps.Length) return;
        if (steps[currentIndex].completesOnAcknowledge) CompleteStep();
    }

    private void OnBuilt(object built)
    {
        if (!IsActive(TutorialStepId.BuildHouse)) return;
        if (built is House) CompleteStep();
    }

    private void OnCitizenChanged()
    {
        if (IsActive(TutorialStepId.RecruitCitizen))
        {
            if (citizenManager.CurrentCitizen > citizenSnapshot) CompleteStep();
        }
        else if (IsActive(TutorialStepId.AssignWorker))
        {
            if (citizenManager.UsedCitizen > usedCitizenSnapshot) CompleteStep();
        }
    }

    private void OnUpgraded()
    {
        if (!IsActive(TutorialStepId.BuildingUpgradeMention)) return;
        CompleteStep();
    }

    private void OnHeroRosterChanged()
    {
        if (!IsActive(TutorialStepId.PlaceHero)) return;

        foreach (var entry in heroRoster.Entries)
        {
            if (entry.State == HeroRosterState.Placed && !placedSnapshot.Contains(entry))
            {
                CompleteStep();
                return;
            }
        }
    }

    private void OnHeroReplaced(GameObject unit, OccupantKind kind)
    {
        if (!IsActive(TutorialStepId.RelocateHero)) return;
        if (kind != OccupantKind.MeleeHero && kind != OccupantKind.RangedHero) return;

        buildModePanel.CancelPlaceModeFromTutorial();
        CompleteStep();
    }

    private void OnChangeToNight()
    {
        if (!IsActive(TutorialStepId.NightMention)) return;
        CompleteStep();
    }

    private void OnDayTransitionComplete()
    {
        if (dayZeroResetDone) return;
        dayZeroResetDone = true;
        DeferredReset().Forget();
    }

    private async UniTaskVoid DeferredReset()
    {
        await UniTask.Yield();

        ResetHeroes();
        ResetBuildings();
        resourcesManager.Reset();
        citizenManager.Reset();
        gameManager.ResetHpToFull();

        gameManager.perfactDefence = false;

        state.MarkSeen();

        TutorialInputGate.BlockSave = false;
        saveManager.SaveNow();

        ShowCompletionMessage();
    }

    private void ShowCompletionMessage()
    {
        showingCompletionMessage = true;
        TutorialInputGate.BlockEscapeClose = true;
        TutorialInputGate.BlockHotkeys = true;
        uiManager.GameSpeedUi.OnButtonClick((int)Speed.Zero);
        overlay.Show(true);
        overlay.SetSpotlight(null);
        overlay.SetMessage(completionMessageKey);
    }

    private void ResetHeroes()
    {
        foreach (var entry in new List<HeroRosterEntry>(heroRoster.Entries))
        {
            GameObject unit = entry.PlacedUnit;
            if (unit != null)
            {
                if (mapGame.Units.TryGetArea(unit, out PlacementArea area))
                {
                    AreaPlace.Remove(area);
                    mapGame.Units.Remove(unit);
                }

                if (unit.TryGetComponent(out Hero hero))
                {
                    hero.PrepareForDespawn();
                    PoolManager.Instance.Despawn(unit);
                }
                else
                {
                    Destroy(unit);
                }
            }

            citizenManager.FreeCitizenForHero(entry.CitizenCost);
            heroRoster.Remove(entry);
        }
    }

    private void ResetBuildings()
    {
        foreach (var region in regionOverviewPanel.Regions)
        {
            for (int i = 0; i < region.Slots.Count; i++)
            {
                if (!region.Slots[i].IsEmpty) baseConstructor.Demolish(region, i);
            }
        }
    }
}
