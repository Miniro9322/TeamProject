using System.Collections.Generic;
using UnityEngine;

// 적 경로를 트레일로 긋는다: 낮이면 반복, 밤이면 1회. 지나간 자리는 TrailRenderer가 자동으로 지운다.
 
[DisallowMultipleComponent]
public class PathTrail : MonoBehaviour
{
    private MapBoard board;
    private ModuleLogic module;
    [SerializeField] private TrailRenderer trailPrefab;   // 모듈마다 자기 밑에 런타임 생성
    private TrailRenderer trail;                          // 생성된 인스턴스

    [SerializeField] private float moveSpeed = 8f;    // runner 이동 속도(월드 단위/초).
    [SerializeField] private float trailLift = 0.15f; // 타일 윗면에서 트레일을 띄우는 높이.
    [SerializeField] private float loopGap = 1.2f;    // 낮 반복 시 한 바퀴 사이 텀(초). 트레일 수명보다 크게 잡아 꼬리가 다 사라진 뒤 다시 긋는다.
    [SerializeField] private bool autoPlay = true;    // 모듈이 열릴 때 스스로 낮 재생을 시작할지.

    private readonly List<Vector3> _points = new();
    private Transform _runner;
    private bool _playing;
    private bool _looping;
    private int _pointIndex;
    private float _gapLeft;

    private void Awake()
    {
         
        board = GetComponent<MapBoard>();
        module = GetComponent<ModuleLogic>();

        if (trailPrefab != null)
        {
            trail = Instantiate(trailPrefab, transform);   // 공유 아님 — 모듈 전용 러너
            trail.transform.localScale = Vector3.one;
            _runner = trail.transform;
            trail.emitting = false;
            trail.Clear();
        }
    }

    private void Start()
    {
        if (autoPlay && module != null && module.IsPreparing)
        {
            LoadPoints();
            PlayLoop();
        }
    }

    private void OnEnable()
    {
        if (module != null)
        {
            module.OnStateChanged += HandleState;
        }
    }

    private void OnDisable()
    {
        if (module != null)
        {
            module.OnStateChanged -= HandleState;
        }
    }

    // 경로점을 보드에서 1회 읽어 캐시한다(스폰→관문 순서, 끝점 포함). 경로가 없으면 비운다.
    public void LoadPoints()
    {
        _points.Clear();
        if (board != null)
        {
            _points.AddRange(board.GetWaypoints(trailLift));
        }
    }

    // 반복해서 긋는다.
    public void PlayLoop()
    {
        if(module != null && !module.IsPreparing)
        {
            return;
        }
        _looping = true;
        BeginRun();
    }

    //한 번만 긋는다.
    public void PlayOnce()
    {
        if(module != null && !module.IsUnlocked)
        {
            return;
        }
        _looping = false;
        BeginRun();
    }

    public void StopTrail()
    {
        _playing = false;
        _gapLeft = 0f;
        if (trail != null)
        {
            trail.emitting = false;
            trail.Clear();
        }
    }

    // 모듈이 열리면  낮 재생을 시작한다. 
    private void HandleState(ModuleState state)
    {
        if (!autoPlay)
        {
            return;
        }
        if (state == ModuleState.Preparing)
        {
            LoadPoints();
            PlayLoop();
        }
    }

    private void BeginRun()
    {
        if (_points.Count < 2)
        {
            LoadPoints();
        }
        if (_points.Count < 2 || _runner == null)
        {
            _playing = false; // 경로가 없으면 재생 상태로 남기지 않는다(다음 프레임 FollowPath가 빈 리스트를 읽지 않게)
            return;
        }

        _runner.position = _points[0];
        trail.Clear();                  
        trail.emitting = true;
        _pointIndex = 0;
        _gapLeft = 0f;
        _playing = true;
    }

    private void Update()
    {
        if (!_playing)
        {
            return;
        }
        if (_gapLeft > 0f)
        {
            WaitGap();
            return;
        }
        FollowPath();
    }

    // 다음 경로점까지 runner를 이동. 마지막 점에 닿으면 한 바퀴를 끝낸다.
    private void FollowPath()
    {
        // 경로가 도중에 바뀌거나 비어 인덱스가 벗어나면 이번 주행을 안전하게 끝낸다.
        if (_pointIndex + 1 >= _points.Count)
        {
            EndRun();
            return;
        }

        Vector3 target = _points[_pointIndex + 1];  // 다음 경로점
        Vector3 stepped = Vector3.MoveTowards(_runner.position, target, moveSpeed * Time.deltaTime);
        _runner.position = stepped;        // 이번 프레임 만큼 전진

        if (stepped == target)
        {
            _pointIndex++;
            if (_pointIndex >= _points.Count - 1) // 마지막 점까지 다 갔다면
            {
                EndRun();
            }
        }
    }

    // 꼬리가 자연히 사라지게 둔다. 낮이면 텀 뒤 다시 시작.
    private void EndRun()
    {
        trail.emitting = false;
        if (_looping)
        {
            _gapLeft = loopGap;
        }
        else
        {
            _playing = false;
        }
    }
    //현재 트레일이 끝나고 다음 반복까지 남은 시간. 낮이면 loopGap, 밤이면 0.
    private void WaitGap()
    {
        _gapLeft -= Time.deltaTime;
        if (_gapLeft <= 0f)
        {
            BeginRun();
        }
    }
}
