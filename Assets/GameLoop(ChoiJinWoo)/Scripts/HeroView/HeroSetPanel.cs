using System.Collections.Generic;
using UnityEngine;
using VContainer;

// 영웅 "생성" 전용 패널. 근접/원거리 버튼과 수량 조절 UI(amountPanel)가 한 패널에 같이 떠 있고,
// 아이콘을 누르면 그 대상으로 amountPanel의 표시만 갱신된다(팝업을 열고 닫는 게 아니다). 거기서
// 정한 수량만큼 자원 비용을 내고 HeroCreateManager가 티어 확률대로 뽑은 영웅들을 로스터에 추가한다.
// 인구수는 뽑힌 영웅의 실제 티어만큼씩 소모되며, 도중에 바닥나면 그 지점에서 생성을 멈추고 못 만든
// 만큼 자원을 환불한다(자원 비용과 달리 인구수는 생성을 사전에 막지 않는다).
public class HeroSetPanel : MonoBehaviour
{
    [SerializeField] private MapView view;
    [SerializeField] private MapGame game;
    [SerializeField] private HeroCreateManager createManager;
    [SerializeField] private HeroCreateIcon meleeIcon;
    [SerializeField] private HeroCreateIcon rangedIcon;
    [SerializeField] private HeroCreateAmountController amountPanel;
    [SerializeField] private List<BaseUpgradeData> costUpgrades; // 타이틀 업그레이드 트리의 HeroCostUpgrade1~5
    private UpgradeState upgradeState;
    private BuildModePanel buildModePanel;
    private HeroCreateIcon openIcon; // 현재 선택된 아이콘 - 하이라이트 토글과 HeroCreateIconHeldLink 노출용
    public HeroCreateIcon OpenIcon => openIcon; // HeroCreateIconHeldLink에서  누가 열었는지 확인할 수 있게 노출

    [Inject]
    private void Construct(UpgradeState upgradeState, BuildModePanel buildModePanel)
    {
        this.upgradeState = upgradeState;
        this.buildModePanel = buildModePanel;
    }

    private void OnEnable()
    {
        view.OnOffMode += RefreshInteractable;

        meleeIcon.Set(false, () => SetResourcesPanel(meleeIcon, OccupantKind.MeleeHero));
        rangedIcon.Set(false, () => SetResourcesPanel(rangedIcon, OccupantKind.RangedHero));
        RefreshInteractable();
        SetResourcesPanel(meleeIcon, OccupantKind.MeleeHero); // 패널이 열릴 때마다(=OnEnable마다) 근접 기본 선택 + 수량 초기화
    }

    private void OnDisable()
    {
        view.OnOffMode -= RefreshInteractable;
    }

    // 타이틀에서 해금한 HeroCostUpgrade1~5만큼 할인된 실제 소모 비용 - House/ProductionFacility와 동일 패턴.
    private (ProductionType Type, int Amount)[] GetCost(HeroCreateIcon icon)
    {
        float discount = upgradeState.GetTotalEffect(costUpgrades);
        return icon.ResourceCost.ToNegatedCostArray().ApplyDiscount(discount);
    }

    private void SetResourcesPanel(HeroCreateIcon icon, OccupantKind kind)
    {
        if (amountPanel == null || !view.IsOff) return;
        openIcon = icon;
        openIcon.SetSelected(true);
        var otherIcon = openIcon != meleeIcon ? meleeIcon : rangedIcon;
        otherIcon.SetSelected(false);

        var cost = GetCost(icon);
        amountPanel.SetTarget(icon.ResourceIcons, cost, GetMaxAffordable(cost), GetSufficiency(cost),
            game.CitizenManager.CheckCanUseCitizen(), GetUnaffordReason(icon),
            amount => BulkCreate(icon, kind, amount));
    }

    // 한 번에 살 수 있는 최대 수량 - 자원 기준과, 인구비용을 1로 가정한 인구수 기준 중 더 작은 값.
    // 실제로 뽑힐 영웅의 티어(=인구비용)가 1보다 크면 이 추정보다 인구수가 더 빨리 소진될 수 있는데,
    // 그런 경우는 실제 생성(BulkCreate) 중에 부족해지면 그 자리에서 멈추고 환불하는 걸로 처리한다.
    private int GetMaxAffordable((ProductionType Type, int Amount)[] unitCost)
    {
        int max = int.MaxValue;
        foreach (var c in unitCost)
        {
            if (c.Amount >= 0) continue;
            max = Mathf.Min(max, view.resourcesManager.GetAmount(c.Type) / -c.Amount);
        }
        max = Mathf.Min(max, game.CitizenManager.CanUseCitizen); // 인구비용 1 기준
        return Mathf.Max(max, 0);
    }

    // 자원별로 유닛 비용 1개라도 감당 가능한지 - amountPanel이 부족한 자원 행을 빨간색으로 표시하는 데 쓴다.
    private bool[] GetSufficiency((ProductionType Type, int Amount)[] unitCost)
    {
        bool[] sufficient = new bool[unitCost.Length];
        for (int i = 0; i < unitCost.Length; i++)
        {
            var c = unitCost[i];
            sufficient[i] = c.Amount >= 0 || view.resourcesManager.GetAmount(c.Type) >= -c.Amount;
        }
        return sufficient;
    }

    private void BulkCreate(HeroCreateIcon icon, OccupantKind kind, int amount)
    {
        if (!view.IsOff || amount <= 0) return;

        var unitCost = GetCost(icon);
        var totalCost = unitCost.Multiply(amount);
        if (!view.resourcesManager.CheckResources(totalCost)) return; // 패널이 떠있는 동안 자원이 바뀌었을 수 있으니 최종 확인.

        view.resourcesManager.ProductChanged(totalCost); // 전체 수량분을 먼저 차감하고, 못 만든 만큼은 아래서 환불한다.

        int created = 0;
        for (int i = 0; i < amount; i++)
        {
            if (!game.CitizenManager.CheckCanUseCitizen()) break; // 인구수 소진 - 여기서 생성 중단.
            if (!createManager.TryRollHero(kind, out HeroData picked)) continue;

            // 뽑힌 영웅의 실제 티어만큼 인구수를 소모한다 — 남은 인구수를 초과해도 생성은 진행되고 음수로 남는다.
            game.CitizenManager.UseCitizenForHero(picked.PopulationCost);

            Placeable slot = new Placeable
            {
                label = picked.HeroName,
                prefab = picked.HeroPrefab,
                kind = kind,
            };
            game.HeroRoster.Add(slot, picked, picked.PopulationCost);
            created++;
        }

        int notCreated = amount - created;
        if (notCreated > 0) view.resourcesManager.ProductChanged(unitCost.Multiply(-notCreated));

        buildModePanel.OpenInventory();
    }

    // 인구 -> 자원 순서로 첫 번째로 부족한 항목의 StringTable 키를 반환한다. 인구수가 하나라도
    // 남아있으면 인구는 통과시킨다 — 뽑힌 영웅의 실제 티어 비용이 남은 인구수를 넘으면 BulkCreate에서
    // 그만큼 음수로 내려간다(인구수가 0 이하일 때만 막는다). 둘 다 충족하면 null.
    private string GetUnaffordReason(HeroCreateIcon icon)
    {
        if (!game.CitizenManager.CheckCanUseCitizen()) return "UI_Hero_NotEnoughPopulation";
        if (!view.resourcesManager.CheckResources(GetCost(icon))) return "UI_Base_NotEnoughResources";
        return null;
    }

    // view가 Off 모드로 들어올 때(OnOffMode)만 불린다 - Off가 아니면(배치·재배치·제거 등) 생성 버튼을
    // 무조건 비활성화한다. 선택 상태/수량 표시는 건드리지 않는다(그건 SetResourcesPanel의 역할).
    // 인구/자원 부족은 더 이상 버튼을 막지 않는다 — amountPanel의 reasonText가 사유를 띄운다.
    private void RefreshInteractable()
    {
        bool off = view.IsOff;
        meleeIcon.SetInteractable(off);
        rangedIcon.SetInteractable(off);
    }
}
