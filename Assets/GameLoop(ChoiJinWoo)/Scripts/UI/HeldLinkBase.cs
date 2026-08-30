using UnityEngine;
using UnityEngine.UI;

// 버튼을 "눌린 모양(+색)"으로 유지하는 컴포넌트들의 공통 골격.
// 무엇을 근거로 지금 눌린 상태로 볼지는 파생 클래스가 CheckHeld()로만 결정한다.
public abstract class HeldLinkBase : MonoBehaviour
{
    [SerializeField] private Image buttonImage;
    [SerializeField] private Color heldTint = new Color(0.55f, 0.7f, 1f);
    [SerializeField] private Color normalTint = Color.white;

    private const string HeldParameter = "Held";
    private Animator buttonAnimator;
    private bool lastHeld;

    // 이 버튼의 눌린 모양을 담당하는 애니메이터를 찾아 둔다
    protected virtual void Awake()
    {
        buttonAnimator = GetComponent<Animator>();
    }

    // 시작 시점의 눌림 상태를 버튼에 한 번 반영해 둔다
    protected virtual void Start()
    {
        lastHeld = CheckHeld();
        ApplyHeld(lastHeld);
    }

    // 눌림 여부가 바뀐 순간엔 표시를 갱신하고, 눌린 채로 유지되는 동안엔 매 프레임 남은 트리거를 계속 청소한다
    // (다른 버튼 클릭으로 포커스를 빼앗기면 Selectable이 몰래 Normal 트리거를 쏘는데, 그걸 그때그때 지워야
    // Held 상태가 Any State 전이에 떠밀려 풀리지 않는다)
    protected virtual void Update()
    {
        bool held = CheckHeld();
        if (held != lastHeld)
        {
            lastHeld = held;
            ApplyHeld(held);
            return;
        }

        if (held) ClearOtherTriggers();
    }

    // 지금 눌린 상태로 봐야 하는지는 파생 클래스가 판단한다
    protected abstract bool CheckHeld();

    // 눌린 모양과 색을 함께 반영한다
    private void ApplyHeld(bool held)
    {
        if (buttonAnimator != null) buttonAnimator.SetBool(HeldParameter, held);
        ClearOtherTriggers();
        buttonImage.color = SelectTint(held);
    }

    // 대기 중인 다른 트리거가 있으면 Held 전환을 가로채므로, 전환할 때마다 비워 둔다.
    // ButtonPressScale 기반의 새 버튼처럼 Animator 없이 스케일만으로 피드백을 주는 경우엔
    // 애초에 트리거로 가로챌 게 없으니 조용히 건너뛴다(Animator 필수였던 예전 제약을 없앤다).
    private void ClearOtherTriggers()
    {
        if (buttonAnimator == null) return;

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
