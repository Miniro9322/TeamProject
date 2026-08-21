using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VContainer;

public class AddCitizen : MonoBehaviour
{
    private CitizenManager citizenManager;
    private ResourcesManager resourcesManager;
    private ResourceIconSet resourceIconSet;
    [SerializeField] private TMP_InputField amountInput;
    [SerializeField] private Image costIcon;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private int costAmount;
    [SerializeField] private Key closeKey = Key.Escape;
    [SerializeField] private RectTransform openButtonRect; // 이 패널을 여닫는 토글 버튼 — 바깥 클릭 판정에서 제외
    [SerializeField] private RectTransform createButtonRect; // "생성" 확인 버튼 - 튜토리얼 스포트라이트용 참조

    public RectTransform CreateButtonRect => createButtonRect;
    private int amount = 0;
    private Keyboard keyboard;
    private Mouse mouse;
    private RectTransform rectTransform;
    private int openedFrame;

    [Inject]
    private void Construct(CitizenManager citizenManager, ResourcesManager resourcesManager, ResourceIconSet resourceIconSet)
    {
        this.citizenManager = citizenManager;
        this.resourcesManager = resourcesManager;
        this.resourceIconSet = resourceIconSet;
    }

    private void Awake()
    {
        rectTransform = (RectTransform)transform;
    }

    public void OpenPanel()
    {
        if(gameObject.activeSelf)
            gameObject.SetActive(false);
        else
        {
            gameObject.SetActive(true);
            keyboard = Keyboard.current;
            mouse = Mouse.current;
            openedFrame = Time.frameCount;
            amount = 0;
            UpdatePanel();
        }
    }

    private void Update()
    {
        if (keyboard == null || mouse == null) return;
        if ((!TutorialInputGate.BlockEscapeClose && keyboard[closeKey].wasPressedThisFrame) || mouse.rightButton.wasPressedThisFrame)
        {
            gameObject.SetActive(false);
            return;
        }

        if (Time.frameCount == openedFrame) return; // 패널이 열린 바로 그 프레임의 클릭은 무시

        if (!mouse.leftButton.wasPressedThisFrame) return;

        var point = mouse.position.ReadValue();
        bool insidePanel = RectTransformUtility.RectangleContainsScreenPoint(rectTransform, point, null);
        bool onOpenButton = openButtonRect != null &&
            RectTransformUtility.RectangleContainsScreenPoint(openButtonRect, point, null);

        if (!insidePanel && !onOpenButton)
        {
            gameObject.SetActive(false);
        }
    }

    private (ProductionType Type, int Amount)[] GetCost()
    {
        return new (ProductionType Type, int Amount)[] { (ProductionType.Food, amount * -costAmount) };
    }

    private void UpdatePanel()
    {
        amountInput.text = $"{amount}";
        if (costIcon != null) costIcon.sprite = resourceIconSet.GetIcon(ProductionType.Food);
        costText.text = $"{amount * costAmount}";
        costText.color = resourcesManager.CheckResources(GetCost()) ? Color.white : Color.red;
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
        var cost = GetCost();

        if (citizenManager.CheckCanIncreaseCitizen(amount) && resourcesManager.CheckResources(cost))
        {
            citizenManager.IncreaseCitizen(amount);
            resourcesManager.ProductChanged(cost);
        }

        gameObject.SetActive(false);
    }
}
