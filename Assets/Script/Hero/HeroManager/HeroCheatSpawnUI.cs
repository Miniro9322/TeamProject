using UnityEngine;

// 테스트/치트 전용: HeroRegistry에 등록된 모든 영웅을 아이콘 리스트로 보여주고,
// 클릭한 영웅을 시민/자원 비용 없이 즉시 로스터에 추가한다(정상 생성 흐름인 HeroCreateManager.TryRollHero 우회).
public class HeroCheatSpawnUI : MonoBehaviour
{
    [SerializeField] private HeroRegistry registry;
    [SerializeField] private MapGame game;
    [SerializeField] private Transform listContent;
    [SerializeField] private HeroCheatListItem itemPrefab;
    [SerializeField] private GameObject rosterPanel;

    private void OnEnable()
    {
        BuildList();
    }

    private void OnDisable()
    {
        ClearList();
    }

    private void Start()
    {
        game.Rule.ChangeToNight += CloseOnNight;
    }

    private void OnDestroy()
    {
        game.Rule.ChangeToNight -= CloseOnNight;
    }

    private void CloseOnNight()
    {
        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }

    // 별도 버튼의 OnClick에 연결해서 이 치트 패널을 열고 닫는다.
    public void ToggleVisible()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }

    private void BuildList()
    {
        ClearList();
        foreach (HeroData data in registry.AllHeroDatas)
        {
            HeroCheatListItem item = Instantiate(itemPrefab, listContent);
            item.Setup(data, OnHeroItemClicked);
        }
    }

    private void ClearList()
    {
        for (int i = listContent.childCount - 1; i >= 0; i--)
            Destroy(listContent.GetChild(i).gameObject);
    }

    private void OnHeroItemClicked(HeroData picked)
    {
        if (picked.HeroPrefab == null)
        {
            return;
        }

        OccupantKind kind = picked.HeroType == 1 ? OccupantKind.RangedHero : OccupantKind.MeleeHero;
        Placeable slot = new Placeable
        {
            label = picked.HeroName,
            prefab = picked.HeroPrefab,
            kind = kind,
        };
        game.HeroRoster.Add(slot, picked);
        if (!rosterPanel.activeSelf) rosterPanel.SetActive(true);
    }
}
