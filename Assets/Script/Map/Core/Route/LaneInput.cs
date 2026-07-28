using System.Collections.Generic;
using UnityEngine;

public class LaneInput
{
    private readonly Dictionary<Vector2Int, Tile> cells;
    private readonly List<Tile> spawns;
    private readonly List<Tile> cores;

    public IReadOnlyDictionary<Vector2Int, Tile> Cells => cells;
    public IReadOnlyList<Tile> Spawns => spawns;
    public IReadOnlyList<Tile> Cores => cores;

    // 셀과 스폰 및 코어 목록을 복사해 경로 계산용 입력을 구성합니다.
    public LaneInput(
        IReadOnlyDictionary<Vector2Int, Tile> source,
        IReadOnlyList<Tile> starts,
        IReadOnlyList<Tile> goals)
    {
        cells = new Dictionary<Vector2Int, Tile>();
        spawns = new List<Tile>();
        cores = new List<Tile>();

        CopyCells(source);
        CopyTiles(starts, spawns);
        CopyTiles(goals, cores);
    }

    // 원본 셀 좌표와 타일 참조를 내부 딕셔너리에 복사합니다.
    private void CopyCells(IReadOnlyDictionary<Vector2Int, Tile> source)
    {
        foreach (KeyValuePair<Vector2Int, Tile> pair in source)
        {
            cells[pair.Key] = pair.Value;
        }
    }

    // 원본 타일 참조를 대상 목록에 순서대로 복사합니다.
    private static void CopyTiles(IReadOnlyList<Tile> source, List<Tile> target)
    {
        for (int i = 0; i < source.Count; i++)
        {
            target.Add(source[i]);
        }
    }
}
