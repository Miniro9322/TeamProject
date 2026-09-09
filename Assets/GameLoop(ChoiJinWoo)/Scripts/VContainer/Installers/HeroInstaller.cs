using System.Collections.Generic;
using VContainer;
using VContainer.Unity;

/// <summary>
/// 영웅 관련 등록.
/// 업그레이드 설정(ScriptableObject), 로스터/업그레이드 상태, 스탯 매니저,
/// 영웅 관련 씬 UI 컴포넌트를 담당한다.
/// </summary>
public sealed class HeroInstaller : IInstaller
{
    private readonly HeroUpgradeConfig heroUpgradeConfig;
    private readonly HeroClassUpgradeConfig heroClassUpgradeConfig;
    private readonly List<BaseUpgradeData> heroStatUpgrades;

    public HeroInstaller(
        HeroUpgradeConfig heroUpgradeConfig,
        HeroClassUpgradeConfig heroClassUpgradeConfig,
        List<BaseUpgradeData> heroStatUpgrades)
    {
        this.heroUpgradeConfig = heroUpgradeConfig;
        this.heroClassUpgradeConfig = heroClassUpgradeConfig;
        this.heroStatUpgrades = heroStatUpgrades;
    }

    public void Install(IContainerBuilder builder)
    {
        builder.RegisterInstance(heroUpgradeConfig);
        builder.RegisterInstance(heroClassUpgradeConfig);
        builder.RegisterInstance(heroStatUpgrades);

        builder.Register<HeroRoster>(Lifetime.Singleton).AsSelf();

        // 무엇이 먼저 Resolve 하든(UI 패널이 시작 콜백에서 ResourcesManager→UpgradeState 를 끌어옴)
        // 항상 복원된 상태가 주입되도록, 등록 전에 미리 만들어 로드해 둔다.
        var upgradeState = new UpgradeState();
        _ = new UpgradeStatePlayerPrefsStore(upgradeState); // 로드 + Changed 구독(자동 저장)
        builder.RegisterInstance(upgradeState);

        builder.Register<HeroTierUpgradeState>(Lifetime.Singleton);
        builder.Register<HeroClassUpgradeState>(Lifetime.Singleton);
        builder.Register<HeroStatManager>(Lifetime.Singleton).AsSelf();

        builder.RegisterComponentInHierarchy<HeroRegistry>();
        builder.RegisterComponentInHierarchy<HeroTierUpgradeMenu>();
        builder.RegisterComponentInHierarchy<HeroClassUpgradeMenu>();
        builder.RegisterComponentInHierarchy<HeroSetPanel>();

        // HeroStatManager는 생성자 의존성으로 끌려오지 않으므로 강제 인스턴스화한다.
        builder.RegisterBuildCallback(resolver => resolver.Resolve<HeroStatManager>());
    }
}
