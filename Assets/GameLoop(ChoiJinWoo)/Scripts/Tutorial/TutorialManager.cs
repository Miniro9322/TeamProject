using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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

    [Tooltip("0일차 밤이 시작되면(밤 전환이 다 끝난 시점) 짚어줄 플레이어 스킬 UI.")]
    [SerializeField] private RectTransform playerSkillTarget;

    [Tooltip("플레이어 스킬을 설명하는 문구. 이 순간엔 Time.timeScale을 0으로 멈춰둔다.")]
    [SerializeField] private string playerSkillMessageKey;

    private CitizenManager citizenManager;
    private BaseConstructor baseConstructor;
    private HeroRoster heroRoster;
    private PlacePalette placePalette;
    private BuildingPanel buildingPanel;
    private GameManager gameManager;
    private ResourcesManager resourcesManager;
    private RegionOverviewPanel regionOverviewPanel;
    private MapGame mapGame;
    private EnviromentManager enviromentManager;
    private TutorialState state;

    private int currentIndex = -1;
    private int citizenSnapshot;
    private int usedCitizenSnapshot;
    private string lastShownMessageKey;
    private readonly HashSet<HeroRosterEntry> placedSnapshot = new();

    // 스텝 시퀀스 자체가 끝났는지, 0일차 리셋까지 끝났는지, 밤 스킬 설명까지 끝났는지 - 셋 다 true여야
    // 컴포넌트를 완전히 끈다(그전에 끄면 ChangeToDay/OnNight를 못 받아서 0일차 정리를 놓친다).
    private bool sequenceFinished;
    private bool dayZeroResetDone;
    private bool nightExplanationDone;
    private bool nightExplanationActive;

    [Inject]
    private void Construct(CitizenManager citizenManager,
        BaseConstructor baseConstructor, HeroRoster heroRoster, PlacePalette placePalette,
        BuildingPanel buildingPanel, GameManager gameManager, ResourcesManager resourcesManager,
        RegionOverviewPanel regionOverviewPanel, MapGame mapGame, EnviromentManager enviromentManager,
        TutorialState state)
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
        this.enviromentManager = enviromentManager;
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
        gameManager.ChangeToDay += OnChangeToDay;
        gameManager.ChangeToNight += OnChangeToNight;
        enviromentManager.OnNight += OnNightTransitionComplete;
    }

    private void OnDisable()
    {
        TutorialInputGate.BlockEscapeClose = false;
        citizenManager.CitizenChanged -= OnCitizenChanged;
        baseConstructor.Built -= OnBuilt;
        heroRoster.Changed -= OnHeroRosterChanged;
        buildingPanel.Upgraded -= OnUpgraded;
        overlay.AcknowledgeClicked -= OnAcknowledgeClicked;
        gameManager.ChangeToDay -= OnChangeToDay;
        gameManager.ChangeToNight -= OnChangeToNight;
        enviromentManager.OnNight -= OnNightTransitionComplete;
    }

    // 현재 단계의 waypoints 중 저작한 순서로 가장 깊이 들어간 활성 상태를 스포트라이트하고,
    // 그 waypoint에 딸린 문구(없으면 단계 기본 문구)를 보여준다. 패널들이 SetActive로 토글되므로
    // 단계가 바뀌지 않아도 매 프레임 다시 계산해야 한다.
    private void Update()
    {
        if (sequenceFinished) return; // 스텝은 끝났고 나머지(0일차 리셋, 밤 스킬 설명)는 이벤트로 처리 - 더 그릴 것 없음

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
        // 밤 버튼은 눌렸지만 전환 애니메이션이 끝나 스킬 설명이 뜨기 전까지는 짚어줄 UI가 없다.
        // 그렇다고 여기서 강제를 풀면 그 몇 초 사이에 자유롭게 다른 조작을 할 수 있게 되므로,
        // 스킬 설명이 뜨고 확인할 때까지(ResumeFromNightExplanation) 화면 전체를 막아둔 채로 대기한다.
        overlay.SetSpotlight(null);
        sequenceFinished = true;
        TryFullyDisable();
    }

    // 스텝 시퀀스, 0일차 리셋, 밤 스킬 설명 - 셋 다 끝나야 완전히 끈다. 하나라도 안 끝났으면 그대로
    // 살려둬서 나중에 올 ChangeToDay/OnNight를 계속 받을 수 있게 한다.
    // state.MarkSeen()도 여기서 한다 - 밤 버튼을 누른 시점이 아니라 진짜 1일차가 시작되는 시점에야
    // "튜토리얼을 다 봤다"고 기록해야, 0일차 밤 도중 앱이 꺼져도 다시 켰을 때 0일차를 다시 겪는다.
    private void TryFullyDisable()
    {
        if (!(sequenceFinished && dayZeroResetDone && nightExplanationDone)) return;
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
        sequenceFinished = false;
        dayZeroResetDone = false;
        nightExplanationDone = false;
        nightExplanationActive = false;
        enabled = true;
        BeginStep(0);
    }

    private bool IsActive(TutorialStepId id) => currentIndex >= 0 && currentIndex < steps.Length && steps[currentIndex].id == id;

    private void OnAcknowledgeClicked()
    {
        if (nightExplanationActive) { ResumeFromNightExplanation(); return; }
        if (sequenceFinished) return; // 방어적 - 시퀀스 끝난 뒤엔 이 경로로 안 와야 정상
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

    // NightMention 단계가 활성일 때 낮/밤 버튼을 실제로 눌러야(ChangeToNight 발생) 완료된다 -
    // 다른 강제 단계들(OnBuilt, OnCitizenChanged 등)과 같은 패턴.
    private void OnChangeToNight()
    {
        if (!IsActive(TutorialStepId.NightMention)) return;
        CompleteStep();
    }

    // 밤 전환 애니메이션이 다 끝나 적이 스폰될 수 있게 된 시점. 시간을 멈추고 플레이어 스킬을 한 번
    // 설명한다 - 0일차에서 딱 한 번만.
    private void OnNightTransitionComplete()
    {
        if (nightExplanationDone) return;
        nightExplanationDone = true;

        Time.timeScale = 0f;
        nightExplanationActive = true;
        overlay.Show(true);
        overlay.SetSpotlight(playerSkillTarget);
        overlay.SetMessage(playerSkillMessageKey);
    }

    private void ResumeFromNightExplanation()
    {
        nightExplanationActive = false;
        overlay.Hide();
        TutorialInputGate.BlockEscapeClose = false; // 이제 정말로 강제 진행이 끝났다 - ESC 막기도 풀어준다
        Time.timeScale = 1f;
        TryFullyDisable();
    }

    // 0일차 밤이 끝나고 진짜 1일차가 시작되는 첫 ChangeToDay - 0일차 동안 쌓은 자원/시민/건물/영웅을
    // 전부 초기 상태로 되돌린다. 그 다음부터 오는 진짜 일차 전환들은 dayZeroResetDone 가드로 무시한다.
    private void OnChangeToDay()
    {
        if (dayZeroResetDone) return;
        dayZeroResetDone = true;
        DeferredReset().Forget();
    }

    private async UniTaskVoid DeferredReset()
    {
        // ChangeToDay 호출 스택을 완전히 빠져나온 뒤 정리한다 - 같은 호출 스택 안에서 영웅을 즉시
        // 풀로 돌려버리면, 씬의 각 배치된 영웅 인스턴스에 걸려있는 다른 ChangeToDay 구독자
        // (Hero.cs의 Resurrection/HealFull/ResetSkillCooldown)가 뒤이어 접근하다 예외가 날 위험이 있다.
        await UniTask.Yield();

        ResetHeroes();
        ResetBuildings();
        resourcesManager.Reset();
        citizenManager.Reset();
        gameManager.ResetHpToFull();

        TryFullyDisable();
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
