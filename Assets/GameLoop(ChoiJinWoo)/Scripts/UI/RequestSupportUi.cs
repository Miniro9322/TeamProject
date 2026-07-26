using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using VContainer;

public class RequestSupportUi : MonoBehaviour
{
    [SerializeField] private List<SupportRegion> supportList;
    [SerializeField] private GameObject firstChoicePanel;
    [SerializeField] private GameObject secondChoicePanel;
    private List<SupportRegion> supportListCopy = new();
    private SupportRegion firstChoice;
    private SupportRegion secondChoice;

    public event Action<byte> OnUnlock;

    [SerializeField] private Button firstButton;
    [SerializeField] private Button secondButton;
    [SerializeField] private TextMeshProUGUI firstLabel;
    [SerializeField] private TextMeshProUGUI secondLabel;
    [SerializeField] private TextMeshProUGUI firstChoiceText;
    [SerializeField] private TextMeshProUGUI secondChoiceText;

    private ExpandEvent expand;

    [Inject]
    private void Construct(ExpandEvent expand)
    {
        this.expand = expand;
    }

    private void Awake()
    {
        foreach(var support in supportList)
        {
            supportListCopy.Add(support);
        }
    }

    private void OnEnable()
    {
        expand.ShowChoices();

        if (supportListCopy != null && supportListCopy.Count >= 2)
        {
            firstChoice = supportListCopy[UnityEngine.Random.Range(0, supportListCopy.Count)];
            firstChoiceText.text = $"영웅 해금 : {firstChoice.UnlockHero}";
            while (true)
            {
                secondChoice = supportListCopy[UnityEngine.Random.Range(0, supportListCopy.Count)];
                if (firstChoice != secondChoice) break;
            }
            secondChoiceText.text = $"영웅 해금 : {secondChoice.UnlockHero}";
        }
        else if(supportListCopy.Count != 0)
        {
            firstChoice = supportListCopy[0];
            secondChoicePanel.SetActive(false); 
        }
 
        if (expand.Choices.Count == 0)
        {
            gameObject.SetActive(false);
            return;
        }

        FillButton(firstButton, firstLabel, supportListCopy.IndexOf(firstChoice));
        FillButton(secondButton, secondLabel, supportListCopy.IndexOf(secondChoice));
    }

    public void FirstButton()
    {
        OnUnlock?.Invoke((byte)firstChoice.UnlockHero);
        SelectChoice(supportListCopy.IndexOf(firstChoice));
        supportListCopy.Remove(firstChoice);
        gameObject.SetActive(false);
    }

    public void SecondButton()
    {
        OnUnlock?.Invoke((byte)secondChoice.UnlockHero);
        SelectChoice(supportListCopy.IndexOf(secondChoice));
        supportListCopy.Remove(secondChoice);
        gameObject.SetActive(false);
    }

    private void FillButton(Button button, TextMeshProUGUI label, int index)
    {
        Debug.Log($"버튼 인덱스 : {index}");
        bool has = index < expand.Choices.Count;
        button.gameObject.SetActive(has);
        if (has)
        {
            label.text = $"지역 {expand.Choices[index].ModuleId}";
        }
    }

    private void SelectChoice(int index)
    {
        Debug.Log($"인덱스 : {index}");
        if (index < expand.Choices.Count)
        {
            Debug.Log($"지역 {expand.Choices[index].ModuleId} 확장");
            expand.SelectModule(expand.Choices[index]);
        }

        gameObject.SetActive(false);
    }
}
