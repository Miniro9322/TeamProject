using UnityEngine;
using UnityEngine.UI;

// 영웅 "생성" 전용 패널. 여기선 배치하지 않고 로스터에 엔트리만 추가한다(배치는 HeroRosterPanel이 담당).
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
        game.CitizenManager.CitizenChanged += ButtonUpdate;
        ButtonUpdate();
    }

    private void OnDisable()
    {
        game.CitizenManager.CitizenChanged -= ButtonUpdate;
    }

    public void OnCreate(string label)
    {
        if (!view.CheckCanBuild(label)) return;

        Placeable slot = view.GetSlot(label);
        game.CitizenManager.UseCitizen(slot.prefab.GetComponent<Hero>().CitizenAmount);
        game.HeroRoster.Add(slot);
    }

    private void ButtonUpdate()
    {
        meleeButton.interactable = view.CheckCanBuild("melee");
        rangeButton.interactable = view.CheckCanBuild("Ranged");
        dualBladerButton.interactable = view.CheckCanBuild("DualBlader");
        spearManButton.interactable = view.CheckCanBuild("SpearMan");
    }
}
