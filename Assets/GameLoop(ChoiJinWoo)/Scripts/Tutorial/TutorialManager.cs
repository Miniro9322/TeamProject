using System.Collections.Generic;
using UnityEngine;
using VContainer;

// 신규 플레이어를 실제 게임 화면에서 순서대로 강제로 안내하는 온보딩 튜토리얼.
// 이벤트 버스가 없는 프로젝트 컨벤션을 따라, 각 매니저의 event Action을 직접 구독해 완료를 판정한다.
// 씬의 RectTransform들을 인스펙터에 직접 연결해야 해서(스포트라이트 대상) plain class가 아니라
// MonoBehaviour로 둔다 - 다른 오케스트레이션 패널들(RegionOverviewPanel 등)과 같은 패턴.
public class TutorialManager : MonoBehaviour
{
    [SerializeField] private TutorialOverlayUI overlay;
    [SerializeField] private TutorialStepDefinition[] steps;

    [Tooltip("영웅 배치 단계에서 로스터 아이콘을 고른 뒤(맵 클릭 대기 중) 보여줄 문구. " +
        "이 순간엔 스포트라이트로 짚어줄 UI가 없어 화면 전체를 막지 않고 이 문구만 띄운다.")]
    [SerializeField] private string placeHeroMapClickMessageKey;

    private CitizenManager citizenManager;
    private BaseConstructor baseConstructor;
    private HeroRoster heroRoster;
    private PlacePalette placePalette;
    private BuildingPanel buildingPanel;
    private TutorialState state;

    private int currentIndex = -1;
    private int citizenSnapshot;
    private int usedCitizenSnapshot;
    private string lastShownMessageKey;
    private readonly HashSet<HeroRosterEntry> placedSnapshot = new();

    [Inject]
    private void Construct(CitizenManager citizenManager,
        BaseConstructor baseConstructor, HeroRoster heroRoster, PlacePalette placePalette,
        BuildingPanel buildingPanel, TutorialState state)
    {
        this.citizenManager = citizenManager;
        this.baseConstructor = baseConstructor;
        this.heroRoster = heroRoster;
        this.placePalette = placePalette;
        this.buildingPanel = buildingPanel;
        this.state = state;
    }

    private void Start()
    {
        if (state.Seen || steps.Length == 0)
        {
            overlay.Hide();
            enabled = false;
            return;
        }

        int startIndex = state.CurrentStepIndex;
        if (startIndex >= steps.Length)
        {
            Finish();
            return;
        }

        BeginStep(startIndex);
    }

    private void OnEnable()
    {
        TutorialInputGate.BlockEscapeClose = true;
        citizenManager.CitizenChanged += OnCitizenChanged;
        baseConstructor.Built += OnBuilt;
        heroRoster.Changed += OnHeroRosterChanged;
        buildingPanel.Upgraded += OnUpgraded;
        overlay.AcknowledgeClicked += OnAcknowledgeClicked;
    }

    private void OnDisable()
    {
        TutorialInputGate.BlockEscapeClose = false;
        citizenManager.CitizenChanged -= OnCitizenChanged;
        baseConstructor.Built -= OnBuilt;
        heroRoster.Changed -= OnHeroRosterChanged;
        buildingPanel.Upgraded -= OnUpgraded;
        overlay.AcknowledgeClicked -= OnAcknowledgeClicked;
    }

    // 현재 단계의 waypoints 중 저작한 순서로 가장 깊이 들어간 활성 상태를 스포트라이트하고,
    // 그 waypoint에 딸린 문구(없으면 단계 기본 문구)를 보여준다. 패널들이 SetActive로 토글되므로
    // 단계가 바뀌지 않아도 매 프레임 다시 계산해야 한다.
    private void Update()
    {
        // 로스터에서 영웅을 고르면(배치 대기 중) 다음 클릭은 UI가 아니라 3D 맵 타일이라 짚어줄
        // 사각형이 없다 - 이 순간만큼은 딤을 전부 끄고 맵을 자유롭게 클릭할 수 있게 한다.
        if (IsActive(TutorialStepId.PlaceHero) && placePalette.Mode == PlaceMode.Place)
        {
            ShowUnblockedMessage(placeHeroMapClickMessageKey);
            return;
        }

        var waypoint = ResolveWaypoint();
        overlay.SetSpotlight(waypoint?.target);
        RefreshMessage(waypoint);
    }

    // 스포트라이트로 짚어줄 UI가 없는 대기 구간(맵 클릭 대기 등)에 쓴다 - 딤을 전부 풀고 문구만 띄운다.
    private void ShowUnblockedMessage(string messageKey)
    {
        overlay.ShowUnblocked();
        if (lastShownMessageKey == messageKey) return;

        lastShownMessageKey = messageKey;
        overlay.SetMessage(messageKey);
    }

    // waypoints는 진입 버튼 -> 최종 액션 버튼 순으로 저작하지만, 뒤에서부터 훑어 "가장 깊이 들어간
    // 활성 상태"를 스포트라이트한다. 거점 화면을 여는 버튼처럼 앞쪽 waypoint는 패널이 열린 뒤에도
    // 계속 activeInHierarchy=true로 남아있는 경우가 많아서, 앞에서부터 훑으면 플레이어가 이미
    // 다음 화면으로 넘어갔어도 스포트라이트가 그 버튼에서 멈춰버린다.
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
        int next = currentIndex + 1;
        state.SaveProgress(next);

        if (next >= steps.Length) Finish();
        else BeginStep(next);
    }

    private void Finish()
    {
        overlay.Hide();
        state.MarkSeen();
        enabled = false;
    }

    // 테스트용 - 인스펙터에서 이 컴포넌트 헤더 우클릭 -> 실행하면 Play 모드 중에도 처음부터 다시 볼 수 있다.
    // UpgradeUI.OnDebugResetButton()과 같은 용도 - 필요하면 디버그 버튼의 OnClick에도 그대로 연결해서 쓸 수 있다.
    [ContextMenu("튜토리얼 초기화 후 재시작")]
    public void DebugRestart()
    {
        if (steps.Length == 0) return;

        state.Reset();
        currentIndex = -1;
        enabled = true;
        BeginStep(0);
    }

    private bool IsActive(TutorialStepId id) => currentIndex >= 0 && currentIndex < steps.Length && steps[currentIndex].id == id;

    private void OnAcknowledgeClicked()
    {
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
}
