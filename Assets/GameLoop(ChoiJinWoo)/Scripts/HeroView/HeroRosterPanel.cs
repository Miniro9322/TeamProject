using UnityEngine;
using UnityEngine.EventSystems;

// 보유 영웅 목록 표시 전용. Available 아이콘 클릭은 배치 모드 진입, Placed 아이콘 클릭은 카메라 이동.
public class HeroRosterPanel : MonoBehaviour
{
    [SerializeField] private MapView view;
    [SerializeField] private MapGame game;
    [SerializeField] private CameraRig cameraRig;
    [SerializeField] private HeroRosterIcon iconPrefab;
    [SerializeField] private Transform container;
    [SerializeField] private HeroCombineManager combineManager;

    private void OnEnable()
    {
        game.HeroRoster.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        game.HeroRoster.Changed -= Refresh;
    }

    private void Refresh()
    {
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        foreach (HeroRosterEntry entry in game.HeroRoster.Entries)
        {
            HeroRosterIcon icon = Instantiate(iconPrefab, container);
            icon.Set(entry, OnIconClicked, OnIconDoubleClicked);

            //// 배치 중이면 실시간 값을, 제거되어 있으면 제거 시점에 저장해둔 값을 보여준다.
            //Hero placedHero = entry.PlacedUnit != null ? entry.PlacedUnit.GetComponent<Hero>() : null;
            //if (placedHero != null)
            //    icon.UpdateLevel(placedHero.StatLevel, placedHero.SkillLevel);
            //else
            //    icon.UpdateLevel(entry.StatLevel, entry.SkillLevel);
        }
    }

    private void OnIconClicked(HeroRosterEntry entry, HeroRosterIcon icon)
    {
        if (entry.State == HeroRosterState.Placed)
        {
            cameraRig.focus = entry.PlacedUnit.transform.position;
            cameraRig.ApplyNow();
            HeroSelectionService.Select(entry.PlacedUnit.GetComponent<Hero>());
            // 바깥 클릭으로 패널이 닫혀 있으면(activeSelf == false) 같은 영웅이라도 다시 열어야 한다.
            //if (!heroUpgradePanel.gameObject.activeSelf || currentObject != entry.PlacedUnit)
            //{
            //    currentObject = entry.PlacedUnit;
            //    heroUpgradePanel.gameObject.SetActive(true);
            //    heroUpgradePanel.InitHeroInfo(entry.PlacedUnit.GetComponent<Hero>());
            //    heroUpgradePanel.PositionAtIconY(icon, container);
            //}
        }
        else
        {
            view.SetHero(entry);
        }
    }

    // 로스터 아이콘을 더블클릭하면(배치 여부 상관없이) 그 영웅의 MergeKey로 바로 합성을 시도한다.
    private void OnIconDoubleClicked(HeroRosterEntry entry)
    {
        if (!entry.TryGetMergeKey(out MergeKey key)) return;
        combineManager.TryCombine(key);
    }
}
