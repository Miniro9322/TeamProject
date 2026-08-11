using System.Collections.Generic;
using UnityEngine;

public class ZoneEffectApplier
{
    private readonly DesertZone desertZone;
    private readonly MapBoard desertBoard;
    private readonly WindShelterData shelterData;
    private readonly Dictionary<Hero, Tile> tileByHero = new();
    private readonly Dictionary<Hero, IceZone> iceByHero = new();
    private readonly Dictionary<IceZone, object[]> iceSources = new();
    private readonly object[] desertSources;

    // 사막 정보와 효과 출처를 보관한다.
    public ZoneEffectApplier(
        DesertZone desertZone,
        MapBoard desertBoard,
        WindShelterData shelterData)
    {
        this.desertZone = desertZone;
        this.desertBoard = desertBoard;
        this.shelterData = shelterData;
        desertSources = CreateSources(desertZone.Debuffs);
    }

    // 영웅이 새 칸에 들어오면 해당 지대의 디버프를 적용한다.
    public void EnterZone(Hero hero, PlacementArea area)
    {
        ApplyIce(hero, area);
        ApplyDesert(hero, area);
    }

    // 영웅이 지대를 벗어나면 해당 지대가 건 디버프만 제거한다.
    public void ExitZone(Hero hero)
    {
        RemoveIce(hero);
        RemoveEffects(hero, desertZone.Debuffs, desertSources);
        tileByHero.Remove(hero);
        Debug.Log($"[Zone] {hero.name} 지대 이탈 - 효과 제거");
    }

    // 하루가 바뀌면 바람과 사막 디버프를 다시 계산한다.
    public void OnDayChanged()
    {
        Vector2Int wind = RandomDirection();
        desertZone.SetWindDirection(wind);
        RefreshDesert(wind);
        ReportWind();
    }

    // 현재 바람 방향을 로그로 출력한다.
    public void ReportWind()
    {
        Vector2Int wind = desertZone.WindDirection;
        string directionText = WindDirectionText.ReadDirection(wind);
        Debug.Log($"[Zone] 현재 바람={directionText} 벡터={wind}");
    }

    // 얼음 지대라면 설정된 디버프를 적용한다.
    private void ApplyIce(Hero hero, PlacementArea area)
    {
        IceZone iceZone = area.Board.GetComponent<IceZone>();
        if (iceZone == null)
        {
            return;
        }

        EnsureSources(iceZone);
        object[] sources = GetSources(iceZone);
        iceByHero[hero] = iceZone;
        RefreshIce(hero, area, iceZone, sources);
    }

    // 기존 얼음 효과를 정리하고 현재 보호 상태에 맞춰 다시 결정합니다.
    private static void RefreshIce(
        Hero hero,
        PlacementArea area,
        IceZone iceZone,
        object[] sources)
    {
        RemoveEffects(hero, iceZone.Debuffs, sources);
        bool protectedCell = CampfireQuery.HasProtected(iceZone.CampfireData, area.Cells);
        if (protectedCell)
        {
            Debug.Log($"[Zone] {hero.name} 모닥불 보호 - 얼음 효과 면역");
            return;
        }

        ApplyEffects(hero, iceZone.Debuffs, sources);
        Debug.Log($"[Zone] {hero.name} 얼음 지대 진입 - 효과 {iceZone.Debuffs.Length}개 적용");
    }

    // 사막 지대라면 위치를 저장하고 바람 노출을 계산한다.
    private void ApplyDesert(Hero hero, PlacementArea area)
    {
        if (area.Board != desertBoard)
        {
            return;
        }

        Tile tile = area.Board.Cells[area.Origin];
        tileByHero[hero] = tile;
        ApplyWind(hero, tile, desertZone.WindDirection);
    }

    // 사막에 있는 영웅들의 디버프를 새 바람 기준으로 갱신한다.
    private void RefreshDesert(Vector2Int wind)
    {
        foreach (KeyValuePair<Hero, Tile> heroTile in tileByHero)
        {
            ApplyWind(heroTile.Key, heroTile.Value, wind);
        }
    }

    // 기존 사막 디버프를 지우고 노출 상태에 맞게 다시 적용한다.
    private void ApplyWind(Hero hero, Tile tile, Vector2Int wind)
    {
        RemoveEffects(hero, desertZone.Debuffs, desertSources);
        ApplyExposed(hero, tile, wind);
        LogShelter(hero, tile, wind);
    }

    // 바람에 노출된 영웅에게 사막 디버프를 적용한다.
    private void ApplyExposed(Hero hero, Tile tile, Vector2Int wind)
    {
        if (IsUnsheltered(tile, wind))
        {
            ApplyEffects(hero, desertZone.Debuffs, desertSources);
        }
    }

    // 영웅의 현재 사막 노출 상태를 로그로 출력한다.
    private void LogShelter(Hero hero, Tile tile, Vector2Int wind)
    {
        Debug.Log($"[Zone] {hero.name} 사막 지대 - 바람({wind}) 기준 노출={IsUnsheltered(tile, wind)}");
    }

    // 타일이 현재 바람을 막지 못하는지 확인한다.
    private bool IsUnsheltered(Tile tile, Vector2Int wind)
    {
        WindShelter tileShelter = shelterData.ReadShelter(tile);
        return WindShelterQuery.IsSheltered(tileShelter, wind) == false;
    }

    // 디버프 목록을 영웅에게 차례대로 적용한다.
    private static void ApplyEffects(Hero hero, DebuffSO[] debuffs, object[] sources)
    {
        for (int effectIndex = 0; effectIndex < debuffs.Length; effectIndex++)
        {
            DebuffSO debuff = debuffs[effectIndex];
            DebuffApply.To(hero, debuff, hero.Buffs, durationOverride: float.PositiveInfinity, source: sources[effectIndex]);
        }
    }

    // 특정 지대가 영웅에게 건 디버프만 제거한다.
    private static void RemoveEffects(Hero hero, DebuffSO[] debuffs, object[] sources)
    {
        for (int effectIndex = 0; effectIndex < debuffs.Length; effectIndex++)
        {
            DotRegistry.Remove(hero, debuffs[effectIndex].type);
            hero.Buffs.RemoveBuffs(hero, sources[effectIndex]);
            DebuffEffectView.Remove(hero, debuffs[effectIndex].type, sources[effectIndex]);
        }
    }

    // 영웅에게 적용된 얼음 지대 디버프를 제거한다.
    private void RemoveIce(Hero hero)
    {
        if (!iceByHero.TryGetValue(hero, out IceZone iceZone))
        {
            return;
        }

        RemoveEffects(hero, iceZone.Debuffs, GetSources(iceZone));
        iceByHero.Remove(hero);
    }

    // 얼음 지대의 독립 출처 목록이 없으면 만든다.
    private void EnsureSources(IceZone iceZone)
    {
        if (!iceSources.ContainsKey(iceZone))
        {
            iceSources[iceZone] = CreateSources(iceZone.Debuffs);
        }
    }

    // 얼음 지대의 독립 출처 목록을 가져온다.
    private object[] GetSources(IceZone iceZone)
    {
        return iceSources[iceZone];
    }

    // 디버프마다 독립적인 적용 출처를 만든다.
    private static object[] CreateSources(DebuffSO[] debuffs)
    {
        object[] sources = new object[debuffs.Length];
        for (int sourceIndex = 0; sourceIndex < sources.Length; sourceIndex++)
        {
            sources[sourceIndex] = new object();
        }

        return sources;
    }

    // 네 방향 중 하나를 무작위로 골라 반환한다.
    private static Vector2Int RandomDirection()
    {
        int directionIndex = Random.Range(0, GridCalculator.Directions.Length);
        return GridCalculator.Directions[directionIndex];
    }
}
