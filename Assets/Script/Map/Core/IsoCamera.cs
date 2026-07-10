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

    private Camera _cam;

    private void Awake() => _cam = GetComponent<Camera>();

    private void Start()
    {
        _cam.orthographic = true;
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        if (autoFrameOnStart) Frame();
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        // 휠 줌.
        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
            _cam.orthographicSize = Mathf.Clamp(
                _cam.orthographicSize - Mathf.Sign(scroll) * zoomSpeed, minSize, maxSize);

        // 우클릭 드래그 팬.
        if (mouse.rightButton.isPressed)
        {
            Vector2 delta = mouse.delta.ReadValue();
            float scale = _cam.orthographicSize * 0.003f * panSpeed;
            Vector3 move = (-transform.right * delta.x - transform.up * delta.y) * scale;
            transform.position += move;
        }
    }
 
    [ContextMenu("Frame Board")]
    public void Frame()
    {
        if (_cam == null) _cam = GetComponent<Camera>();
        if (board == null || board.CellCount == 0) return;

        Bounds bound = board.WorldBounds;
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Quaternion inv = Quaternion.Inverse(rot);

        // 보드 8개 코너를 카메라 시점 좌표로 투영해 담아야 할 범위를 구한다.
        float maxX = 0f, maxY = 0f, maxZ = 0f;
        Vector3 c = bound.center;
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

        float dist = maxZ + bound.size.magnitude + 10f;
        transform.rotation = rot;
        transform.position = c - rot * Vector3.forward * dist;
        _cam.nearClipPlane = 0.1f;
        _cam.farClipPlane = dist * 2f + bound.size.magnitude;
    }
}
