using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

[DisallowMultipleComponent]
public class PathTrail : MonoBehaviour
{
    private class TrailRun
    {
        public readonly List<Vector3> Points = new();
        public TrailRenderer Trail;
        public Transform Runner;
        public float Length;
        public int Index;
    }

    [SerializeField] private TrailRenderer trailPrefab;
    [SerializeField] private float moveSpeed = 8f; //실제 값은 inspector에서 조절한다.
    [SerializeField] private float trailLift = 0.15f;
    [SerializeField] private float loopGap = 1.2f;
    [SerializeField] private bool isAutoPlay = true;

    private readonly List<TrailRun> runs = new();
    private WaveSpawner spawner; //활성화된 스폰 지점 참조.
    private ModuleLogic module;
    private bool isLooping;
    private bool isPlaying;
    private bool isWaiting; 
    private bool isRefreshPending; //재생 요청이 대기 중인지 여부. 재생 중이면 무시.
    private float elapsed;
    private float moveTime;

    // 필요한 컴포넌트 참조를 초기화합니다.
    private void Awake()
    {
        spawner = GetComponent<WaveSpawner>();
        module = GetComponent<ModuleLogic>();
    }

    // 모듈 상태 변경 이벤트를 구독합니다.
    private void OnEnable()
    {
        module.OnStateChanged += HandleState;
    }

    // 초기 자동 재생을 요청합니다.
    private void Start()
    {
        if (!isAutoPlay || !module.IsPreparing) return;

        PlayLoop();
    }

    // 모듈 상태 변경 이벤트 구독을 해제합니다.
    private void OnDisable()
    {
        module.OnStateChanged -= HandleState;
    }

    // 이번 라운드의 활성 경로로 Trail 목록을 구성합니다.
    public void LoadPoints()
    {
        StopTrail();
        ClearRuns();

        IReadOnlyList<IReadOnlyList<Vector3>> paths = spawner.ActivePaths;
        for (int i = 0; i < paths.Count; i++)
        {
            AddRun(paths[i]);
        }
    }

    // 포털 갱신 다음 프레임에 최신 활성 경로를 반복 재생합니다.
    public async void PlayLoop()
    {
        if (!module.IsPreparing || isRefreshPending) return;

        isRefreshPending = true;
        bool isCanceled = await UniTask.NextFrame(this.GetCancellationTokenOnDestroy()).SuppressCancellationThrow();
        isRefreshPending = false;

        if (isCanceled || !isActiveAndEnabled || !module.IsPreparing) return;

        LoadPoints();
        isLooping = true;
        BeginRuns();
    }

    // 해금 상태에서 모든 활성 경로를 한 번 재생합니다.
    public void PlayOnce()
    {
        if (!module.IsUnlocked) return;

        LoadPoints();
        isLooping = false;
        BeginRuns();
    }

    // 모든 Trail의 재생과 대기 상태를 정지합니다.
    public void StopTrail()
    {
        isPlaying = false;
        isWaiting = false;
        elapsed = 0f;

        for (int i = 0; i < runs.Count; i++)
        {
            StopRun(runs[i]);
        }
    }

    // 공통 재생 상태에 따라 이동 또는 대기를 갱신합니다.
    private void Update()
    {
        if (!isPlaying) return;

        if (isWaiting)
        {
            WaitRuns();
            return;
        }

        MoveRuns();
    }

    // 활성 경로 하나의 Trail과 누적 길이를 생성합니다.
    private void AddRun(IReadOnlyList<Vector3> path)
    {
        if (path == null || path.Count < 2) return;

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
            Vector3 point = path[i] + lift;
            if (run.Points.Count > 0)
            {
                int last = run.Points.Count - 1;
                run.Length += Vector3.Distance(run.Points[last], point);
            }

            run.Points.Add(point);
        }
        runs.Add(run);
    }

    // 생성된 Trail을 제거하고 실행 목록을 비웁니다.
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

    // 모든 Trail을 같은 시점에 재생하도록 초기화합니다.
    private void BeginRuns()
    {
        moveTime = GetMoveTime();
        if (moveTime <= 0f)
        {
            StopTrail();
            return;
        }

        elapsed = 0f;
        isWaiting = false;
        isPlaying = true;

        for (int i = 0; i < runs.Count; i++)
        {
            BeginRun(runs[i]);
        }
    }

    // Trail 하나를 경로 시작점에서 방출합니다.
    private void BeginRun(TrailRun run)
    {
        if (run.Runner == null) return;

        run.Runner.position = run.Points[0];
        run.Trail.Clear();
        run.Trail.emitting = true;
        run.Trail.AddPosition(run.Points[0]);
        run.Index = 1;
    }

    // 공통 진행률로 모든 Trail의 위치를 갱신합니다.
    private void MoveRuns()
    {
        elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsed / moveTime);

        for (int i = 0; i < runs.Count; i++)
        {
            MoveRun(runs[i], progress);
        }

        if (progress < 1f) return;
        EndRuns();
    }

    // Trail 하나를 진행률에 해당하는 경로 위치로 이동합니다.
    private void MoveRun(TrailRun run, float progress)
    {
        if (run.Runner == null) return;

        float distance = run.Length * progress;
        for (int i = 1; i < run.Points.Count; i++)
        {
            Vector3 start = run.Points[i - 1];
            Vector3 target = run.Points[i];
            float length = Vector3.Distance(start, target);
            if (distance < length)
            {
                Vector3 position = Vector3.MoveTowards(start, target, distance);
                run.Runner.position = position;
                if (position == start) return;

                run.Trail.AddPosition(position);
                return;
            }

            distance -= length;
            if (i < run.Index) continue;
            run.Trail.AddPosition(target);
            run.Index = i + 1;
        }

        run.Runner.position = run.Points[run.Points.Count - 1];
    }

    // 모든 Trail의 방출을 동시에 끝내고 반복 여부를 처리합니다.
    private void EndRuns()
    {
        for (int i = 0; i < runs.Count; i++)
        {
            runs[i].Trail.emitting = false;
        }

        if (!isLooping)
        {
            isPlaying = false;
            return;
        }

        elapsed = 0f;
        isWaiting = true;
    }

    // 공통 반복 대기 후 모든 Trail을 다시 시작합니다.
    private void WaitRuns()
    {
        elapsed += Time.deltaTime;
        if (elapsed < loopGap) return;
        BeginRuns();
    }

    // 가장 긴 경로를 기준으로 공통 이동 시간을 계산합니다.
    private float GetMoveTime()
    {
        if (moveSpeed <= 0f) return 0f;

        float maxLength = 0f;
        for (int i = 0; i < runs.Count; i++)
        {
            maxLength = Mathf.Max(maxLength, runs[i].Length);
        }

        return maxLength / moveSpeed;
    }

    // Trail 하나의 방출과 화면 잔상을 초기화합니다.
    private void StopRun(TrailRun run)
    {
        if (run.Trail == null) return;

        run.Trail.emitting = false;
        run.Trail.Clear();
    }

    // 모듈이 준비 상태가 되면 자동 반복 재생을 요청합니다.
    private void HandleState(ModuleState state)
    {
        if (!isAutoPlay || state != ModuleState.Preparing) return;

        PlayLoop();
    }
}
