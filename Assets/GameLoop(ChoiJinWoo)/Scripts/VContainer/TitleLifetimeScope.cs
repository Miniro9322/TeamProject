using VContainer;
using VContainer.Unity;

/// <summary>
/// 타이틀 씬의 루트 LifetimeScope.
/// 타이틀에는 DI 소비자가 UpgradeUI(→ UpgradeState) 하나뿐이라 등록도 그만큼만 한다.
/// (기존엔 타이틀 씬에 스코프가 없어서 UpgradeUI 가 new UpgradeState() 로 직접 만들었다.)
///
/// UpgradeState 는 MainScene 의 GameLifeTimeScope 에도 별도 인스턴스로 등록돼 있고,
/// 두 씬의 인스턴스는 UpgradeStatePlayerPrefsStore(PlayerPrefs)를 통해 동기화된다.
/// 씬 간 단일 인스턴스 공유는 세이브 시스템 정리와 함께 다뤄야 하는 별개 사안.
/// </summary>
public class TitleLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // 무엇이 먼저 Resolve 하든 항상 복원된 상태가 주입되도록, 등록 전에 미리 만들어 로드해 둔다.
        var upgradeState = new UpgradeState();
        _ = new UpgradeStatePlayerPrefsStore(upgradeState); // 로드 + Changed 구독(자동 저장)
        builder.RegisterInstance(upgradeState);

        // 씬에 배치된 UpgradeUI 를 찾아 주입한다. 빌드 콜백에서 강제로 Resolve 해 시작 시점에 주입되게 한다.
        builder.RegisterComponentInHierarchy<UpgradeUI>();
        builder.RegisterBuildCallback(resolver => resolver.Resolve<UpgradeUI>());
    }
}
