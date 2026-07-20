using UnityEngine;
using UnityEngine.InputSystem;


// 입력만 담당한다. CameraRig의 궤도 값을 직접 고치고 반영은 Rig에 맡긴다.
// 감도 값을 전부 여기서 소유하므로, 조작 방식을 바꿔도 Rig는 건드리지 않는다.
public class CameraInput : MonoBehaviour
{
    [SerializeField] private CameraRig rig;

    [Header("Sensitivity")]
    [Tooltip("마우스 델타 1픽셀당 회전 각(도).")]
    [Range(0.02f, 1f)] public float rotateSensitivity = 0.2f;
    [Tooltip("휠 한 칸당 거리 변화 비율.")]
    [Range(0.02f, 0.4f)] public float zoomStep = 0.1f;
    public float dragSpeed = 1.5f;
    public float keySpeed = 20f;

    private void Reset()
    {
        rig = GetComponent<CameraRig>();
    }

    // 같은 우클릭 드래그를 Alt 여부로 회전/팬으로 가른다.
    private static bool AltHeld
    {
        get
        {
            Keyboard key = Keyboard.current;
            return key != null && (key.leftAltKey.isPressed || key.rightAltKey.isPressed);
        }
    }

    private void Update()
    {
        if (rig == null)
        {
            return;
        }

        bool moved = Rotate();
        moved |= Pan();
        moved |= Zoom();

        // 입력이 있었던 프레임에만 필드를 되받아쓴다.
        // 매 프레임 클램프하면 Play 중 인스펙터 타이핑을 덮어써서 값이 튄다.
        if (moved)
        {
            rig.ClampState();
        }
        rig.ApplyNow();
    }

    // Alt + 우클릭 드래그 = 궤도 회전. 좌클릭은 MapGame이 배치에 쓰므로 카메라가 잡지 않는다.
    private bool Rotate()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.rightButton.isPressed || !AltHeld)
        {
            return false;
        }

        Vector2 delta = mouse.delta.ReadValue();
        if (delta == Vector2.zero)
        {
            return false;
        }

        rig.yaw += delta.x * rotateSensitivity;
        rig.pitch -= delta.y * rotateSensitivity;
        return true;
    }

    // 우클릭 드래그 = 화면 기준 팬. WASD = 수평 팬, QE = 높이.
    private bool Pan()
    {
        Transform view = rig.transform;
        bool moved = false;

        // 지면(XZ)에 투영한 축. 카메라 up을 그대로 쓰면 pitch만큼 월드 Y가 섞여
        // 위아래 드래그에 맵이 뜨거나 가라앉는다. 높이는 QE로만 바꾼다.
        Vector3 forward = Vector3.ProjectOnPlane(view.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(view.right, Vector3.up).normalized;

        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.rightButton.isPressed && !AltHeld)
        {
            Vector2 delta = mouse.delta.ReadValue();
            if (delta != Vector2.zero)
            {
                float scale = rig.distance * 0.002f * dragSpeed;
                rig.focus += (-right * delta.x - forward * delta.y) * scale;
                moved = true;
            }
        }

        Keyboard key = Keyboard.current;
        if (key == null)
        {
            return moved;
        }

        Vector3 move = Vector3.zero;
        if (key.wKey.isPressed) { move += forward; }
        if (key.sKey.isPressed) { move -= forward; }
        if (key.dKey.isPressed) { move += right; }
        if (key.aKey.isPressed) { move -= right; }
        if (key.eKey.isPressed) { move += Vector3.up; }
        if (key.qKey.isPressed) { move -= Vector3.up; }

        if (move != Vector3.zero)
        {
            rig.focus += move.normalized * (keySpeed * Time.unscaledDeltaTime);
            moved = true;
        }
        return moved;
    }

    // 휠 = 궤도 반경. 원근이므로 거리로 당긴다.
    private bool Zoom()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return false;
        }

        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) <= 0.01f)
        {
            return false;
        }

        rig.distance -= Mathf.Sign(scroll) * rig.distance * zoomStep;
        return true;
    }
}
