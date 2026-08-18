using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpecificGuide : MonoBehaviour
{
    [SerializeField] private Button guideButton;
    [SerializeField] private Image guideImage;
    [SerializeField, TextArea] private string guideInfo;

    public Button GuideButton => guideButton;
    public Image GuideImage => guideImage;
    public string GuideInfo => guideInfo;
}
