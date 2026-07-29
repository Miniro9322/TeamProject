using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 지금 가리키는 칸을 클릭하면 무슨 일이 일어나는지 미리 한 줄로 말한다.
///
/// 같은 클릭이 상황마다 다른 일을 한다 — 스왑이 켜져 있으면 판을 얹거나 걷어내거나 밑판을 갈고,
/// 꺼져 있으면 데이터만 칠한다. 무엇이 될지 누르기 전에 보이지 않으면 눌러 보고서야 알게 된다.
///
/// 여기는 말만 만든다. 실제로 바꾸는 것은 TileSwap과 TileStamp다 —
/// 두 곳의 판단 규칙이 갈리면 예고가 거짓말이 되므로 분기 순서를 같게 맞춰 둔다.
/// </summary>
public static class TileActionPreview
{
    /// <summary>지형 붓이 가리키는 지형. 지형 붓이 아니면 Empty로 떨어진다.</summary>
    public static TerrainType TerrainOf(MapBrush brush)
    {
        switch (brush)
        {
            case MapBrush.Ground: return TerrainType.Ground;
            case MapBrush.High: return TerrainType.High;
            case MapBrush.Core: return TerrainType.Core;
            case MapBrush.Special: return TerrainType.Special;
            case MapBrush.Empty: return TerrainType.Empty;
            default: return TerrainType.Empty;
        }
    }

    /// <summary>이 칸에 쌓인 겹을 사람 말로. 예: "2겹 — 외곽 + 고지 판"</summary>
    public static string DescribeStack(Grid module, Vector2Int coord)
    {
        List<Tile> layers = TileSwap.Stack(module, coord);
        if (layers.Count <= 1)
        {
            return "1겹";
        }

        var words = new List<string>();
        foreach (Tile layer in layers)
        {
            words.Add(Word(layer.State.Terrain));
        }

        return $"{layers.Count}겹 — {string.Join(" + ", words)}";
    }

    /// <summary>클릭하면 무엇이 되는지. turnOff는 Alt를 누르고 있는 상태(끄는 쪽으로 찍기).</summary>
    public static string Describe(Grid module, Vector2Int coord, Tile tile,
        MapTool tool, MapBrush brush, TilePrefabSet prefabs, bool turnOff)
    {
        if (tool == MapTool.Select)
        {
            return "계층에서 이 타일을 고릅니다 — 데이터는 바뀌지 않습니다";
        }

        if (tool == MapTool.Pick)
        {
            return $"이 칸의 {Word(tile.Terrain)}을(를) 팔레트로 가져옵니다";
        }

        if (tool == MapTool.Erase)
        {
            return EraseWord(module, coord);
        }

        if (brush == MapBrush.None)
        {
            return "팔레트에서 무엇을 칠할지 먼저 고르세요";
        }

        if (brush == MapBrush.Spawn)
        {
            return turnOff ? "적 스폰 끄기" : "적 스폰 켜기";
        }

        if (IsPlaceBrush(brush))
        {
            return PlaceWord(tile, brush, turnOff);
        }

        TerrainType want = TerrainOf(brush);
        if (tool == MapTool.Swap)
        {
            GameObject prefab = prefabs != null ? prefabs.For(want) : null;
            if (prefab == null)
            {
                return $"{Word(want)} 슬롯이 비어 교체할 프리팹이 없습니다 ({SlotHint(prefabs)})";
            }

            return SwapWord(module, coord, want, prefab);
        }

        if (tile.Terrain == want)
        {
            return $"이미 {Word(want)} — 바뀌는 것 없음";
        }

        return $"{Word(tile.Terrain)} → {Word(want)} · 데이터만 (큐브는 그대로라 겉모습이 어긋납니다)";
    }

    // 지우기는 제일 위 한 겹만 없앤다. 마지막 겹이면 그 칸에 타일이 아예 없어진다.
    private static string EraseWord(Grid module, Vector2Int coord)
    {
        List<Tile> layers = TileSwap.Stack(module, coord);
        if (layers.Count == 0)
        {
            return "타일이 없는 칸 — 지울 것이 없습니다";
        }

        Tile top = layers[layers.Count - 1];
        if (layers.Count == 1)
        {
            return $"마지막 {Word(top.State.Terrain)} 한 겹을 지웁니다 — 이 칸에 타일이 없어집니다";
        }

        return $"{Word(top.State.Terrain)} 한 겹을 걷어냅니다 ({layers.Count}겹 → {layers.Count - 1}겹)";
    }

    // 스왑이 실제로 할 일. TileSwap.Apply의 분기와 같은 순서로 판단한다.
    private static string SwapWord(Grid module, Vector2Int coord, TerrainType want, GameObject prefab)
    {
        List<Tile> layers = TileSwap.Stack(module, coord);
        if (layers.Count == 0)
        {
            return "타일이 없는 칸 — 아무것도 하지 않습니다";
        }

        Tile bottom = layers[0];
        Tile top = layers[layers.Count - 1];

        if (want == TerrainType.High)
        {
            if (top.State.Terrain == TerrainType.High)
            {
                return "이미 고지 — 판을 겹쳐 쌓지 않습니다";
            }

            return $"{Word(top.State.Terrain)} → 고지 · 위에 판을 얹습니다 " +
                   $"({layers.Count}겹 → {layers.Count + 1}겹, {prefab.name})";
        }

        int strip = 0;
        foreach (Tile layer in layers)
        {
            if (layer != bottom && layer.State.Terrain == TerrainType.High)
            {
                strip++;
            }
        }

        string after = $"{layers.Count}겹 → {layers.Count - strip}겹";
        if (strip > 0)
        {
            return $"고지 → {Word(want)} · 판을 걷어내고 밑판을 교체합니다 ({after}, {prefab.name})";
        }

        if (bottom.State.Terrain == want && layers.Count == 1)
        {
            return $"이미 {Word(want)} — 같은 프리팹으로 다시 깝니다 ({prefab.name})";
        }

        return $"{Word(bottom.State.Terrain)} → {Word(want)} · 밑판을 교체합니다 ({prefab.name})";
    }

    // 배치 허용 붓. 켜도 지금 지형에선 효과가 없으면 그 자리에서 알린다.
    private static string PlaceWord(Tile tile, MapBrush brush, bool turnOff)
    {
        string word = AllowWord(brush);
        if (turnOff)
        {
            return $"{word} 끄기";
        }

        if (TileFlagQuery.TakesEffect(tile.Terrain, brush))
        {
            return $"{word} 켜기";
        }

        return $"{word} 켜기 — 다만 {Word(tile.Terrain)}에서는 지금 규칙상 효과가 없습니다";
    }

    private static bool IsPlaceBrush(MapBrush brush)
    {
        return brush == MapBrush.Melee || brush == MapBrush.Ranged || brush == MapBrush.Build;
    }

    private static string AllowWord(MapBrush brush)
    {
        switch (brush)
        {
            case MapBrush.Melee: return "근접";
            case MapBrush.Ranged: return "원거리";
            default: return "생산";
        }
    }

    private static string SlotHint(TilePrefabSet prefabs)
    {
        return prefabs == null ? "매핑 에셋 없음" : "TilePrefabSet 확인";
    }

    private static string Word(TerrainType terrain)
    {
        switch (terrain)
        {
            case TerrainType.Ground: return "지상";
            case TerrainType.High: return "고지";
            case TerrainType.Special: return "외곽";
            case TerrainType.Core: return "본진";
            default: return "빈 칸(벽)";
        }
    }
}
