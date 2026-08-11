using System.Collections.Generic;
using UnityEngine;

public class ZoneEffectApplier
{
    private readonly DesertZone desertZone;
    private readonly MapBoard desertBoard;
    private readonly WindShelterData shelterData;
    private readonly Dictionary<Hero, Tile> tileByHero = new();

    public ZoneEffectApplier(
        DesertZone desertZone,
        MapBoard desertBoard,
        WindShelterData shelterData)
    {
        this.desertZone = desertZone;
        this.desertBoard = desertBoard;
        this.shelterData = shelterData;
    }

    // 영웅이 새 칸에 들어왔을 때, 얼음/사막 지대 효과를 걸어준다.
    public void EnterZone(Hero hero, PlacementArea area)
    {
        ApplyIceZone(hero, area.Board);
        ApplyDesertZone(hero, area);
    }

    // 영웅이 지대를 벗어났을 때 걸려있던 효과를 없앤다.
    public void ExitZone(Hero hero)
    {
        hero.SC.RemoveModifier(this);
        tileByHero.Remove(hero);
        Debug.Log($"[Zone] {hero.name} 지대 이탈 - 효과 제거");
    }

    // 하루가 바뀌면 바람 방향을 새로 뽑고, 사막에 있는 영웅들 효과를 다시 계산한다.
    public void OnDayChanged()
    {
        Vector2Int wind = RandomDirection();
        desertZone.SetWindDirection(wind);
        RefreshDesertHeroes(wind);
        ReportWindDirection();
    }

    // 지금 바람이 어느 방향으로 부는지 로그로 알려준다.
    public void ReportWindDirection()
    {
        Vector2Int wind = desertZone.WindDirection;
        string directionText = WindDirectionText.ReadDirection(wind);
        Debug.Log($"[Zone] 현재 바람={directionText} 벡터={wind}");
    }

    // 얼음 지대 위라면 그 효과를 영웅에게 건다.
    private void ApplyIceZone(Hero hero, MapBoard board)
    {
        IceZone iceZone = board.GetComponent<IceZone>();
        if (iceZone != null)
        {
            ApplyEffects(hero, iceZone.Effects);
            Debug.Log($"[Zone] {hero.name} 얼음 지대 진입 - 효과 {iceZone.Effects.Length}개 적용");
        }
    }

    // 사막 지대 위라면 위치를 기억해두고 바람 효과를 계산한다.
    private void ApplyDesertZone(Hero hero, PlacementArea area)
    {
        if (area.Board == desertBoard)
        {
            Tile tile = area.Board.Cells[area.Origin];
            tileByHero[hero] = tile;
            ApplyDesertEffect(hero, tile, desertZone.WindDirection);
        }
    }

    // 사막에 있는 모든 영웅의 효과를 새 바람 방향 기준으로 다시 계산한다.
    private void RefreshDesertHeroes(Vector2Int wind)
    {
        foreach (KeyValuePair<Hero, Tile> heroTile in tileByHero)
        {
            ApplyDesertEffect(heroTile.Key, heroTile.Value, wind);
        }
    }

    // 기존 사막 효과를 지우고, 바람에 노출됐으면 다시 걸고 로그를 남긴다.
    private void ApplyDesertEffect(Hero hero, Tile tile, Vector2Int wind)
    {
        hero.SC.RemoveModifier(this);
        ApplyIfUnsheltered(hero, tile, wind);
        LogShelterState(hero, tile, wind);
    }

    // 바람을 막아줄 게 없으면 사막 효과를 건다.
    private void ApplyIfUnsheltered(Hero hero, Tile tile, Vector2Int wind)
    {
        if (IsUnsheltered(tile, wind))
        {
            ApplyEffects(hero, desertZone.Effects);
        }
    }

    // 지금 이 영웅이 바람을 맞고 있는지 로그로 남긴다.
    private void LogShelterState(Hero hero, Tile tile, Vector2Int wind)
    {
        Debug.Log($"[Zone] {hero.name} 사막 지대 - 바람({wind}) 기준 노출={IsUnsheltered(tile, wind)}");
    }

    // 사막 지대 안이라도 바람 반대쪽에 High 타일이 있으면 가려져서 노출 안 됨.
    private bool IsUnsheltered(Tile tile, Vector2Int wind)
    {
        WindShelter tileShelter = shelterData.ReadShelter(tile);
        return WindShelterQuery.IsSheltered(tileShelter, wind) == false;
    }

    // 효과 목록을 하나씩 이 영웅에게 적용한다.
    private void ApplyEffects(Hero hero, ZoneStatEffect[] effects)
    {
        foreach (ZoneStatEffect zoneEffect in effects)
        {
            ApplyEffect(hero, zoneEffect);
        }
    }

    // 스탯 값을 적용 전/후로 재서 로그까지 남긴다.
    private void ApplyEffect(Hero hero, ZoneStatEffect zoneEffect)
    {
        float beforeValue = hero.SC.GetValue(zoneEffect.statType);
        AddZoneModifier(hero.SC, zoneEffect);
        float afterValue = hero.SC.GetValue(zoneEffect.statType);
        LogEffectApplied(hero, zoneEffect.statType, beforeValue, afterValue);
    }

    // 지대 효과 하나를 실제로 스탯에 건다.
    private void AddZoneModifier(StatContainer statContainer, ZoneStatEffect zoneEffect)
    {
        Modifier statModifier = new Modifier(
            ModifierType.Additive,
            zoneEffect.percentAmount / 100f,
            this);
        statContainer.AddModifier(zoneEffect.statType, statModifier);
    }

    // 이 스탯이 얼마에서 얼마로 바뀌었는지 로그로 남긴다.
    private void LogEffectApplied(Hero hero, StatType statType, float beforeValue, float afterValue)
    {
        Debug.Log($"[Zone] {hero.name} {statType} {beforeValue} → {afterValue}");
    }

    // 네 방향 중 하나를 무작위로 골라 돌려준다.
    private Vector2Int RandomDirection()
    {
        int directionIndex = Random.Range(0, GridCalculator.Directions.Length);
        return GridCalculator.Directions[directionIndex];
    }
}
