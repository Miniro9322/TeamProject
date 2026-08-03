using UnityEngine;
using VContainer;

// VContainer 주입을 받아 배치 담당을 조립한다. 맵 로직은 하나도 갖지 않는다.
public class MapGame : MonoBehaviour
{
    private readonly PlacedUnitData unitList = new();
    private UnitPlacer unitPlacer;
    private UiManager uiManager;
    private GameManager gameManager;
    private ResourcesManager resourcesManager;
    private CitizenManager citizenManager;
    private EnviromentManager enviromentManager;
    private HeroRoster heroRoster;

    public PlacedUnitData Units { get { return unitList; } }
    public UnitPlacer Placer { get { return unitPlacer; } }
    public UiManager Ui { get { return uiManager; } }
    public GameManager Rule { get { return gameManager; } }
    public ResourcesManager ResourcesManager { get { return resourcesManager; } }
    public CitizenManager CitizenManager { get { return citizenManager; } }
    public EnviromentManager EnviromentManager { get { return enviromentManager; } }
    public HeroRoster HeroRoster { get { return heroRoster; } }


    [Inject]
    private void Construct(IObjectResolver resolver, ResourcesManager resourcesManager, BuildingPool buildingPool, UiManager uiManager, GameManager gameManager, CitizenManager citizenManager, EnviromentManager enviromentManager, HeroRoster heroRoster)
    {
        this.uiManager = uiManager;
        this.gameManager = gameManager;
        this.resourcesManager = resourcesManager;
        this.citizenManager = citizenManager;
        this.enviromentManager = enviromentManager;
        this.heroRoster = heroRoster;
        unitPlacer = new UnitPlacer();
        unitPlacer.unitList = unitList;
        unitPlacer.resolver = resolver;
        unitPlacer.resourcesManager = resourcesManager;
        unitPlacer.pool = buildingPool;
    }
}
