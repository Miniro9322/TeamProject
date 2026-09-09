using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

/// <summary>
/// 튜토리얼 각 단계의 "게임 이벤트로 완료되는" 조건만 담당한다.
/// 게임플레이 이벤트 구독/스냅샷 비교를 여기서 처리하고,
/// 현재 단계가 완료되어야 할 때 <see cref="StepShouldComplete"/> 하나만 밖으로 던진다.
/// </summary>
public class TutorialTriggers
{
    public event Action<TutorialStepId> StepShouldComplete;

    private readonly CitizenManager citizenManager;
    private readonly BaseConstructor baseConstructor;
    private readonly HeroRoster heroRoster;
    private readonly BuildingPanel buildingPanel;
    private readonly GameManager gameManager;
    private readonly MapView mapView;
    private readonly BuildModePanel buildModePanel;

    private int citizenSnapshot;
    private int usedCitizenSnapshot;
    private readonly HashSet<HeroRosterEntry> placedSnapshot = new();

    private bool active;
    private TutorialStepId activeStep;

    [Inject]
    public TutorialTriggers(CitizenManager citizenManager, BaseConstructor baseConstructor,
        HeroRoster heroRoster, BuildingPanel buildingPanel, GameManager gameManager,
        MapView mapView, BuildModePanel buildModePanel)
    {
        this.citizenManager = citizenManager;
        this.baseConstructor = baseConstructor;
        this.heroRoster = heroRoster;
        this.buildingPanel = buildingPanel;
        this.gameManager = gameManager;
        this.mapView = mapView;
        this.buildModePanel = buildModePanel;
    }

    public void Enable()
    {
        citizenManager.CitizenChanged += OnCitizenChanged;
        baseConstructor.Built += OnBuilt;
        heroRoster.Changed += OnHeroRosterChanged;
        buildingPanel.Upgraded += OnUpgraded;
        gameManager.ChangeToNight += OnChangeToNight;
        mapView.Replaced += OnHeroReplaced;
    }

    public void Disable()
    {
        citizenManager.CitizenChanged -= OnCitizenChanged;
        baseConstructor.Built -= OnBuilt;
        heroRoster.Changed -= OnHeroRosterChanged;
        buildingPanel.Upgraded -= OnUpgraded;
        gameManager.ChangeToNight -= OnChangeToNight;
        mapView.Replaced -= OnHeroReplaced;
        active = false;
    }

    /// <summary>단계 진입 시점의 게임 상태를 스냅샷하고, 이 단계의 트리거를 켠다.</summary>
    public void BeginStep(TutorialStepId step)
    {
        active = true;
        activeStep = step;
        citizenSnapshot = citizenManager.CurrentCitizen;
        usedCitizenSnapshot = citizenManager.UsedCitizen;
        SnapshotPlacedHeroes();
    }

    /// <summary>시퀀스가 끝나 더 이상 어떤 단계도 트리거되면 안 될 때.</summary>
    public void Stop() => active = false;

    private void SnapshotPlacedHeroes()
    {
        placedSnapshot.Clear();
        foreach (var entry in heroRoster.Entries)
        {
            if (entry.State == HeroRosterState.Placed) placedSnapshot.Add(entry);
        }
    }

    private bool IsActive(TutorialStepId id) => active && activeStep == id;

    private void Fire() => StepShouldComplete?.Invoke(activeStep);

    private void OnBuilt(object built)
    {
        if (!IsActive(TutorialStepId.BuildHouse)) return;
        if (built is House) Fire();
    }

    private void OnCitizenChanged()
    {
        if (IsActive(TutorialStepId.RecruitCitizen))
        {
            if (citizenManager.CurrentCitizen > citizenSnapshot) Fire();
        }
        else if (IsActive(TutorialStepId.AssignWorker))
        {
            if (citizenManager.UsedCitizen > usedCitizenSnapshot) Fire();
        }
    }

    private void OnUpgraded()
    {
        if (!IsActive(TutorialStepId.BuildingUpgradeMention)) return;
        Fire();
    }

    private void OnHeroRosterChanged()
    {
        if (!IsActive(TutorialStepId.PlaceHero)) return;

        foreach (var entry in heroRoster.Entries)
        {
            if (entry.State == HeroRosterState.Placed && !placedSnapshot.Contains(entry))
            {
                Fire();
                return;
            }
        }
    }

    private void OnHeroReplaced(GameObject unit, OccupantKind kind)
    {
        if (!IsActive(TutorialStepId.RelocateHero)) return;
        if (kind != OccupantKind.MeleeHero && kind != OccupantKind.RangedHero) return;

        buildModePanel.CancelPlaceModeFromTutorial();
        Fire();
    }

    private void OnChangeToNight()
    {
        if (!IsActive(TutorialStepId.NightMention)) return;
        Fire();
    }
}
