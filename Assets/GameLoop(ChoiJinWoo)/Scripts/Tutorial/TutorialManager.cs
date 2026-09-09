using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

/// <summary>
/// 튜토리얼 시퀀스의 진행(어느 단계인지 / 다음으로 넘김)과 오버레이 표시만 담당한다.
/// 단계 완료 판정은 <see cref="TutorialTriggers"/>, 입력/HUD 차단은 <see cref="TutorialGate"/>,
/// 0일차 리셋은 <see cref="TutorialRollback"/>이 맡는다.
/// </summary>
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

    [Tooltip("0일차 리셋이 끝난 뒤(진짜 1일차 시작) 한 번 보여줄 완료 메시지 키.")]
    [SerializeField] private string completionMessageKey;

    private TutorialState state;
    private TutorialTriggers triggers;
    private TutorialGate gate;
    private TutorialRollback rollback;
    private PlacePalette placePalette;
    private EnviromentManager enviromentManager;
    private UiManager uiManager;

    private int currentIndex = -1;
    private string lastShownMessageKey;

    private TutorialWaypoint currentWaypoint;
    private bool waypointDirty = true;

    private bool sequenceFinished;
    private bool dayZeroResetDone;
    private bool showingCompletionMessage;

    [Inject]
    private void Construct(TutorialState state, TutorialTriggers triggers, TutorialGate gate,
        TutorialRollback rollback, PlacePalette placePalette, EnviromentManager enviromentManager,
        UiManager uiManager)
    {
        this.state = state;
        this.triggers = triggers;
        this.gate = gate;
        this.rollback = rollback;
        this.placePalette = placePalette;
        this.enviromentManager = enviromentManager;
        this.uiManager = uiManager;
    }

    private void Start()
    {
        WireRuntimeWaypoints();
        gate.Resolve(steps);

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

    private void OnEnable()
    {
        TutorialInputGate.BlockEscapeClose = true;
        TutorialInputGate.BlockHotkeys = true;
        TutorialInputGate.BlockSave = true;
        TutorialInputGate.BlockHeroRetrieve = true;
        TutorialActivityWatcher.Changed += OnWaypointActivityChanged;
        triggers.StepShouldComplete += OnStepShouldComplete;
        triggers.Enable();
        overlay.AcknowledgeClicked += OnAcknowledgeClicked;
        enviromentManager.OnDay += OnDayTransitionComplete;
    }

    private void OnDisable()
    {
        TutorialInputGate.BlockEscapeClose = false;
        TutorialInputGate.BlockHotkeys = false;
        TutorialInputGate.BlockSave = false;
        TutorialInputGate.BlockHeroRetrieve = false;
        TutorialActivityWatcher.Changed -= OnWaypointActivityChanged;
        triggers.StepShouldComplete -= OnStepShouldComplete;
        triggers.Disable();
        overlay.AcknowledgeClicked -= OnAcknowledgeClicked;
        enviromentManager.OnDay -= OnDayTransitionComplete;
        gate.ReleaseAll();
    }

    private void Update()
    {
        if (sequenceFinished) return;

        bool awaitingPlaceClick = IsActive(TutorialStepId.PlaceHero) && placePalette.Mode == PlaceMode.Place;
        bool awaitingRelocateClick = IsActive(TutorialStepId.RelocateHero) && placePalette.Mode == PlaceMode.Replace;
        bool awaitingMapClick = awaitingPlaceClick || awaitingRelocateClick;

        gate.UpdateGating(steps[currentIndex].id, awaitingMapClick);

        if (awaitingMapClick)
        {
            ShowUnblockedMessage(awaitingRelocateClick ? relocateHeroMapClickMessageKey : placeHeroMapClickMessageKey);
            return;
        }

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
            gate.SetHudBlocked(true);
            lastShownMessageKey = null;
            return;
        }

        gate.SetHudBlocked(false);
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

            GameObject gateObject = waypoint.activationCheck != null ? waypoint.activationCheck : waypoint.target.gameObject;
            if (gateObject.activeInHierarchy) return waypoint;
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

        triggers.BeginStep(step.id);

        lastShownMessageKey = null;
        currentWaypoint = ResolveWaypoint();
        waypointDirty = false;
        overlay.Show(step.completesOnAcknowledge);
        RefreshMessage(currentWaypoint);
    }

    private void OnStepShouldComplete(TutorialStepId id)
    {
        if (sequenceFinished) return;
        if (currentIndex < 0 || currentIndex >= steps.Length) return;
        if (steps[currentIndex].id != id) return;
        CompleteStep();
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
        gate.ReleaseAll();
        triggers.Stop();
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

        rollback.PrepareReplay();
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

    private void OnDayTransitionComplete()
    {
        if (dayZeroResetDone) return;
        dayZeroResetDone = true;
        RunRollback().Forget();
    }

    private async UniTaskVoid RunRollback()
    {
        await rollback.RunAsync();
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
}
