using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UpgradeUI : MonoBehaviour
{
    [SerializeField] private List<BaseUpgradeData> SystemUpgradeData;
    [SerializeField] private List<GameObject> SystemUpgradeParent;
    [SerializeField] private List<BaseUpgradeData> FacilityUpgradeData;
    [SerializeField] private List<GameObject> FacilityUpgradeParent;
    [SerializeField] private List<BaseUpgradeData> HeroUpgradeData;
    [SerializeField] private List<GameObject> HeroUpgradeParent;
    [SerializeField] private UpgradeInfoUI upgradeInfoPanel;
    [SerializeField] private BaseUpgradeButton buttonPrefab;
    [SerializeField] private TextMeshProUGUI pointsText;
    [SerializeField] private RectTransform openButtonRect; // 이 패널을 여는 버튼 — 바깥 클릭 판정에서 제외

    [Header("치트 (테스트용)")]
    [SerializeField] private Key cheatAddPointsKey = Key.F1;
    [SerializeField] private int cheatAddPointsAmount = 100;

    private UpgradeState upgradeState;
    private RectTransform rectTransform;
    private int openedFrame;
    private readonly Dictionary<BaseUpgradeData, BaseUpgradeButton> nodes = new();

    private void Awake()
    {
        upgradeState = new UpgradeState();
        rectTransform = (RectTransform)transform;
    }

    private void OnEnable()
    {
        openedFrame = Time.frameCount;
    }

    private void Update()
    {
        if (Keyboard.current[cheatAddPointsKey].wasPressedThisFrame)
        {
            upgradeState.AddPoints(cheatAddPointsAmount);
            RefreshAll();
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            gameObject.SetActive(false);
            return;
        }

        if (Time.frameCount == openedFrame) return; // 패널이 열린 바로 그 프레임의 클릭은 무시
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        var point = Mouse.current.position.ReadValue();
        bool insidePanel = RectTransformUtility.RectangleContainsScreenPoint(rectTransform, point, null);
        bool onOpenButton = openButtonRect != null &&
            RectTransformUtility.RectangleContainsScreenPoint(openButtonRect, point, null);

        if (!insidePanel && !onOpenButton)
        {
            gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        CreateButton(SystemUpgradeData, SystemUpgradeParent);
        CreateButton(FacilityUpgradeData, FacilityUpgradeParent);
        CreateButton(HeroUpgradeData, HeroUpgradeParent);
        upgradeInfoPanel.ConfirmClicked += OnConfirmUnlock;
        RefreshAll();
        OnNodeClicked(nodes.First().Key);
    }

    private void CreateButton(List<BaseUpgradeData> datas, List<GameObject> parents)
    {
        int i = 0;

        foreach (var data in datas)
        {
            var node = Instantiate(buttonPrefab, parents[i].transform);
            ((RectTransform)node.transform).anchoredPosition = data.anchoredPosition;
            node.Clicked += OnNodeClicked;
            nodes[data] = node;
            if (i < parents.Count - 1)
                i++;
            else
                i = 0;
        }
    }

    private void OnNodeClicked(BaseUpgradeData data)
    {
        bool alreadyUnlocked = upgradeState.IsUnlocked(data.id);
        upgradeInfoPanel.Show(data, !alreadyUnlocked && CanUnlock(data));
    }

    private void OnConfirmUnlock(BaseUpgradeData data)
    {
        if (!CanUnlock(data)) return;
        if (!upgradeState.TrySpendPoints(data.cost)) return;

        upgradeState.Unlock(data.id);
        RefreshAll();
        upgradeInfoPanel.Show(data, false); // 방금 해금했으니 확정 버튼은 다시 비활성화
    }

    private bool CanUnlock(BaseUpgradeData data) =>
        !upgradeState.IsUnlocked(data.id) &&
        upgradeState.CanAfford(data.cost) &&
        data.prerequisites.All(p => upgradeState.IsUnlocked(p.id));

    private void RefreshAll()
    {
        foreach (var kv in nodes)
            kv.Value.Set(kv.Key, upgradeState.IsUnlocked(kv.Key.id));

        if (pointsText != null)
            pointsText.text = upgradeState.Points.ToString();
    }

    // 리셋 버튼의 OnClick에 연결 — 해금됐던 업그레이드들의 cost를 환불하고 전부 리셋(리스펙)
    public void OnResetButton()
    {
        upgradeState.ResetAll(nodes.Keys);
        RefreshAll();
    }

    // 디버그용 리셋 버튼의 OnClick에 연결 — 자원 환불 없이 해금 상태만 초기화
    public void OnDebugResetButton()
    {
        upgradeState.DebugResetWithoutRefund();
        RefreshAll();
    }

    public void OnClose()
    {
        gameObject.SetActive(false);
    }
}
