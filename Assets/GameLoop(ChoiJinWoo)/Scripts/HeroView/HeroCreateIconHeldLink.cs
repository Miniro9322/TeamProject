using UnityEngine;
using UnityEngine.UI;

// HeroCreateAmountPanel처럼 근접/원거리 아이콘 2개가 패널 하나를 같이 쓸 때,
// 패널이 켜져 있는지뿐 아니라 그 패널을 연 게 나 자신인지까지 함께 확인해서 눌린 모양을 유지한다.
// 패널 하나 : 버튼 하나뿐인 일반 케이스는 ButtonHeldLink를 그대로 쓴다.
public class HeroCreateIconHeldLink : MonoBehaviour
{
    [SerializeField] private GameObject watchedPanel;
    [SerializeField] private HeroSetPanel ownerSource;
    [SerializeField] private HeroCreateIcon ownerIdentity;
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

    // 시작 시점의 소유 상태를 버튼에 한 번 반영해 둔다
    private void Start()
    {
        lastHeld = CheckOwned();
        ApplyHeld(lastHeld);
    }

    // 소유 여부가 바뀐 순간만 골라 버튼 표시를 갱신한다
    private void Update()
    {
        bool held = CheckOwned();
        if (held == lastHeld) return;

        lastHeld = held;
        ApplyHeld(held);
    }

    // 패널이 켜져 있고, 그 패널을 연 게 나 자신인지 함께 확인한다
    private bool CheckOwned()
    {
        return watchedPanel.activeSelf && ownerSource.OpenIcon == ownerIdentity;
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
