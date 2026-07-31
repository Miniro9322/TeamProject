using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class CitizenManager : MonoBehaviour
{
    [SerializeField] private int maxCitizen;
    [SerializeField] private int currentCitizen;
    [SerializeField] private List<BaseUpgradeData> maxCitizenUpgrades;
    private int usedCitizen;

    public int MaxCitizen => maxCitizen;
    public int CurrentCitizen => currentCitizen;
    public int UsedCitizen => usedCitizen;
    public int CanUseCitizen => currentCitizen - usedCitizen;

    public event Action CitizenChanged;

    [Inject]
    private void Construct(UpgradeState upgradeState)
    {
        int bonus = (int)upgradeState.GetTotalEffect(maxCitizenUpgrades);
        maxCitizen += bonus;
        currentCitizen += bonus;
    }

    public void IncreaseMaxCitizen(int amount)
    {
        maxCitizen += amount;
        UpdateCitizen();
    }

    public bool CheckCanUseCitizen()
    {
        return CanUseCitizen > 0;
    }

    public bool CheckCanUseCitizen(int amount)
    {
        return CanUseCitizen - amount >= 0;
    }

    public void UseCitizen()
    {
        usedCitizen++;
        UpdateCitizen();
    }

    public void UseCitizen(int amount)
    {
        usedCitizen += amount;
        UpdateCitizen();
    }

    public void RecycleCitizen()
    {
        usedCitizen--;
        UpdateCitizen();
    }

    public bool CheckCanIncreaseCitizen(int amount)
    {
        if (currentCitizen + amount <= maxCitizen)
        {
            return true;
        }
        else
            return false;
    }

    public void IncreaseCitizen(int amount)
    {
        currentCitizen += amount;
        UpdateCitizen();
    }

    private void UpdateCitizen()
    {
        CitizenChanged?.Invoke();
    }
}
