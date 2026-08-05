// 맵 배치가 사라지면서 GameObject/Transform이 필요 없어져 일반 클래스로 전환했다.
// 예전 Start()/OnDestroy()가 하던 일을 Init()/Release()로 명시적으로 호출한다.
public class House
{
    private readonly HouseConfig config;
    private readonly CitizenManager citizenManager;
    private readonly ResourcesManager resourcesManager;

    public string HouseName => config.HouseName;
    public string HouseInfo => config.HouseInfo;
    public (ProductionType Type, int Amount)[] Resources => config.Resources;

    public House(HouseConfig config, CitizenManager citizenManager, ResourcesManager resourcesManager)
    {
        this.config = config;
        this.citizenManager = citizenManager;
        this.resourcesManager = resourcesManager;
    }

    public void Init()
    {
        citizenManager.IncreaseMaxCitizen(config.MaxCitizenAmount);
        resourcesManager.ProductChanged(Resources);
    }

    public void Release()
    {
        var resources = Resources;
        var refund = new (ProductionType Type, int Amount)[resources.Length];
        for (int i = 0; i < resources.Length; i++)
        {
            refund[i] = (resources[i].Type, -resources[i].Amount);
        }
        resourcesManager.ProductChanged(refund);
    }
}
