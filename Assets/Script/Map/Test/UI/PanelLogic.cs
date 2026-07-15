using System.Collections.Generic;
using UnityEngine;

public class PanelLogic : MonoBehaviour
{
    [SerializeField] private MapPanel panel;
    [SerializeField] private MapGame game;
    [SerializeField] private EnemyPathView enemyPath; // 경로 소유자(표시/재계산은 여기로)

    private readonly PanelData data = new();

    private void Awake()
    {
        if (panel != null)
        {
            panel.SetData(data);
        }
    }

    private void OnEnable()
    {
        if (panel == null)
        {
            return;
        }

        panel.PathClicked += PathClick;
        panel.PathToggle += PathToggle;
        panel.UnitCleared += UnitClear;
        panel.RemoveClicked += RemoveClick;
        panel.ModeCleared += ModeClear;
        panel.UnitClicked += UnitClick;
    }

    private void OnDisable()
    {
        if (panel == null)
        {
            return;
        }

        panel.PathClicked -= PathClick;
        panel.PathToggle -= PathToggle;
        panel.UnitCleared -= UnitClear;
        panel.RemoveClicked -= RemoveClick;
        panel.ModeCleared -= ModeClear;
        panel.UnitClicked -= UnitClick;

        if (game != null)
        {
            game.SetBlock(false);
        }
    }

    private void Update()
    {
        if (panel == null || game == null)
        {
            return;
        }

        game.SetBlock(panel.HasPointer);
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
        game.ClearPlaced();
    }

    private void RemoveClick()
    {
        game.SetRemove();
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
        data.Status = game.Status;
        data.TileText = TileInfo(game.Selected);
        data.Mode = game.Mode;
        data.ShowPath = enemyPath != null && enemyPath.PathVisible;
        data.UnitIndex = game.UnitIndex;
        data.Units = UnitLabels(game.Items);
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

    private static string[] UnitLabels(IReadOnlyList<MapGame.Placeable> items)
    {
        string[] labels = new string[items.Count];
        for (int i = 0; i < items.Count; i++)
        {
            labels[i] = $"{items[i].label} ({i + 1})";
        }

        return labels;
    }
}
