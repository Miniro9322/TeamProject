using System;
using System.Collections.Generic;
using UnityEngine;

public class RequestSupportUi : MonoBehaviour
{
    [SerializeField] private List<SupportRegion> supportList;
    private List<SupportRegion> supportListCopy = new();
    private SupportRegion firstChoice;
    private SupportRegion secondChoice;

    public event Action<byte> OnUnlock;

    private void Awake()
    {
        foreach(var support in supportList)
        {
            supportListCopy.Add(support);
        }
    }

    private void OnEnable()
    {
        if (supportList == null || supportList.Count == 0)
            return;

        firstChoice = supportListCopy[UnityEngine.Random.Range(0, supportListCopy.Count)];
        secondChoice = supportListCopy[UnityEngine.Random.Range(0, supportListCopy.Count)];
    }

    public void FirstButton()
    {
        Debug.Log((byte)firstChoice.UnlockHero);
        OnUnlock?.Invoke((byte)firstChoice.UnlockHero);
        supportListCopy.Remove(firstChoice);
        gameObject.SetActive(false);
    }

    public void SecondButton()
    {
        OnUnlock?.Invoke((byte)secondChoice.UnlockHero);
        supportListCopy.Remove(secondChoice);
        gameObject.SetActive(false);
    }
}
