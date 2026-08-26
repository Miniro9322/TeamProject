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
    private BuildingPanel buildingPanel;
    private GameManager gameManager;
    private ResourcesManager resourcesManager;
    private RegionOverviewPanel regionOverviewPanel;
    private MapGame mapGame;
    private MapView mapView;
    private UiManager uiManager;
    private PlayerSkillPanel playerSkillPanel;
    private EnviromentManager enviromentManager;
    private SaveManager saveManager;
    private TutorialState state;

    private int currentIndex = -1;
    private int citizenSnapshot;
    private int usedCitizenSnapshot;
    private string lastShownMessageKey;
    private readonly HashSet<HeroRosterEntry> placedSnapshot = new();

    private bool sequenceFinished;
    private bool dayZeroResetDone;

    // 상시 HUD를 감싸는 두 최상위 Canvas(씬 정적 "Canvas": RegionOverviewPanel/HeroInventory/
    // DayNightButton 등, UiManager 런타임 "Canvas": 가이드/메뉴/배속 등) - PlaceHero 맵 클릭 대기
    // 중에만 CanvasGroup으로 통째로 잠근다. ResolveHudGroups 참고.
    private CanvasGroup sceneHudGroup;
    private CanvasGroup uiManagerHudGroup;
    private bool hudBlocked;

    // GameSpeedMention/PlayerSkillMention도 HeroUpgradeMention과 같은 순수 언급 스텝이다 - 스포트라이트
    // 구멍으로 실제 배속/스킬 버튼이 눌리면 배속이 바뀌거나 스킬이 나가버린다. 각 패널 자체에
    // CanvasGroup을 붙여 그 스텝인 동안만 막는다. TutorialManager가 uiManager.GameSpeedUi.OnButtonClick을
    // 코드로 직접 호출해 배속을 걸 때(pauseTimeWhileActive)는 CanvasGroup과 무관한 일반 메서드 호출이라
    // 영향받지 않는다 - blocksRaycasts는 EventSystem 레이캐스트에만 관여한다.
    private CanvasGroup gameSpeedGroup;
    private CanvasGroup playerSkillGroup;
    private bool gameSpeedBlocked;
    private bool playerSkillBlocked;

    [Tooltip("0일차 리셋이 끝난 뒤(진짜 1일차 시작) 한 번 보여줄 완료 메시지 키.")]
    [SerializeField] private string completionMessageKey;
    private bool showingCompletionMessage;

    [Inject]
    private void Construct(CitizenManager citizenManager,
        BaseConstructor baseConstructor, HeroRoster heroRoster, PlacePalette placePalette,
        BuildingPanel buildingPanel, GameManager gameManager, ResourcesManager resourcesManager,
        RegionOverviewPanel regionOverviewPanel, MapGame mapGame, MapView mapView, UiManager uiManager,
        PlayerSkillPanel playerSkillPanel, EnviromentManager enviromentManager,
        SaveManager saveManager, TutorialState state)
    {
        this.citizenManager = citizenManager;
        this.baseConstructor = baseConstructor;
        this.heroRoster = heroRoster;
        this.placePalette = placePalette;
        this.buildingPanel = buildingPanel;
        this.gameManager = gameManager;
        this.resourcesManager = resourcesManager;
        this.regionOverviewPanel = regionOverviewPanel;
        this.mapGame = mapGame;
        this.mapView = mapView;
        this.uiManager = uiManager;
        this.playerSkillPanel = playerSkillPanel;
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
        foreach (var step in steps)
        {
            if (step.id != TutorialStepId.GameSpeedMention || step.waypoints == null) continue;
            foreach (var waypoint in step.waypoints)
            {
                if (waypoint != null) waypoint.target = uiManager.GameSpeedUiRect;
            }
        }
    }

    // MapInput.cs는 맵 클릭을 처리하기 전에 EventSystem.IsPointerOverGameObject()로 UI 위인지부터
    // 본다 - 그래서 화면을 덮는 raycastTarget UI가 하나라도 있으면 맵 클릭 자체가 막힌다. PlaceHero
    // 단계가 맵 클릭을 기다리며 오버레이 딤/fullscreenBlocker를 전부 꺼야 하는 이유가 이것이다.
    // 문제는 그 순간 스포트라이트 밖의 다른 버튼(가이드, 메뉴, 지역, 영웅 로스터, 낮/밤, 배속 등)도
    // 전부 눌려버린다는 것 - 상시 HUD가 서로 다른 최상위 Canvas 두 개(씬 정적 "Canvas"와 UiManager
    // 런타임 "Canvas")에 나뉘어 있어 하나로 감쌀 공통 부모가 없으므로, 각 Canvas에 CanvasGroup을
    // 찾거나 붙여 레퍼런스만 들고 있는다 - 매 프레임 SetHudBlocked가 blocksRaycasts만 껐다 켠다.
    // (fullscreenBlocker처럼 Graphic 자체를 지우는 게 아니라 레이캐스트 대상에서만 빼는 것이라
    // IsPointerOverGameObject()는 자연히 false가 되어 맵 클릭은 그대로 통과한다.)
    private void ResolveHudGroups()
    {
        sceneHudGroup = ResolveHudGroup(regionOverviewPanel.transform);
        uiManagerHudGroup = ResolveHudGroup(uiManager.GameSpeedUiRect);

        // 이 둘은 조상 Canvas가 아니라 패널 자기 자신만 막는다 - GameSpeedUi가 속한 Canvas를 통째로
        // 막아버리면(uiManagerHudGroup) 그 안의 가이드/메뉴 등 무관한 버튼까지 같이 막히기 때문.
        gameSpeedGroup = GetOrAddCanvasGroup(uiManager.GameSpeedUiRect);
        playerSkillGroup = GetOrAddCanvasGroup(playerSkillPanel != null ? playerSkillPanel.transform : null);
    }

    private static CanvasGroup ResolveHudGroup(Transform anchor)
    {
        if (anchor == null) return null;

        // RegionOverviewPanel/GameSpeedUi 모두 기본적으로 SetActive(false)로 시작하는 패널이라
        // (RegionOverviewPanel.Close, UiManager.Awake의 gameSpeedUi.SetActive(false)), 비활성
        // 상태에서도 조상 Canvas를 찾을 수 있도록 includeInactive를 켠다.
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

    // 두 HUD Canvas를 한꺼번에 잠그거나 푼다 - 값이 실제로 바뀔 때만 건드린다.
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
        // interactable은 안 건드린다 - Selectable의 disabledColor 트랜지션이 걸려, 이미지가 여러
        // 장인 버튼은 target graphic만 탁하게 변해 다른 이미지들과 색이 어긋나 보인다(Button 인스펙터
        // Transition 참고). blocksRaycasts만 꺼도 클릭 자체가 전혀 안 먹히니 충분하다.
        group.blocksRaycasts = !blocked;
    }

    private void OnEnable()
    {
        TutorialInputGate.BlockEscapeClose = true;
        TutorialInputGate.BlockHotkeys = true;
        // 0일차 연습 상태는 다음 날이 되는 순간 전부 초기화되는 임시 데이터라, 그 사이에
        // SaveManager가 저장하지 못하게 막는다 - 컴포넌트가 완전히 꺼질 때(0일차 리셋까지 끝난
        // 뒤)까지 유지한다.
        TutorialInputGate.BlockSave = true;
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
        TutorialInputGate.BlockPanelOpen = false;
        TutorialInputGate.BlockHeroUpgradeOpen = false;
        TutorialInputGate.BlockHeroPlacementFromInventory = false;
        SetHudBlocked(false);
        SetBlocked(gameSpeedGroup, ref gameSpeedBlocked, false);
        SetBlocked(playerSkillGroup, ref playerSkillBlocked, false);
        citizenManager.CitizenChanged -= OnCitizenChanged;
        baseConstructor.Built -= OnBuilt;
        heroRoster.Changed -= OnHeroRosterChanged;
        buildingPanel.Upgraded -= OnUpgraded;
        overlay.AcknowledgeClicked -= OnAcknowledgeClicked;
        gameManager.ChangeToNight -= OnChangeToNight;
        enviromentManager.OnDay -= OnDayTransitionComplete;
        mapView.Replaced -= OnHeroReplaced;
    }

    // 현재 단계의 waypoints 중 저작한 순서로 가장 깊이 들어간 활성 상태를 스포트라이트하고,
    // 그 waypoint에 딸린 문구(없으면 단계 기본 문구)를 보여준다. 패널들이 SetActive로 토글되므로
    // 단계가 바뀌지 않아도 매 프레임 다시 계산해야 한다.
    private void Update()
    {
        if (sequenceFinished) return; // 스텝은 끝났고 0일차 리셋은 EnviromentManager.OnDay가 알아서 처리 - 더 그릴 것 없음

        // 로스터에서 영웅을 고르면(배치 대기 중) 다음 클릭은 UI가 아니라 3D 맵 타일이라 짚어줄
        // 사각형이 없다 - 이 순간만큼은 딤을 전부 끄고 맵을 자유롭게 클릭할 수 있게 한다.
        bool awaitingPlaceClick = IsActive(TutorialStepId.PlaceHero) && placePalette.Mode == PlaceMode.Place;
        // 재배치 버튼을 누른 뒤(영웅을 집거나 새 자리에 내려놓는 동안)도 똑같이 3D 맵 타일을 자유롭게
        // 클릭할 수 있어야 한다.
        bool awaitingRelocateClick = IsActive(TutorialStepId.RelocateHero) && placePalette.Mode == PlaceMode.Replace;
        bool awaitingMapClick = awaitingPlaceClick || awaitingRelocateClick;
        // 딤을 끄는 동안엔 스포트라이트 밖의 다른 버튼(지역 슬롯, 거점 화면, 가이드 등)도 함께
        // 풀려버리니, 그런 패널이 마우스로 열리는 것만 따로 막아둔다 - HUD CanvasGroup 차단(아래)의
        // 이중 방어선이다.
        TutorialInputGate.BlockPanelOpen = awaitingMapClick;
        SetHudBlocked(awaitingMapClick);
        if (awaitingMapClick)
        {
            ShowUnblockedMessage(awaitingRelocateClick ? relocateHeroMapClickMessageKey : placeHeroMapClickMessageKey);
            return;
        }

        // HeroUpgradeMention은 스포트라이트로 강화 버튼을 짚어 언급만 할 뿐 실제로 눌러서 완료되는
        // 스텝이 아니다 - 그 구멍으로 진짜 버튼이 눌려 메뉴가 열리는 걸 막는다.
        TutorialInputGate.BlockHeroUpgradeOpen = IsActive(TutorialStepId.HeroUpgradeMention);
        // HeroCombineMention은 합성(더블클릭/일괄합성)은 그대로 두되, 로스터 아이콘 단일 클릭으로
        // 배치 모드에 들어가는 것만 막는다.
        TutorialInputGate.BlockHeroPlacementFromInventory = IsActive(TutorialStepId.HeroCombineMention);

        // GameSpeedMention/PlayerSkillMention도 스포트라이트로 짚어 언급만 하는 스텝이라, 그 구멍으로
        // 실제 버튼이 눌리는 걸 막는다.
        SetBlocked(gameSpeedGroup, ref gameSpeedBlocked, IsActive(TutorialStepId.GameSpeedMention));
        SetBlocked(playerSkillGroup, ref playerSkillBlocked, IsActive(TutorialStepId.PlayerSkillMention));

        var step = steps[currentIndex];
        var waypoint = ResolveWaypoint();

        // waypoint가 지정돼는 있는데 아직 하나도 활성화 안 된 상태 - 예를 들어 밤 전환 애니메이션이
        // 끝나기 전이라 PlayerSkillPanel/배속 패널이 아직 안 켜진 순간. 이럴 땐 엉뚱한 문구(키 없음
        // 등)를 보여주는 대신 오버레이를 잠깐 숨기고, 실제로 켜지는 순간 다시 나타난다. waypoint를
        // 아예 안 쓰는 스텝(순수 문구 안내)까지 숨기면 안 되니 그 경우는 그대로 둔다.
        // 오버레이 딤이 꺼진 동안엔 스포트라이트 보호가 없어지므로, awaitingMapClick과 마찬가지로
        // HUD(메뉴/가이드/도감 등)는 계속 잠가둔다 - 안 그러면 낮->밤 전환 같은 이 대기 구간에
        // 그런 버튼들이 그대로 눌려버린다.
        if (waypoint == null && step.waypoints != null && step.waypoints.Length > 0)
        {
            overlay.Hide();
            SetHudBlocked(true);
            lastShownMessageKey = null;
            return;
        }

        SetHudBlocked(false);
        overlay.Show(step.completesOnAcknowledge);
        // 메시지를 먼저 갱신해야 SetSpotlight가 그 문구로 리빌드된 messageBox 크기를 보고 위치를
        // 잡는다 - 순서가 바뀌면 문구가 바뀌는 첫 프레임에 직전 문구 크기로 잘못 배치된다.
        RefreshMessage(waypoint);
        overlay.SetSpotlight(waypoint?.target);
    }

    // 스포트라이트로 짚어줄 UI가 없는 대기 구간(맵 클릭 대기 등)에 쓴다 - 딤을 전부 풀고 문구만 띄운다.
    private void ShowUnblockedMessage(string messageKey)
    {
        overlay.ShowUnblocked();
        if (lastShownMessageKey == messageKey) return;

        lastShownMessageKey = messageKey;
        overlay.SetMessage(messageKey);
    }

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

    // waypoint별 문구가 있으면 그걸, 없으면 단계의 기본 문구를 보여준다. 같은 문구를 매 프레임
    // 다시 설정하지 않도록 마지막으로 보여준 키를 기억해둔다.
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

        lastShownMessageKey = null; // 새 단계 진입 - 문구를 무조건 다시 갱신하게 한다
        overlay.Show(step.completesOnAcknowledge);
        RefreshMessage(ResolveWaypoint());
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
        SetBlocked(playerSkillGroup, ref playerSkillBlocked, false);
        sequenceFinished = true;
        TryFullyDisable();
    }

    private void TryFullyDisable()
    {
        if (sequenceFinished && dayZeroResetDone && !showingCompletionMessage) enabled = false;
    }

    // 테스트용 - 인스펙터에서 이 컴포넌트 헤더 우클릭 -> 실행하면 Play 모드 중에도 처음부터 다시 볼 수 있다.
    // UpgradeUI.OnDebugResetButton()과 같은 용도 - 필요하면 디버그 버튼의 OnClick에도 그대로 연결해서 쓸 수 있다.
    [ContextMenu("튜토리얼 초기화 후 재시작")]
    public void DebugRestart()
    {
        if (steps.Length == 0) return;

        state.Reset();
        gameManager.ResetDayCountForTutorialReplay(); // 실제 진행 중이던 날짜를 다시 "0일차"로 맞춘다
        currentIndex = -1;
        sequenceFinished = false;
        dayZeroResetDone = false;
        showingCompletionMessage = false;
        uiManager.GameSpeedUi.OnButtonClick((int)Speed.Normal); // 재시작 시점에 정지 상태였을 수도 있으니 방어적으로 되돌린다
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

        if (sequenceFinished) return; // 방어적 - 시퀀스 끝난 뒤엔 오버레이가 숨겨져 있어 이 경로로 안 와야 정상
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
        // UsedCitizen = 생산 시설 일꾼 + 영웅 배치 인력. AssignWorker 단계는 PlaceHero보다 앞서 있어
        // 이 시점엔 영웅 소모 인력이 아직 0이므로 증가분은 곧 "일꾼 배치"를 뜻한다.
        else if (IsActive(TutorialStepId.AssignWorker))
        {
            if (citizenManager.UsedCitizen > usedCitizenSnapshot) CompleteStep();
        }
    }

    // 건물 업그레이드 언급 단계는 completesOnAcknowledge로 "다음"을 눌러도 넘어가지만,
    // 실제로 업그레이드 버튼을 눌렀다면 굳이 "다음"을 또 누르게 하지 않고 그걸로 바로 완료한다.
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

    // RelocateHero 단계에서 재배치(집기->내려놓기)가 실제로 성공했을 때만 완료한다 - UnitReplace.Replaced는
    // 취소(ReturnHeld)에서는 안 울리므로 여기선 성공만 들어온다. kind 체크는 방어적인 것 - 이 스텝에서
    // 재배치 모드로 집을 수 있는 건 방금 배치한 영웅뿐이라 항상 참이어야 정상이다.
    private void OnHeroReplaced(GameObject unit, OccupantKind kind)
    {
        if (!IsActive(TutorialStepId.RelocateHero)) return;
        if (kind != OccupantKind.MeleeHero && kind != OccupantKind.RangedHero) return;

        // Replace 모드는 Place와 달리 한 번 내려놨다고 저절로 꺼지지 않는다(연달아 다른 유닛을
        // 더 옮길 수 있게 하려고 일부러 그렇게 만든 상시 모드) - 튜토리얼 입장에선 한 번의 재배치만
        // 확인하면 끝이므로, 여기서 직접 꺼줘야 다음 스텝으로 넘어간 뒤에도 Replace 모드가 남아
        // 엉뚱한 클릭에 다른 유닛을 집어버리는 일이 없다.
        placePalette.ClearMode();
        CompleteStep();
    }

    // NightMention 단계가 활성일 때 낮/밤 버튼을 실제로 눌러야(ChangeToNight 발생) 완료된다 -
    // 다른 강제 단계들(OnBuilt, OnCitizenChanged 등)과 같은 패턴. 완료 즉시 다음 단계
    // (PlayerSkillMention)로 넘어가지만, 그 스텝의 waypoint(PlayerSkillPanel)는 아직 안 켜져 있을 수
    // 있다 - PlayerSkillPanel은 이제 ChangeToNight이 아니라 밤 전환이 실제로 다 끝나는
    // EnviromentManager.OnNight에 맞춰 켜진다(PlayerSkillPanel.cs). GameSpeedMention과 마찬가지로
    // Update()가 매 프레임 다시 스포트라이트를 계산하므로, 패널이 늦게 켜지면 그 순간 자동으로 잡힌다.
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
        // 0일차 밤의 완벽방어 여부는 이 리셋으로 이미 의미가 없어졌다 - 지워두지 않으면 바로 아래
        // SaveNow()가 이 값을 perfectDefensePending으로 그대로 저장해버려서, 나중에 이 세이브를
        // 불러올 때 LoadManager가 특수자원 보상을 또 한 번 적용해(GetSpecial) 방금 0으로 되돌린
        // 특수자원이 다시 늘어나 보인다.
        gameManager.perfactDefence = false;

        state.MarkSeen();

        // ChangeToDay 시점엔 TutorialInputGate.BlockSave 때문에 SaveManager의 자동 저장이 건너뛰어졌다
        // (그때는 아직 0일차의 지워질 상태였으므로). 리셋이 끝나 진짜 깨끗한 1일차가 된 지금, 이 상태를
        // 놓치지 않도록 명시적으로 한 번 저장한다.
        TutorialInputGate.BlockSave = false;
        saveManager.SaveNow();

        ShowCompletionMessage();
    }

    // 0일차 리셋과 낮 전환까지 전부 끝난 뒤, 확인을 눌러야 넘어가는 완료 메시지를 한 번 보여준다.
    // 이걸 확인해야(OnAcknowledgeClicked) 비로소 컴포넌트를 완전히 끈다.
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

    // HeroCombineManager.Combine()의 제거 절차(영역 해제 -> 풀 반환 -> FreeCitizenForHero)를 그대로
    // 따른다 - UnitRemover는 영웅이 소모한 인력을 제대로 안 돌려준다.
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

    // 모든 지역의 채워진 슬롯을 전부 철거한다 - RegionOverviewPanel.Regions가 전체 지역의 유일한 소스.
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
