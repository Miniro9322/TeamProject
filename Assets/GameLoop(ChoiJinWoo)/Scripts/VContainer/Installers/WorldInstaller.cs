using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// 게임 월드의 핵심 시스템 등록.
/// 게임 진행 상태(GameManager), 마나(PlayerManaManager), 오브젝트 풀(PoolManager),
/// 버프 틱(BuffManager), 기믹 데이터, 맵/스포너 씬 컴포넌트를 담당한다.
/// </summary>
public sealed class WorldInstaller : IInstaller
{
    private readonly GameManager gameManagerPrefab;
    private readonly PlayerManaManager playerManaManagerPrefab;

    public WorldInstaller(GameManager gameManagerPrefab, PlayerManaManager playerManaManagerPrefab)
    {
        this.gameManagerPrefab = gameManagerPrefab;
        this.playerManaManagerPrefab = playerManaManagerPrefab;
    }

    public void Install(IContainerBuilder builder)
    {
        builder.RegisterComponentInHierarchy<ExpandEvent>().AsSelf();

        builder.RegisterComponentInNewPrefab(gameManagerPrefab, Lifetime.Singleton).AsSelf();
        builder.RegisterComponentInNewPrefab(playerManaManagerPrefab, Lifetime.Singleton).AsSelf();
        builder.RegisterComponentOnNewGameObject<PoolManager>(Lifetime.Singleton).AsSelf();

        builder.Register<BuffManager>(Lifetime.Singleton).As<ITickable>().AsSelf();
        builder.Register<GimmickTileData>(Lifetime.Singleton).AsSelf();

        builder.RegisterComponentInHierarchy<MapGame>();
        builder.RegisterComponentInHierarchy<MapRegistry>();
        builder.RegisterComponentInHierarchy<MapAssemble>();
        builder.RegisterComponentInHierarchy<MapView>();
        builder.RegisterComponentInHierarchy<PlacePalette>();
        builder.RegisterComponentInHierarchy<SpawnerManager>().AsSelf();
        builder.RegisterComponentInHierarchy<WaveSpawner>();

        // PoolManager는 아무도 생성자 의존성으로 끌어오지 않으므로 여기서 강제 인스턴스화한다.
        builder.RegisterBuildCallback(resolver => resolver.Resolve<PoolManager>());

        // 씬에 여러 개 있을 수 있는 WaveSpawner 전부에 주입한다.
        builder.RegisterBuildCallback(resolver =>
        {
            var waveSpawners = Object.FindObjectsByType<WaveSpawner>(FindObjectsSortMode.None);
            foreach (var ws in waveSpawners)
                resolver.InjectGameObject(ws.gameObject);
        });
    }
}
