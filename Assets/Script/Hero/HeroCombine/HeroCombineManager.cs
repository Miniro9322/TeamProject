using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

// 같은 영웅 3개를 합성해 다음 티어 영웅 1개를 만드는 담당.
// 위치·Kind는 이어받지 않는다 — 결과 영웅은 HeroRoster에 Available 엔트리로만 추가되고,
// 실제 배치는 플레이어가 로스터에서 골라 타일을 클릭하는 기존 흐름(UnitPlacer.TryPlace)이 그대로 처리한다.
// 합성 후보/다음 티어 프리팹 정보는 HeroRegistry에서 받아오고, 여긴 합성 실행(제거/파괴/로스터 갱신)만 한다.
public class HeroCombineManager : MonoBehaviour
{
    [SerializeField] private HeroRegistry heroRegistry;
    [SerializeField] private MapGame game;

    // 특정 MergeKey로 합성 가능한(3개 이상 모인) 후보가 있는지.
    public bool TryGetCombinable(MergeKey key, out List<Hero> candidates)
    {
        return heroRegistry.TryGetCombinable(key, out candidates);
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
        if (!heroRegistry.TryGetNextTierPrefabs(tier, out List<GameObject> nextTierPrefabs))
            return false; // 최고 티어거나 매핑 데이터 없음

        GameObject nextTierPrefab = nextTierPrefabs[Random.Range(0, nextTierPrefabs.Count)];

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
