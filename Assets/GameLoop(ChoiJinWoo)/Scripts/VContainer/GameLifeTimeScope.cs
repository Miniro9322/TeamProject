using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class GameLifeTimeScope : LifetimeScope
{
    [SerializeField] private ResourcesManager resourcesManagerPrefab;
    [SerializeField] private CitizenManager citizenManagerPrefab;
    [SerializeField] private CitizenWanderManager citizenWanderManagerPrefab;
    [SerializeField] private UiManager UiManagerPrefab;
    [SerializeField] private EnviromentManager EnviromentManagerPrefab;
    [SerializeField] private GameManager GameManagerPrefab;
    [SerializeField] private PlayerManaManager playerManaManagerPrefab;
    [SerializeField] private FacilityManager FacilityManager;
    [SerializeField] private ProductionEconomyConfig economyConfig;
    [SerializeField] private ResourceIconSet resourceIconSet;
    [SerializeField] private HeroUpgradeConfig heroUpgradeConfig;
    [SerializeField] private HeroClassUpgradeConfig heroClassUpgradeConfig;
    [SerializeField] private List<BaseUpgradeData> heroStatUpgrades;
    [SerializeField] private Light sunLight;
    [SerializeField] private Transform citizenHubPoint;
    [SerializeField] private Transform[] citizenHomePoints;

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
        builder.RegisterComponentInNewPrefab(citizenWanderManagerPrefab, Lifetime.Singleton).AsSelf();
        builder.RegisterComponentInNewPrefab(UiManagerPrefab, Lifetime.Singleton).AsSelf();
        builder.RegisterComponentInNewPrefab(EnviromentManagerPrefab, Lifetime.Singleton).AsSelf();
        builder.RegisterComponentInNewPrefab(GameManagerPrefab, Lifetime.Singleton).AsSelf();
        builder.RegisterComponentInNewPrefab(playerManaManagerPrefab, Lifetime.Singleton).AsSelf();
        builder.RegisterInstance(economyConfig);
        builder.RegisterInstance(resourceIconSet);
        builder.RegisterInstance(heroUpgradeConfig);
        builder.RegisterInstance(heroClassUpgradeConfig);
        builder.RegisterInstance(heroStatUpgrades);
        builder.RegisterComponentOnNewGameObject<PoolManager>(Lifetime.Singleton).AsSelf();
        builder.Register<FacilityManager>(Lifetime.Singleton).AsSelf();
        builder.Register<BaseConstructor>(Lifetime.Singleton).AsSelf();
        builder.Register<BuffManager>(Lifetime.Singleton).As<ITickable>().AsSelf();
        builder.Register<HeroRoster>(Lifetime.Singleton).AsSelf();
        builder.Register<UpgradeState>(Lifetime.Singleton);
        builder.Register<HeroTierUpgradeState>(Lifetime.Singleton);
        builder.Register<HeroClassUpgradeState>(Lifetime.Singleton);
        builder.Register<HeroStatManager>(Lifetime.Singleton).AsSelf();

        builder.RegisterBuildCallback(resolver =>
        {
            resolver.Resolve<HeroStatManager>();
        });
        builder.Register<UiPanelStack>(Lifetime.Singleton).AsSelf();
        builder.Register<TutorialState>(Lifetime.Singleton).AsSelf();

        builder.Register<GimmickTileData>(Lifetime.Singleton).AsSelf();

        builder.Register<SaveCheck>(Lifetime.Singleton).AsSelf();
        builder.Register<SaveIO>(Lifetime.Singleton).AsSelf();
        builder.Register<SaveSlot>(Lifetime.Singleton).AsSelf();
        builder.Register<SaveCapture>(Lifetime.Singleton).AsSelf();
        builder.Register<SaveRestore>(Lifetime.Singleton).AsSelf();
        builder.Register<SaveTimeData>(Lifetime.Singleton).As<ITickable>().AsSelf();
        builder.Register<SaveManager>(Lifetime.Singleton).As<IStartable>().AsSelf();
        builder.Register<LoadManager>(Lifetime.Singleton).As<IStartable>().AsSelf();
        builder.Register<SaveExitHook>(Lifetime.Singleton).As<IStartable>().AsSelf();
        builder.Register<SaveChangeTracker>(Lifetime.Singleton).As<IStartable>().As<ITickable>().AsSelf();
        builder.Register<SaveKey>(Lifetime.Singleton).AsSelf();
        builder.Register<SaveCipher>(Lifetime.Singleton).AsSelf();
        builder.Register<DayNightData>(Lifetime.Singleton).AsSelf();

        if (sunLight != null)
        {
            builder.RegisterBuildCallback(resolver =>
            {
                var toggle = resolver.Resolve<EnviromentManager>();
                toggle.SetSunLight(sunLight);
            });
        }

        builder.RegisterBuildCallback(resolver =>
        {
            var wanderManager = resolver.Resolve<CitizenWanderManager>();
            if (citizenHubPoint != null)
                wanderManager.SetHubPoint(citizenHubPoint);
            wanderManager.SetHomePoints(citizenHomePoints);
        });

        builder.RegisterComponentInHierarchy<TopBar>();
        builder.RegisterComponentInHierarchy<DayNightButton>();
        builder.RegisterComponentInHierarchy<MenuUI>();
        builder.RegisterComponentInHierarchy<PlayerSkillPanel>();
        builder.RegisterComponentInHierarchy<MapGame>();
        builder.RegisterComponentInHierarchy<MapRegistry>();
        builder.RegisterComponentInHierarchy<MapAssemble>();
        builder.RegisterComponentInHierarchy<HeroRegistry>();
        builder.RegisterComponentInHierarchy<AddCitizen>();
        builder.RegisterComponentInHierarchy<SpawnerManager>().AsSelf();
        builder.RegisterComponentInHierarchy<FacilityBuildChoicePanel>();
        builder.RegisterComponentInHierarchy<CenterHubPanel>();
        builder.RegisterComponentInHierarchy<RegionOverviewPanel>();
        builder.RegisterComponentInHierarchy<RegionDetailPanel>();
        builder.RegisterComponentInHierarchy<BuildingPanel>();
        builder.RegisterComponentInHierarchy<HeroTierUpgradeMenu>();
        builder.RegisterComponentInHierarchy<HeroClassUpgradeMenu>();
        builder.RegisterComponentInHierarchy<HeroSetPanel>();
        builder.RegisterComponentInHierarchy<BuildModePanel>();
        builder.RegisterComponentInHierarchy<TutorialOverlayUI>();
        builder.RegisterComponentInHierarchy<TutorialManager>();
        builder.RegisterComponentInHierarchy<PlacePalette>();
        builder.RegisterComponentInHierarchy<MapView>();

        builder.RegisterBuildCallback(resolver =>
        {
            resolver.Resolve<PoolManager>();
        });

        builder.RegisterComponentInHierarchy<WaveSpawner>();

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
