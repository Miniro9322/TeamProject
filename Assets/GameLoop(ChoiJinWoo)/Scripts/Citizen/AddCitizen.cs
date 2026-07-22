using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VContainer;

public class AddCitizen : MonoBehaviour
{
    private CitizenManager citizenManager;
    [SerializeField] private TMP_InputField amountInput;
    private int amount = 0;

    [Inject]
    private void Construct(CitizenManager citizenManager)
    {
        this.citizenManager = citizenManager;
    }

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    public void OpenPanel()
    {
        gameObject.SetActive(true);
        amountInput.text = $"{0}";
        amount = 0;
    }

    private void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            gameObject.SetActive(false);
        }
    }

    public void ChangeAmount(string amount)
    {
        if (int.TryParse(amount, out this.amount))
        {
            this.amount = Mathf.Clamp(this.amount, 0, citizenManager.MaxCitizen - citizenManager.CurrentCitizen);
            amountInput.text = $"{this.amount}";
        }
        else
        {
            amountInput.text = $"{this.amount}";
        }
    }

    public void IncreaseAmount()
    {
        if(amount + citizenManager.CurrentCitizen < citizenManager.MaxCitizen)
            amount++;
        amountInput.text = $"{this.amount}";
    }

    public void DecreaseAmount()
    {
        if(amount > 0)
            amount--;
        amountInput.text = $"{this.amount}";
    }


    public void CreateCitizen()
    {
        if (citizenManager.CheckCanIncreaseCitizen(amount))
        {
            citizenManager.IncreaseCitizen(amount); 
        }

        gameObject.SetActive(false);
    }
}
