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

    [Header("Limit")]
    public float minPitch = 5f;
    public float maxPitch = 89f;
    public float minDistance = 5f;
    public float maxDistance = 120f;

    private Camera _cam;

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

    private void OnValidate()
    {
        ApplyNow();
    }

    public void ApplyNow()
    {
        ApplyLens();
        ApplyOrbit();
    }

    /// <summary>
    /// 입력이나 코드로 값을 바꾼 뒤 호출한다. 필드 자체를 한계 안으로 되받아쓴다.
    /// OnValidate에서는 절대 부르지 않는다. 인스펙터는 키 입력마다 값을 적용하므로,
    /// 타이핑 중간값(45를 치는 도중의 4)까지 클램프하면 필드 텍스트가 덮어써져
    /// 다음 글자가 그 뒤에 붙는다("4"→10→"105"→89). 그래서 편집 중에는 손대지 않는다.
    /// </summary>
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
