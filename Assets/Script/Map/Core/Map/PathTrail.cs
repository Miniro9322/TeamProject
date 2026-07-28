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

    // 경로 표시를 위해 필요한 컴포넌트 참조를 준비합니다.
    private void Awake()
    {
        Prepare();
    }

    // 모듈 상태와 레인 변경 이벤트를 구독합니다.
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

    // 자동 재생 조건이 맞으면 레인 좌표를 불러와 반복 재생합니다.
    private void Start()
    {
        if (!CanAutoPlay())
        {
            return;
        }

        LoadPoints();
        PlayLoop();
    }

    // 모듈 상태와 레인 변경 이벤트 구독을 해제합니다.
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

    // 현재 유효한 레인을 TrailRenderer 실행 목록으로 다시 구성합니다.
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

    // 모든 레인 Trail을 반복 재생합니다.
    public void PlayLoop()
    {
        if (module != null && !module.IsPreparing)
        {
            return;
        }

        looping = true;
        BeginRuns();
    }

    // 모든 레인 Trail을 한 번만 재생합니다.
    public void PlayOnce()
    {
        if (module != null && !module.IsUnlocked)
        {
            return;
        }

        looping = false;
        BeginRuns();
    }

    // 실행 중인 모든 레인 Trail을 정지하고 지웁니다.
    public void StopTrail()
    {
        for (int i = 0; i < runs.Count; i++)
        {
            StopRun(runs[i]);
        }
    }

    // 각 Trail 실행의 이동 상태를 프레임마다 갱신합니다.
    private void Update()
    {
        for (int i = 0; i < runs.Count; i++)
        {
            TickRun(runs[i]);
        }
    }

    // 같은 GameObject의 EnemyLanes와 ModuleLogic을 가져옵니다.
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

    // 자동 재생 옵션과 모듈 준비 상태를 확인합니다.
    private bool CanAutoPlay()
    {
        return autoPlay && module != null && module.IsPreparing;
    }

    // 레인과 Trail 프리팹이 준비되어 좌표를 불러올 수 있는지 확인합니다.
    private bool CanLoad()
    {
        return enemyLanes != null && trailPrefab != null;
    }

    // 레인 하나에 대응하는 TrailRenderer와 실행 데이터를 생성합니다.
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

    // 생성된 모든 TrailRenderer를 제거하고 실행 목록을 비웁니다.
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

    // 실행 목록을 준비하고 모든 레인 Trail을 시작합니다.
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

    // Trail 하나를 첫 경로 점에서 재생할 수 있도록 초기화합니다.
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

    // Trail 하나의 대기 또는 경로 추적 상태를 갱신합니다.
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

    // Trail Runner를 현재 경로의 다음 점까지 이동시킵니다.
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

    // Trail 하나의 재생을 끝내고 반복 여부에 따라 대기 상태로 전환합니다.
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

    // 반복 재생 대기 시간을 줄이고 끝나면 Trail을 다시 시작합니다.
    private void WaitRun(TrailRun run)
    {
        run.Gap -= Time.deltaTime;
        if (run.Gap <= 0f)
        {
            BeginRun(run);
        }
    }

    // Trail 하나의 실행 상태와 화면 잔상을 초기화합니다.
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

    // 모듈이 준비 상태가 되면 현재 레인을 불러와 자동 재생합니다.
    private void HandleState(ModuleState state)
    {
        if (!autoPlay || state != ModuleState.Preparing)
        {
            return;
        }

        LoadPoints();
        PlayLoop();
    }

    // 레인 변경 후 Trail 목록을 다시 만들고 기존 재생 상태를 이어갑니다.
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
