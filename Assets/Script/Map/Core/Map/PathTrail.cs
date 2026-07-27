using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PathTrail : MonoBehaviour
{
    private class TrailRun
    {
        public readonly List<Vector3> Points = new();
        public TrailRenderer Trail;
        public Transform Runner;
        public bool Playing;
        public int Point;
        public float Gap;
    }

    [SerializeField] private TrailRenderer trailPrefab;
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float trailLift = 0.15f;
    [SerializeField] private float loopGap = 1.2f;
    [SerializeField] private bool autoPlay = true;

    private readonly List<TrailRun> runs = new();
    private EnemyLanes enemyLanes;
    private ModuleLogic module;
    private bool looping;

    private void Awake()
    {
        Prepare();
    }

    private void OnEnable()
    {
        Prepare();

        if (module != null)
        {
            module.OnStateChanged += HandleState;
        }

        if (enemyLanes != null)
        {
            enemyLanes.Changed += HandleLanes;
        }
    }

    private void Start()
    {
        if (!CanAutoPlay())
        {
            return;
        }

        LoadPoints();
        PlayLoop();
    }

    private void OnDisable()
    {
        if (module != null)
        {
            module.OnStateChanged -= HandleState;
        }

        if (enemyLanes != null)
        {
            enemyLanes.Changed -= HandleLanes;
        }
    }

    public void LoadPoints()
    {
        StopTrail();
        ClearRuns();
        Prepare();

        if (!CanLoad())
        {
            return;
        }

        for (int i = 0; i < enemyLanes.Lanes.Count; i++)
        {
            AddRun(enemyLanes.Lanes[i]);
        }
    }

    public void PlayLoop()
    {
        if (module != null && !module.IsPreparing)
        {
            return;
        }

        looping = true;
        BeginRuns();
    }

    public void PlayOnce()
    {
        if (module != null && !module.IsUnlocked)
        {
            return;
        }

        looping = false;
        BeginRuns();
    }

    public void StopTrail()
    {
        for (int i = 0; i < runs.Count; i++)
        {
            StopRun(runs[i]);
        }
    }

    private void Update()
    {
        for (int i = 0; i < runs.Count; i++)
        {
            TickRun(runs[i]);
        }
    }

    private void Prepare()
    {
        if (enemyLanes == null)
        {
            enemyLanes = GetComponent<EnemyLanes>();
        }

        if (module == null)
        {
            module = GetComponent<ModuleLogic>();
        }
    }

    private bool CanAutoPlay()
    {
        return autoPlay && module != null && module.IsPreparing;
    }

    private bool CanLoad()
    {
        return enemyLanes != null && trailPrefab != null;
    }

    private void AddRun(LaneData lane)
    {
        if (!lane.IsValid || lane.Tiles.Count < 2)
        {
            return;
        }

        TrailRenderer trail = Instantiate(trailPrefab, transform);
        trail.transform.localScale = Vector3.one;
        trail.emitting = false;
        trail.Clear();

        var run = new TrailRun();
        run.Trail = trail;
        run.Runner = trail.transform;
        run.Points.AddRange(lane.GetPoints(trailLift));
        runs.Add(run);
    }

    private void ClearRuns()
    {
        for (int i = 0; i < runs.Count; i++)
        {
            TrailRun run = runs[i];
            if (run.Trail != null)
            {
                Destroy(run.Trail.gameObject);
            }
        }

        runs.Clear();
    }

    private void BeginRuns()
    {
        if (runs.Count == 0)
        {
            LoadPoints();
        }

        for (int i = 0; i < runs.Count; i++)
        {
            BeginRun(runs[i]);
        }
    }

    private static void BeginRun(TrailRun run)
    {
        if (run.Points.Count < 2 || run.Runner == null)
        {
            run.Playing = false;
            return;
        }

        run.Runner.position = run.Points[0];
        run.Trail.Clear();
        run.Trail.emitting = true;
        run.Point = 0;
        run.Gap = 0f;
        run.Playing = true;
    }

    private void TickRun(TrailRun run)
    {
        if (!run.Playing)
        {
            return;
        }

        if (run.Gap > 0f)
        {
            WaitRun(run);
            return;
        }

        FollowRun(run);
    }

    private void FollowRun(TrailRun run)
    {
        if (run.Point + 1 >= run.Points.Count)
        {
            EndRun(run);
            return;
        }

        Vector3 target = run.Points[run.Point + 1];
        Vector3 next = Vector3.MoveTowards(
            run.Runner.position,
            target,
            moveSpeed * Time.deltaTime);

        run.Runner.position = next;

        if (next != target)
        {
            return;
        }

        run.Point++;
        if (run.Point >= run.Points.Count - 1)
        {
            EndRun(run);
        }
    }

    private void EndRun(TrailRun run)
    {
        run.Trail.emitting = false;

        if (looping)
        {
            run.Gap = loopGap;
            return;
        }

        run.Playing = false;
    }

    private void WaitRun(TrailRun run)
    {
        run.Gap -= Time.deltaTime;
        if (run.Gap <= 0f)
        {
            BeginRun(run);
        }
    }

    private static void StopRun(TrailRun run)
    {
        run.Playing = false;
        run.Gap = 0f;

        if (run.Trail == null)
        {
            return;
        }

        run.Trail.emitting = false;
        run.Trail.Clear();
    }

    private void HandleState(ModuleState state)
    {
        if (!autoPlay || state != ModuleState.Preparing)
        {
            return;
        }

        LoadPoints();
        PlayLoop();
    }

    private void HandleLanes()
    {
        bool wasPlaying = false;
        for (int i = 0; i < runs.Count; i++)
        {
            if (runs[i].Playing)
            {
                wasPlaying = true;
                break;
            }
        }

        LoadPoints();
        if (wasPlaying)
        {
            BeginRuns();
        }
    }
}
