using UnityEngine;
using VContainer;
using VContainer.Unity;

public class GameLifeTimeScope : LifetimeScope
{
    [SerializeField] private ResourcesManager resourcesManagerPrefab;
    [SerializeField] private CitizenManager citizenManagerPrefab;
    [SerializeField] private UiManager UiManagerPrefab;
    [SerializeField] private Canvas UiManagerParent;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponentInNewPrefab(resourcesManagerPrefab, Lifetime.Singleton).AsSelf();
        builder.RegisterComponentInNewPrefab(citizenManagerPrefab, Lifetime.Singleton).AsSelf();
        builder.RegisterComponentInNewPrefab(UiManagerPrefab, Lifetime.Singleton).UnderTransform(UiManagerParent.transform).AsSelf();
        builder.RegisterComponentInHierarchy<ResourceTest>();
        builder.RegisterComponentInHierarchy<TopBar>();
    }
}
