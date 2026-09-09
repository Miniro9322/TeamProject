using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// 환경(낮/밤) 관련 등록.
/// 환경 매니저 프리팹과, 씬에 배치된 태양광 Light 를 매니저에 주입하는 콜백을 담당한다.
/// </summary>
public sealed class EnvironmentInstaller : IInstaller
{
    private readonly EnviromentManager enviromentManagerPrefab;
    private readonly Light sunLight;

    public EnvironmentInstaller(EnviromentManager enviromentManagerPrefab, Light sunLight)
    {
        this.enviromentManagerPrefab = enviromentManagerPrefab;
        this.sunLight = sunLight;
    }

    public void Install(IContainerBuilder builder)
    {
        builder.RegisterComponentInNewPrefab(enviromentManagerPrefab, Lifetime.Singleton).AsSelf();

        if (sunLight != null)
        {
            builder.RegisterBuildCallback(resolver =>
            {
                var toggle = resolver.Resolve<EnviromentManager>();
                toggle.SetSunLight(sunLight);
            });
        }
    }
}
