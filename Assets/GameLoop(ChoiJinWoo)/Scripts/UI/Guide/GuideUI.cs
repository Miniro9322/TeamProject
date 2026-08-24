using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GuideUI : MonoBehaviour
{
    private const int ContentLeaveDelayMs = 80;

    [SerializeField] private List<SpecificGuide> gamePlayGuides;
    [SerializeField] private List<SpecificGuide> heroGuides;
    [SerializeField] private List<SpecificGuide> baseGuides;
    [SerializeField] private List<SpecificGuide> enemyGuides;
    [SerializeField] private Image guideImage;
    [SerializeField] private TextMeshProUGUI guideText;
    [SerializeField] private Animator guideCopyAnimator;
    [SerializeField] private Animator guidePictureAnimator;
    [SerializeField] private GameObject shadeObject;
    [SerializeField] private Animator shadeAnimator;

    private List<SpecificGuide> activatedButtons = new();

    [SerializeField] private Button openButton;

    private ClickOutsideCloser outsideCloser;

    private void Awake()
    {
        outsideCloser = new ClickOutsideCloser((RectTransform)transform, openButton != null ? openButton.transform : null);
    }

    private void OnEnable()
    {
        ShowShade();
        OnGamePlayGuide(true);
    }

    private void OnDisable()
    {
        HideShade();
        DisableButtons();
    }

    private void Update()
    {
        if (outsideCloser.ClickedOutside()) OnCloseButton();
        if (Keyboard.current.escapeKey.wasPressedThisFrame) OnCloseButton();
    }

    public void OnCloseButton()
    {
        gameObject.SetActive(false);
    }

    // 배경 셰이드를 켜고 페이드인 연출을 재생한다
    private void ShowShade()
    {
        if (shadeObject == null) return;
        shadeObject.SetActive(true);
        if (shadeAnimator != null) shadeAnimator.Play("FadeIn", -1, 0f);
    }

    // 배경 셰이드를 즉시 끈다
    private void HideShade()
    {
        if (shadeObject == null) return;
        shadeObject.SetActive(false);
    }

    public void OnGamePlayGuide(bool isInitialOpen = false)
    {
        DisableButtons();

        foreach (var guide in gamePlayGuides)
        {
            guide.gameObject.SetActive(true);
            guide.GuideButton.onClick.AddListener(() => ShowSpecificGuide(guide));
            activatedButtons.Add(guide);
        }

        ShowSpecificGuide(activatedButtons[0], false, !isInitialOpen);
    }

    public void OnHeroGuide()
    {
        DisableButtons();

        foreach (var guide in heroGuides)
        {
            guide.gameObject.SetActive(true);
            guide.GuideButton.onClick.AddListener(() => ShowSpecificGuide(guide));
            activatedButtons.Add(guide);
        }

        ShowSpecificGuide(activatedButtons[0], false);
    }

    public void OnBaseGuide()
    {
        DisableButtons();

        foreach (var guide in baseGuides)
        {
            guide.gameObject.SetActive(true);
            guide.GuideButton.onClick.AddListener(() => ShowSpecificGuide(guide));
            activatedButtons.Add(guide);
        }

        ShowSpecificGuide(activatedButtons[0], false);
    }

    public void OnEnemyGuide()
    {
        DisableButtons();

        foreach (var guide in enemyGuides)
        {
            guide.gameObject.SetActive(true);
            guide.GuideButton.onClick.AddListener(() => ShowSpecificGuide(guide));
            activatedButtons.Add(guide);
        }

        ShowSpecificGuide(activatedButtons[0], false);
    }

    private void DisableButtons()
    {
        foreach (var button in activatedButtons)
        {
            button.GuideButton.onClick.RemoveAllListeners();
            button.gameObject.SetActive(false);
        }

        activatedButtons.Clear();
    }
    
    private void ShowSpecificGuide(SpecificGuide guide, bool playBurst = true, bool playLeave = true)
    {
        UpdateItemSelection(guide, playBurst);
        PlayContentChangeAsync(guide, playLeave).Forget();
    }

    // 현재 카테고리 안에서 클릭한 항목만 선택 상태로 표시한다
    private void UpdateItemSelection(SpecificGuide guide, bool playBurst)
    {
        for (int i = 0; i < activatedButtons.Count; i++)
        {
            var target = activatedButtons[i];
            target.GetComponent<GuideItemHighlight>().SetSelected(target == guide, playBurst);
        }
    }

    // 기존 내용을 퇴장시키고, 내용을 바꾼 뒤 새 내용을 등장시킨다 (처음 열 때는 퇴장 단계를 건너뛴다)
    private async UniTaskVoid PlayContentChangeAsync(SpecificGuide guide, bool playLeave)
    {
        if (playLeave)
        {
            if (guideCopyAnimator != null) guideCopyAnimator.Play("Leave", -1, 0f);
            if (guidePictureAnimator != null) guidePictureAnimator.Play("Leave", -1, 0f);

            await UniTask.Delay(ContentLeaveDelayMs);
        }

        ApplyGuideContent(guide);

        if (guideCopyAnimator != null) guideCopyAnimator.Play("Arrive", -1, 0f);
        if (guidePictureAnimator != null) guidePictureAnimator.Play("Arrive", -1, 0f);
    }

    // 가이드 이미지와 본문 텍스트를 실제로 교체한다
    private void ApplyGuideContent(SpecificGuide guide)
    {
        if (guide.GuideImage == null)
            guideImage.gameObject.SetActive(false);
        else
        {
            guideImage.sprite = guide.GuideImage;
            guideImage.gameObject.SetActive(true);
        }
        guideText.text = DataTableManager.StringTable.Get(guide.GuideInfo);
    }
}
