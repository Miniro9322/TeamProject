using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class ProductionFacility : MonoBehaviour, IPlaceAble
{
    [SerializeField] private ProductionValue basicValue;
    [SerializeField] private List<BaseUpgradeData> constructCostUpgrades;
    [SerializeField] private List<BaseUpgradeData> upgradeCostUpgrades;
    [SerializeField] private List<BaseUpgradeData> productAmountUpgrades;
    private int productAmount;
    private int workerAmount = 0;
    private int maxWorker;
    private ResourcesManager resourcesManager;
    private BuildingPool buildingPool;
    private CitizenManager citizenManager;
    private FacilityManager facilityManager;
    private UpgradeState upgradeState;
    public ProductionType ProductionType => basicValue.Type;

    // slot.prefab.GetComponent<ProductionFacility>()처럼 Instantiate/Inject를 거치지 않은
    // 프리팹 원본에서 값을 읽는 경우 upgradeState가 주입돼 있지 않다. UpgradeState는 PlayerPrefs만
    // 읽으면 되는 가벼운 객체라, 주입이 안 된 경우 즉석에서 하나 만들어 최신 해금 상태를 반영한다.
    private UpgradeState UpgradeStateOrFallback => upgradeState ?? new UpgradeState();
    private float ConstructCostDiscount => UpgradeStateOrFallback.GetTotalEffect(constructCostUpgrades);
    private float UpgradeCostDiscount => UpgradeStateOrFallback.GetTotalEffect(upgradeCostUpgrades);
    private int ProductAmountBonus => (int)UpgradeStateOrFallback.GetTotalEffect(productAmountUpgrades);

    // 건설 비용 미리보기/체크용 — Init() 전에도(배치 전 정보 패널, 자원 체크) 안전하게 호출 가능.
    public (ProductionType Type, int Amount)[] GetConstructCost() =>
        ApplyDiscount(basicValue.ConstructProduct, ConstructCostDiscount);

    public event Action OnWorkerChanged;
    public event Action OnBreak;
    public event Action OnResur;
    private MapBoard board;

    public ProductionValue BasicValue => basicValue;

    public MapBoard Board => board;

    private int maxUpgrade = 4;
    private int upgradeCount = 0;
    private int amountUpgrade = 0;
    private int citizenUpgrade = 0;
    public int WorkerAmount => workerAmount;
    public int MaxWorker => maxWorker;
    public int ProductAmount => productAmount;
    private (ProductionType Type, int Amount)[] upgradeCostCopy = Array.Empty<(ProductionType, int)>();
    public (ProductionType Type, int Amount)[] UpgradeCostCopy => upgradeCostCopy;
    public int UpgradeCount => upgradeCount;

    [Inject]
    private void Construct(ResourcesManager resourcesManager, CitizenManager citizenManager, BuildingPool buildingPool, FacilityManager facilityManager, UpgradeState upgradeState)
    {
        this.resourcesManager = resourcesManager;
        this.citizenManager = citizenManager;
        this.buildingPool = buildingPool;
        this.facilityManager = facilityManager;
        this.upgradeState = upgradeState;
    }

    private static (ProductionType Type, int Amount)[] ApplyDiscount((ProductionType Type, int Amount)[] cost, float discount)
    {
        var result = new (ProductionType, int)[cost.Length];
        for (int i = 0; i < cost.Length; i++)
            result[i] = (cost[i].Type, Mathf.RoundToInt(cost[i].Amount * (1f - discount)));
        return result;
    }

    public void Init()
    {
        productAmount = basicValue.DefaultAmount + amountUpgrade * 10 + ProductAmountBonus;
        maxWorker = basicValue.DefaultMaxWorker + citizenUpgrade;
        workerAmount = 0;
        upgradeCostCopy = ApplyDiscount(BasicValue.UpgradeCost, UpgradeCostDiscount);

        resourcesManager.ProductChanged(GetConstructCost());
        facilityManager.AddFacility(this);
    }

    private void OnDisable()
    {
        ReleaseAllWorkers();
        if(facilityManager != null)
            facilityManager.RemoveFacility(this);
    }

    private void ReleaseAllWorkers()
    {
        while (workerAmount > 0)
        {
            workerAmount--;
            citizenManager.RecycleCitizen();
        }
        UpdateInfo();
    }

    public void Release()
    {
        var construct = basicValue.ConstructProduct;
        var refund = new (ProductionType Type, int Amount)[construct.Length];
        for (int i = 0; i < construct.Length; i++)
        {
            refund[i] = (construct[i].Type, -construct[i].Amount);
        }
        resourcesManager.ProductChanged(refund);
        buildingPool.Return(gameObject);
    }

    public void IncreaseWorker()
    {
        if (citizenManager == null) return;

        if (workerAmount < maxWorker && citizenManager.CheckCanUseCitizen())
        {
            workerAmount++;
            UpdateInfo();
            citizenManager.UseCitizen();
        }
    }

    public void DecreaseWorker()
    {
        if (citizenManager == null) return;

        if (workerAmount > 0)
        {
            workerAmount--;
            UpdateInfo();
            citizenManager.RecycleCitizen();
        }
    }

    public void UpdateInfo()
    {
        OnWorkerChanged?.Invoke();
    }

    public (ProductionType, int) ProduceProduction()
    {
        return (basicValue.Type, productAmount * workerAmount);
    }

    public void SetBoard(MapBoard board)
    {
        this.board = board;
    }

    public void Upgrade()
    {
        upgradeCount++;
        if(upgradeCount % 5 == 0)
        {
            Debug.Log("특수 자원 생산 시작");
        }
        else if(upgradeCount % 2 == 0)
        {
            amountUpgrade++;
            productAmount += amountUpgrade * 10;
        }
        else
        {
            citizenUpgrade++;
            maxWorker = basicValue.DefaultMaxWorker + citizenUpgrade;
        }

        resourcesManager.ProductChanged(upgradeCostCopy);

        var baseCost = ApplyDiscount(basicValue.UpgradeCost, UpgradeCostDiscount);
        for (int i = 0; i < upgradeCostCopy.Length; i++)
        {
            upgradeCostCopy[i] = (baseCost[i].Type, baseCost[i].Amount * upgradeCount);
        }

        UpdateInfo();
    }

    public bool CheckCanUpgrade()
    {
        return upgradeCount < maxUpgrade && resourcesManager.CheckResources(upgradeCostCopy);
    }

    //건물 마다 업그레이드
    //생산 건물 타입 마다 업그레이드
    //디펜스동안은 건물 마다 업그레이드 단 상한선 존재
    //게임 끝나고 얻은 포인트로 생산 건물 타입의 업그레이드
    // - 건물 업그레이드 최대 횟수 증가
    // - 건물 건설 비용 감소
    // - 건물 업그레이드 비용 감소
    //건물 마다 업그레이드
    // - 생산량 증가
    // - 배치 가능한 시민 수 증가
    // - 특수 자원 생산 가능(업그레이드 일정 횟수 이상부터)
}
