using System.Collections.Generic;

// 단축키로 열리는 최상위 패널(메뉴/도감/가이드 등)이 서로의 존재를 몰라도 배타적으로 동작하게 해준다.
// 패널은 열릴 때 NotifyOpened, 닫힐 때 NotifyClosed만 호출하면 되고, 다른 패널을 인스펙터로
// 서로 참조해 닫아줄 필요가 없다. UiPanelStack과 달리 "누가 열렸는지"만 보고 즉시 나머지를
// 전부 닫는 단순 배타 그룹이다(우선순위 스택이 아님).
public interface IExclusiveUiPanel
{
    void RequestClose();
}

public static class ExclusiveUiCoordinator
{
    private static readonly List<IExclusiveUiPanel> openPanels = new();

    public static void NotifyOpened(IExclusiveUiPanel self)
    {
        // ToArray로 스냅샷을 떠서 순회한다 - RequestClose 안에서 NotifyClosed가 호출되며
        // openPanels 리스트 자체가 변경되므로, 원본을 그대로 순회하면 예외가 난다.
        foreach (IExclusiveUiPanel panel in openPanels.ToArray())
            if (!ReferenceEquals(panel, self)) panel.RequestClose();

        if (!openPanels.Contains(self)) openPanels.Add(self);
    }

    public static void NotifyClosed(IExclusiveUiPanel self) => openPanels.Remove(self);
}
