using UnityEngine;
using VContainer;
using VContainer.Unity;

public class TestLifeTimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<BuffManager>(Lifetime.Singleton).As<ITickable>().AsSelf();

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
