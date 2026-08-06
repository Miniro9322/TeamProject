using System.Collections.Generic;
using UnityEngine;

// 영웅 "생성" 전용 패널. 근접/원거리 버튼 2개뿐 — 누르면 고정 비용을 내고 HeroCreateManager가
// 티어 확률대로 뽑은 영웅 하나를 로스터에 추가한다.
public class HeroSetPanel : MonoBehaviour
{
    [SerializeField] private MapView view;
    [SerializeField] private MapGame game;
    [SerializeField] private HeroCreateManager createManager;
    [SerializeField] private HeroCreateIcon meleeIcon;
    [SerializeField] private HeroCreateIcon rangedIcon;

    [Header("생성 1회당 고정 소모 비용")]
    [SerializeField] private int citizenCost = 2;
    [SerializeField] private List<ResourceCost> resourceCost;

    private bool wasBlocked;

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

        meleeIcon.Set(false, () => OnCreate(OccupantKind.MeleeHero));
        rangedIcon.Set(false, () => OnCreate(OccupantKind.RangedHero));
        RefreshInteractable();
    }

    private void OnDisable()
    {
        game.CitizenManager.CitizenChanged -= RefreshInteractable;
        game.ResourcesManager.ProductUpdate -= RefreshInteractable;
        view.OnOffMode -= RefreshInteractable;
    }

    private void OnCreate(OccupantKind kind)
    {
        if (!view.IsOff || !CanAffordFixedCost()) return;
        if (!createManager.TryRollHero(kind, out HeroData picked)) return; // 비용은 결과가 나온 뒤에 낸다.

        game.CitizenManager.UseCitizen(citizenCost);
        view.resourcesManager.ProductChanged(resourceCost.ToNegatedCostArray());

        Placeable slot = new Placeable
        {
            label = picked.HeroName,
            prefab = picked.HeroPrefab,
            icon = picked.Icon,
            placedIcon = picked.Icon,
            kind = kind,
        };
        game.HeroRoster.Add(slot);
    }

    private bool CanAffordFixedCost()
    {
        return game.CitizenManager.CheckCanUseCitizen(citizenCost)
            && view.resourcesManager.CheckResources(resourceCost.ToNegatedCostArray());
    }

    // 자원/시민 변화, 모드 진입/종료 시 여기로 온다. Off 모드가 아니면(배치·재배치·제거 등) 무조건 비활성화.
    private void RefreshInteractable()
    {
        bool canCreate = view.IsOff && CanAffordFixedCost();
        meleeIcon.SetInteractable(canCreate);
        rangedIcon.SetInteractable(canCreate);
    }
}
