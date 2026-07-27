using System.Collections.Generic;
using UnityEngine;

// 영웅 "생성" 전용 패널. 버튼을 수동으로 늘리지 않고, PlacePalette에 등록된 영웅 슬롯 수만큼 자동으로 만든다.
// 아이콘은 패널이 켜질 때 한 번만 만들고, 자원/시민 변화에는 파괴/재생성 없이 interactable만 갱신한다.
public class HeroSetPanel : MonoBehaviour
{
    [SerializeField] private MapView view;
    [SerializeField] private MapGame game;
    [SerializeField] private HeroCreateIcon iconPrefab;
    [SerializeField] private Transform container;
    [SerializeField] private GameObject HeroInfoPanel;

    private readonly Dictionary<Placeable, HeroCreateIcon> icons = new();
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
        game.CitizenManager.CitizenChanged += RefreshInteractable;
        game.ResourcesManager.ProductUpdate += RefreshInteractable;
        game.Ui.UnlockChanged += AddNewlyUnlockedIcons;
        view.OnOffMode += RefreshInteractable;
        AddNewlyUnlockedIcons();
    }

    private void OnDisable()
    {
        game.CitizenManager.CitizenChanged -= RefreshInteractable;
        game.ResourcesManager.ProductUpdate -= RefreshInteractable;
        game.Ui.UnlockChanged -= AddNewlyUnlockedIcons;
        view.OnOffMode -= RefreshInteractable;
    }

    public void OnCreate(Placeable slot)
    {
        if (!view.CheckCanBuild(slot.label)) return;

        game.CitizenManager.UseCitizen(slot.prefab.GetComponent<Hero>().CitizenAmount);
        view.resourcesManager.ProductChanged(slot.prefab.GetComponent<Hero>().Cost);
        HeroRosterEntry entry = game.HeroRoster.Add(slot);
        view.SetHero(entry);   // 생성과 동시에 배치 모드로 진입(타일 클릭하면 바로 배치)
    }

    // 해금된 영웅 슬롯 중 아직 아이콘이 없는 것만 만든다(이미 만든 아이콘은 안 건드림).
    // OnEnable 최초 빌드와, 실시간 해금(UnlockChanged) 둘 다 이걸 그대로 쓴다.
    private void AddNewlyUnlockedIcons()
    {
        byte unlocked = game.Ui.UnlockedHero;

        foreach (Placeable slot in view.Items)
        {
            if (icons.ContainsKey(slot)) continue;
            if (slot.kind != OccupantKind.MeleeHero && slot.kind != OccupantKind.RangedHero) continue;

            HeroType type = TypeOf(slot.label);
            if (((byte)type & unlocked) != (byte)type) continue;

            HeroCreateIcon icon = Instantiate(iconPrefab, container);
            icon.Set(slot, view.IsOff && view.CheckCanBuild(slot.label), OnCreate, slot.label);
            icons[slot] = icon;
        }
    }

    // 자원/시민 변화, 모드 진입/종료 시 여기로 온다 — 아이콘을 새로 만들지 않고 interactable만 갱신한다.
    // Off 모드가 아니면(배치·재배치·제거 등) 무조건 비활성화(자원/시민 여유가 있어도 또 생성 못 하게).
    private void RefreshInteractable()
    {
        bool off = view.IsOff;
        foreach (var kv in icons)
        {
            kv.Value.SetInteractable(off && view.CheckCanBuild(kv.Key.label));
        }
    }

    // 해금 체크는 기존 방식(라벨 → HeroType) 그대로 유지.
    private static HeroType TypeOf(string label)
    {
        switch (label)
        {
            case "SwordMan": return HeroType.SwordMan;
            case "Archer": return HeroType.Archer;
            case "DualSwordMan": return HeroType.DualSwordMan;
            case "SpearMan": return HeroType.SpearMan;
            case "Mage": return HeroType.Mage;
            case "THS": return HeroType.THS;
            default: return 0;
        }
    }
}
