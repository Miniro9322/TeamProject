using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VContainer;

public class AddCitizen : MonoBehaviour
{
    private CitizenManager citizenManager;
    private ResourcesManager resourcesManager;
    [SerializeField] private TMP_InputField amountInput;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private int costAmount;
    private int amount = 0;

    [Inject]
    private void Construct(CitizenManager citizenManager, ResourcesManager resourcesManager)
    {
        this.citizenManager = citizenManager;
        this.resourcesManager = resourcesManager;
    }

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    public void OpenPanel()
    {
        gameObject.SetActive(true);
        amount = 0;
        UpdatePanel();
    }

    private void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            gameObject.SetActive(false);
        }
    }

    private void UpdatePanel()
    {
        var cost = new Dictionary<ProductionType, int>()
        {
            {ProductionType.Food, amount * -costAmount }
        };
        amountInput.text = $"{amount}";
        costText.text = $"ºñ¿ë: {ProductionType.Food} {amount * costAmount}";
        costText.color = resourcesManager.CheckResources(cost) ? Color.white : Color.red;
    }

    public void ChangeAmount(string amount)
    {
        if (int.TryParse(amount, out this.amount))
        {
            this.amount = Mathf.Clamp(this.amount, 0, citizenManager.MaxCitizen - citizenManager.CurrentCitizen);
            UpdatePanel();
        }
        else
        {
            UpdatePanel();
        }
    }

    public void IncreaseAmount()
    {
        if(amount + citizenManager.CurrentCitizen < citizenManager.MaxCitizen)
            amount++;
        UpdatePanel();
    }

    public void DecreaseAmount()
    {
        if(amount > 0)
            amount--;
        UpdatePanel();
    }

    public void IncreaseTen()
    {
        if (amount + citizenManager.CurrentCitizen < citizenManager.MaxCitizen)
            amount = Mathf.Clamp(amount + 10, 0, citizenManager.MaxCitizen - citizenManager.CurrentCitizen);
        UpdatePanel();
    }

    public void DecreaseTen()
    {
        if (amount > 0)
            amount = Mathf.Clamp(amount - 10, 0, citizenManager.MaxCitizen - citizenManager.CurrentCitizen);
        UpdatePanel();
    }

    public void CreateCitizen()
    {
        var cost = new Dictionary<ProductionType, int>()
        {
            {ProductionType.Food, amount * -costAmount }
        };

        if (citizenManager.CheckCanIncreaseCitizen(amount) && resourcesManager.CheckResources(cost))
        {
            citizenManager.IncreaseCitizen(amount);
            resourcesManager.ProductChanged(cost);
        }

        gameObject.SetActive(false);
    }
}
