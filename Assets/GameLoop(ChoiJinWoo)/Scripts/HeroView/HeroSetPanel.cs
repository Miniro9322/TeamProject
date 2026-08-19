using System.Collections.Generic;
using UnityEngine;
using VContainer;

// 영웅 "생성" 전용 패널. 근접/원거리 버튼 2개뿐 — 누르면 자원 비용을 내고 HeroCreateManager가
// 티어 확률대로 뽑은 영웅 하나를 로스터에 추가한다. 인구수는 뽑힌 영웅의 실제 티어만큼 소모되며,
// 남은 인구수를 넘으면 음수로 내려간다(자원 비용과 달리 생성을 막지 않는다).
public class HeroSetPanel : MonoBehaviour
{
    [SerializeField] private MapView view;
    [SerializeField] private MapGame game;
    [SerializeField] private HeroCreateManager createManager;
    [SerializeField] private HeroCreateIcon meleeIcon;
    [SerializeField] private HeroCreateIcon rangedIcon;
    [SerializeField] private GameObject rosterPanel;
    [SerializeField] private List<BaseUpgradeData> costUpgrades; // 타이틀 업그레이드 트리의 HeroCostUpgrade1~5
    private UpgradeState upgradeState;
    private bool wasBlocked;

    [Inject]
    private void Construct(UpgradeState upgradeState)
    {
        this.upgradeState = upgradeState;
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

        meleeIcon.Set(false, () => OnCreate(meleeIcon, OccupantKind.MeleeHero));
        rangedIcon.Set(false, () => OnCreate(rangedIcon, OccupantKind.RangedHero));
        RefreshInteractable();
        RefreshCostDisplay(meleeIcon);
        RefreshCostDisplay(rangedIcon);
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

    // 실제 소모량과 같은 계산으로 툴팁 가격 표시도 맞춘다 - 안 그러면 할인을 해금해도 화면엔 원가가 계속 뜬다.
    private void RefreshCostDisplay(HeroCreateIcon icon)
    {
        var cost = GetCost(icon);
        var display = new List<ResourceCost>(cost.Length);
        foreach (var c in cost) display.Add(new ResourceCost { Type = c.Type, Amount = -c.Amount });
        icon.RefreshCostDisplay(display);
    }

    private void OnCreate(HeroCreateIcon icon, OccupantKind kind)
    {
        if (!view.IsOff || !CanAfford(icon)) return;
        if (!createManager.TryRollHero(kind, out HeroData picked)) return; // 비용은 결과가 나온 뒤에 낸다.

        // 뽑힌 영웅의 실제 티어만큼 인구수를 소모한다 — 남은 인구수를 초과해도 생성은 진행되고 음수로 남는다.
        game.CitizenManager.UseCitizenForHero(picked.PopulationCost);
        view.resourcesManager.ProductChanged(GetCost(icon));

        Placeable slot = new Placeable
        {
            label = picked.HeroName,
            prefab = picked.HeroPrefab,
            kind = kind,
        };
        game.HeroRoster.Add(slot, picked, picked.PopulationCost);
        if (!rosterPanel.activeSelf) rosterPanel.SetActive(true);
    }

    // 인구수가 하나라도 남아있으면 생성은 허용한다 — 뽑힌 영웅의 실제 티어 비용이 남은 인구수를 넘으면
    // OnCreate에서 그만큼 음수로 내려간다(인구수가 0 이하일 때만 버튼을 막는다).
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
    }
}
