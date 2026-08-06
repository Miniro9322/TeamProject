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
    public bool TryGetCombinable(MergeKey key, out List<HeroRosterEntry> candidates)
    {
        return heroRegistry.TryGetCombinable(key, out candidates);
    }

    // Key로 바로 합성 시도 — 그 Key로 모인 것 중 앞의 3개를 쓴다(로스터 전용 사본이 먼저 오도록 정렬돼 있음).
    public bool TryCombine(MergeKey key)
    {
        if (!TryGetCombinable(key, out List<HeroRosterEntry> candidates)) return false;
        return Combine(candidates.GetRange(0, 3));
    }

    // 정확히 이 3개로 합성 시도(테스트 UI용) — 맵에서 선택된 배치 영웅만 다루므로 MergeKey는 인스턴스에서 바로 읽는다.
    public bool TryCombine(List<Hero> heroes)
    {
        if (heroes == null || heroes.Count != 3) return false;

        MergeKey key = heroes[0].MergeKey;
        var entries = new List<HeroRosterEntry>(3);
        foreach (Hero hero in heroes)
        {
            if (!hero.MergeKey.Equals(key)) return false;
            if (!hero.TryGetComponent(out HeroRosterLink link) || link.Entry == null) return false;
            entries.Add(link.Entry);
        }

        return Combine(entries);
    }

    private bool Combine(List<HeroRosterEntry> entries)
    {
        if (game.Rule != null && !game.Rule.CanBuild) return false; // 밤에는 합성 불가
        if (!entries[0].TryGetMergeKey(out MergeKey key)) return false;

        int tier = key.Tier;
        if (!heroRegistry.TryGetNextTierHeroDatas(tier, out List<HeroData> nextTierDatas))
            return false; // 최고 티어거나 매핑 데이터 없음

        HeroData nextTierData = nextTierDatas[Random.Range(0, nextTierDatas.Count)];
        GameObject nextTierPrefab = nextTierData.HeroPrefab;

        foreach (HeroRosterEntry entry in entries)
        {
            GameObject unit = entry.PlacedUnit;
            if (unit != null)
            {
                if (unit.TryGetComponent(out Hero hero)) HeroSelectionService.ClearIfSelected(hero);

                if (game.Units.TryGetArea(unit, out PlacementArea area))
                {
                    AreaPlace.Remove(area);
                    game.Units.Remove(unit);
                }

                Destroy(unit);
            }

            game.HeroRoster.Remove(entry);
        }

        Placeable newSlot = new Placeable
        {
            label = nextTierPrefab.name,
            prefab = nextTierPrefab,
            icon = nextTierData.Icon,
            placedIcon = nextTierData.Icon,
            kind = nextTierPrefab.GetComponent<Hero>().OccupantKind,
        };
        game.HeroRoster.Add(newSlot);

        return true;
    }
}
