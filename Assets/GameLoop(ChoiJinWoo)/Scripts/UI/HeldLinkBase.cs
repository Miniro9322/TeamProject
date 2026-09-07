using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class HeldLinkBase : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerExitHandler
{
    [SerializeField] private Image buttonImage;
    [SerializeField] private Color heldTint = new Color(0.55f, 0.7f, 1f);
    [SerializeField] private Color normalTint = Color.white;

    private const string HeldParameter = "Held";
    private Animator buttonAnimator;
    private ButtonPressScale pressScale;
    private bool lastHeld;

    protected virtual void Awake()
    {
        buttonAnimator = GetComponent<Animator>();
        pressScale = GetComponent<ButtonPressScale>();
    }

    protected virtual void OnEnable()
    {
        lastHeld = CheckHeld();
        ApplyHeld(lastHeld);
    }

    protected virtual void OnDisable()
    {
    }

    protected void Refresh()
    {
        bool held = CheckHeld();
        if (held == lastHeld) return;

        lastHeld = held;
        ApplyHeld(held);
    }

    protected abstract bool CheckHeld();

    private void ApplyHeld(bool held)
    {
        if (buttonAnimator != null)
        {
            buttonAnimator.SetBool(HeldParameter, held);
            ClearOtherTriggers();
        }

        pressScale?.SetHeld(held);
        buttonImage.color = SelectTint(held);
    }

    private void ClearOtherTriggers()
    {
        if (buttonAnimator == null) return;

        buttonAnimator.ResetTrigger("Normal");
        buttonAnimator.ResetTrigger("Highlighted");
        buttonAnimator.ResetTrigger("Pressed");
        buttonAnimator.ResetTrigger("Selected");
        buttonAnimator.ResetTrigger("Disabled");
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (lastHeld) ClearOtherTriggers();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (lastHeld) ClearOtherTriggers();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (lastHeld) ClearOtherTriggers();
    }

    private Color SelectTint(bool held)
    {
        if (held) return heldTint;
        return normalTint;
    }
}
