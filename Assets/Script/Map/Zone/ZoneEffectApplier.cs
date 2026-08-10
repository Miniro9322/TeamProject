using System.Collections.Generic;
using UnityEngine;

public class ZoneEffectApplier
{
    private readonly DesertZone desertZone;
    private readonly MapBoard desertBoard;
    private readonly WindShelterData shelterData;
    private readonly Dictionary<Hero, Tile> desertTileByHero = new();

    public ZoneEffectApplier(
        DesertZone desertZone,
        MapBoard desertBoard,
        WindShelterData shelterData)
    {
        this.desertZone = desertZone;
        this.desertBoard = desertBoard;
        this.shelterData = shelterData;
    }

    public void EnterZone(Hero hero, PlacementArea area)
    {
        ApplyIceZone(hero, area.Board);
        ApplyDesertZone(hero, area);
    }

    public void ExitZone(Hero hero)
    {
        hero.SC.RemoveModifier(this);
        desertTileByHero.Remove(hero);
        Debug.Log($"[Zone] {hero.name} 지대 이탈 - 효과 제거");
    }

    public void OnDayChanged()
    {
        Vector2Int windDirection = RandomDirection();
        desertZone.SetWindDirection(windDirection);
        RefreshDesertHeroes(windDirection);
        ReportWindDirection();
    }

    public void ReportWindDirection()
    {
        Vector2Int windDirection = desertZone.WindDirection;
        string directionText = WindDirectionText.ReadDirection(windDirection);
        Debug.Log($"[Zone] 현재 바람={directionText} 벡터={windDirection}");
    }

    private void ApplyIceZone(Hero hero, MapBoard board)
    {
        IceZone iceZone = board.GetComponent<IceZone>();
        if (iceZone != null)
        {
            ApplyEffects(hero.SC, iceZone.Effects);
            Debug.Log($"[Zone] {hero.name} 얼음 지대 진입 - 효과 {iceZone.Effects.Length}개 적용");
        }
    }

    private void ApplyDesertZone(Hero hero, PlacementArea area)
    {
        if (area.Board == desertBoard)
        {
            Tile desertTile = area.Board.Cells[area.Origin];
            desertTileByHero[hero] = desertTile;
            ApplyDesertEffect(hero, desertTile, desertZone.WindDirection);
        }
    }

    private void RefreshDesertHeroes(Vector2Int windDirection)
    {
        foreach (KeyValuePair<Hero, Tile> heroTile in desertTileByHero)
        {
            ApplyDesertEffect(heroTile.Key, heroTile.Value, windDirection);
        }
    }

    private void ApplyDesertEffect(Hero hero, Tile desertTile, Vector2Int windDirection)
    {
        hero.SC.RemoveModifier(this);
        WindShelter tileShelter = shelterData.ReadShelter(desertTile);
        bool isSheltered = WindShelterQuery.IsSheltered(tileShelter, windDirection);
        if (!isSheltered)
        {
            ApplyEffects(hero.SC, desertZone.Effects);
        }

        Debug.Log($"[Zone] {hero.name} 사막 지대 - 바람({windDirection}) 기준 면역={isSheltered}");
    }

    private void ApplyEffects(StatContainer statContainer, ZoneStatEffect[] effects)
    {
        foreach (ZoneStatEffect zoneEffect in effects)
        {
            Modifier statModifier = new Modifier(
                zoneEffect.modifierType,
                zoneEffect.amount,
                this);
            statContainer.AddModifier(zoneEffect.statType, statModifier);
        }
    }

    private Vector2Int RandomDirection()
    {
        int directionIndex = Random.Range(0, GridCalculator.Directions.Length);
        return GridCalculator.Directions[directionIndex];
    }
}
