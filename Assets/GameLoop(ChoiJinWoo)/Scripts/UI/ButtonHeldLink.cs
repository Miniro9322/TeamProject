using UnityEngine;
using UnityEngine.UI;

// 지정한 패널이 열려 있는 동안 이 버튼을 눌린 모양(+선택색)으로 붙잡아 둔다.
// 눌린 모양은 애니메이터의 Held 파라미터가, 색은 이 스크립트가 담당한다.
public class ButtonHeldLink : MonoBehaviour
{
    [SerializeField] private GameObject watchedPanel;
    [SerializeField] private Image buttonImage;
    [SerializeField] private Color heldTint = new Color(0.502f, 0.361f, 0.204f);
    [SerializeField] private Color normalTint = Color.white;

    private const string HeldParameter = "Held";
    private Animator buttonAnimator;
    private bool lastHeld;

    // 이 버튼의 눌린 모양을 담당하는 애니메이터를 찾아 둔다
    private void Awake()
    {
        buttonAnimator = GetComponent<Animator>();
    }

    // 시작 시점의 패널 상태를 버튼에 한 번 반영해 둔다
    private void Start()
    {
        lastHeld = watchedPanel.activeSelf;
        ApplyHeld(lastHeld);
    }

    // 패널이 열리거나 닫힌 순간만 골라 버튼 표시를 갱신한다
    private void Update()
    {
        bool held = watchedPanel.activeSelf;
        if (held == lastHeld) return;

        lastHeld = held;
        ApplyHeld(held);
    }

    // 눌린 모양과 색을 함께 반영한다
    private void ApplyHeld(bool held)
    {
        buttonAnimator.SetBool(HeldParameter, held);
        ClearOtherTriggers();
        buttonImage.color = SelectTint(held);
    }

    // 대기 중인 다른 트리거가 있으면 Held 전환을 가로채므로, 전환할 때마다 비워 둔다
    private void ClearOtherTriggers()
    {
        buttonAnimator.ResetTrigger("Normal");
        buttonAnimator.ResetTrigger("Highlighted");
        buttonAnimator.ResetTrigger("Pressed");
        buttonAnimator.ResetTrigger("Selected");
        buttonAnimator.ResetTrigger("Disabled");
    }

    // 눌림 여부에 맞는 색을 고른다
    private Color SelectTint(bool held)
    {
        if (held) return heldTint;
        return normalTint;
    }
}
