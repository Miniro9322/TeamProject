using System.Collections.Generic;
using UnityEngine;

// 지대(얼음/사막) 효과를 유닛 스탯에 걸고 떼는 역할
public class ZoneEffectApplier
{
    private readonly DesertZone desertZone;
    private readonly MapBoard desertBoard;

    // 사막 지대 안에 지금 있는 유닛과 칸 — 바람이 바뀌면 이걸 훑어 재계산.
    private readonly Dictionary<Hero, Vector2Int> desertHeroCells = new();

    public ZoneEffectApplier(DesertZone desertZone)
    {
        this.desertZone = desertZone;
        desertBoard = desertZone.GetComponent<MapBoard>();
    }

    public void EnterZone(Hero hero, PlacementArea area)
    {
        ApplyIceZone(hero, area.Board);
        ApplyDesertZone(hero, area);
    }

    public void ExitZone(Hero hero)
    {
        hero.SC.RemoveModifier(this);
        desertHeroCells.Remove(hero);
        Debug.Log($"[Zone] {hero.name} 지대 이탈 - 효과 제거");
    }

    // GameManager.ChangeToDay 구독용
    public void OnDayChanged()
    {
        desertZone.SetWindDirection(RandomDirection());
        RefreshDesertHeroes();
        Debug.Log($"[Zone] 날짜 변경 - 새 바람 방향 {desertZone.WindDirection}");
    }

    private void ApplyIceZone(Hero hero, MapBoard board)
    {
        IceZone zone = board.GetComponent<IceZone>();
        if (Exists(zone))
        {
            ApplyEffects(hero.SC, zone.Effects);
            Debug.Log($"[Zone] {hero.name} 얼음 지대 진입 - 효과 {zone.Effects.Length}개 적용");
        }
    }

    private void ApplyDesertZone(Hero hero, PlacementArea area)
    {
        if (IsDesertBoard(area.Board))
        {
            desertHeroCells[hero] = area.Origin;
            ApplyDesertEffectForHero(hero, area.Board, area.Origin, desertZone);
        }
    }

    // 이 보드가 생성자에서 받은 사막 보드와 같은지 확인
    private bool IsDesertBoard(MapBoard board)
    {
        return board == desertBoard;
    }

    private void RefreshDesertHeroes()
    {
        foreach (KeyValuePair<Hero, Vector2Int> entry in desertHeroCells)
        {
            entry.Key.SC.RemoveModifier(this);
            ApplyDesertEffectForHero(entry.Key, desertBoard, entry.Value, desertZone);
        }
    }

    private void ApplyDesertEffectForHero(Hero hero, MapBoard board, Vector2Int cell, DesertZone zone)
    {
        bool exposed = IsExposedToWind(board, cell, zone.WindDirection);
        Debug.Log($"[Zone] {hero.name} 사막 지대 - 바람({zone.WindDirection}) 기준 노출={exposed}");
        if (exposed)
        {
            ApplyEffects(hero.SC, zone.Effects);
        }
    }

    private void ApplyEffects(StatContainer stats, ZoneStatEffect[] effects)
    {
        foreach (ZoneStatEffect effect in effects)
        {
            Modifier modifier = new Modifier(effect.modifierType, effect.amount, this);
            stats.AddModifier(effect.statType, modifier);
        }
    }

    private bool IsExposedToWind(MapBoard board, Vector2Int cell, Vector2Int windDirection)
    {
        return !IsBlockedByWind(board, cell, windDirection);
    }

    // 유닛 칸에서 바람 불어오는 쪽으로 한 칸씩 훑는다. 고지를 먼저 만나면 차단, 보드 밖으로 나가면 노출.
    private bool IsBlockedByWind(MapBoard board, Vector2Int cell, Vector2Int windDirection)
    {
        Vector2Int scanCell = cell - windDirection;
        while (board.TryGetCell(scanCell, out Tile scanTile))
        {
            if (scanTile.IsHigh)
            {
                return true;
            }
            scanCell -= windDirection;
        }
        return false;
    }

    private Vector2Int RandomDirection()
    {
        int index = Random.Range(0, GridCalculator.Directions.Length);
        return GridCalculator.Directions[index];
    }

    private static bool Exists(Object candidate)
    {
        return candidate != null;
    }
}
