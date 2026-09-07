using UnityEngine;
using UnityEngine.UI;

public class UiGlowPulse : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private float pulseSpeed = 2.5f;
    [SerializeField, Range(0f, 1f)] private float minAlpha = 0.35f;
    [SerializeField, Range(0f, 1f)] private float maxAlpha = 1f;

    private Color baseColor;

    private void Awake()
    {
        if (image == null) image = GetComponent<Image>();
        baseColor = image.color;
    }

    private void Update()
    {
        float wave = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f; // 0..1
        Color c = baseColor;
        c.a = Mathf.Lerp(minAlpha, maxAlpha, wave) * baseColor.a;
        image.color = c;
    }
}
