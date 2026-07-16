using System.Collections.Generic;
using UnityEngine;

public class RequestSupportUi : MonoBehaviour
{
    [SerializeField] private List<SupportRegion> supportList;
    private SupportRegion firstChoice;
    private SupportRegion secondChoice;

    private void OnEnable()
    {
        if (supportList == null || supportList.Count == 0)
            return;

        firstChoice = supportList[Random.Range(0, supportList.Count)];
        secondChoice = supportList[Random.Range(0, supportList.Count)];
    }

    public void FirstButton()
    {
        gameObject.SetActive(false);
    }

    public void SecondButton()
    {
        gameObject.SetActive(false);
    }
}
