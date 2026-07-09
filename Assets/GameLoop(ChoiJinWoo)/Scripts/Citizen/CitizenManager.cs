using System;
using UnityEngine;

public class CitizenManager : MonoBehaviour
{
    [SerializeField] private int maxCitizen;
    [SerializeField] private int currentCitizen;
    private int usedCitizen;
    private int canUseCitizen;

    public event Action<int, int, int, int> CitizenChanged;

    private void Start()
    {
        usedCitizen = 0;
        canUseCitizen = currentCitizen - usedCitizen;
        CitizenChanged?.Invoke(maxCitizen, currentCitizen, usedCitizen, canUseCitizen);
    }

    public void IncreaseMaxCitizen(int amount)
    {
        maxCitizen += amount;
        Debug.Log(maxCitizen);
        UpdateCitizen();
    }

    public bool CheckCanUseCitizen()
    {
        return canUseCitizen > 0;
    }

    public void UseCitizen()
    {
        canUseCitizen--;
        usedCitizen++;
        UpdateCitizen();
    }

    public void RecycleCitizen()
    {
        canUseCitizen++;
        usedCitizen--;
        UpdateCitizen();
    }

    public bool CheckCanIncreaseCitizen(int amount)
    {
        if (currentCitizen + amount <= maxCitizen)
        {
            IncreaseCitizen(amount);
            return true;
        }
        else
            return false;
    }

    private void IncreaseCitizen(int amount)
    {
        currentCitizen += amount;
        canUseCitizen += amount;
        UpdateCitizen();
    }

    private void UpdateCitizen()
    {
        CitizenChanged?.Invoke(maxCitizen, currentCitizen, usedCitizen, canUseCitizen);
    }
}
