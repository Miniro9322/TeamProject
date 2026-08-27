using UnityEngine;

// HeroCreateAmountPanel처럼 근접/원거리 아이콘 2개가 패널 하나를 같이 쓸 때,
// 패널이 켜져 있는지뿐 아니라 그 패널을 연 게 나 자신인지까지 함께 확인해서 눌린 모양을 유지한다.
// 패널 하나 : 버튼 하나뿐인 일반 케이스는 ButtonHeldLink를 그대로 쓴다.
public class HeroCreateIconHeldLink : HeldLinkBase
{
    [SerializeField] private GameObject watchedPanel;
    [SerializeField] private HeroSetPanel ownerSource;
    [SerializeField] private HeroCreateIcon ownerIdentity;

    // 패널이 켜져 있고, 그 패널을 연 게 나 자신인지 함께 확인한다
    protected override bool CheckHeld()
    {
        return watchedPanel.activeSelf && ownerSource.OpenIcon == ownerIdentity;
    }
}
