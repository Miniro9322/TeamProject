using System.Collections.Generic;
using UnityEngine;
using VContainer;

// 영웅 "생성" 전용 패널. 근접/원거리 버튼 2개뿐 — 누르면 수량 선택 모달(amountPanel)이 뜨고,
// 거기서 정한 수량만큼 자원 비용을 내고 HeroCreateManager가 티어 확률대로 뽑은 영웅들을 로스터에
// 추가한다. 인구수는 뽑힌 영웅의 실제 티어만큼씩 소모되며, 도중에 바닥나면 그 지점에서 생성을
// 멈추고 못 만든 만큼 자원을 환불한다(자원 비용과 달리 인구수는 생성을 사전에 막지 않는다).
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
    private bool wasBlocked;
    private HeroCreateIcon openIcon; // amountPanel이 떠있는 동안 그 대상 아이콘 - RefreshInteractable에서 최대치 갱신용

    [Inject]
    private void Construct(UpgradeState upgradeState, BuildModePanel buildModePanel)
    {
        this.upgradeState = upgradeState;
        this.buildModePanel = buildModePanel;
    }

    //모드 전환을 알리는 이벤트가 없어서 BuildModePanel의 Esc 감지처럼 매 프레임 폴링한다.
    private void Update()
    {
        bool isBlocked = !view.IsOff;
        if (isBlocked == wasBlocked) return;
        wasBlocked = isBlocked;
        RefreshInteractable();
    }

    private void OnEnable()
    {
        wasBlocked = view.IsOff;
        game.CitizenManager.CitizenChanged += RefreshInteractable;
        game.ResourcesManager.ProductUpdate += RefreshInteractable;
        view.OnOffMode += RefreshInteractable;

        meleeIcon.Set(false, () => OpenAmountPanel(meleeIcon, OccupantKind.MeleeHero));
        rangedIcon.Set(false, () => OpenAmountPanel(rangedIcon, OccupantKind.RangedHero));
        RefreshInteractable();
    }

    private void OnDisable()
    {
        game.CitizenManager.CitizenChanged -= RefreshInteractable;
        game.ResourcesManager.ProductUpdate -= RefreshInteractable;
        view.OnOffMode -= RefreshInteractable;
    }

    // 타이틀에서 해금한 HeroCostUpgrade1~5만큼 할인된 실제 소모 비용 - House/ProductionFacility와 동일 패턴.
    private (ProductionType Type, int Amount)[] GetCost(HeroCreateIcon icon)
    {
        float discount = upgradeState.GetTotalEffect(costUpgrades);
        return icon.ResourceCost.ToNegatedCostArray().ApplyDiscount(discount);
    }

    private void OpenAmountPanel(HeroCreateIcon icon, OccupantKind kind)
    {
        if (amountPanel == null || !view.IsOff || !CanAfford(icon)) return;

        openIcon = icon;
        var cost = GetCost(icon);
        amountPanel.Open(icon.ResourceIcons, cost, GetMaxAffordable(cost), game.CitizenManager.CheckCanUseCitizen(),
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

    // 인구수가 하나라도 남아있으면 생성은 허용한다 — 뽑힌 영웅의 실제 티어 비용이 남은 인구수를 넘으면
    // BulkCreate에서 그만큼 음수로 내려간다(인구수가 0 이하일 때만 버튼을 막는다).
    private bool CanAfford(HeroCreateIcon icon)
    {
        return game.CitizenManager.CheckCanUseCitizen()
            && view.resourcesManager.CheckResources(GetCost(icon));
    }

    // 자원/시민 변화, 모드 진입/종료 시 여기로 온다. Off 모드가 아니면(배치·재배치·제거 등) 무조건 비활성화.
    // 버튼마다 비용이 달라 구매 가능 여부도 따로 계산해야 한다(근접은 되는데 원거리는 안 되는 경우가 있음).
    private void RefreshInteractable()
    {
        bool off = view.IsOff;
        meleeIcon.SetInteractable(off && CanAfford(meleeIcon));
        rangedIcon.SetInteractable(off && CanAfford(rangedIcon));

        if (amountPanel == null || openIcon == null || !amountPanel.gameObject.activeSelf) return;
        if (!off)
        {
            amountPanel.Close();
            openIcon = null;
            return;
        }
        var cost = GetCost(openIcon);
        amountPanel.RefreshMax(GetMaxAffordable(cost), game.CitizenManager.CheckCanUseCitizen());
    }
}
