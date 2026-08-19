using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GuideUI : MonoBehaviour
{
    [SerializeField] private List<SpecificGuide> gamePlayGuides;
    [SerializeField] private List<SpecificGuide> heroGuides;
    [SerializeField] private List<SpecificGuide> baseGuides;
    [SerializeField] private List<SpecificGuide> enemyGuides;
    [SerializeField] private Image guideImage;
    [SerializeField] private TextMeshProUGUI guideText;

    private List<SpecificGuide> activatedButtons = new();

    [SerializeField] private Button openButton;

    private ClickOutsideCloser outsideCloser;

    private void Awake()
    {
        outsideCloser = new ClickOutsideCloser((RectTransform)transform, openButton != null ? openButton.transform : null);
    }

    private void OnEnable()
    {
        OnGamePlayGuide();
        activatedButtons[0].GuideButton.onClick?.Invoke();
    }

    private void OnDisable()
    {
        DisableButtons();
    }

    private void Update()
    {
        if (outsideCloser.ClickedOutside()) OnCloseButton();
    }

    public void OnCloseButton()
    {
        gameObject.SetActive(false);
    }

    public void OnGamePlayGuide()
    {
        DisableButtons();

        foreach (var guide in gamePlayGuides)
        {
            guide.gameObject.SetActive(true);
            guide.GuideButton.onClick.AddListener(() => ShowSpecificGuide(guide));
            activatedButtons.Add(guide);
        }
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
    
    private void ShowSpecificGuide(SpecificGuide guide)
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
