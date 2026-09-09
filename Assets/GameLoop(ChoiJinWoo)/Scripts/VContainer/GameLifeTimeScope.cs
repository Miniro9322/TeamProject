using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// 메인 씬의 루트 LifetimeScope.
/// 인스펙터로 물려받는 프리팹/설정 참조만 보관하고, 실제 등록은 도메인별 IInstaller 로 위임한다.
/// 각 등록 세부는 Installers/ 폴더 참고.
/// </summary>
public class GameLifeTimeScope : LifetimeScope
{
    [SerializeField] private ResourcesManager resourcesManagerPrefab;
    [SerializeField] private CitizenManager citizenManagerPrefab;
    [SerializeField] private CitizenWanderManager citizenWanderManagerPrefab;
    [SerializeField] private UiManager UiManagerPrefab;
    [SerializeField] private EnviromentManager EnviromentManagerPrefab;
    [SerializeField] private GameManager GameManagerPrefab;
    [SerializeField] private PlayerManaManager playerManaManagerPrefab;
    [SerializeField] private FacilityManager FacilityManager; // 미사용(기존 구조 유지). FacilityManager는 POCO로 등록된다.
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
        void Install(IInstaller installer) => installer.Install(builder);

        Install(new UiInstaller(UiManagerPrefab));
        Install(new HeroInstaller(heroUpgradeConfig, heroClassUpgradeConfig, heroStatUpgrades));
        Install(new EnvironmentInstaller(EnviromentManagerPrefab, sunLight));
        Install(new CitizenInstaller(citizenManagerPrefab, citizenWanderManagerPrefab, citizenHubPoint, citizenHomePoints));
        Install(new WorldInstaller(GameManagerPrefab, playerManaManagerPrefab));
        Install(new FacilityInstaller(resourcesManagerPrefab, economyConfig, resourceIconSet));
        Install(new TutorialInstaller());
        Install(new SaveInstaller());
    }
}
