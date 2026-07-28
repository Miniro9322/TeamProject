using UnityEngine;

// 카메라의 궤도 값(초점·좌우각·상하각·거리)을 실제 트랜스폼과 렌즈에 반영한다.
// 값을 누가 어떻게 바꾸는지는 관여하지 않는다 — 조작은 CameraInput, 이동 범위는 ExpandFocus가 넣어준다.
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
    [Tooltip("가로 이동 한계: 맵 좌우 끝이 화면에서 닿는 위치(1=화면 끝, 낮출수록 밖이 더 보임).")]
    [SerializeField, Range(0.2f, 1f)] private float fillH = 1f;
    [Tooltip("세로 이동 한계: 낮출수록 위·아래로 더 이동(여백↑). 위쪽 타일 윗면 여유가 필요하면 낮춘다.")]
    [SerializeField, Range(0.4f, 1f)] private float fillV = 0.8f;

    private Camera cam;
    private readonly CameraClamp clamp = new();
    private Bounds area;
    private bool hasArea;

    private Quaternion Rotation => Quaternion.Euler(pitch, yaw, 0f);

    private void OnEnable()
    {
        cam = GetComponent<Camera>();
        ApplyNow();
    }

    // 인스펙터에서 값을 고치면 Play 없이 바로 보여준다. 여기서 값을 되돌리지는 않는다 —
    // 타이핑 도중의 중간 값(150을 치려고 누른 1)에 반응하면 다른 값까지 덮어써 버린다.
    private void OnValidate()
    {
        cam = GetComponent<Camera>();
        ApplyNow();
    }

    // 이동 가능 영역. ExpandFocus가 모듈 해금 때마다 갱신해 넣는다. 넣기 전엔 클램프가 놀고 있다.
    public void SetArea(Bounds next)
    {
        area = next;
        hasArea = true;
        ApplyNow();
    }

    public void ApplyNow()
    {
        ApplyLimit();
        ApplyLens();
        ApplyOrbit();
    }

    // 범위를 벗어난 값이 들어왔을 때 되돌린다. 입력이 있었던 프레임에만 CameraInput이 부른다.
    // (매 프레임 부르면 Play 중 인스펙터 타이핑을 덮어써서 값이 튄다.)
    public void ClampState()
    {
        maxPitch = Mathf.Max(maxPitch, minPitch);
        maxDistance = Mathf.Max(maxDistance, minDistance);
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        distance = Mathf.Clamp(distance, minDistance, maxDistance);
    }

    private void ApplyLimit()
    {
        if (!hasArea)
        {
            return;
        }
        focus = clamp.FitFocus(focus, area, Rotation, distance, fieldOfView, cam.aspect, fillH, fillV);
    }

    private void ApplyLens()
    {
        cam.orthographic = !perspective;
        cam.fieldOfView = fieldOfView;
        // 원근·직교를 오가도 화면상 크기가 유지되도록 distance에서 직교 크기를 유도한다.
        cam.orthographicSize = distance * Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad);
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = Mathf.Max(1000f, distance * 4f);
    }

    private void ApplyOrbit()
    {
        Quaternion rot = Rotation;
        transform.rotation = rot;
        transform.position = focus - rot * Vector3.forward * distance;
    }
}
