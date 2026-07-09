using UnityEngine;
using VContainer;
using VContainer.Unity;

public class GameLifeTimeScope : LifetimeScope
{
    [SerializeField] private ResourcesManager prefab;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponentInNewPrefab(prefab, Lifetime.Singleton).AsSelf();
        builder.RegisterComponentInHierarchy<ResourceTest>();
    }
}
