using System.Collections.Generic;
using UnityEngine;

// 고정된 사막 맵을 읽어 모든 타일의 방향별 바람 면역 결과를 완성합니다.
public class WindShelterCalc
{
    private readonly List<Tile> highTiles = new();
    private readonly List<Tile> groundTiles = new();
    private readonly Dictionary<int, int> leftHighByRow = new();
    private readonly Dictionary<int, int> rightHighByRow = new();
    private readonly Dictionary<int, int> bottomHighByColumn = new();
    private readonly Dictionary<int, int> topHighByColumn = new();

    private int minimumColumn = int.MaxValue;
    private int maximumColumn = int.MinValue;
    private int minimumRow = int.MaxValue;
    private int maximumRow = int.MinValue;

    public WindShelterData BuildData(IReadOnlyDictionary<Vector2Int, Tile> cells)
    {
        WindShelterData resultData = new();
        CollectTiles(cells, resultData);
        CollectHighs();
        ResolveGrounds(resultData);
        return resultData;
    }

    private void CollectTiles(
        IReadOnlyDictionary<Vector2Int, Tile> cells,
        WindShelterData resultData)
    {
        foreach (Tile tile in cells.Values)
        {
            resultData.KeepShelter(tile, WindShelter.NoShelter);
            KeepBounds(tile.Coord);
            KeepTileType(tile);
        }
    }

    private void KeepBounds(Vector2Int cell)
    {
        minimumColumn = Mathf.Min(minimumColumn, cell.x);
        maximumColumn = Mathf.Max(maximumColumn, cell.x);
        minimumRow = Mathf.Min(minimumRow, cell.y);
        maximumRow = Mathf.Max(maximumRow, cell.y);
    }

    private void KeepTileType(Tile tile)
    {
        if (tile.Terrain == TerrainType.High)
        {
            highTiles.Add(tile);
        }

        if (tile.Terrain == TerrainType.Ground)
        {
            groundTiles.Add(tile);
        }
    }

    private void CollectHighs()
    {
        for (int highIndex = 0; highIndex < highTiles.Count; highIndex++)
        {
            Tile highTile = highTiles[highIndex];
            if (IsInnerCell(highTile.Coord))
            {
                KeepHigh(highTile.Coord);
            }
        }
    }

    private bool IsInnerCell(Vector2Int cell)
    {
        return cell.x > minimumColumn
            && cell.x < maximumColumn
            && cell.y > minimumRow
            && cell.y < maximumRow;
    }

    private void KeepHigh(Vector2Int cell)
    {
        KeepMinimum(leftHighByRow, cell.y, cell.x);
        KeepMaximum(rightHighByRow, cell.y, cell.x);
        KeepMinimum(bottomHighByColumn, cell.x, cell.y);
        KeepMaximum(topHighByColumn, cell.x, cell.y);
    }

    private static void KeepMinimum(Dictionary<int, int> values, int line, int position)
    {
        if (!values.TryGetValue(line, out int current) || position < current)
        {
            values[line] = position;
        }
    }

    private static void KeepMaximum(Dictionary<int, int> values, int line, int position)
    {
        if (!values.TryGetValue(line, out int current) || position > current)
        {
            values[line] = position;
        }
    }

    private void ResolveGrounds(WindShelterData resultData)
    {
        for (int groundIndex = 0; groundIndex < groundTiles.Count; groundIndex++)
        {
            Tile groundTile = groundTiles[groundIndex];
            WindShelter shelter = ResolveShelter(groundTile.Coord);
            resultData.KeepShelter(groundTile, shelter);
        }
    }

    private WindShelter ResolveShelter(Vector2Int cell)
    {
        WindShelter shelter = WindShelter.NoShelter;

        if (HasLowerHigh(leftHighByRow, cell.y, cell.x))
        {
            shelter |= WindShelter.FromWest;
        }

        if (HasHigherHigh(rightHighByRow, cell.y, cell.x))
        {
            shelter |= WindShelter.FromEast;
        }

        if (HasLowerHigh(bottomHighByColumn, cell.x, cell.y))
        {
            shelter |= WindShelter.FromSouth;
        }

        if (HasHigherHigh(topHighByColumn, cell.x, cell.y))
        {
            shelter |= WindShelter.FromNorth;
        }

        return shelter;
    }

    private static bool HasLowerHigh(Dictionary<int, int> values, int line, int position)
    {
        return values.TryGetValue(line, out int highPosition) && highPosition < position;
    }

    private static bool HasHigherHigh(Dictionary<int, int> values, int line, int position)
    {
        return values.TryGetValue(line, out int highPosition) && highPosition > position;
    }
}
