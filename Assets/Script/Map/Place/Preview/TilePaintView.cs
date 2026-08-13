using UnityEngine;
using System.Collections.Generic;

// 타일 표시만 전담한다. 무엇을 칠할지는 판단하지 않고 TilePaintSync가 정한 목록만 칠한다.
[DefaultExecutionOrder(-100)]
public class TilePaintView : MonoBehaviour
{
    [SerializeField] private TilePainter painter;
    [SerializeField] private Material edgeMat;
    [Range(0.01f, 0.2f)]
    [SerializeField] private float edgeWidth = 0.06f;
    [SerializeField] private float edgeLift = 0.025f;
    [SerializeField] private Material campfireEdgeMat;

    // MapAssemble이 조립 시점에 넣어준다.
    public TilePaintSync sync;

    public TilePainter Painter => painter;

    private readonly List<Tile> cellPainted = new();
    private PlaceEdgeView edgeView;
    private CampfireEdgeView campfireEdgeView;

    // 외곽선 출력기와 대상 맵 보드를 준비합니다.
    public void SetupEdges(List<MapBoard> boards)
    {
        edgeView = new PlaceEdgeView(transform, edgeMat, edgeWidth, edgeLift);
        edgeView.Setup(boards);
        campfireEdgeView = new CampfireEdgeView(transform, campfireEdgeMat, edgeWidth, edgeLift);
    }

    private void Update()
    {
        if (sync == null)
        {
            return;
        }

        bool changed = sync.TryBuildPlan(out List<PaintEntry> plan);
        ShowEdges();
        ShowCampfireEdges();

        if (changed)
        {
            RestoreCells();
            ApplyPlan(plan);
        }
    }

    // 현재 배치 모드에 맞는 지형 외곽선을 표시합니다.
    private void ShowEdges()
    {
        if (edgeView != null)
        {
            edgeView.Show(sync.EdgeData);
        }
    }

    // 클릭한 모닥불의 범위 외곽선을 표시합니다.
    private void ShowCampfireEdges()
    {
        if (campfireEdgeView != null)
        {
            campfireEdgeView.Show(sync.CampfireEdgeTiles, sync.CampfireEdgeVersion);
        }
    }

    // 생성한 외곽선 출력기를 정리합니다.
    private void OnDestroy()
    {
        edgeView?.Dispose();
        campfireEdgeView?.Dispose();
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
