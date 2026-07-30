using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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

    private UpgradeState upgradeState;
    private readonly Dictionary<BaseUpgradeData, BaseUpgradeButton> nodes = new();

    private void Awake()
    {
        upgradeState = new UpgradeState();
    }

    private void Start()
    {
        CreateButton(SystemUpgradeData, SystemUpgradeParent);
        CreateButton(FacilityUpgradeData, FacilityUpgradeParent);
        CreateButton(HeroUpgradeData, HeroUpgradeParent);
        upgradeInfoPanel.ConfirmClicked += OnConfirmUnlock;
        RefreshAll();
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
        upgradeState.Unlock(data.id);
        RefreshAll();
        upgradeInfoPanel.Show(data, false); // 방금 해금했으니 확정 버튼은 다시 비활성화
    }

    private bool CanUnlock(BaseUpgradeData data) =>
        !upgradeState.IsUnlocked(data.id) &&
        data.prerequisites.All(p => upgradeState.IsUnlocked(p.id));

    private void RefreshAll()
    {
        foreach (var kv in nodes)
            kv.Value.Set(kv.Key, upgradeState.IsUnlocked(kv.Key.id));
    }
}
