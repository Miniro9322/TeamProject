using UnityEngine;
using UnityEngine.UI;

public class BuildFacilityPanel : MonoBehaviour
{
    [SerializeField] private MapView view;
    [SerializeField] private MapGame game;
    [SerializeField] private Button foodFacility;
    [SerializeField] private Button goldFacility;
    [SerializeField] private Button stoneFacility;
    [SerializeField] private Button woodFacility;
    [SerializeField] private Button ironFacility;
    [SerializeField] private Button house;

    private void OnEnable()
    {
        game.Placer.resourcesManager.ProductUpdate += ButtonUpdate; 

        ButtonUpdate();
    }

    private void OnDisable()
    {
        game.Placer.resourcesManager.ProductUpdate -= ButtonUpdate;
    }

    public void OnBuild(string label)
    {
        view.SetUnit(label);
    }

    private void ButtonUpdate()
    {
        foodFacility.interactable = view.CheckCanBuild("Food");
        goldFacility.interactable = view.CheckCanBuild("Gold");
        stoneFacility.interactable = view.CheckCanBuild("Stone");
        woodFacility.interactable = view.CheckCanBuild("Wood");
        ironFacility.interactable = view.CheckCanBuild("Iron");
        house.interactable = view.CheckCanBuild("House");

        if (view.IsPlacing && !view.CheckCanBuild(view.PlacingLabel))
            view.ClearMode();
    }
}
