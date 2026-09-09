using System.Collections.Generic;

public interface IExclusiveUiPanel
{
    void RequestClose();
}

public interface IPersistentAcrossExclusivePanels
{
}

public static class ExclusiveUiCoordinator
{
    private static readonly List<IExclusiveUiPanel> openPanels = new();

    public static void NotifyOpened(IExclusiveUiPanel self)
    {
        foreach (IExclusiveUiPanel panel in openPanels.ToArray())
        {
            if (ReferenceEquals(panel, self)) continue;
            if (panel is IPersistentAcrossExclusivePanels) continue;
            panel.RequestClose();
        }

        if (!openPanels.Contains(self)) openPanels.Add(self);
    }

    public static void NotifyClosed(IExclusiveUiPanel self) => openPanels.Remove(self);
}
