using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 지역 오버뷰의 다이아몬드 노드 하나. 잠김 상태를 시각적으로 반영하고 클릭을 RegionOverviewPanel에 전달한다.
public class RegionNodeView : MonoBehaviour
{
    [SerializeField] private int moduleId;
    [SerializeField] private Button button;
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private TextMeshProUGUI regionLabel;

    public int ModuleId => moduleId;

    private void Awake()
    {
        if (button != null)
        {
            button.onClick.AddListener(OnClick);
        }
    }

    private void OnClick()
    {
        GetComponentInParent<RegionOverviewPanel>().OnNodeClicked(this);
    }

    public void SetLocked(bool locked)
    {
        if (lockedOverlay != null) lockedOverlay.SetActive(locked);
        if (button != null) button.interactable = !locked;
    }

    public void SetLabel(string label)
    {
        if (regionLabel != null) regionLabel.text = label;
    }
}
