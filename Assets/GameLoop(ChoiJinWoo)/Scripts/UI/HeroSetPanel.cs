//using UnityEngine;
//using UnityEngine.UI;

//public class HeroSetPanel : MonoBehaviour
//{
//    [SerializeField] private MapGame game;
//    [SerializeField] private Button meleeButton;
//    [SerializeField] private Button rangeButton;

//    private void OnEnable()
//    {
//        game.OnPlaced += ButtonUpdate;

//        ButtonUpdate();
//    }

//    private void OnDisable()
//    {
//        game.OnPlaced -= ButtonUpdate;
//    }

//    public void OnBuild(string label)
//    {
//        game.SetUnit(label);
//    }

//    private void ButtonUpdate()
//    {
//        meleeButton.interactable = game.CheckCanBuild("melee");
//        rangeButton.interactable = game.CheckCanBuild("Ranged");
//    }
//}
