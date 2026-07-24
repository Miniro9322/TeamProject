using UnityEngine;
using UnityEngine.InputSystem;

// 테스트용: U 키를 누르면 다음 잠긴 모듈을 즉시 해금한다. 확장→트레일 확인용. 정식 빌드에서 제거.
public class ExpandTester : MonoBehaviour
{
    [SerializeField] private MapRegistry registry;

    private void Update()
    {
        if (registry == null)
        {
            return;
        }
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }
        if (keyboard.uKey.wasPressedThisFrame)
        {
            bool opened = registry.UnlockNextModule();
            Debug.Log(opened ? "[ExpandTester] 다음 모듈 해금" : "[ExpandTester] 더 해금할 모듈 없음");
        }
    }
}
