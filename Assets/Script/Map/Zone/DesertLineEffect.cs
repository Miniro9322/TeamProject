using System.Collections.Generic;
using UnityEngine;

// 밤마다 사막 줄 단위로 강한 칸 1개·약한 칸 나머지에 파티클을 배치합니다. 낮엔 전부 치웁니다.
public class DesertLineEffect
{
    private readonly MapBoard board;
    private readonly WindShelterData terrain;
    private readonly GameObject strongPrefab;
    private readonly GameObject weakPrefab;
    private readonly List<GameObject> spawned = new();

    public DesertLineEffect(MapBoard board, WindShelterData terrain, GameObject strongPrefab, GameObject weakPrefab)
    {
        this.board = board;
        this.terrain = terrain;
        this.strongPrefab = strongPrefab;
        this.weakPrefab = weakPrefab;
    }

    // 이 밤의 바람과 유닛 배치 기준으로 줄마다 강한 칸 1개·약한 칸을 배치합니다.
    public void Show(Vector2Int wind, UnitShelter units)
    {
        Hide();
        RectInt play = board.PlayRect;
        Quaternion rotation = ReadRotation(wind);

        if (wind == GridCalculator.Right)
        {
            ShowRows(wind, units, play, rotation, play.xMin, 1);
            return;
        }

        if (wind == GridCalculator.Left)
        {
            ShowRows(wind, units, play, rotation, play.xMax - 1, -1);
            return;
        }

        if (wind == GridCalculator.Up)
        {
            ShowColumns(wind, units, play, rotation, play.yMin, 1);
            return;
        }

        ShowColumns(wind, units, play, rotation, play.yMax - 1, -1);
    }

    // 배치된 모든 이펙트를 치운다.
    public void Hide()
    {
        for (int index = 0; index < spawned.Count; index++)
        {
            Object.Destroy(spawned[index]);
        }

        spawned.Clear();
    }

    // 동·서 바람일 때 각 행(row)을 처리한다. 벽이 진입칸을 막아 범위 밖 결과가 나오면 그 행은 완전히 가려진 것이라 건너뛴다.
    private void ShowRows(Vector2Int wind, UnitShelter units, RectInt play, Quaternion rotation, int entryX, int step)
    {
        for (int row = play.yMin; row < play.yMax; row++)
        {
            Vector2Int strongCell = WindShelterQuery.FindLastUnsheltered(terrain, units, play, wind, row);
            if (strongCell.x < play.xMin || strongCell.x >= play.xMax)
            {
                continue;
            }

            WalkRow(row, entryX, strongCell.x, step, rotation);
        }
    }

    // 남·북 바람일 때 각 열(col)을 처리한다. 벽이 진입칸을 막아 범위 밖 결과가 나오면 그 열은 완전히 가려진 것이라 건너뛴다.
    private void ShowColumns(Vector2Int wind, UnitShelter units, RectInt play, Quaternion rotation, int entryY, int step)
    {
        for (int col = play.xMin; col < play.xMax; col++)
        {
            Vector2Int strongCell = WindShelterQuery.FindLastUnsheltered(terrain, units, play, wind, col);
            if (strongCell.y < play.yMin || strongCell.y >= play.yMax)
            {
                continue;
            }

            WalkColumn(col, entryY, strongCell.y, step, rotation);
        }
    }

    // 한 행에서 진입 칸부터 강한 칸까지 약한 이펙트를 심고, 강한 칸엔 강한 이펙트를 심는다.
    private void WalkRow(int row, int entryX, int strongX, int step, Quaternion rotation)
    {
        for (int x = entryX; x != strongX; x += step)
        {
            Spawn(weakPrefab, new Vector2Int(x, row), rotation);
        }

        Spawn(strongPrefab, new Vector2Int(strongX, row), rotation);
    }

    // 한 열에서 진입 칸부터 강한 칸까지 약한 이펙트를 심고, 강한 칸엔 강한 이펙트를 심는다.
    private void WalkColumn(int col, int entryY, int strongY, int step, Quaternion rotation)
    {
        for (int y = entryY; y != strongY; y += step)
        {
            Spawn(weakPrefab, new Vector2Int(col, y), rotation);
        }

        Spawn(strongPrefab, new Vector2Int(col, strongY), rotation);
    }

    // 실제 타일 위치에 이펙트 하나를 만들어 목록에 보관한다.
    private void Spawn(GameObject prefab, Vector2Int cell, Quaternion rotation)
    {
        if (!board.TryGetCell(cell, out Tile tile))
        {
            return;
        }

        GameObject effect = Object.Instantiate(prefab, tile.WorldTop, rotation, board.transform);
        spawned.Add(effect);
    }

    // 바람 벡터를 실제 보드의 월드 진행 방향 회전으로 바꾼다.
    private Quaternion ReadRotation(Vector2Int wind)
    {
        Vector3 origin = board.CellPointToWorld(Vector2.zero);
        Vector3 target = board.CellPointToWorld(new Vector2(wind.x, wind.y));
        Vector3 direction = (target - origin).normalized;
        return Quaternion.LookRotation(direction, Vector3.up);
    }
}
