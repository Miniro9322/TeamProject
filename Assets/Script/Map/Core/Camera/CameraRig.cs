using UnityEngine;


// 궤도 상태(focus·yaw·pitch·distance)를 트랜스폼과 렌즈로 확정한다.
// 입력은 다루지 않는다. CameraInput이 이 필드를 직접 수정한 뒤 ApplyNow를 부른다.
// [ExecuteAlways] + OnValidate로 Play 없이 인스펙터 수정만으로 Game 뷰에 반영된다.
[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class CameraRig : MonoBehaviour
{
    [Header("Orbit")]
    public Vector3 focus;
    public float yaw = 45f;
    public float pitch = 45f;
    public float distance = 40f;

    [Header("Lens")]
    public bool perspective = true;
    [Range(10f, 70f)] public float fieldOfView = 30f;

    [Header("제한")]
    public float minPitch = 5f;
    public float maxPitch = 89f;
    public float minDistance = 5f;
    public float maxDistance = 120f;
    [SerializeField] private MapRegistry registry; // 시야 경계의 소스(해금 모듈 union). FogController와 동일 주입
    [Tooltip("가로 이동 한계: 맵 좌우 끝이 화면에서 닿는 위치(1=화면 끝, 낮출수록 밖이 더 보임).")]
    [SerializeField, Range(0.6f, 1f)] private float fillH = 1f;
    [Tooltip("세로 이동 한계: 위/아래로 얼마나 더 갈 수 있는지(낮출수록 위·아래로 더 이동=여백↑). 맵이 3D라 위쪽 타일 윗면 여유가 필요하면 낮춘다.")]
    [SerializeField, Range(0.4f, 1f)] private float fillV = 0.8f;

    private Camera _cam;
    private readonly CameraLimit _limit = new();

    private Camera Cam
    {
        get
        {
            if (_cam == null)
            {
                _cam = GetComponent<Camera>();
            }
            return _cam;
        }
    }

    private void OnEnable()
    {
        ApplyNow();
    }

    private void Start()
    {
        // 클램프·구독은 런타임만. 에디터에선 디자이너가 카메라를 자유롭게 잡는다.
        if (!Application.isPlaying)
        {
            return;
        }
        RebuildLimit();
        BindModules();
    }

    private void OnDestroy()
    {
        if (!Application.isPlaying)
        {
            return;
        }
        UnbindModules();
    }

    private void OnValidate()
    {
        ApplyNow();
    }

    public void ApplyNow()
    {
        ApplyLimit();
        ApplyLens();
        ApplyOrbit();
    }

    public void ClampState()
    {
        maxPitch = Mathf.Max(maxPitch, minPitch);
        maxDistance = Mathf.Max(maxDistance, minDistance);

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        distance = Mathf.Clamp(distance, minDistance, maxDistance);
    }

    // 화면에 실제로 쓰는 값. 필드는 그대로 두고 여기서만 한계를 건다.
    // 한계값이 뒤집혀 있어도 Mathf.Clamp가 조용히 min을 뱉지 않도록 max를 먼저 정렬한다.
    private float ViewPitch => Mathf.Clamp(pitch, minPitch, Mathf.Max(maxPitch, minPitch));
    private float ViewDistance => Mathf.Clamp(distance, minDistance, Mathf.Max(maxDistance, minDistance));

    // 이동(팬) 한계만 건다. 줌은 건드리지 않는다.
    // 맵 밖 빈 공간이 화면에 크게 들어오려 할 때만 focus를 되민다. 맵이 화면을 덮는 동안엔 자유 이동.
    // 판정은 맵 3D 박스 8코너(타일 높이 포함)를 뷰포트에 투영해서 하므로 가장자리 윗면이 안 잘린다.
    private void ApplyLimit()
    {
        if (!_limit.Ready)
        {
            return;
        }

        Bounds box = _limit.Area;
        Quaternion rot = Quaternion.Euler(ViewPitch, yaw, 0f);
        Vector3 right = rot * Vector3.right;          // 화면 가로 = 카메라 오른쪽(수평)
        Vector3 forwardGround = rot * Vector3.forward; // 화면 세로 = 카메라 전방의 지면 투영
        forwardGround.y = 0f;
        if (forwardGround.sqrMagnitude < 1e-6f)
        {
            return; // 거의 수직으로 내려보면 세로 축이 사라짐 → 클램프 생략
        }
        forwardGround.Normalize();

        const float eps = 0.5f;
        for (int pass = 0; pass < 6; pass++)
        {
            ProjectBox(box, focus, rot, out Vector2 nMin, out Vector2 nMax);
            float dx = Overflow(nMin.x, nMax.x, fillH);
            float dy = Overflow(nMin.y, nMax.y, fillV);
            if (Mathf.Approximately(dx, 0f) && Mathf.Approximately(dy, 0f))
            {
                break;
            }

            // focus를 축 방향으로 옮겼을 때 ndc가 얼마나 변하는지(부호 포함)를 유한차분으로 구해 역산.
            if (!Mathf.Approximately(dx, 0f))
            {
                float s = (ProjectCenter(box, focus + right * eps, rot).x - ProjectCenter(box, focus, rot).x) / eps;
                if (Mathf.Abs(s) > 1e-5f)
                {
                    focus += right * (dx / s);
                }
            }
            if (!Mathf.Approximately(dy, 0f))
            {
                float s = (ProjectCenter(box, focus + forwardGround * eps, rot).y - ProjectCenter(box, focus, rot).y) / eps;
                if (Mathf.Abs(s) > 1e-5f)
                {
                    focus += forwardGround * (dy / s);
                }
            }
        }
    }

    // 한 축의 ndc 범위[lo,hi]에 대해 필요한 보정량. 맵이 화면보다 좁으면 중앙, 넓으면 빈 쪽만 메움.
    // fill = 이 축의 이동 한계(1=화면 끝, 낮출수록 맵 끝을 화면 안쪽까지 끌어와 더 이동 가능).
    private float Overflow(float lo, float hi, float fill)
    {
        if (hi - lo <= 2f * fill)
        {
            return -(lo + hi) * 0.5f;   // 화면보다 좁음 → 중앙(중심을 0으로)
        }
        if (hi < fill)
        {
            return fill - hi;           // + 쪽에 빈 공간 → 콘텐츠를 + 방향으로
        }
        if (lo > -fill)
        {
            return -fill - lo;          // - 쪽에 빈 공간
        }
        return 0f;                       // 맵이 화면을 덮음 → 자유 이동
    }

    // 박스 8코너의 뷰포트 ndc 경계(±1이 화면 끝). focusTrial 시점 기준.
    private void ProjectBox(Bounds box, Vector3 focusTrial, Quaternion rot, out Vector2 nMin, out Vector2 nMax)
    {
        Quaternion inv = Quaternion.Inverse(rot);
        Vector3 camPos = focusTrial - rot * Vector3.forward * ViewDistance;
        float tanV = Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad);
        float tanH = tanV * Cam.aspect;

        nMin = new Vector2(float.MaxValue, float.MaxValue);
        nMax = new Vector2(float.MinValue, float.MinValue);
        for (int sx = 0; sx < 2; sx++)
        {
            for (int sy = 0; sy < 2; sy++)
            {
                for (int sz = 0; sz < 2; sz++)
                {
                    Vector3 c = new(
                        sx == 0 ? box.min.x : box.max.x,
                        sy == 0 ? box.min.y : box.max.y,
                        sz == 0 ? box.min.z : box.max.z);
                    Vector3 local = inv * (c - camPos);
                    float dz = Mathf.Max(local.z, 0.01f);
                    Vector2 nd = new(local.x / (dz * tanH), local.y / (dz * tanV));
                    nMin = Vector2.Min(nMin, nd);
                    nMax = Vector2.Max(nMax, nd);
                }
            }
        }
    }

    // 박스 중심의 ndc(유한차분 감도용).
    private Vector2 ProjectCenter(Bounds box, Vector3 focusTrial, Quaternion rot)
    {
        Quaternion inv = Quaternion.Inverse(rot);
        Vector3 camPos = focusTrial - rot * Vector3.forward * ViewDistance;
        float tanV = Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad);
        float tanH = tanV * Cam.aspect;
        Vector3 local = inv * (box.center - camPos);
        float dz = Mathf.Max(local.z, 0.01f);
        return new Vector2(local.x / (dz * tanH), local.y / (dz * tanV));
    }

    private void RebuildLimit()
    {
        _limit.Build(registry);
        ApplyNow();
    }

    // 모듈 해금 시 경계를 다시 잡아, 새로 열린 영역까지 팬이 닿게 한다.
    private void BindModules()
    {
        if (registry == null)
        {
            return;
        }
        foreach (ModuleLogic module in registry.AllModules.Values)
        {
            if (module != null)
            {
                module.OnStateChanged += OnModuleState;
            }
        }
    }

    private void UnbindModules()
    {
        if (registry == null)
        {
            return;
        }
        foreach (ModuleLogic module in registry.AllModules.Values)
        {
            if (module != null)
            {
                module.OnStateChanged -= OnModuleState;
            }
        }
    }

    private void OnModuleState(ModuleState state)
    {
        RebuildLimit();
    }

    private void ApplyLens()
    {
        float dist = ViewDistance;
        Cam.orthographic = !perspective;
        Cam.fieldOfView = fieldOfView;
        // 원근·직교를 오가도 화면상 크기가 유지되도록 distance에서 직교 크기를 유도한다.
        Cam.orthographicSize = dist * Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad);
        Cam.nearClipPlane = 0.1f;
        Cam.farClipPlane = Mathf.Max(1000f, dist * 4f);
    }

    private void ApplyOrbit()
    {
        Quaternion rot = Quaternion.Euler(ViewPitch, yaw, 0f);
        transform.rotation = rot;
        transform.position = focus - rot * Vector3.forward * ViewDistance;
    }
}
