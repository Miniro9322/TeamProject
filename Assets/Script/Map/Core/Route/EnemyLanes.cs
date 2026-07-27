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

    private void Reset()
    {
        board = GetComponent<MapBoard>();
    }

    private void Awake()
    {
        Prepare();
        RefreshLanes();
    }

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

        LaneInput input = CreateInput();
        IReadOnlyList<LaneData> built = builder.BuildLanes(input);
        ApplyLanes(built);
    }

    public void SetBuilder(ILaneBuilder value)
    {
        if (value == null)
        {
            return;
        }

        builder = value;
    }

    
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

    private void Prepare()
    {
        if (board == null)
        {
            board = GetComponent<MapBoard>();
        }

        if (builder == null)
        {
            builder = new LaneBuilder();
        }
    }

    private bool CanBuild()
    {
        return board != null && board.CellCount > 0;
    }

    private LaneInput CreateInput()
    {
        var spawns = new List<Tile>();
        var cores = new List<Tile>();

        foreach (Tile tile in board.Cells.Values)
        {
            AddEndpoint(tile, spawns, cores);
        }

        return new LaneInput(board.Cells, spawns, cores);
    }

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
