using UnityEngine;
using System.Collections.Generic;

// 타일 표시를 전담하며 매 프레임 지우고 다시 그린다.
// 이번 프레임 배치 자리 계산을 여기서 한 번만 하고 hoverPlace에 남긴다 — MapCommand는 그 결과만 읽는다.
[DefaultExecutionOrder(-100)]
public class TilePaintView : MonoBehaviour
{
    [SerializeField] private MapView game;            // 호버·배치 상태(읽기 전용)
    [SerializeField] private TilePainter painter;

    // HeroSkillCastController는 일반 C# 클래스(비-MonoBehaviour)라 인스펙터로 연결할 수 없다 —
    // MapAssemble이 조립 시점에 코드로 넣어준다.
    public HeroSkillCastController skillCast;
    public PlaceFinder finder;
    public RangeTileData rangeStore;
    public HoverPlaceData hoverPlace;

    private readonly List<Tile> cellPainted = new();

    private void Update()
    {
        RestoreCells();
        PaintHover();
        PaintSkillRange();
    }

    private void RestoreCells()
    {
        for (int i = 0; i < cellPainted.Count; i++)
        {
            painter.ClearColor(cellPainted[i]);
        }
        cellPainted.Clear();
    }

    private void PaintHover()
    {
        if (game.InputBlocked)
        {
            hoverPlace.Clear();
            return;
        }

        if (game.IsPlacing)
        {
            PaintPlacePreview();
            return;
        }

        if (game.IsReplacing && game.IsHolding)
        {
            PaintHeldPreview();
            return;
        }

        hoverPlace.Clear();
        PaintUnitRange();
    }

    // 배치하려는 것이 놓일 자리를 칠한다. 이번 프레임 결과는 hoverPlace에도 남긴다.
    private void PaintPlacePreview()
    {
        if (!finder.TryResolveSlot(out Placeable slot, out PlaceData data))
        {
            hoverPlace.Clear();
            return;
        }

        hoverPlace.Keep(data);
        PaintPreview(data, slot.prefab);
    }

    // 집어 든 유닛이 놓일 자리를 칠한다. 이번 프레임 결과는 hoverPlace에도 남긴다.
    private void PaintHeldPreview()
    {
        if (!finder.TryResolve(game.HeldKind, game.HeldSize, out PlaceData data))
        {
            hoverPlace.Clear();
            return;
        }

        hoverPlace.Keep(data);
        PaintPreview(data, game.HeldUnit);
    }

    // 누르고 있는 유닛의 사거리를 칠한다.
    private void PaintUnitRange()
    {
        foreach (Tile tile in rangeStore.Tiles)
        {
            Paint(tile, painter.rangeColor);
        }
    }

    private void PaintPreview(
        PlaceData data,
        GameObject unit)
    {
        Color placeColor;
        Color rangeColor = painter.rangeColor;

        if (data.CanPlace)
        {
            placeColor = painter.okColor;
        }
        else
        {
            placeColor = painter.denyColor;
            rangeColor = painter.denyColor;
        }

        bool hasCenter = data.Area.Board.TryGetCell(
            data.Area.Origin,
            out Tile center);

        if (hasCenter)
        {
            bool hasRange = game.TryRange(
                unit,
                out int range,
                out RangeShape shape);

            if (hasRange)
            {
                PaintRange(
                    center,
                    range,
                    shape,
                    rangeColor);
            }
        }

        foreach (Vector2Int cell in data.Area.Cells)
        {
            bool hasTile = data.Area.Board.TryGetCell(cell, out Tile tile);
            if (hasTile)
            {
                Paint(tile, placeColor);
            }
        }
    }

    private void PaintRange(
        Tile center,
        int range,
        RangeShape shape,
        Color color)
    {
        List<Tile> tiles = TileShapeQuery.GetTiles(
            center.Board,
            center.Coord,
            range,
            shape);

        foreach (Tile tile in tiles)
        {
            Paint(tile, color);
        }
    }

    // 영웅을 클릭해 스킬 시전자로 선택된 상태일 때, 스킬이 "실제로 때리는 범위"를 미리 보여준다.
    // (targetScope는 "어디를 클릭할 수 있는가"일 뿐, 여기서 칠하는 건 그 지점 중심의 타격 범위다.)
    private void PaintSkillRange()
    {
        if (skillCast == null)
        {
            return;
        }

        Hero caster = skillCast.SelectedCaster;
        if (caster == null || caster.ActiveSkill == null)
        {
            return;
        }

        HeroActiveSkill skill = caster.ActiveSkill;
        int hitRadius = 0;
        RangeShape hitShape = RangeShape.Diamond;
        GameObject zonePrefab = skill.groundZonePrefab;
        if (zonePrefab != null && zonePrefab.TryGetComponent(out GroundZoneEffect zone))
        {
            hitRadius = zone.radius;
            hitShape = zone.shape;
        }

        Tile origin = SkillOrigin(caster, skill);
        if (origin == null)
        {
            return;
        }

        PaintRange(
            origin,
            hitRadius,
            hitShape,
            painter.skillColor);
    }

    private Tile SkillOrigin(
        Hero caster,
        HeroActiveSkill skill)
    {
        bool isSelf = skill.targetScope == SkillTargetScope.Self;
        if (isSelf)
        {
            return caster.CurrentTile;
        }

        Tile origin = game.HoverTile;
        if (origin == null || origin.Board != caster.Board)
        {
            return null;
        }

        return origin;
    }

    private void Paint(Tile tile, Color color)
    {
        painter.SetColor(tile, color);
        cellPainted.Add(tile);
    }
}
