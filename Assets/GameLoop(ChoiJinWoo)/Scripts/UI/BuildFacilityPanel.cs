using UnityEngine;
using UnityEngine.UI;

public class BuildFacilityPanel : MonoBehaviour
{
    [SerializeField] private MapGame game;
    [SerializeField] private Button foodFacility;
    [SerializeField] private Button goldFacility;
    [SerializeField] private Button stoneFacility;
    [SerializeField] private Button woodFacility;
    [SerializeField] private Button ironFacility;
    [SerializeField] private Button house;

    private void OnEnable()
    {
        game.OnPlaced += ButtonUpdate;

        ButtonUpdate();
    }

    private void OnDisable()
    {
        game.OnPlaced -= ButtonUpdate;
    }

    public void OnBuild(string label)
    {
        game.SetUnit(label);
    }

    private void ButtonUpdate()
    {
        foodFacility.interactable = game.CheckCanBuild("food");
        goldFacility.interactable = game.CheckCanBuild("gold");
        stoneFacility.interactable = game.CheckCanBuild("stone");
        woodFacility.interactable = game.CheckCanBuild("wood");
        ironFacility.interactable = game.CheckCanBuild("iron");
        house.interactable = game.CheckCanBuild("house");
    }
}
