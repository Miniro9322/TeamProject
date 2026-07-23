using UnityEngine;

// 영웅 "생성" 전용 패널. 버튼을 수동으로 늘리지 않고, PlacePalette에 등록된 영웅 슬롯 수만큼 자동으로 만든다.
public class HeroSetPanel : MonoBehaviour
{
    [SerializeField] private MapView view;
    [SerializeField] private MapGame game;
    [SerializeField] private HeroCreateIcon iconPrefab;
    [SerializeField] private Transform container;

    private void OnEnable()
    {
        game.CitizenManager.CitizenChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        game.CitizenManager.CitizenChanged -= Refresh;
    }

    public void OnCreate(Placeable slot)
    {
        if (!view.CheckCanBuild(slot.label)) return;

        game.CitizenManager.UseCitizen(slot.prefab.GetComponent<Hero>().CitizenAmount);
        HeroRosterEntry entry = game.HeroRoster.Add(slot);
        view.SetHero(entry);   // 생성과 동시에 배치 모드로 진입(타일 클릭하면 바로 배치)
    }

    private void Refresh()
    {
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        byte unlocked = game.Rule.UnlockHero;

        foreach (Placeable slot in view.Items)
        {
            if (slot.kind != OccupantKind.MeleeHero && slot.kind != OccupantKind.RangedHero) continue;

            HeroType type = TypeOf(slot.label);
            if (((byte)type & unlocked) != (byte)type) continue;

            HeroCreateIcon icon = Instantiate(iconPrefab, container);
            icon.Set(slot, view.CheckCanBuild(slot.label), OnCreate, slot.label);
        }
    }

    // 해금 체크는 기존 방식(라벨 → HeroType) 그대로 유지.
    private static HeroType TypeOf(string label)
    {
        switch (label)
        {
            case "melee": return HeroType.SwordMan;
            case "Ranged": return HeroType.Archer;
            case "DualBlader": return HeroType.DualSwordMan;
            case "SpearMan": return HeroType.SpearMan;
            default: return 0;
        }
    }
}
