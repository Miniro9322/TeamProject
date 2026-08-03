using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MapBoard))]
[DefaultExecutionOrder(100)]
public class EnemyLanes : MonoBehaviour
{
    [SerializeField] private MapBoard board;

    private readonly List<LaneData> lanes = new();
    private ILaneBuilder builder;

    public IReadOnlyList<LaneData> Lanes => lanes;
    public bool IsReady { get; private set; }

    public event Action Changed;

    // 같은 GameObject의 MapBoard를 인스펙터 참조에 자동 할당합니다.
    private void Reset()
    {
        board = GetComponent<MapBoard>();
    }

    // 필수 참조를 준비하고 현재 맵의 모든 적 이동 레인을 계산합니다.
    private void Awake()
    {
        Prepare();
        RefreshLanes();
    }

    // 기존 레인을 비우고 현재 MapBoard 상태를 기준으로 전체 레인을 다시 계산합니다.
    public void RefreshLanes()
    {
        IsReady = false;
        lanes.Clear();
        Prepare();

        if (!CanBuild())
        {
            Changed?.Invoke();
            return;
        }

        LaneInputData input = CreateInput();
        IReadOnlyList<LaneData> built = builder.BuildLanes(input);
        ApplyLanes(built);
    }

    // 레인 계산에 사용할 ILaneBuilder 구현체를 교체합니다.
    public void SetBuilder(ILaneBuilder value)
    {
        if (value == null)
        {
            return;
        }

        builder = value;
    }

    // 유효한 모든 레인을 지정 높이가 적용된 월드 좌표 경로 목록으로 반환합니다.
    public IReadOnlyList<IReadOnlyList<Vector3>> GetPaths(float yOffset)
    {
        //외부 호출시 
        //IReadOnlyList<IReadOnlyList<Vector3>> paths = enemyLanes.GetPaths(0f);
        //로 선언.
        var paths = new List<IReadOnlyList<Vector3>>();

        if (!IsReady)
        {
            return paths;
        }

        for (int i = 0; i < lanes.Count; i++)
        {
            LaneData lane = lanes[i];

            if (!lane.IsValid)
            {
                continue;
            }

            paths.Add(lane.GetPoints(yOffset));
        }

        return paths;
    }

    // MapBoard 참조와 기본 LaneBuilder가 준비되었는지 확인합니다.
    private void Prepare()
    {
        board = GetComponent<MapBoard>();

        if (builder == null)
        {
            builder = new LaneBuilder();
        }
    }

    // 레인을 계산할 MapBoard와 셀이 준비되었는지 확인합니다.
    private bool CanBuild()
    {
        return board.CellCount > 0;
    }

    // MapBoard의 셀에서 스폰과 코어를 수집해 LaneInput을 만듭니다.
    private LaneInputData CreateInput()
    {
        var spawns = new List<Tile>();
        var cores = new List<Tile>();

        foreach (Tile tile in board.Cells.Values)
        {
            AddEndpoint(tile, spawns, cores);
        }

        return new LaneInputData(board.Cells, spawns, cores);
    }

    // 타일의 역할에 따라 스폰 또는 코어 목록에 추가합니다.
    private static void AddEndpoint(Tile tile, List<Tile> spawns, List<Tile> cores)
    {
        if (tile.IsEnemySpawn)
        {
            spawns.Add(tile);
        }

        if (tile.IsCore)
        {
            cores.Add(tile);
        }
    }

    // 계산된 레인을 내부 목록에 보관하고 준비 완료 이벤트를 알립니다.
    private void ApplyLanes(IReadOnlyList<LaneData> built)
    {
        for (int i = 0; i < built.Count; i++)
        {
            lanes.Add(built[i]);
        }

        IsReady = true;
        Changed?.Invoke();
    }
}
