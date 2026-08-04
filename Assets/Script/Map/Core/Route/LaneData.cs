using System.Collections.Generic;
using UnityEngine;

public class LaneData
{
    private readonly List<Tile> tiles;

    public Tile Start { get; }
    public Tile Goal { get; }

    // 이 레인의 신원. 계산 단계가 찾아낸 지정 항목을 그대로 넘겨받는다 — 좌표로 다시 조회하지 않는다.
    public RouteData Route { get; }

    public IReadOnlyList<Tile> Tiles => tiles;
    public bool IsValid => Goal != null && tiles.Count > 0;

    // 스폰과 코어 및 순서가 정해진 경로 타일을 레인 결과로 보관합니다.
    public LaneData(Tile start, Tile goal, RouteData route, IReadOnlyList<Tile> source)
    {
        Start = start;
        Goal = goal;
        Route = route;
        tiles = new List<Tile>();
        
        for (int i = 0; i < source.Count; i++)
        {
            tiles.Add(source[i]);
        }
    }

    // 경로 타일을 지정 높이가 적용된 월드 좌표 목록으로 변환합니다.
    public List<Vector3> GetPoints(float yOffset)
    {
        var points = new List<Vector3>(tiles.Count);
        Vector3 lift = Vector3.up * yOffset;

        for (int i = 0; i < tiles.Count; i++)
        {
            points.Add(tiles[i].WorldTop + lift);
        }

        return points;
    }
}
