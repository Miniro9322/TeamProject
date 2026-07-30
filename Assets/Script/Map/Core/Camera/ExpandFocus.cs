using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// 맵 모듈이 해금되면 입력을 막고 새 지역으로 카메라를 강제 이동시킨다.
// 카메라 이동과 안개 제거가 모두 끝나면 입력을 다시 활성화한다.
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
    private int moveId = -1;
    private bool moveDone;
    private bool fogDone;

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
        FogController.RevealDone += OnFogDone;
    }

    private void OnDestroy()
    {
        UnbindModules();
        FogController.RevealDone -= OnFogDone;
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

        FinishMove(); // 이동이 끝났거나 제한에 막히면 이동 완료로 처리한다.
    }

    // 개발용 자유 카메라가 자동 이동을 중단할 때 사용한다.
    public void CancelMove()
    {
        moving = false;
        moveVel = Vector3.zero;
    }

    private void FinishMove()
    {
        moving = false; // 카메라 이동을 정지한다.
        moveVel = Vector3.zero; // 이동 속도를 초기화한다.
        moveDone = true; // 카메라 이동 완료를 기록한다.
        TryUnlock();
    }

    private void OnFogDone(int moduleId)
    {
        if (moduleId != moveId) // 현재 확장 지역의 완료 신호만 처리한다.
        {
            return;
        }

        fogDone = true; // 안개 제거 완료를 기록한다.
        TryUnlock();
    }

    private void TryUnlock()
    {
        if (!CanUnlock()) // 두 작업 중 하나라도 남아 있으면 입력을 유지한다.
        {
            return;
        }

        input.enabled = true; // 카메라 입력을 다시 활성화한다.
        moveId = -1; // 현재 확장 지역 기록을 초기화한다.
    }

    private bool CanUnlock()
    {
        return moveDone && fogDone;
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
        moveId = module.ModuleId; // 완료 신호를 비교할 확장 지역을 기록한다.
        moveDone = false; // 카메라 이동 완료 상태를 초기화한다.
        fogDone = false; // 안개 제거 완료 상태를 초기화한다.
        input.enabled = false; // 확장 연출 중 카메라 입력을 차단한다.
        moving = true; // 다음 Update부터 자동 이동을 시작한다.
    }
}
