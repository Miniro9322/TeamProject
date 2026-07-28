using UnityEngine;
using UnityEngine.EventSystems;

// 보유 영웅 목록 표시 전용. Available 아이콘 클릭은 배치 모드 진입, Placed 아이콘 클릭은 카메라 이동.
public class HeroRosterPanel : MonoBehaviour, IBeginDragHandler, IScrollHandler
{
    [SerializeField] private MapView view;
    [SerializeField] private MapGame game;
    [SerializeField] private CameraRig cameraRig;
    [SerializeField] private HeroRosterIcon iconPrefab;
    [SerializeField] private Transform container;
    [SerializeField] private HeroUpgradePanel heroUpgradePanel;
    private GameObject currentObject = null;

    private void OnEnable()
    {
        game.HeroRoster.Changed += Refresh;
        Refresh();
        heroUpgradePanel.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        game.HeroRoster.Changed -= Refresh;
        currentObject = null;
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
            icon.Set(entry, OnIconClicked);
        }
    }

    private void OnIconClicked(HeroRosterEntry entry, HeroRosterIcon icon)
    {
        if (entry.State == HeroRosterState.Placed)
        {
            cameraRig.focus = entry.PlacedUnit.transform.position;
            cameraRig.ApplyNow();
            if (currentObject != entry.PlacedUnit)
            {
                currentObject = entry.PlacedUnit;
                heroUpgradePanel.gameObject.SetActive(true);
                heroUpgradePanel.InitHeroInfo(entry.PlacedUnit.GetComponent<Hero>());
                heroUpgradePanel.PositionAtIconY((RectTransform)icon.transform);
            }
        }
        else
        {
            view.SetHero(entry);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        heroUpgradePanel.gameObject.SetActive(false);
        currentObject = null;
    }

    public void OnScroll(PointerEventData eventData)
    {
        heroUpgradePanel.gameObject.SetActive(false);
        currentObject = null;
    }
}
