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
    [SerializeField] private GameObject rosterPanel;
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

        meleeIcon.Set(false, () => OnCreate(meleeIcon, OccupantKind.MeleeHero));
        rangedIcon.Set(false, () => OnCreate(rangedIcon, OccupantKind.RangedHero));
        RefreshInteractable();
    }

    private void OnDisable()
    {
        game.CitizenManager.CitizenChanged -= RefreshInteractable;
        game.ResourcesManager.ProductUpdate -= RefreshInteractable;
        view.OnOffMode -= RefreshInteractable;
    }

    private void OnCreate(HeroCreateIcon icon, OccupantKind kind)
    {
        if (!view.IsOff || !CanAfford(icon)) return;
        if (!createManager.TryRollHero(kind, out HeroData picked)) return; // 비용은 결과가 나온 뒤에 낸다.

        game.CitizenManager.UseCitizenForHero(icon.CitizenCost);
        view.resourcesManager.ProductChanged(icon.ResourceCost.ToNegatedCostArray());

        Placeable slot = new Placeable
        {
            label = picked.HeroName,
            prefab = picked.HeroPrefab,
            kind = kind,
        };
        game.HeroRoster.Add(slot, picked);
        if (!rosterPanel.activeSelf) rosterPanel.SetActive(true);
    }

    private bool CanAfford(HeroCreateIcon icon)
    {
        return game.CitizenManager.CheckCanUseCitizen(icon.CitizenCost)
            && view.resourcesManager.CheckResources(icon.ResourceCost.ToNegatedCostArray());
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
