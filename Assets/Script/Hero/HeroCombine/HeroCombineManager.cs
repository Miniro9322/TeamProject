using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public class HeroTierList
{
    public List<GameObject> heroPrefabs;
}

// 같은 영웅 3개를 합성해 다음 티어 영웅 1개를 만드는 담당.
// 위치·Kind는 이어받지 않는다 — 결과 영웅은 HeroRoster에 Available 엔트리로만 추가되고,
// 실제 배치는 플레이어가 로스터에서 골라 타일을 클릭하는 기존 흐름(UnitPlacer.TryPlace)이 그대로 처리한다.
public class HeroCombineManager : MonoBehaviour
{
    // index = 현재 Tier, 값 = 그 Tier에서 합성했을 때 나올 수 있는 다음 티어 프리팹 후보들.
    [SerializeField] private List<HeroTierList> heroForTierPrefabs;
    [SerializeField] private MapGame game;

    // HeroRoster.Entries를 MergeKey로 묶어둔 캐시. HeroRoster가 원본, 여긴 조회용 인덱스일 뿐.
    private readonly Dictionary<MergeKey, List<Hero>> heroesByKey = new();

    private void OnEnable()
    {
        game.HeroRoster.Changed += RebuildIndex;
        RebuildIndex();
    }

    private void OnDisable()
    {
        game.HeroRoster.Changed -= RebuildIndex;
    }

    // 로스터가 바뀔 때마다(배치/제거/합성 등) 배치된 영웅들을 MergeKey로 다시 그룹핑한다.
    private void RebuildIndex()
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

    // Key로 바로 합성 시도 — 그 Key로 모인 것 중 앞의 3개를 쓴다.
    public bool TryCombine(MergeKey key)
    {
        if (!TryGetCombinable(key, out List<Hero> candidates)) return false;
        return Combine(candidates.GetRange(0, 3));
    }

    // 정확히 이 3개로 합성 시도(테스트 UI용) — MergeKey 캐시를 거치지 않고 직접 검증한다.
    public bool TryCombine(List<Hero> heroes)
    {
        if (heroes == null || heroes.Count != 3) return false;

        MergeKey key = heroes[0].MergeKey;
        for (int i = 1; i < heroes.Count; i++)
            if (!heroes[i].MergeKey.Equals(key)) return false;

        return Combine(heroes);
    }

    private bool Combine(List<Hero> heroes)
    {
        if (game.Rule != null && !game.Rule.CanBuild) return false; // 밤에는 합성 불가

        int tier = heroes[0].MergeKey.Tier;
        if (tier < 0 || tier >= heroForTierPrefabs.Count || heroForTierPrefabs[tier].heroPrefabs.Count == 0)
            return false; // 최고 티어거나 매핑 데이터 없음

        HeroTierList nextTierPrefabs = heroForTierPrefabs[tier];
        GameObject nextTierPrefab = nextTierPrefabs.heroPrefabs[Random.Range(0, nextTierPrefabs.heroPrefabs.Count)];

        foreach (Hero hero in heroes)
        {
            HeroRosterLink link = hero.GetComponent<HeroRosterLink>();
            HeroRosterEntry entry = link != null ? link.Entry : null;

            if (game.Units.TryGetArea(hero.gameObject, out PlacementArea area))
            {
                AreaPlace.Remove(area);
                game.Units.Remove(hero.gameObject);
            }

            if (entry != null) game.HeroRoster.Remove(entry);

            HeroSelectionService.ClearIfSelected(hero);
            Destroy(hero.gameObject);
        }

        Placeable newSlot = new Placeable { label = nextTierPrefab.name, prefab = nextTierPrefab, kind = nextTierPrefab.GetComponent<Hero>().OccupantKind };
        game.HeroRoster.Add(newSlot);

        return true;
    }
}
