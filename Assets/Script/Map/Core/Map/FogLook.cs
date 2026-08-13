using UnityEngine;

// 안개 외형 수치(색·밀도·경계 노이즈·구름)를 인스펙터에서 조절해 셰이더 전역에 밀어준다.
// FogView(구멍 데이터)와 분리된 순수 외형 튜닝 컴포넌트. OnValidate로 Play 중 실시간 반영.
[ExecuteAlways]  // 씬을 열자마자 편집 상태(showInEditor)를 반영한다.
public class FogLook : MonoBehaviour
{
    [Header("편집")]
    [Tooltip("에디터(비플레이)에서 안개를 보일지. 끄면 다른 파일·맵 편집 시 안개가 사라진다. 플레이에는 영향 없음.")]
    [SerializeField] private bool showInEditor = false;

    [Header("안개 기본")]
    [Tooltip("안개의 기본 색입니다. 알파값은 사용하지 않으며 투명도는 Density로 조절합니다.")]
    [SerializeField] private Color fogColor = new Color(0.55f, 0.6f, 0.7f, 1f);

    [Tooltip("안개의 전체 불투명도입니다. 0이면 투명하고 1이면 가장 진합니다.")]
    [SerializeField, Range(0f, 1f)] private float density = 0.9f;

    [Tooltip("안개 내부 구름 명암의 강도입니다. 0이면 단색에 가까워집니다.")]
    [SerializeField, Range(0f, 1f)] private float cloudTint = 0.5f;

    [Header("안개 경계")]
    [Tooltip("열린 구역과 안개 사이 경계의 부드러운 폭입니다. 높을수록 경계가 넓고 흐릿해집니다.")]
    [SerializeField, Range(0f, 10f)] private float edgeSoft = 2f;

    [Tooltip("경계가 울퉁불퉁하게 흔들리는 거리입니다. 0이면 직선에 가까워집니다.")]
    [SerializeField, Range(0f, 20f)] private float edgeRough = 6f;

    [Tooltip("경계 굴곡의 촘촘함입니다. 높을수록 작고 촘촘한 굴곡이 생깁니다.")]
    [SerializeField, Range(0.005f, 0.5f)] private float edgeScale = 0.06f;

    [Header("구름 무늬")]
    [Tooltip("구름 무늬의 촘촘함입니다. 높을수록 구름 무늬가 작아집니다.")]
    [SerializeField, Range(0.01f, 1f)] private float cloudScale = 0.15f;

    [Tooltip("안개 무늬가 흐르는 속도입니다. 0이면 정지합니다.")]
    [SerializeField, Range(0f, 5f)] private float windSpeed = 1.5f;

    private static readonly int ColorId = Shader.PropertyToID("_FogColor");
    private static readonly int DensityId = Shader.PropertyToID("_FogDensity");
    private static readonly int TintId = Shader.PropertyToID("_CloudTint");
    private static readonly int SoftId = Shader.PropertyToID("_EdgeSoft");
    private static readonly int RoughId = Shader.PropertyToID("_EdgeRough");
    private static readonly int EdgeScaleId = Shader.PropertyToID("_EdgeScale");
    private static readonly int CloudScaleId = Shader.PropertyToID("_CloudScale");
    private static readonly int WindId = Shader.PropertyToID("_WindSpeed");

    private void OnEnable()
    {
        ApplyFogLook();  // ExecuteAlways 하 편집 중에도 씬 로드 즉시 반영.
    }

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
        // 편집(비플레이) 중이고 편집 표시가 꺼져 있으면 안개를 걷는다. 플레이 진입 시 Start가 정상값을 다시 민다.
        bool hideInEditor = !Application.isPlaying && !showInEditor;

        Shader.SetGlobalColor(ColorId, fogColor);
        Shader.SetGlobalFloat(DensityId, hideInEditor ? 0f : density);
        Shader.SetGlobalFloat(TintId, cloudTint);
        Shader.SetGlobalFloat(SoftId, edgeSoft);
        Shader.SetGlobalFloat(RoughId, edgeRough);
        Shader.SetGlobalFloat(EdgeScaleId, edgeScale);
        Shader.SetGlobalFloat(CloudScaleId, cloudScale);
        Shader.SetGlobalFloat(WindId, windSpeed);
    }
}
