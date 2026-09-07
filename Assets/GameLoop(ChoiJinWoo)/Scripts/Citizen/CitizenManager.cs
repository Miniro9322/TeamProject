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
    private int heroUsedCitizen;

    public int MaxCitizen => maxCitizen;
    public int CurrentCitizen => currentCitizen;
    public int UsedCitizen => usedCitizen + heroUsedCitizen;
    public int HeroUsedCitizen => heroUsedCitizen;
    public int FacilityUsedCitizen => usedCitizen;
    public int CanUseCitizen => currentCitizen - UsedCitizen;

    public event Action CitizenChanged;

    private int initialMaxCitizen;
    private int initialCurrentCitizen;

    [Inject]
    private void Construct(UpgradeState upgradeState)
    {
        int bonus = (int)upgradeState.GetTotalEffect(maxCitizenUpgrades);
        maxCitizen += bonus;
        currentCitizen += bonus;

        initialMaxCitizen = maxCitizen;
        initialCurrentCitizen = currentCitizen;
    }

    public void Reset()
    {
        maxCitizen = initialMaxCitizen;
        currentCitizen = initialCurrentCitizen;
        usedCitizen = 0;
        heroUsedCitizen = 0;

        UpdateCitizen();
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

    public void UseCitizenForHero(int amount)
    {
        heroUsedCitizen += amount;
        UpdateCitizen();
    }

    public void FreeCitizenForHero(int amount)
    {
        heroUsedCitizen -= amount;
        UpdateCitizen();
    }

    public void RecycleCitizen()
    {
        usedCitizen--;
        UpdateCitizen();
    }

    public bool CheckCanIncreaseCitizen(int amount)
    {
        return currentCitizen + amount <= maxCitizen;
    }

    public void IncreaseCitizen(int amount)
    {
        currentCitizen += amount;
        UpdateCitizen();
    }

    public void RestoreCitizen(int amount)
    {
        currentCitizen = amount;
        UpdateCitizen();
    }

    public void RestoreUsedCitizen(int facilityUsed, int heroUsed)
    {
        usedCitizen = facilityUsed;
        heroUsedCitizen = heroUsed;
        UpdateCitizen();
    }

    private void UpdateCitizen()
    {
        CitizenChanged?.Invoke();
    }
}
