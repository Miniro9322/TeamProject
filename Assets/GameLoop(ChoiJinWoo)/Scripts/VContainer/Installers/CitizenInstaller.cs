using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// 주민 관련 등록.
/// 주민 매니저/배회 매니저 프리팹, 주민 추가 UI, 그리고 씬에 배치된
/// 허브/집 위치 Transform 을 배회 매니저에 주입하는 콜백을 담당한다.
/// </summary>
public sealed class CitizenInstaller : IInstaller
{
    private readonly CitizenManager citizenManagerPrefab;
    private readonly CitizenWanderManager citizenWanderManagerPrefab;
    private readonly Transform hubPoint;
    private readonly Transform[] homePoints;

    public CitizenInstaller(
        CitizenManager citizenManagerPrefab,
        CitizenWanderManager citizenWanderManagerPrefab,
        Transform hubPoint,
        Transform[] homePoints)
    {
        this.citizenManagerPrefab = citizenManagerPrefab;
        this.citizenWanderManagerPrefab = citizenWanderManagerPrefab;
        this.hubPoint = hubPoint;
        this.homePoints = homePoints;
    }

    public void Install(IContainerBuilder builder)
    {
        builder.RegisterComponentInNewPrefab(citizenManagerPrefab, Lifetime.Singleton).AsSelf();
        builder.RegisterComponentInNewPrefab(citizenWanderManagerPrefab, Lifetime.Singleton).AsSelf();
        builder.RegisterComponentInHierarchy<AddCitizen>();

        builder.RegisterBuildCallback(resolver =>
        {
            var wanderManager = resolver.Resolve<CitizenWanderManager>();
            if (hubPoint != null)
                wanderManager.SetHubPoint(hubPoint);
            wanderManager.SetHomePoints(homePoints);
        });
    }
}
