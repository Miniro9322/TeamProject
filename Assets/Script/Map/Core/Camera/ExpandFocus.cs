using System.Collections.Generic;
using UnityEngine;

// 맵 모듈이 해금되면 카메라가 갈 수 있는 범위를 넓히고, 새로 열린 지역으로 부드럽게 이동시킨다.
// 사용자가 카메라를 직접 조작하면 이동을 즉시 멈춘다.
[RequireComponent(typeof(CameraRig), typeof(CameraInput))]
public class ExpandFocus : MonoBehaviour
{
    [SerializeField] private MapRegistry registry;
    [Tooltip("새로 열린 지역으로 부드럽게 이동하는 시간(초).")]
    [SerializeField, Min(0.05f)] private float panTime = 0.4f;

    private CameraRig rig;
    private CameraInput input;
    private readonly CameraLimit limit = new();
    private readonly HashSet<int> openedSeen = new();   // 이미 카메라가 다녀온 해금 모듈

    private bool panning;
    private Vector3 panTarget;
    private Vector3 panVel;

    private void Awake()
    {
        rig = GetComponent<CameraRig>();
        input = GetComponent<CameraInput>();
    }

    private void Start()
    {
        RebuildLimit();
        BindModules();
        MarkOpened();   // 시작 시 이미 열린 모듈은 이동 대상에서 제외
        input.UserMoved += CancelPan;
    }

    private void OnDestroy()
    {
        UnbindModules();
        input.UserMoved -= CancelPan;
    }

    private void Update()
    {
        if (!panning)
        {
            return;
        }
        Vector3 before = rig.focus;
        rig.focus = Vector3.SmoothDamp(rig.focus, panTarget, ref panVel, panTime);
        rig.ApplyNow();   // 이동 중에도 클램프 유지
        if ((rig.focus - before).sqrMagnitude < 1e-4f)   // 더 못 가면(도착 또는 클램프 한계) 종료
        {
            CancelPan();
        }
    }

    // 사용자가 팬/회전/줌을 하면 자동 이동을 즉시 놓아준다.
    public void CancelPan()
    {
        panning = false;
        panVel = Vector3.zero;
    }

    // 해금 모듈이 하나도 없으면 경계가 없다 → 클램프를 걸지 않는다(0 크기 박스면 카메라가 원점에 박힌다).
    private void RebuildLimit()
    {
        if (limit.Build(registry))
        {
            rig.SetArea(limit.Area);
        }
    }
    // 새로 열린 모듈이 있으면 그쪽으로 부드럽게 이동한다. 없으면 그냥 경계만 확장한다.
    private void BindModules()
    {
        foreach (ModuleLogic module in registry.AllModules.Values)
        {
            module.OnStateChanged += OnModuleState;
        }
    }
    // 해금 모듈이 하나도 없으면 경계가 없다 → 클램프를 걸지 않는다(0 크기 박스면 카메라가 원점에 박힌다).
    private void UnbindModules()
    {
        foreach (ModuleLogic module in registry.AllModules.Values)
        {
            module.OnStateChanged -= OnModuleState;
        }
    }
    // 모듈이 해금되면 경계를 확장하고, 새로 열린 모듈이 있으면 그쪽으로 부드럽게 이동한다.
    private void OnModuleState(ModuleState state)
    {
        RebuildLimit();   // 경계 먼저 확장(새 모듈 포함)
        ModuleLogic opened = NewOpened();
        if (opened != null)
        {
            StartPan(opened);
        }
    }
    
    // 이미 카메라가 다녀온 해금 모듈은 이동 대상에서 제외한다.
    private void MarkOpened()
    {
        foreach (ModuleLogic module in registry.AllModules.Values)
        {
            if (module.IsUnlocked)
            {
                openedSeen.Add(module.ModuleId);
            }
        }
    }

    // 아직 안 다녀온 '새로 해금된' 모듈 하나. 없으면 null(낮/밤 전이는 여기서 걸러진다).
    private ModuleLogic NewOpened()
    {
        foreach (ModuleLogic module in registry.AllModules.Values)
        {
            if (module.IsUnlocked && openedSeen.Add(module.ModuleId))   // 처음 보는 해금이면 true
            {
                return module;
            }
        }
        return null;
    }

    // focus 목표를 해당 모듈 중앙으로. 실제 이동은 Update가 부드럽게 처리.
    private void StartPan(ModuleLogic module)
    {
        Vector3 center = module.GetComponent<MapBoard>().WorldBounds.center;
        panTarget = new Vector3(center.x, rig.focus.y, center.z);
        panVel = Vector3.zero;
        panning = true;
    }
}
