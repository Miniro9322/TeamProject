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
    [SerializeField] private BuildingPrefabRegistry buildingPrefabRegistry;
    [SerializeField] private Canvas UiManagerParent;
    [SerializeField] private Light sunLight;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponentInNewPrefab(resourcesManagerPrefab, Lifetime.Singleton).AsSelf();
        builder.RegisterComponentInNewPrefab(citizenManagerPrefab, Lifetime.Singleton).AsSelf();
        if(UiManagerParent != null)
            builder.RegisterComponentInNewPrefab(UiManagerPrefab, Lifetime.Singleton).UnderTransform(UiManagerParent.transform).AsSelf();
        builder.RegisterComponentInNewPrefab(EnviromentManagerPrefab, Lifetime.Singleton).AsSelf();
        builder.RegisterComponentInNewPrefab(GameManagerPrefab, Lifetime.Singleton).AsSelf();
        builder.RegisterInstance(buildingPrefabRegistry);
        builder.RegisterComponentOnNewGameObject<PoolManager>(Lifetime.Singleton).AsSelf();
        builder.Register<BuildingPool>(Lifetime.Singleton);
        builder.Register<FacilityManager>(Lifetime.Singleton).AsSelf();
        builder.Register<BuffManager>(Lifetime.Singleton).As<ITickable>().AsSelf();

        if (sunLight != null)
        {
            builder.RegisterBuildCallback(resolver =>
            {
                var toggle = resolver.Resolve<EnviromentManager>();
                toggle.SetSunLight(sunLight);
            });
        }

        //builder.RegisterComponentInHierarchy<ResourceTest>();
        builder.RegisterComponentInHierarchy<TopBar>();
        builder.RegisterComponentInHierarchy<DayNightButton>();
        builder.RegisterComponentInHierarchy<MapGame>();
        builder.RegisterComponentInHierarchy<WaveSpawner>().AsSelf();
        builder.RegisterBuildCallback(resolver =>
        {
            var testObjects = FindObjectsByType<StatContainerTest>(FindObjectsSortMode.None);
            foreach (var obj in testObjects)
            {
                resolver.InjectGameObject(obj.gameObject);
            }
        });
    }
}
