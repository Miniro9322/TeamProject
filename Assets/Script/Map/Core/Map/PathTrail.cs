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
    [SerializeField] private float moveSpeed = 8f; //실제 값은 inspector에서 조절한다.
    [SerializeField] private float trailLift = 0.15f;
    [SerializeField] private float loopGap = 1.2f;
    [SerializeField] private bool autoPlay = true;

    private readonly List<TrailRun> runs = new();
    private WaveSpawner spawner; //활성화된 스폰 지점 참조.
    private ModuleLogic module;
    private bool looping;

    // 경로 표시를 위해 필요한 컴포넌트 참조를 준비합니다.
    private void Awake()
    {
        Prepare();
    }

    // 모듈 상태 변경 이벤트를 구독합니다.
    private void OnEnable()
    {
        Prepare();
        module.OnStateChanged += HandleState;
    }

    // 자동 재생 조건이 맞으면 활성 경로를 반복 재생합니다.
    private void Start()
    {
        if (!AutoReady())
        {
            return;
        }

        PlayLoop();
    }

    // 모듈 상태 변경 이벤트 구독을 해제합니다.
    private void OnDisable()
    { 
        module.OnStateChanged -= HandleState; 
    }

    // 이번 라운드의 활성 경로를 TrailRenderer 실행 목록으로 다시 구성합니다.
    public void LoadPoints()
    {
        StopTrail();
        ClearRuns();
        Prepare();

        if (!CanLoad())
        {
            return;
        }

        IReadOnlyList<IReadOnlyList<Vector3>> paths = spawner.ActivePaths;
        for (int i = 0; i < paths.Count; i++)
        {
            AddRun(paths[i]);
        }
    }

    // 모든 활성 경로 Trail을 반복 재생합니다.(낮 전용)
    public void PlayLoop()
    {
        if (module != null && !module.IsPreparing)
        {
            return;
        }

        LoadPoints();
        looping = true;
        BeginRuns();
    }

    // 모든 활성 경로 Trail을 한 번만 재생합니다.(밤 전용.)
    public void PlayOnce()
    {
        if (!module.IsUnlocked)
        {
            return;
        }

        LoadPoints();
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

    // 같은 GameObject의 WaveSpawner와 ModuleLogic을 가져옵니다.
    private void Prepare()
    {
        spawner = GetComponent<WaveSpawner>(); 
        module = GetComponent<ModuleLogic>();
    }

    // 자동 재생 옵션과 모듈 준비 상태를 확인합니다.
    private bool AutoReady()
    {
        return autoPlay && module.IsPreparing;
    }

    // 활성 경로 제공자와 Trail 프리팹이 준비되었는지 확인합니다.
    private bool CanLoad()
    {
        return trailPrefab != null;
    }

    // 활성 경로 하나에 대응하는 TrailRenderer와 실행 데이터를 생성합니다.
    private void AddRun(IReadOnlyList<Vector3> path)
    {
        if (path == null || path.Count < 2)
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
        Vector3 lift = Vector3.up * trailLift;
        for (int i = 0; i < path.Count; i++)
        {
            run.Points.Add(path[i] + lift);
        }
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

    // 실행 목록의 모든 활성 경로 Trail을 시작합니다.
    private void BeginRuns()
    {
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

    // 모듈이 준비 상태가 되면 현재 활성 경로를 자동 재생합니다.
    private void HandleState(ModuleState state)
    {
        if (!autoPlay || state != ModuleState.Preparing)
        {
            return;
        }

        PlayLoop();
    }
}
