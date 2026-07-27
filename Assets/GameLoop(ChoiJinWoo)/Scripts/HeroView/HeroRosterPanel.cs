using UnityEngine;

// 보유 영웅 목록 표시 전용. Available 아이콘 클릭은 배치 모드 진입, Placed 아이콘 클릭은 카메라 이동.
public class HeroRosterPanel : MonoBehaviour
{
    [SerializeField] private MapView view;
    [SerializeField] private MapGame game;
    [SerializeField] private CameraRig cameraRig;
    [SerializeField] private HeroRosterIcon iconPrefab;
    [SerializeField] private Transform container;

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
            icon.Set(entry, OnIconClicked);
        }
    }

    private void OnIconClicked(HeroRosterEntry entry)
    {
        if (entry.State == HeroRosterState.Placed)
        {
            cameraRig.focus = entry.PlacedUnit.transform.position;
            cameraRig.ApplyNow();
        }
        else
        {
            view.SetHero(entry);
        }
    }
}
