using UnityEngine;

[RequireComponent(typeof(Animator))]
public class SkillSlide : MonoBehaviour
{
    private static readonly int InHash = Animator.StringToHash("SkillIn");
    private static readonly int OutHash = Animator.StringToHash("SkillOut");
    private Animator slideAnim;

    private void Awake()
    {
        slideAnim = GetComponent<Animator>();
    }

    public void Open()
    {
        gameObject.SetActive(true);
        slideAnim.Play(InHash, 0, 0f);
    }

    public void Close()
    {
        slideAnim.Play(OutHash, 0, 0f);
    }

    public void HideNow()
    {
        gameObject.SetActive(false);
    }

    public void FinishClose()
    {
        gameObject.SetActive(false);
    }
}
