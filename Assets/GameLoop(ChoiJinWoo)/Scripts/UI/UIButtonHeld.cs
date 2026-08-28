using UnityEngine;
using UnityEngine.UI;

// 지정한 패널이 열려 있는 동안 이 버튼을 눌린 모양(+선택색)으로 붙잡아 두고, 이 버튼 클릭으로
// 그 패널을 직접 열고 닫는다(토글). 패널에 PanelReveal이 붙어 있으면 그 스케일 연출로, 없으면
// SetActive로. ButtonHeldLink의 UI 전용(Animator 없는 새 버튼) 버전.
public class UIButtonHeld : UIHeldBase
{
    [SerializeField] private GameObject watchedPanel;

    private Button button;
    private PanelReveal panelReveal;

    protected override void Awake()
    {
        base.Awake();
        button = GetComponent<Button>();
        panelReveal = watchedPanel.GetComponent<PanelReveal>();
        if (button != null) button.onClick.AddListener(Toggle);
    }

    // 패널이 켜져 있는지로 눌림 여부를 판단한다
    protected override bool CheckHeld()
    {
        return watchedPanel.activeSelf;
    }

    // 클릭할 때마다 패널을 토글한다 - 열려 있으면 닫고, 닫혀 있으면 연다.
    public void Toggle()
    {
        bool isOpen = watchedPanel.activeSelf;

        if (panelReveal != null)
        {
            if (isOpen) panelReveal.Hide();
            else panelReveal.Show();
        }
        else
        {
            watchedPanel.SetActive(!isOpen);
        }
    }
}
