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
    public int CanUseCitizen => currentCitizen - UsedCitizen;

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

    // 세이브 데이터로 현재 시민 수를 그대로 덮어쓴다 (로드 복원 전용)
    public void RestoreCitizen(int amount)
    {
        currentCitizen = amount;
        UpdateCitizen();
    }

    // 시설·영웅 사용 시민 합계로 파생값을 다시 맞춘다 (기반시설·로스터 복원 후 1회, 로드 복원 전용)
    public void RecalculateUsedCitizen(int facilityUsed, int heroUsed)
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
