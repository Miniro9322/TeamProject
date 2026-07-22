using UnityEngine;
using UnityEngine.UI;

public class HeroSetPanel : MonoBehaviour
{
    [SerializeField] private MapView view;
    [SerializeField] private MapGame game;
    [SerializeField] private Button meleeButton;
    [SerializeField] private Button rangeButton;
    [SerializeField] private Button dualBladerButton;
    [SerializeField] private Button spearManButton;

    private void OnEnable()
    {
        game.Placer.citizenManager.CitizenChanged += ButtonUpdate;
        if(view != null)
        {
            ButtonUpdate();
        }
    }

    private void OnDisable()
    {
        game.Placer.citizenManager.CitizenChanged -= ButtonUpdate;
    }

    public void OnBuild(string label)
    {
        view.SetUnit(label);
    }

    private void ButtonUpdate()
    {
        meleeButton.interactable = view.CheckCanBuild("melee");
        rangeButton.interactable = view.CheckCanBuild("Ranged");
        dualBladerButton.interactable = view.CheckCanBuild("DualBlader");
        spearManButton.interactable = view.CheckCanBuild("SpearMan");

        if (view.IsPlacing && !view.CheckCanBuild(view.PlacingLabel))
            view.ClearMode();   
    }
}
