using System;
using UnityEngine;

public class CitizenManager : MonoBehaviour
{
    [SerializeField] private int maxCitizen;
    [SerializeField] private int currentCitizen;
    private int usedCitizen;
    private int canUseCitizen;

    public int MaxCitizen => maxCitizen;
    public int CurrentCitizen => currentCitizen;
    public int UsedCitizen => usedCitizen;
    public int CanUseCitizen => canUseCitizen;

    public event Action CitizenChanged;

    private void Start()
    {
        usedCitizen = 0;
        canUseCitizen = currentCitizen - usedCitizen;
        CitizenChanged?.Invoke();
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

    public bool CheckCanUseCitizen(int amount)
    {
        return canUseCitizen - amount >= 0;
    }

    public void UseCitizen()
    {
        canUseCitizen--;
        usedCitizen++;
        UpdateCitizen();
    }

    public void UseCitizen(int amount)
    {
        canUseCitizen -= amount;
        usedCitizen += amount;
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
            return true;
        }
        else
            return false;
    }

    public void IncreaseCitizen(int amount)
    {
        currentCitizen += amount;
        canUseCitizen += amount;
        UpdateCitizen();
    }

    private void UpdateCitizen()
    {
        CitizenChanged?.Invoke();
    }
}
