using UnityEngine;

public class ExclusivePanelPresence : MonoBehaviour, IExclusiveUiPanel
{
    private void OnEnable() => ExclusiveUiCoordinator.NotifyOpened(this);
    private void OnDisable() => ExclusiveUiCoordinator.NotifyClosed(this);

    public void RequestClose()
    {
        PanelReveal reveal = GetComponent<PanelReveal>();
        if (reveal != null) reveal.Hide();
        else gameObject.SetActive(false);
    }
}
