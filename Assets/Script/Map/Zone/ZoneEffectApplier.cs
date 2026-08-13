using System;
using System.Collections.Generic;
using UnityEngine;

public class ZoneEffectApplier : IDisposable
{
    private readonly DesertZone desertZone;
    private readonly MapBoard desertBoard;
    private readonly WindShelterData shelterData;
    private readonly WindwallData windwallData;
    private readonly PlacedUnitData unitList;
    private readonly WindPreview preview;
    private readonly DesertLineEffect lineEffect;
    private readonly Dictionary<Hero, IceZone> iceByHero = new();
    private readonly List<Hero> iceHeroes = new();
    private readonly Dictionary<IceZone, object[]> iceSources = new();
    private readonly object[] desertSources;
    private bool isNight;

    // 지대 효과에 필요한 고정 정보와 현재 배치 장부를 보관합니다.
    public ZoneEffectApplier(
        DesertZone desertZone,
        MapBoard desertBoard,
        WindShelterData shelterData,
        WindwallData windwallData,
        PlacedUnitData unitList,
        WindPreview preview,
        DesertLineEffect lineEffect)
    {
        this.desertZone = desertZone;
        this.desertBoard = desertBoard;
        this.shelterData = shelterData;
        this.windwallData = windwallData;
        this.unitList = unitList;
        this.preview = preview;
        this.lineEffect = lineEffect;
        desertSources = CreateSources(desertZone.Debuffs);
    }

    // 영웅이 배치되면 얼음 지대 소속만 기록하고, 디버프는 밤일 때만 적용됩니다.
    public void EnterZone(Hero hero, PlacementArea area)
    {
        ApplyIce(hero, area);
    }

    // 영웅이 지대를 벗어나면 해당 지대가 건 효과만 제거합니다.
    public void ExitZone(Hero hero)
    {
        RemoveIce(hero);
        RemoveEffects(hero, desertZone.Debuffs, desertSources);
        Debug.Log($"[Zone] {hero.name} 지대 이탈 - 효과 제거");
    }

    // 낮이 시작되면 지난 사막 효과를 지우고 새 바람과 화살표를 준비하며, 얼음 디버프도 걷어냅니다.
    public void OnDayChanged()
    {
        RemoveDesert();
        lineEffect.Hide();
        Vector2Int wind = WindPicker.Next(desertZone.WindDirection);
        desertZone.SetWindDirection(wind);
        preview.Show(wind);
        ReportWind();

        isNight = false;
        RemoveAllIce();
    }

    // 밤이 시작되면 화살표를 숨기고 현재 배치를 한 번 읽어 사막 효과를 확정하며, 얼음 지대도 다시 판정합니다.
    public void OnNightChanged()
    {
        preview.Hide();
        DesertUnits units = ReadUnits();
        ApplyNight(units, desertZone.WindDirection);

        isNight = true;
        ApplyAllIce();
    }

    // 현재 바람 방향을 로그로 출력합니다.
    public void ReportWind()
    {
        Vector2Int wind = desertZone.WindDirection;
        string directionText = WindDirectionText.ReadDirection(wind);
        Debug.Log($"[Zone] 현재 바람={directionText} 벡터={wind}");
    }

    // 생성한 낮 안내 화살표 자원을 정리합니다.
    public void Dispose()
    {
        preview.Dispose();
        lineEffect.Hide();
    }

    // 얼음 지대라면 소속을 기록해두고, 밤일 때만 디버프를 실제로 적용합니다.
    private void ApplyIce(Hero hero, PlacementArea area)
    {
        if (!area.Board.TryGetComponent(out IceZone iceZone))
        {
            return;
        }

        EnsureSources(iceZone);
        if (!iceByHero.ContainsKey(hero))
        {
            iceHeroes.Add(hero);
        }

        iceByHero[hero] = iceZone;

        if (!isNight)
        {
            Debug.Log($"[Zone] {hero.name} 얼음 지대 진입 - 낮이라 효과 보류");
            return;
        }

        object[] sources = GetSources(iceZone);
        RefreshIce(hero, area, iceZone, sources);
    }

    // 밤이 시작되면 얼음 지대에 서 있는 영웅 전원을 그 순간 자리 기준으로 다시 판정합니다.
    private void ApplyAllIce()
    {
        for (int heroIndex = 0; heroIndex < iceHeroes.Count; heroIndex++)
        {
            Hero hero = iceHeroes[heroIndex];
            IceZone iceZone = iceByHero[hero];
            if (!unitList.TryGetArea(hero.gameObject, out PlacementArea area))
            {
                continue;
            }

            RefreshIce(hero, area, iceZone, GetSources(iceZone));
        }
    }

    // 낮이 시작되면 얼음 지대에 서 있는 영웅 전원의 디버프만 걷어냅니다
    private void RemoveAllIce()
    {
        for (int heroIndex = 0; heroIndex < iceHeroes.Count; heroIndex++)
        {
            Hero hero = iceHeroes[heroIndex];
            IceZone iceZone = iceByHero[hero];
            RemoveEffects(hero, iceZone.Debuffs, GetSources(iceZone));
        }
    }

    // 기존 얼음 효과를 정리하고 현재 모닥불 보호 상태에 맞춰 다시 결정합니다.
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

    // 현재 배치 장부에서 사막 보드의 영웅과 한 칸 좌표를 한 번 모읍니다.
    private DesertUnits ReadUnits()
    {
        DesertUnits units = new();
        for (int unitIndex = 0; unitIndex < unitList.Count; unitIndex++)
        {
            GameObject unit = unitList.UnitAt(unitIndex);
            PlacementArea area = unitList.AreaAt(unitIndex);
            KeepDesert(units, unit, area);
        }

        return units;
    }

    // 사막 보드에 배치된 영웅만 밤 계산 목록에 보관합니다.
    private void KeepDesert(DesertUnits units, GameObject unit, PlacementArea area)
    {
        if (area.Board != desertBoard)
        {
            return;
        }

        if (!unit.TryGetComponent(out Hero hero))
        {
            return;
        }

        if (!desertBoard.TryGetCell(area.Origin, out Tile tile))
        {
            return;
        }

        units.Heroes.Add(hero);
        units.Tiles.Add(tile);
        units.Cells.Add(area.Origin);
    }

    // 같은 밤의 고정 유닛 가림막을 만들고 각 영웅의 노출 결과를 적용합니다.
    private void ApplyNight(DesertUnits units, Vector2Int wind)
    {
        UnitShelter unitShelter = new(units.Cells);
        lineEffect.Show(wind, unitShelter);
        for (int unitIndex = 0; unitIndex < units.Heroes.Count; unitIndex++)
        {
            Hero hero = units.Heroes[unitIndex];
            Tile tile = units.Tiles[unitIndex];
            Vector2Int cell = units.Cells[unitIndex];
            ApplyWind(hero, tile, cell, wind, unitShelter);
        }
    }

    // 한 영웅의 고지·유닛·가림막 보호 결과에 맞춰 사막 효과를 적용합니다.
    private void ApplyWind(
        Hero hero,
        Tile tile,
        Vector2Int cell,
        Vector2Int wind,
        UnitShelter unitShelter)
    {
        bool unsheltered = DesertShelterQuery.IsUnsheltered(shelterData, unitShelter, windwallData, tile, wind);
        ApplyUnsheltered(hero, unsheltered);
        LogShelter(hero, cell, wind, unsheltered);
    }

    // 가려지지 않은 영웅에게만 사막 디버프를 무한 시간으로 적용합니다.
    private void ApplyUnsheltered(Hero hero, bool unsheltered)
    {
        if (!unsheltered)
        {
            return;
        }

        ApplyEffects(hero, desertZone.Debuffs, desertSources);
    }

    // 영웅의 확정된 밤 노출 상태를 로그로 출력합니다.
    private static void LogShelter(
        Hero hero,
        Vector2Int cell,
        Vector2Int wind,
        bool unsheltered)
    {
        Debug.Log($"[Zone] {hero.name} 사막 밤 - 좌표={cell} 바람={wind} 노출={unsheltered}");
    }

    // 현재 사막 영웅에게 이 지대가 건 디버프를 제거합니다.
    private void RemoveDesert()
    {
        DesertUnits units = ReadUnits();
        for (int unitIndex = 0; unitIndex < units.Heroes.Count; unitIndex++)
        {
            RemoveEffects(units.Heroes[unitIndex], desertZone.Debuffs, desertSources);
        }
    }

    // 디버프 목록을 영웅에게 출처별로 적용합니다.
    private static void ApplyEffects(Hero hero, DebuffSO[] debuffs, object[] sources)
    {
        for (int effectIndex = 0; effectIndex < debuffs.Length; effectIndex++)
        {
            DebuffSO debuff = debuffs[effectIndex];
            DebuffApply.To(hero, debuff, hero.Buffs, durationOverride: float.PositiveInfinity, source: sources[effectIndex]);
        }
    }

    // 특정 지대가 영웅에게 건 디버프만 제거합니다.
    private static void RemoveEffects(Hero hero, DebuffSO[] debuffs, object[] sources)
    {
        for (int effectIndex = 0; effectIndex < debuffs.Length; effectIndex++)
        {
            DotRegistry.Remove(hero, debuffs[effectIndex].type);
            hero.Buffs.RemoveBuffs(hero, sources[effectIndex]);
            DebuffEffectView.Remove(hero, debuffs[effectIndex].type, sources[effectIndex]);
        }
    }

    // 영웅에게 적용한 얼음 지대 디버프를 제거합니다.
    private void RemoveIce(Hero hero)
    {
        if (!iceByHero.TryGetValue(hero, out IceZone iceZone))
        {
            return;
        }

        RemoveEffects(hero, iceZone.Debuffs, GetSources(iceZone));
        iceByHero.Remove(hero);
        iceHeroes.Remove(hero);
    }

    // 얼음 지대의 독립적인 적용 출처가 없으면 만듭니다.
    private void EnsureSources(IceZone iceZone)
    {
        if (!iceSources.ContainsKey(iceZone))
        {
            iceSources[iceZone] = CreateSources(iceZone.Debuffs);
        }
    }

    // 얼음 지대의 적용 출처 목록을 가져옵니다.
    private object[] GetSources(IceZone iceZone)
    {
        return iceSources[iceZone];
    }

    // 디버프마다 독립적인 적용 출처를 만듭니다.
    private static object[] CreateSources(DebuffSO[] debuffs)
    {
        object[] sources = new object[debuffs.Length];
        for (int sourceIndex = 0; sourceIndex < sources.Length; sourceIndex++)
        {
            sources[sourceIndex] = new object();
        }

        return sources;
    }

    private sealed class DesertUnits
    {
        public readonly List<Hero> Heroes = new();
        public readonly List<Tile> Tiles = new();
        public readonly List<Vector2Int> Cells = new();
    }
}
