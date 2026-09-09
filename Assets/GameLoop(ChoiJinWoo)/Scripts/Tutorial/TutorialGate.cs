using UnityEngine;
using VContainer;

/// <summary>
/// 튜토리얼이 도는 동안의 입력/HUD 차단만 담당한다.
/// 프레임마다 현재 단계에 맞춰 <see cref="TutorialInputGate"/> 플래그와
/// 관련 <see cref="CanvasGroup"/>의 레이캐스트를 켜고 끈다.
/// (BlockEscapeClose / BlockHotkeys / BlockSave / BlockHeroRetrieve 같은
///  "튜토리얼 실행 중" 플래그는 단계와 무관하므로 TutorialManager가 직접 든다.)
/// </summary>
public class TutorialGate
{
    private readonly UiManager uiManager;
    private readonly RegionOverviewPanel regionOverviewPanel;

    private CanvasGroup sceneHudGroup;
    private CanvasGroup uiManagerHudGroup;
    private bool hudBlocked;

    private CanvasGroup gameSpeedGroup;
    private bool gameSpeedBlocked;

    private CanvasGroup heroUpgradeGroup;
    private bool heroUpgradeBlocked;

    [Inject]
    public TutorialGate(UiManager uiManager, RegionOverviewPanel regionOverviewPanel)
    {
        this.uiManager = uiManager;
        this.regionOverviewPanel = regionOverviewPanel;
    }

    /// <summary>차단에 쓸 CanvasGroup들을 해석한다. Start에서 한 번 호출.</summary>
    public void Resolve(TutorialStepDefinition[] steps)
    {
        sceneHudGroup = ResolveHudGroup(regionOverviewPanel.transform);
        uiManagerHudGroup = ResolveHudGroup(uiManager.GameSpeedUiRect);

        gameSpeedGroup = GetOrAddCanvasGroup(uiManager.GameSpeedUiRect);
        heroUpgradeGroup = GetOrAddCanvasGroup(FindStepTarget(steps, TutorialStepId.HeroUpgradeMention));
    }

    /// <summary>
    /// TutorialManager.Update가 매 프레임 호출. 원본 Update의 게이팅 로직을 그대로 옮긴 것.
    /// </summary>
    public void UpdateGating(TutorialStepId activeStep, bool awaitingMapClick)
    {
        TutorialInputGate.BlockPanelOpen = awaitingMapClick;
        SetHudBlocked(awaitingMapClick);
        if (awaitingMapClick) return;

        bool heroUpgrade = activeStep == TutorialStepId.HeroUpgradeMention;
        TutorialInputGate.BlockHeroUpgradeOpen = heroUpgrade;
        SetBlocked(heroUpgradeGroup, ref heroUpgradeBlocked, heroUpgrade);

        TutorialInputGate.BlockHeroPlacementFromInventory = activeStep == TutorialStepId.HeroCombineMention;
        SetBlocked(gameSpeedGroup, ref gameSpeedBlocked, activeStep == TutorialStepId.GameSpeedMention);
        TutorialInputGate.BlockPlayerSkillCast = activeStep == TutorialStepId.PlayerSkillMention;
    }

    public void SetHudBlocked(bool blocked)
    {
        if (hudBlocked == blocked) return;
        hudBlocked = blocked;

        SetGroupBlocked(sceneHudGroup, blocked);
        SetGroupBlocked(uiManagerHudGroup, blocked);
    }

    /// <summary>단계 스코프의 모든 차단을 해제한다. FinishSequence / OnDisable에서 호출.</summary>
    public void ReleaseAll()
    {
        TutorialInputGate.BlockPanelOpen = false;
        TutorialInputGate.BlockHeroUpgradeOpen = false;
        TutorialInputGate.BlockHeroPlacementFromInventory = false;
        TutorialInputGate.BlockPlayerSkillCast = false;

        SetHudBlocked(false);
        SetBlocked(gameSpeedGroup, ref gameSpeedBlocked, false);
        SetBlocked(heroUpgradeGroup, ref heroUpgradeBlocked, false);
    }

    private void SetBlocked(CanvasGroup group, ref bool current, bool blocked)
    {
        if (current == blocked) return;
        current = blocked;
        SetGroupBlocked(group, blocked);
    }

    private static RectTransform FindStepTarget(TutorialStepDefinition[] steps, TutorialStepId id)
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

    private static void SetGroupBlocked(CanvasGroup group, bool blocked)
    {
        if (group == null) return;
        group.blocksRaycasts = !blocked;
    }
}
