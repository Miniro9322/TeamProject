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

    // 0일차 튜토리얼 리셋용 스냅샷 - 보너스 적용 직후 값을 그대로 기억해뒀다가 Reset()에서 되돌린다.
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

    // 0일차 튜토리얼에서 지은 집/모집한 시민/일꾼 배치를 전부 시작 상태로 되돌린다.
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

    private void UpdateCitizen()
    {
        CitizenChanged?.Invoke();
    }
}
