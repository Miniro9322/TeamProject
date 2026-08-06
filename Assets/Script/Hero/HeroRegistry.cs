using System.Collections.Generic;
using UnityEngine;

// HeroData/HeroPrefab(정적 데이터)과 현재 배치된 Hero 인스턴스(동적 상태)를 함께 보관하는 창구.
// HeroCombineManager는 여기서 후보/후보프리팹만 받아 합성 실행(제거/파괴/로스터 갱신)만 담당한다.
public class HeroRegistry : MonoBehaviour
{
    [SerializeField] private List<HeroData> datas;
    [SerializeField] private MapGame game;

    private readonly Dictionary<int, List<HeroData>> heroDatasByType = new();
    private readonly Dictionary<int, List<GameObject>> heroForTierPrefabs = new();
    public Dictionary<int, List<HeroData>> HeroDataToType => heroDatasByType;

    // HeroRoster.Entries를 MergeKey로 묶어둔 캐시. HeroRoster가 원본, 여긴 조회용 인덱스일 뿐.
    private readonly Dictionary<MergeKey, List<Hero>> heroesByKey = new();

    private void Awake()
    {
        foreach (HeroData data in datas)
        {
            if (heroDatasByType.ContainsKey(data.HeroType) == false)
                heroDatasByType[data.HeroType] = new List<HeroData>();
            heroDatasByType[data.HeroType].Add(data);

            if (data.HeroPrefab == null) continue; // 프리팹 참조가 끊긴 데이터는 합성 후보 풀에서 제외.

            if (heroForTierPrefabs.ContainsKey(data.Tier) == false)
                heroForTierPrefabs[data.Tier] = new List<GameObject>();
            heroForTierPrefabs[data.Tier].Add(data.HeroPrefab);
        }
    }

    // MapGame의 [Inject] Construct가 Awake 단계에 끝나므로, HeroRoster를 읽는 구독은 Start에서 시작한다.
    private void Start()
    {
        game.HeroRoster.Changed += RebuildHeroIndex;
        RebuildHeroIndex();
    }

    private void OnDestroy()
    {
        game.HeroRoster.Changed -= RebuildHeroIndex;
    }

    // 로스터가 바뀔 때마다(배치/제거/합성 등) 배치된 영웅들을 MergeKey로 다시 그룹핑한다.
    private void RebuildHeroIndex()
    {
        heroesByKey.Clear();

        foreach (HeroRosterEntry entry in game.HeroRoster.Entries)
        {
            if (entry.PlacedUnit == null) continue;
            if (!entry.PlacedUnit.TryGetComponent(out Hero hero)) continue;

            MergeKey key = hero.MergeKey;
            if (!heroesByKey.TryGetValue(key, out List<Hero> list))
            {
                list = new List<Hero>();
                heroesByKey.Add(key, list);
            }
            list.Add(hero);
        }
    }

    // 특정 MergeKey로 합성 가능한(3개 이상 모인) 후보가 있는지.
    public bool TryGetCombinable(MergeKey key, out List<Hero> candidates)
    {
        return heroesByKey.TryGetValue(key, out candidates) && candidates.Count >= 3;
    }

    // currentTier 영웅들을 합성했을 때 나올 수 있는 다음 티어(currentTier + 1) 프리팹 후보들.
    public bool TryGetNextTierPrefabs(int currentTier, out List<GameObject> prefabs)
    {
        return heroForTierPrefabs.TryGetValue(currentTier + 1, out prefabs) && prefabs.Count > 0;
    }
}
