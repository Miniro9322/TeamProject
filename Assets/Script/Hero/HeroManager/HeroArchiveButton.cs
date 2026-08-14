using UnityEngine;
using UnityEngine.UI;

public class HeroArchiveButton : MonoBehaviour
{
    [SerializeField] private GameObject archiveUI;
    private bool isOpen = false;
    private void Start()
    {
        archiveUI.SetActive(false);
    }

    public void OnClick()
    {
        archiveUI.SetActive(!archiveUI.activeSelf);
    }
}
