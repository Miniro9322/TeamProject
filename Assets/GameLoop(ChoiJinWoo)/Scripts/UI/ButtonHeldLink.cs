using UnityEngine;

// 지정한 패널이 열려 있는 동안 이 버튼을 눌린 모양(+선택색)으로 붙잡아 둔다.
public class ButtonHeldLink : HeldLinkBase
{
    [SerializeField] private GameObject watchedPanel;

    // 패널이 켜져 있는지로 눌림 여부를 판단한다
    protected override bool CheckHeld()
    {
        return watchedPanel.activeSelf;
    }
}
