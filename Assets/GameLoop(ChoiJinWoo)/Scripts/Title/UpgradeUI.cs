using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeUI : MonoBehaviour
{
    [SerializeField] private List<Button> SystemUpgradeButtons;
    [SerializeField] private List<Button> FacilityUpgradeButtons;
    [SerializeField] private List<Button> HeroUpgradeButtons;
    [SerializeField] private GameObject upgradeInfoPanel;

    private void OnEnable()
    {
        
    }
}
