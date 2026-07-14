using UnityEngine;
using UnityEngine.InputSystem;


[RequireComponent(typeof(Camera))]
public class IsoCamera : MonoBehaviour
{
    [Header("Angle")]
    public float yaw = 225f;
    public float pitch = 35.264f;

    [Header("Framing")]
    public MapBoard board;
    public bool autoFrameOnStart = true;
    [Range(1f, 1.6f)] public float framePadding = 1.15f;

    [Header("Control")]
    public float panSpeed = 1.5f;
    public float zoomSpeed = 4f;
    public float minSize = 2f;
    public float maxSize = 40f;

    [Header("Rotate (Play 실시간 — 오클루전 확인용)")]
    [Tooltip("Q/E = 좌우 회전(yaw), R/F = 상하 각도(pitch). 각도를 세우면 High 뒤 가려진 타일이 드러난다.")]
    public float rotateSpeed = 90f;   // 초당 회전 각(도)
    public float minPitch = 10f;
    public float maxPitch = 89f;

    private Camera _cam;
    private Vector3 _focus;    // 궤도 중심(보드 중심 기준, 팬으로 이동). 각도를 바꿔도 이 점을 바라본다.
    private float _distance;   // 궤도 반경(카메라~focus 거리)

    private void Awake() => _cam = GetComponent<Camera>();

    private void Start()
    {
        _cam.orthographic = true;
        if (autoFrameOnStart)
        {
            Frame(); // focus·distance·크기 산출 후 궤도 적용까지.
        }
        else
        {
            // 프레이밍을 안 할 땐 현재 위치를 유지하도록 focus를 시선 앞에 둔다.
            _distance = Mathf.Max(_distance, 20f);
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            _focus = transform.position + rot * Vector3.forward * _distance;
            ApplyOrbit();
        }
    }

    private void Update()
    {
        HandleZoom();
        HandleRotate(); // pitch/yaw 실시간 변경
        HandlePan();
        ApplyOrbit();   // 매 프레임 focus·distance·각도로 위치·회전 확정(인스펙터 각도 수정도 즉시 반영)
    }

    private void HandleZoom()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            _cam.orthographicSize = Mathf.Clamp(
                _cam.orthographicSize - Mathf.Sign(scroll) * zoomSpeed, minSize, maxSize);
        }
    }

    // Q/E로 yaw, R/F로 pitch를 조절. pitch는 뒤집힘 방지를 위해 [minPitch, maxPitch]로 클램프.
    private void HandleRotate()
    {
        Keyboard key = Keyboard.current;
        if (key == null)
        {
            return;
        }

        float dYaw = 0f;
        float dPitch = 0f;
        if (key.aKey.isPressed) { dYaw -= 1f; }
        if (key.dKey.isPressed) { dYaw += 1f; }
        if (key.wKey.isPressed) { dPitch += 1f; }
        if (key.sKey.isPressed) { dPitch -= 1f; }

        if (dYaw == 0f && dPitch == 0f)
        {
            return;
        }

        float step = rotateSpeed * Time.deltaTime;
        yaw += dYaw * step;
        pitch = Mathf.Clamp(pitch + dPitch * step, minPitch, maxPitch);
    }

    // 우클릭 드래그 팬. 궤도 중심(focus)을 옮겨 카메라가 따라오게 한다.
    private void HandlePan()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.rightButton.isPressed)
        {
            return;
        }

        Vector2 delta = mouse.delta.ReadValue();
        float scale = _cam.orthographicSize * 0.003f * panSpeed;
        _focus += (-transform.right * delta.x - transform.up * delta.y) * scale;
    }

    // 현재 pitch/yaw·focus·distance로 카메라 위치·회전을 확정한다.
    private void ApplyOrbit()
    {
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        transform.rotation = rot;
        transform.position = _focus - rot * Vector3.forward * _distance;
    }

    [ContextMenu("Frame Board")]
    public void Frame()
    {
        if (_cam == null)
        {
            _cam = GetComponent<Camera>();
        }

        if (board == null || board.CellCount == 0)
        {
            return;
        }

        Bounds bound = board.WorldBounds;
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Quaternion inv = Quaternion.Inverse(rot);

        // 보드 8개 코너를 카메라 시점 좌표로 투영해 담아야 할 범위를 구한다.
        float maxX = 0f, maxY = 0f, maxZ = 0f;
        Vector3 e = bound.extents;
        float[] signs = { -1f, 1f };
        foreach (float sx in signs)
        {
            foreach (float sy in signs)
            {
                foreach (float sz in signs)
                {
                    Vector3 corner = new(e.x * sx, e.y * sy, e.z * sz);
                    Vector3 v = inv * corner;
                    maxX = Mathf.Max(maxX, Mathf.Abs(v.x));
                    maxY = Mathf.Max(maxY, Mathf.Abs(v.y));
                    maxZ = Mathf.Max(maxZ, Mathf.Abs(v.z));
                }
            }
        }

        float aspect = _cam.aspect > 0.01f ? _cam.aspect : 16f / 9f;
        _cam.orthographicSize = Mathf.Max(maxY, maxX / aspect) * framePadding;

        _focus = bound.center;
        _distance = maxZ + bound.size.magnitude + 10f;
        _cam.nearClipPlane = 0.1f;
        _cam.farClipPlane = _distance * 2f + bound.size.magnitude;
        ApplyOrbit();
    }
}
