using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class HeroSetPanel : MonoBehaviour
{
    [SerializeField] private MapView view;
    [SerializeField] private MapGame game;
    [SerializeField] private HeroCreateManager createManager;
    [SerializeField] private HeroCreateIcon meleeIcon;
    [SerializeField] private HeroCreateIcon rangedIcon;
    [SerializeField] private HeroCreateAmountController amountPanel;
    [SerializeField] private List<BaseUpgradeData> costUpgrades;
    [SerializeField] private bool useEscalatingHeroPrice = true;
    [SerializeField, Tooltip("영웅 생성 1회당 가격이 원가 대비 증가하는 비율 (0.25 = 25%p씩 선형 증가, 복리 아님)")]
    private float heroPriceIncreaseRate = 0.25f;
    [SerializeField] private bool useRegionHeroPrice = true;
    [SerializeField, Tooltip("지역이 하나 늘어날 때마다 가격이 원가 대비 증가하는 비율 (0.25 = 25%p씩 선형 증가, 하루 점증과 별개로 계속 누적되고 하루가 지나도 초기화되지 않음)")]
    private float regionPriceIncreaseRate = 0.25f;
    private UpgradeState upgradeState;
    private BuildModePanel buildModePanel;
    private HeroCreateIcon openIcon;
    public HeroCreateIcon OpenIcon => openIcon;

    public event System.Action OpenIconChanged;

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
        SetResourcesPanel(meleeIcon, OccupantKind.MeleeHero);
    }

    private void OnDisable()
    {
        view.OnOffMode -= RefreshInteractable;
    }
    private (ProductionType Type, int Amount)[] GetCost(HeroCreateIcon icon)
    {
        float discount = upgradeState.GetTotalEffect(costUpgrades);
        return icon.ResourceCost.ToNegatedCostArray().ApplyDiscount(discount);
    }

    private (ProductionType Type, int Amount)[] GetEscalatedUnitCost((ProductionType Type, int Amount)[] baseCost, int occurrenceIndex)
    {
        float multiplier = 1f;
        if (useEscalatingHeroPrice) multiplier += heroPriceIncreaseRate * occurrenceIndex;
        if (useRegionHeroPrice) multiplier += regionPriceIncreaseRate * createManager.ExtraUnlockedRegions;
        if (multiplier == 1f) return baseCost;
        return baseCost.Scale(multiplier);
    }

    private (ProductionType Type, int Amount)[] GetNextUnitCost(HeroCreateIcon icon)
    {
        return GetEscalatedUnitCost(GetCost(icon), game.Rule.HeroesCreatedToday);
    }

    private (ProductionType Type, int Amount)[][] BuildSlotCosts(HeroCreateIcon icon, int amount)
    {
        var baseCost = GetCost(icon);
        int start = useEscalatingHeroPrice ? game.Rule.HeroesCreatedToday : 0;
        var result = new (ProductionType Type, int Amount)[amount][];
        for (int i = 0; i < amount; i++)
        {
            result[i] = GetEscalatedUnitCost(baseCost, start + i);
        }
        return result;
    }

    private static (ProductionType Type, int Amount)[] SumSlotCosts((ProductionType Type, int Amount)[][] slotCosts)
    {
        var total = slotCosts[0];
        for (int i = 1; i < slotCosts.Length; i++)
        {
            total = total.Add(slotCosts[i]);
        }
        return total;
    }

    private (ProductionType Type, int Amount)[] GetBatchCost(HeroCreateIcon icon, int amount)
    {
        return SumSlotCosts(BuildSlotCosts(icon, Mathf.Max(amount, 1)));
    }

    private void SetResourcesPanel(HeroCreateIcon icon, OccupantKind kind)
    {
        if (amountPanel == null || !view.IsOff) return;
        openIcon = icon;
        openIcon.SetSelected(true);
        var otherIcon = openIcon != meleeIcon ? meleeIcon : rangedIcon;
        otherIcon.SetSelected(false);
        OpenIconChanged?.Invoke();

        var nextUnitCost = GetNextUnitCost(icon);
        amountPanel.SetTarget(icon.ResourceIcons, amt => GetBatchCost(icon, amt), GetMaxAffordable(icon), GetSufficiency(nextUnitCost),
            game.CitizenManager.CheckCanUseCitizen(), GetUnaffordReason(icon),
            amount => BulkCreate(icon, kind, amount));
    }

    private int GetMaxAffordable(HeroCreateIcon icon)
    {
        var baseCost = GetCost(icon);
        int start = useEscalatingHeroPrice ? game.Rule.HeroesCreatedToday : 0;
        int popCap = Mathf.Max(0, game.CitizenManager.CanUseCitizen);

        int[] remaining = new int[baseCost.Length];
        for (int i = 0; i < baseCost.Length; i++)
        {
            remaining[i] = baseCost[i].Amount < 0 ? view.resourcesManager.GetAmount(baseCost[i].Type) : int.MaxValue;
        }

        int n = 0;
        for (; n < popCap; n++)
        {
            var slot = GetEscalatedUnitCost(baseCost, start + n);
            bool affordable = true;
            for (int i = 0; i < slot.Length; i++)
            {
                if (slot[i].Amount < 0 && remaining[i] < -slot[i].Amount) { affordable = false; break; }
            }
            if (!affordable) break;

            for (int i = 0; i < slot.Length; i++)
            {
                if (slot[i].Amount < 0) remaining[i] += slot[i].Amount;
            }
        }
        return n;
    }

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

        var slotCosts = BuildSlotCosts(icon, amount);
        var totalCost = SumSlotCosts(slotCosts);
        if (!view.resourcesManager.CheckResources(totalCost)) return;

        view.resourcesManager.ProductChanged(totalCost);

        int created = 0;
        for (int i = 0; i < amount; i++)
        {
            if (!game.CitizenManager.CheckCanUseCitizen())
            {
                for (int j = i; j < amount; j++) view.resourcesManager.ProductChanged(slotCosts[j].Multiply(-1));
                break;
            }
            if (!createManager.TryRollHero(kind, out HeroData picked))
            {
                view.resourcesManager.ProductChanged(slotCosts[i].Multiply(-1));
                continue;
            }

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

        if (created > 0) game.Rule.AddHeroesCreatedToday(created);

        SetResourcesPanel(icon, kind);
        buildModePanel.OpenInventory();
    }

    private string GetUnaffordReason(HeroCreateIcon icon)
    {
        if (!game.CitizenManager.CheckCanUseCitizen()) return "UI_Hero_NotEnoughPopulation";
        if (!view.resourcesManager.CheckResources(GetNextUnitCost(icon))) return "UI_Base_NotEnoughResources";
        return null;
    }

    private void RefreshInteractable()
    {
        bool off = view.IsOff;
        meleeIcon.SetInteractable(off);
        rangedIcon.SetInteractable(off);
    }
}
