using VContainer;
using VContainer.Unity;

/// <summary>
/// 생산/건설 시설 관련 등록.
/// 자원 매니저 프리팹, 경제 설정, 자원 아이콘 세트, 시설 매니저와 건설 로직을 담당한다.
/// </summary>
public sealed class FacilityInstaller : IInstaller
{
    private readonly ResourcesManager resourcesManagerPrefab;
    private readonly ProductionEconomyConfig economyConfig;
    private readonly ResourceIconSet resourceIconSet;

    public FacilityInstaller(
        ResourcesManager resourcesManagerPrefab,
        ProductionEconomyConfig economyConfig,
        ResourceIconSet resourceIconSet)
    {
        this.resourcesManagerPrefab = resourcesManagerPrefab;
        this.economyConfig = economyConfig;
        this.resourceIconSet = resourceIconSet;
    }

    public void Install(IContainerBuilder builder)
    {
        builder.RegisterComponentInNewPrefab(resourcesManagerPrefab, Lifetime.Singleton).AsSelf();
        builder.RegisterInstance(economyConfig);
        builder.RegisterInstance(resourceIconSet);
        builder.Register<FacilityManager>(Lifetime.Singleton).AsSelf();
        builder.Register<BaseConstructor>(Lifetime.Singleton).AsSelf();
    }
}
