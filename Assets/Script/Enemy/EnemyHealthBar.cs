using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 적 머리 위 체력바만 담당한다. EnemyBase가 소유하고 매 프레임 Tick으로 굴린다(EnemyCloak과 같은 구조).
/// 프리팹에 꽂아둔 Slider를 받아 쓰며, Slider가 없는 프리팹이면 Setup/Tick/Reset이 전부 조용히 no-op.
///
/// 월드스페이스 바(머리 위)에는 빌보드가 필수다 — CameraRig의 yaw는 클램프 없이 무제한 회전하므로(CameraInput.Rotate),
/// 회전을 안 맞추면 카메라를 90도만 돌려도 바가 선으로 보이고 180도면 뒤집힌다.
/// 반대로 스크린스페이스 바(화면 고정 보스바)는 절대 회전시키지 않는다 — Setup이 캔버스 모드로 구분한다.
/// </summary>
public class EnemyHealthBar
{
    private Slider _slider;
    private Transform _tf;
    private Camera _cam;
    private float _smoothSpeed = 10f;
    private bool _shown;      // 마지막으로 반영한 표시 상태 — 바뀐 프레임에만 SetActive 호출
    private bool _billboard;  // 이 바가 카메라를 향해 돌아야 하는지. Setup에서 캔버스 모드로 1회 판정

    public bool IsSetup => _slider != null;

    /// <summary>EnemyBase가 Awake에서 1회 호출. slider가 null이면 이후 모든 호출이 no-op가 된다.</summary>
    public void Setup(Slider slider, float smoothSpeed)
    {
        _slider = slider;
        if (_slider == null) return;

        _tf = _slider.transform;
        _smoothSpeed = Mathf.Max(0.01f, smoothSpeed);
        _slider.transition = Selectable.Transition.None;
        _slider.interactable = false;

        // 적마다 UI 레이캐스트가 쌓이면 SpawnerManager의 포탈 클릭까지 가로챌 수 있다.
        foreach (Graphic g in _slider.GetComponentsInChildren<Graphic>(true))
            g.raycastTarget = false;

        // 빌보드는 월드스페이스 캔버스(머리 위 바)에만 적용한다.
        // 스크린스페이스 캔버스(화면 고정 보스바)에 카메라의 월드 회전을 박으면 화면에서 슬라이더만 비스듬히 기울고,
        // 같은 캔버스의 이름/패널은 안 돌아가므로 서로 어긋난다.
        // 비활성 프리팹에서 Awake가 돌 수 있으므로 includeInactive=true로 찾는다.
        Canvas canvas = _slider.GetComponentInParent<Canvas>(true);
        _billboard = canvas != null && canvas.rootCanvas.renderMode == RenderMode.WorldSpace;

        Reset();
    }
    public void ResetTo(float hp, float maxHp)
    {
        if (_slider == null) return;

        _slider.maxValue = Mathf.Max(1f, maxHp);
        _slider.value = Mathf.Clamp(hp, 0f, _slider.maxValue);
    }

    // 표시 판정 여유값. 부동소수 오차로 풀피인데 바가 떴다 사라졌다 하는 걸 막는다.
    private const float FullEpsilon = 0.01f;
    // "다 깎였다"로 볼 비율. 지수 감쇠는 0에 점근하므로 정확히 0이 되길 기다리지 않는다.
    private const float DrainedRatio = 0.005f;

    public void Tick(float hp, float maxHp, bool isDead, bool forceHidden,EnemyClass enemyClass)
    {
        if (_slider == null) return;

        _slider.maxValue = Mathf.Max(1f, maxHp); // 버프로 최대 체력이 바뀌어도 따라가게
        float target = Mathf.Clamp(hp, 0f, _slider.maxValue);

        bool damaged = target < _slider.maxValue - FullEpsilon;
        bool drained = _slider.value <= _slider.maxValue * DrainedRatio;
        // 사망 중엔 다 깎일 때까지 계속 보여준다. 다 깎이면 숨긴다(사망 애니가 남아 있어도 빈 바를 띄워두지 않음).
        bool visible = !forceHidden && (isDead ? !drained : damaged);
        if(enemyClass == EnemyClass.Boss)
        visible = true;
        if (_shown != visible)
        {
            _shown = visible;
            _slider.gameObject.SetActive(visible);
        }
        if (!visible) return;
        // 프레임률에 독립적인 지수 감쇠 보간. Lerp(a,b,dt*speed)는 fps에 따라 속도가 달라진다.
        _slider.value = Mathf.Lerp(_slider.value, target, 1f - Mathf.Exp(-_smoothSpeed * Time.deltaTime));

        if (!_billboard) return;                 // 화면 고정 바는 회전 대상이 아니다
        if (_cam == null) _cam = Camera.main;    // 매 프레임 Camera.main을 부르지 않도록 캐시
        if (_cam != null) _tf.rotation = _cam.transform.rotation;
    }

    /// <summary>
    /// 풀 반납 등에서 호출. 숨긴 상태로 되돌려, 다음 스폰의 첫 Tick이 표시 여부를 다시 판단하게 한다.
    /// (은신 적이 스폰되는 순간 한 프레임 바가 노출되는 것도 이걸로 막힌다.)
    /// </summary>
    public void Reset()
    {
        if (_slider == null) return;

        _shown = false;
        _slider.gameObject.SetActive(false);
    }
}
