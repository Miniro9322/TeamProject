using VContainer;
using VContainer.Unity;

/// <summary>
/// UI 계층 등록.
/// UiManager 프리팹, 패널 스택, 씬에 존재하는 각종 패널/HUD 컴포넌트,
/// 그리고 UiManager 프리팹 하위 전체 주입 콜백을 담당한다.
/// </summary>
public sealed class UiInstaller : IInstaller
{
    private readonly UiManager uiManagerPrefab;

    public UiInstaller(UiManager uiManagerPrefab)
    {
        this.uiManagerPrefab = uiManagerPrefab;
    }

    public void Install(IContainerBuilder builder)
    {
        builder.RegisterComponentInNewPrefab(uiManagerPrefab, Lifetime.Singleton).AsSelf();
        builder.Register<UiPanelStack>(Lifetime.Singleton).AsSelf();

        builder.RegisterComponentInHierarchy<TopBar>();
        builder.RegisterComponentInHierarchy<DayNightButton>();
        builder.RegisterComponentInHierarchy<MenuUI>();
        builder.RegisterComponentInHierarchy<PlayerSkillPanel>();
        builder.RegisterComponentInHierarchy<FacilityBuildChoicePanel>();
        builder.RegisterComponentInHierarchy<CenterHubPanel>();
        builder.RegisterComponentInHierarchy<RegionOverviewPanel>();
        builder.RegisterComponentInHierarchy<RegionDetailPanel>();
        builder.RegisterComponentInHierarchy<BuildingPanel>();
        builder.RegisterComponentInHierarchy<BuildModePanel>();

        // 정적 싱글턴 대신 컨테이너로 관리하는 피드백/툴팁 UI (UiManager 프리팹 하위에 존재).
        builder.RegisterComponentInHierarchy<TooltipUi>();
        builder.RegisterComponentInHierarchy<CenterFeedbackUi>();

        // UiManager 프리팹은 RegisterComponentInNewPrefab이 루트 컴포넌트만 주입하므로,
        // 하위 패널들까지 주입되도록 트리 전체를 한 번 더 돈다.
        builder.RegisterBuildCallback(resolver =>
        {
            var ui = resolver.Resolve<UiManager>();
            resolver.InjectGameObject(ui.gameObject);
        });
    }
}
