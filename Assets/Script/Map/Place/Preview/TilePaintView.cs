using UnityEngine;
using System.Collections.Generic;

// 타일 표시만 전담한다. 무엇을 칠할지는 판단하지 않고 TilePaintSync가 정한 목록만 칠한다.
[DefaultExecutionOrder(-100)]
public class TilePaintView : MonoBehaviour
{
    [SerializeField] private TilePainter painter;

    // MapAssemble이 조립 시점에 넣어준다.
    public TilePaintSync sync;

    public TilePainter Painter => painter;

    private readonly List<Tile> cellPainted = new();

    private void Update()
    {
        if (sync.TryBuildPlan(out List<PaintEntry> plan))
        {
            RestoreCells();
            ApplyPlan(plan);
        }
    }

    private void RestoreCells()
    {
        for (int i = 0; i < cellPainted.Count; i++)
        {
            painter.ClearColor(cellPainted[i]);
        }

        cellPainted.Clear();
    }

    private void ApplyPlan(List<PaintEntry> plan)
    {
        for (int i = 0; i < plan.Count; i++)
        {
            Paint(plan[i]);
        }
    }

    // 표시 정보를 타일 메시로 출력합니다.
    private void Paint(PaintEntry entry)
    {
        painter.SetColor(entry.Tile, entry.Color);
        cellPainted.Add(entry.Tile);
    }
}
