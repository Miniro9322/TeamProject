using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// 맵 모듈이 해금되면 카메라가 갈 수 있는 범위를 넓히고, 새로 열린 지역으로 부드럽게 이동시킨다.
// 사용자가 카메라를 직접 조작하면 이동을 즉시 멈춘다.
[RequireComponent(typeof(CameraRig), typeof(CameraInput))]
public class ExpandFocus : MonoBehaviour
{
    [SerializeField] private MapRegistry registry;
    [Tooltip("새로 열린 지역으로 부드럽게 이동하는 시간(초).")]
    [FormerlySerializedAs("panTime")]
    [SerializeField, Min(0.05f)] private float moveTime = 0.4f;

    private CameraRig rig;
    private CameraInput input;
    private readonly CameraLimit limit = new();
    private readonly HashSet<int> openedSeen = new();   // 이미 카메라가 다녀온 해금 모듈

    private bool moving;
    private Vector3 moveTarget;
    private Vector3 moveVel;

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
        input.UserMoved += CancelMove;
    }

    private void OnDestroy()
    {
        UnbindModules();
        input.UserMoved -= CancelMove;
    }

    private void Update()
    {
        if (!CanMove()) // 이동 중이고 시간이 흐를 때만 실행한다.
        {
            return;
        }

        MoveFocus();
    }

    private bool CanMove()
    {
        if (!moving) // 자동 이동 중이 아니면 실행하지 않는다.
        {
            return false;
        }

        return Time.deltaTime > 0f; // UI 일시정지가 끝날 때까지 이동을 대기한다.
    }

    private void MoveFocus()
    {
        Vector3 before = rig.focus; // 이동 전 초점 위치를 저장한다.
        rig.focus = Vector3.SmoothDamp(rig.focus, moveTarget, ref moveVel, moveTime); // 새 지역으로 부드럽게 이동한다.
        rig.ApplyNow();   // 이동 중에도 클램프 유지
        bool stopped = (rig.focus - before).sqrMagnitude < 1e-4f; // 더 움직이지 못하는지 확인한다.
        if (!stopped)
        {
            return;
        }

        CancelMove(); // 이동이 끝났거나 제한에 막히면 자동 이동을 종료한다.
    }

    // 사용자가 팬/회전/줌을 하면 자동 이동을 즉시 놓아준다.
    public void CancelMove()
    {
        moving = false;
        moveVel = Vector3.zero;
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
            StartMove(opened);
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
    private void StartMove(ModuleLogic module)
    {
        Vector3 center = module.GetComponent<MapBoard>().WorldBounds.center; // 새로 열린 지역의 중심을 구한다.
        moveTarget = new Vector3(center.x, rig.focus.y, center.z); // 현재 높이를 유지한 이동 목표를 만든다.
        moveVel = Vector3.zero; // 이전 이동 속도를 초기화한다.
        moving = true; // 다음 Update부터 자동 이동을 시작한다.
    }
}
