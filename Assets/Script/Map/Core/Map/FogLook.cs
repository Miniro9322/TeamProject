using UnityEngine;

// 안개 외형 수치(색·밀도·경계 노이즈·구름)를 인스펙터에서 조절해 셰이더 전역에 밀어준다.
// FogView(구멍 데이터)와 분리된 순수 외형 튜닝 컴포넌트. OnValidate로 Play 중 실시간 반영.
public class FogLook : MonoBehaviour
{
    [Header("Fog")]
    [SerializeField] private Color fogColor = new Color(0.55f, 0.6f, 0.7f, 1f);
    [SerializeField, Range(0f, 1f)] private float density = 0.9f;
    [SerializeField, Range(0f, 1f)] private float cloudTint = 0.5f;

    [Header("Edge")]
    [SerializeField, Range(0f, 10f)] private float edgeSoft = 2f;
    [SerializeField, Range(0f, 20f)] private float edgeRough = 6f;
    [SerializeField, Range(0.005f, 0.5f)] private float edgeScale = 0.06f;

    [Header("Cloud")]
    [SerializeField, Range(0.01f, 1f)] private float cloudScale = 0.15f;
    [SerializeField, Range(0f, 5f)] private float windSpeed = 1.5f;

    private static readonly int ColorId = Shader.PropertyToID("_FogColor");
    private static readonly int DensityId = Shader.PropertyToID("_FogDensity");
    private static readonly int TintId = Shader.PropertyToID("_CloudTint");
    private static readonly int SoftId = Shader.PropertyToID("_EdgeSoft");
    private static readonly int RoughId = Shader.PropertyToID("_EdgeRough");
    private static readonly int EdgeScaleId = Shader.PropertyToID("_EdgeScale");
    private static readonly int CloudScaleId = Shader.PropertyToID("_CloudScale");
    private static readonly int WindId = Shader.PropertyToID("_WindSpeed");

    private void Start()
    {
        ApplyFogLook();
    }

    private void OnValidate()
    {
        ApplyFogLook();
    }

    private void ApplyFogLook()
    {
        Shader.SetGlobalColor(ColorId, fogColor);
        Shader.SetGlobalFloat(DensityId, density);
        Shader.SetGlobalFloat(TintId, cloudTint);
        Shader.SetGlobalFloat(SoftId, edgeSoft);
        Shader.SetGlobalFloat(RoughId, edgeRough);
        Shader.SetGlobalFloat(EdgeScaleId, edgeScale);
        Shader.SetGlobalFloat(CloudScaleId, cloudScale);
        Shader.SetGlobalFloat(WindId, windSpeed);
    }
}
