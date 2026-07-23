using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using VContainer;

public class RequestSupportUi : MonoBehaviour
{
    [SerializeField] private List<SupportRegion> supportList;
    private List<SupportRegion> supportListCopy = new();
    private SupportRegion firstChoice;
    private SupportRegion secondChoice;

    public event Action<byte> OnUnlock;

    [SerializeField] private Button firstButton;
    [SerializeField] private Button secondButton;
    [SerializeField] private TextMeshProUGUI firstLabel;
    [SerializeField] private TextMeshProUGUI secondLabel;

    // 선택지는 버튼 수만큼.
    private const int BUTTONCOUNT = 2;

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
        if (supportList != null && supportList.Count > 0)
        {
            firstChoice = supportList[Random.Range(0, supportList.Count)];
            secondChoice = supportList[Random.Range(0, supportList.Count)];
        }

        expand.ShowChoices(BUTTONCOUNT);

 
        if (expand.Choices.Count == 0)
        {
            gameObject.SetActive(false);
            return;
        }

        FillButton(firstButton, firstLabel, 0);
        FillButton(secondButton, secondLabel, 1);
    }

    public void FirstButton()
    {
        Debug.Log((byte)firstChoice.UnlockHero);
        OnUnlock?.Invoke((byte)firstChoice.UnlockHero);
        supportListCopy.Remove(firstChoice);
        SelectChoice(0);
        gameObject.SetActive(false);
    }

    public void SecondButton()
    {
        OnUnlock?.Invoke((byte)secondChoice.UnlockHero);
        supportListCopy.Remove(secondChoice);
        SelectChoice(1);
        gameObject.SetActive(false);
    }

    private void FillButton(Button button, TextMeshProUGUI label, int index)
    {
        bool has = index < expand.Choices.Count;
        button.gameObject.SetActive(has);
        if (has)
        {
            label.text = $"지역 {expand.Choices[index].ModuleId}";
        }
    }

    private void SelectChoice(int index)
    {
        if (index < expand.Choices.Count)
        {
            expand.SelectModule(expand.Choices[index]);
        }

        gameObject.SetActive(false);
    }
}
