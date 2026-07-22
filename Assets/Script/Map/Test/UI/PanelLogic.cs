using System.Collections.Generic;
using UnityEngine;

public class PanelLogic : MonoBehaviour
{
    [SerializeField] private MapView game;
    [SerializeField] private MapCommand command;
    [SerializeField] private EnemyPathView enemyPath;

    private void Update()
    {
        if (game == null)
        {
            return;
        }

        Refresh();
    }

    private void PathClick()
    {
        enemyPath.RebuildPath();
    }

    private void PathToggle()
    {
        enemyPath.ToggleVisibility();
    }

    private void UnitClear()
    {
        command.action.ClearAllPlacedUnit();
    }

    private void RemoveClick()
    {
        game.SetRemove();
    }

    private void ReplaceClick()
    {
        game.SetReplace();
    }

    private void ModeClear()
    {
        game.ClearMode();
    }

    private void UnitClick(int index)
    {
        game.SetUnit(index);
    }

    private void Refresh()
    {
        TileInfo(game.Selected);
    }

    private static string TileInfo(Tile tile)
    {
        if (tile == null)
        {
            return "";
        }

        string unit = tile.State.Occupant == OccupantKind.None
            ? "없음"
            : tile.State.Occupant.ToString();

        return $"선택 타일 {tile.Coord}\n" +
               $"지형: {tile.Terrain}\n" +
               $"적 경로: {(tile.IsEnemyLane ? "포함" : "아님")}\n" +
               $"배치: {unit}\n" +
               $"적: {tile.EnemyCount}\n" +
               $"공격범위: {(tile.IsRangeCovered ? $"덮임({tile.RangeCoverCount})" : "없음")}\n" +
               $"저지: {tile.BlockedCount}/{tile.BlockCapacity}";
    }

    private static string[] UnitLabels(IReadOnlyList<Placeable> items)
    {
        string[] labels = new string[items.Count];
        for (int i = 0; i < items.Count; i++)
        {
            labels[i] = $"{items[i].label} ({i + 1})";
        }

        return labels;
    }
}
