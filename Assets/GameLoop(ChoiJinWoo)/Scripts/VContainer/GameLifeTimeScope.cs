using UnityEngine;
using VContainer;
using VContainer.Unity;

public class GameLifeTimeScope : LifetimeScope
{
    [SerializeField] private ResourcesManager resourcesManagerPrefab;
    [SerializeField] private CitizenManager citizenManagerPrefab;
    [SerializeField] private UiManager UiManagerPrefab;
    [SerializeField] private EnviromentManager EnviromentManagerPrefab;
    [SerializeField] private GameManager GameManagerPrefab;
    [SerializeField] private FacilityManager FacilityManager;
    [SerializeField] private ProductionEconomyConfig economyConfig;
    [SerializeField] private Light sunLight;

    protected override void Configure(IContainerBuilder builder)
    {

        builder.RegisterComponentInHierarchy<ExpandEvent>().AsSelf();

        builder.RegisterBuildCallback(resolver =>
        {
            var ui = resolver.Resolve<UiManager>();
            resolver.InjectGameObject(ui.gameObject);
        });
        
        builder.RegisterComponentInNewPrefab(resourcesManagerPrefab, Lifetime.Singleton).AsSelf();
        builder.RegisterComponentInNewPrefab(citizenManagerPrefab, Lifetime.Singleton).AsSelf();
        builder.RegisterComponentInNewPrefab(UiManagerPrefab, Lifetime.Singleton).AsSelf();
        builder.RegisterComponentInNewPrefab(EnviromentManagerPrefab, Lifetime.Singleton).AsSelf();
        builder.RegisterComponentInNewPrefab(GameManagerPrefab, Lifetime.Singleton).AsSelf();
        builder.RegisterInstance(economyConfig);
        builder.RegisterComponentOnNewGameObject<PoolManager>(Lifetime.Singleton).AsSelf();
        builder.Register<FacilityManager>(Lifetime.Singleton).AsSelf();
        builder.Register<BaseConstructor>(Lifetime.Singleton).AsSelf();
        builder.Register<BuffManager>(Lifetime.Singleton).As<ITickable>().AsSelf();
        builder.Register<HeroRoster>(Lifetime.Singleton).AsSelf();
        builder.Register<UpgradeState>(Lifetime.Singleton);
        builder.Register<UiPanelStack>(Lifetime.Singleton).AsSelf();

        if (sunLight != null)
        {
            builder.RegisterBuildCallback(resolver =>
            {
                var toggle = resolver.Resolve<EnviromentManager>();
                toggle.SetSunLight(sunLight);
            });
        }

        builder.RegisterComponentInHierarchy<TopBar>();
        builder.RegisterComponentInHierarchy<DayNightButton>();
        builder.RegisterComponentInHierarchy<MapGame>();
        builder.RegisterComponentInHierarchy<AddCitizen>();
        builder.RegisterComponentInHierarchy<SpawnerManager>().AsSelf();
        builder.RegisterComponentInHierarchy<FacilityBuildChoicePanel>();
        builder.RegisterComponentInHierarchy<CenterHubPanel>();
        builder.RegisterComponentInHierarchy<RegionOverviewPanel>();
        builder.RegisterComponentInHierarchy<RegionDetailPanel>();
        builder.RegisterComponentInHierarchy<BuildingPanel>();

        // PoolManager는 RegisterComponentOnNewGameObject라 아무도 Resolve하지 않으면 실제로 생성되지 않는다(lazy).
        // 여기서 강제로 한 번 Resolve해 _resolver가 붙은 상태로 즉시 만들어지게 한다.
        // (안 하면 스폰 경로가 전부 PoolManager.Instance의 미주입 폴백 인스턴스를 타게 됨)
        builder.RegisterBuildCallback(resolver =>
        {
            resolver.Resolve<PoolManager>();
        });

        // EnemyBase.Construct(PoolManager, WaveSpawner, GameManager)가 WaveSpawner 타입을 요구하므로
        // 컨테이너에 타입 등록 자체가 있어야 한다(InjectGameObject는 등록을 만들어주지 않음).
        // 여기서 잡히는 인스턴스는 스폰 직후 WaveSpawner.SpawnWaveRout의 enemy.SetOwner(this)로
        // 곧바로 실제 소유 스포너로 덮어써지므로, 어떤 WaveSpawner가 잡히든 무방하다.
        builder.RegisterComponentInHierarchy<WaveSpawner>();

        // WaveSpawner는 SpawnerManager.spawners에 인스펙터로만 연결돼 있어 컨테이너가 자동으로 찾지 못한다.
        // 씬의 모든 WaveSpawner를 찾아 직접 주입 → 각자의 Construct(PoolManager)가 실제로 호출되게 한다.
        builder.RegisterBuildCallback(resolver =>
        {
            var waveSpawners = FindObjectsByType<WaveSpawner>(FindObjectsSortMode.None);
            foreach (var ws in waveSpawners)
            {
                resolver.InjectGameObject(ws.gameObject);
            }
        });
    }
}
