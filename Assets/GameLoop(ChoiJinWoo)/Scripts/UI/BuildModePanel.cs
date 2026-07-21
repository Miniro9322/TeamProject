using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BuildModePanel : MonoBehaviour
{
    [SerializeField] private GameObject facilityPanel;
    [SerializeField] private GameObject heroPanel;
    [SerializeField] private Button facilityButton;
    [SerializeField] private Button heroButton;
    [SerializeField] private Button removeButton;
    [SerializeField] private MapView view;

    private void Awake()
    {
        facilityPanel.SetActive(false);
        heroPanel.SetActive(false);
    }

    private void Update()
    {
        if (!Keyboard.current.escapeKey.wasPressedThisFrame) return;

        if (view.IsPlacing)
        {
            view.ClearMode();
        }
        else if (facilityPanel.activeSelf || heroPanel.activeSelf)
        {
            facilityPanel.SetActive(false);
            heroPanel.SetActive(false);
        }
    }

    public void OnFacilityButton()
    {
        if (heroPanel.activeSelf == true)
            heroPanel.SetActive(false);
        facilityPanel.SetActive(true);
    }

    public void OnHeroButton()
    {
        if (facilityPanel.activeSelf == true)
            facilityPanel.SetActive(false);
        heroPanel.SetActive(true);
    }

    public void OnRemoveButton()
    {
        if (facilityPanel.activeSelf == true)
            facilityPanel.SetActive(false);
        if (heroPanel.activeSelf == true)
            heroPanel.SetActive(false);
        view.SetRemove();
    }
}
