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
    [Tooltip("구름 덩어리가 얇은 곳(그림자)의 색입니다. 알파값은 사용하지 않으며 투명도는 Density로 조절합니다.")]
    [SerializeField] private Color fogColor = new Color(0.45f, 0.5f, 0.62f, 1f);

    [Tooltip("구름 덩어리가 두꺼운 곳(상단)의 밝은 하이라이트 색입니다.")]
    [SerializeField] private Color cloudHighlight = new Color(0.96f, 0.97f, 1f, 1f);

    [Tooltip("안개의 전체 불투명도입니다. 0이면 투명하고 1이면 가장 진합니다.")]
    [SerializeField, Range(0f, 1f)] private float density = 0.9f;

    [Tooltip("그림자색과 하이라이트색 사이 대비 강도입니다. 0이면 그림자색 단색에 가까워집니다.")]
    [SerializeField, Range(0f, 1f)] private float cloudTint = 0.6f;

    [Header("안개 경계")]
    [Tooltip("열린 구역과 안개 사이 경계의 부드러운 폭입니다. 높을수록 경계가 넓고 흐릿해집니다.")]
    [SerializeField, Range(0f, 10f)] private float edgeSoft = 2f;

    [Tooltip("경계가 울퉁불퉁하게 흔들리는 거리입니다. 0이면 직선에 가까워집니다.")]
    [SerializeField, Range(0f, 20f)] private float edgeRough = 6f;

    [Tooltip("경계 굴곡의 촘촘함입니다. 높을수록 작고 촘촘한 굴곡이 생깁니다.")]
    [SerializeField, Range(0.005f, 0.5f)] private float edgeScale = 0.06f;

    [Header("가짜 그림자")]
    [Tooltip("안개가 빛을 가려 바깥 맨눈 지형에 드리우는 그림자의 길이(월드 단위)입니다. 0이면 그림자가 사라집니다.")]
    [SerializeField, Range(0f, 15f)] private float shadowOffsetDist = 3f;

    [Tooltip("그림자 가장자리의 부드러움입니다.")]
    [SerializeField, Range(0.1f, 10f)] private float shadowSoft = 2f;

    [Tooltip("그림자 부분이 얼마나 어두워지는지입니다. 낮을수록 더 어둡습니다.")]
    [SerializeField, Range(0f, 1f)] private float shadowDarken = 0.55f;

    [Tooltip("그림자의 전체 강도(불투명도)입니다. 0이면 그림자가 보이지 않습니다.")]
    [SerializeField, Range(0f, 1f)] private float shadowStrength = 0.5f;

    [Header("시야 경계선")]
    [Tooltip("열림/닫힘 경계에 그려지는 또렷한 테두리 색입니다. 알파값은 경계선 자체의 최소 불투명도로 쓰입니다.")]
    [SerializeField] private Color edgeLineColor = new Color(0.10f, 0.12f, 0.2f, 0.85f);

    [Tooltip("경계선의 얇기입니다. 높을수록 선이 가늘고 뚜렷해집니다.")]
    [SerializeField, Range(1f, 12f)] private float edgeLineSharpness = 3f;

    [Tooltip("경계선의 강도입니다. 0이면 경계선이 보이지 않습니다.")]
    [SerializeField, Range(0f, 1f)] private float edgeLineStrength = 0.55f;

    [Header("구름 덩어리")]
    [Tooltip("구름 덩어리의 크기입니다. 낮을수록 덩어리가 커집니다. 화면에 보이는 안개 영역보다 뭉텅이가 훨씬 크면 뭉치가 아니라 밋밋한 면처럼 보이니, 한 화면에 여러 뭉텅이가 겹쳐 보일 정도로 맞추세요.")]
    [SerializeField, Range(0.01f, 1f)] private float cloudScale = 0.35f;

    [Tooltip("구름이 뒤덮는 범위입니다. 낮을수록 구름 덩어리가 넓게 뒤덮고, 높을수록 틈이 많아집니다.")]
    [SerializeField, Range(0f, 1f)] private float cloudCoverage = 0.45f;

    [Tooltip("구름 덩어리 경계의 부드러움입니다. 높을수록 뭉텅이 가장자리가 뽀글뽀글 부드러워집니다.")]
    [SerializeField, Range(0.01f, 0.5f)] private float cloudSoftness = 0.18f;

    [Tooltip("중간 회색을 밝은/어두운 쪽으로 밀어 붙이는 대비입니다. 낮으면 전체가 뿌옇게 뭉치고, 높으면 또렷하게 갈라집니다.")]
    [SerializeField, Range(1f, 6f)] private float cloudContrast = 2.6f;

    [Tooltip("뭉텅이 옆면(경사)에 생기는 방향성 명암(하이라이트/그림자)의 강도입니다. 0이면 평평해 보이고, 높이면 솜뭉치처럼 입체감이 생깁니다.")]
    [SerializeField, Range(0f, 3f)] private float cloudShadeStrength = 1.6f;

    [Tooltip("경사가 급한 곳에만 명암이 몰리는 정도입니다. 높을수록 뭉텅이 꼭대기/깊은 틈은 평평하게 남고 옆면에만 명암이 집중됩니다.")]
    [SerializeField, Range(0.5f, 20f)] private float cloudShadeSharpness = 6f;

    [Tooltip("구름 틈에서도 유지되는 최소 불투명도입니다. 낮추면 틈으로 지형이 더 드러납니다.")]
    [SerializeField, Range(0f, 1f)] private float cloudMinAlpha = 0.65f;

    [Tooltip("구름 무늬를 투영할 가상 수평면의 월드 높이(카메라 기준 절대 Y)입니다. 지형/벽 표면이 아니라 이 높이에서 무늬를 그려, 카메라가 움직일 때 지형과 다른 시차로 움직이는 '떠 있는 레이어' 느낌을 만듭니다. 맵 지형보다 높고 카메라보다는 낮게 맞추세요.")]
    [SerializeField, Range(0f, 50f)] private float cloudAltitude = 6f;

    [Tooltip("구름이 흐르는 속도입니다. 0이면 정지합니다.")]
    [SerializeField, Range(0f, 5f)] private float windSpeed = 1.5f;

    private static readonly int ColorId = Shader.PropertyToID("_FogColor");
    private static readonly int HighlightId = Shader.PropertyToID("_CloudHighlight");
    private static readonly int DensityId = Shader.PropertyToID("_FogDensity");
    private static readonly int TintId = Shader.PropertyToID("_CloudTint");
    private static readonly int SoftId = Shader.PropertyToID("_EdgeSoft");
    private static readonly int RoughId = Shader.PropertyToID("_EdgeRough");
    private static readonly int EdgeScaleId = Shader.PropertyToID("_EdgeScale");
    private static readonly int CloudScaleId = Shader.PropertyToID("_CloudScale");
    private static readonly int CloudCoverageId = Shader.PropertyToID("_CloudCoverage");
    private static readonly int CloudSoftnessId = Shader.PropertyToID("_CloudSoftness");
    private static readonly int CloudContrastId = Shader.PropertyToID("_CloudContrast");
    private static readonly int CloudMinAlphaId = Shader.PropertyToID("_CloudMinAlpha");
    private static readonly int CloudShadeStrengthId = Shader.PropertyToID("_CloudShadeStrength");
    private static readonly int CloudShadeSharpnessId = Shader.PropertyToID("_CloudShadeSharpness");
    private static readonly int ShadowOffsetDistId = Shader.PropertyToID("_ShadowOffsetDist");
    private static readonly int ShadowSoftId = Shader.PropertyToID("_ShadowSoft");
    private static readonly int ShadowDarkenId = Shader.PropertyToID("_ShadowDarken");
    private static readonly int ShadowStrengthId = Shader.PropertyToID("_ShadowStrength");
    private static readonly int CloudAltitudeId = Shader.PropertyToID("_CloudAltitude");
    private static readonly int WindId = Shader.PropertyToID("_WindSpeed");
    private static readonly int EdgeLineColorId = Shader.PropertyToID("_EdgeLineColor");
    private static readonly int EdgeLineSharpnessId = Shader.PropertyToID("_EdgeLineSharpness");
    private static readonly int EdgeLineStrengthId = Shader.PropertyToID("_EdgeLineStrength");

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
        Shader.SetGlobalColor(HighlightId, cloudHighlight);
        Shader.SetGlobalFloat(DensityId, hideInEditor ? 0f : density);
        Shader.SetGlobalFloat(TintId, cloudTint);
        Shader.SetGlobalFloat(SoftId, edgeSoft);
        Shader.SetGlobalFloat(RoughId, edgeRough);
        Shader.SetGlobalFloat(EdgeScaleId, edgeScale);
        Shader.SetGlobalFloat(CloudScaleId, cloudScale);
        Shader.SetGlobalFloat(CloudCoverageId, cloudCoverage);
        Shader.SetGlobalFloat(CloudSoftnessId, cloudSoftness);
        Shader.SetGlobalFloat(CloudContrastId, cloudContrast);
        Shader.SetGlobalFloat(CloudMinAlphaId, cloudMinAlpha);
        Shader.SetGlobalFloat(CloudShadeStrengthId, cloudShadeStrength);
        Shader.SetGlobalFloat(CloudShadeSharpnessId, cloudShadeSharpness);
        Shader.SetGlobalFloat(ShadowOffsetDistId, shadowOffsetDist);
        Shader.SetGlobalFloat(ShadowSoftId, shadowSoft);
        Shader.SetGlobalFloat(ShadowDarkenId, shadowDarken);
        // 그림자는 Density와 별도 경로로 그려지므로, 에디터에서 안개를 숨겼을 때 같이 꺼지게 맞춰준다.
        Shader.SetGlobalFloat(ShadowStrengthId, hideInEditor ? 0f : shadowStrength);
        Shader.SetGlobalFloat(CloudAltitudeId, cloudAltitude);
        Shader.SetGlobalFloat(WindId, windSpeed);
        Shader.SetGlobalColor(EdgeLineColorId, edgeLineColor);
        Shader.SetGlobalFloat(EdgeLineSharpnessId, edgeLineSharpness);
        // 경계선은 Density와 별도 경로로 그려지므로, 에디터에서 안개를 숨겼을 때 같이 꺼지게 맞춰준다.
        Shader.SetGlobalFloat(EdgeLineStrengthId, hideInEditor ? 0f : edgeLineStrength);
    }
}
