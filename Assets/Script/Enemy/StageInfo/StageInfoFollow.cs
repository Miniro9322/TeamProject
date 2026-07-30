using UnityEngine;

// 스테이지 정보 팝업(스크린 스페이스 오버레이 캔버스)을 월드의 포탈 위치에 붙여 따라다니게 한다.
//
// 왜 오버레이인가:
//   안개는 URP FullScreenPassRendererFeature('FogPass', injectionPoint 500 = AfterRenderingTransparents)다.
//   월드 스페이스 캔버스 UI는 Transparent 큐에 그려지므로 그 뒤에 오는 안개 패스가 통째로 덮어쓴다.
//   게다가 UI는 깊이를 안 써서 안개가 그 픽셀을 '뒤쪽 지면'으로 보고 미탐색 취급해 진하게 칠한다.
//   오버레이 캔버스는 URP 패스가 다 끝난 뒤 합성되므로 안개에 영향받지 않는다.
//
// 오버레이 캔버스는 루트 Transform이 무시되기 때문에, 실제로 움직일 대상은 캔버스 자식 패널이다.
public class StageInfoFollow : MonoBehaviour
{
    [Tooltip("실제로 움직일 패널(캔버스의 자식). 오버레이 캔버스는 자기 Transform이 무시된다.")]
    [SerializeField] private RectTransform panel;

    private Transform target;      // 따라갈 포탈
    private Vector3 worldOffset;
    private Camera cam;
    private StageInfoView view;

    void Awake()
    {
        view = GetComponent<StageInfoView>();

        // panel은 위치만 옮기는 껍데기다. 여기에 Image가 붙어 있으면(UI>Panel 기본값이 전체 화면 스트레치)
        // 화면 전체 클릭을 먹어 IsPointerOverGameObject()가 항상 true가 되고,
        // MapCommand→BuildingUiLink 경로가 막혀 타일·바닥 클릭이 전부 죽는다.
        if (panel != null && panel.TryGetComponent(out UnityEngine.UI.Graphic g))
        {
            if (g.raycastTarget)
            {
                g.raycastTarget = false;
                Debug.LogWarning($"StageInfoFollow: '{panel.name}'의 Raycast Target을 껐습니다. " +
                                 "이 패널은 컨테이너라 클릭을 받으면 안 됩니다(배경이 필요하면 자식으로 두세요).", this);
            }
        }
    }

    public void Follow(Transform target, Vector3 worldOffset, Camera cam)
    {
        this.target = target;
        this.worldOffset = worldOffset;
        this.cam = cam;
        Apply();                   // 스폰된 첫 프레임부터 제자리에 있게 즉시 한 번
    }

    // 풀에 반납될 때 끊는다. 안 끊으면 다음에 재사용될 때까지 옛 포탈을 계속 따라간다.
    void OnDisable() => target = null;

    // 카메라가 움직여도 포탈 옆에 붙어 있도록 카메라 이동이 끝난 뒤 갱신한다.
    void LateUpdate() => Apply();

    private void Apply()
    {
        if (panel == null || target == null || cam == null) return;

        Vector3 screen = cam.WorldToScreenPoint(target.position + worldOffset);
        // 포탈이 카메라 뒤로 넘어가면 z가 음수가 되고 좌표가 뒤집혀 엉뚱한 자리에 뜬다 → 숨긴다.
        if (screen.z <= 0f)
        {
            if (panel.gameObject.activeSelf) panel.gameObject.SetActive(false);
            return;
        }

        if (!panel.gameObject.activeSelf) panel.gameObject.SetActive(true);
        panel.position = new Vector3(screen.x, screen.y, 0f);

        // 툴팁은 이 패널의 자식이라 같이 끌려간다 → 옮긴 직후 화면 밖 보정을 다시 시킨다.
        if (view != null) view.RefreshTooltipPosition();
    }
}
