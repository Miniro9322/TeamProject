using UnityEngine;
using VContainer;

// VContainer 주입을 받아 배치 담당을 조립한다. 맵 로직은 하나도 갖지 않는다.
public class MapGame : MonoBehaviour
{
    public MapBoard board;

    private readonly UnitList unitList = new();
    private UnitPlacer unitPlacer;
    private UiManager uiManager;
    private GameManager gameManager;

    public UnitList Units { get { return unitList; } }
    public UnitPlacer Placer { get { return unitPlacer; } }
    public UiManager Ui { get { return uiManager; } }
    public GameManager Rule { get { return gameManager; } }

    [Inject]
    private void Construct(IObjectResolver resolver, ResourcesManager resourcesManager, BuildingPool buildingPool, UiManager uiManager, GameManager gameManager)
    {
        this.uiManager = uiManager;
        this.gameManager = gameManager;
        unitPlacer = new UnitPlacer(board, unitList, resolver, resourcesManager, buildingPool);
    }
}
