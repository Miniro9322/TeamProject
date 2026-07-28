using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 적 머리 위 체력바만 담당한다. EnemyBase가 소유하고 매 프레임 Tick으로 굴린다(EnemyCloak과 같은 구조).
/// 프리팹에 꽂아둔 Slider를 받아 쓰며, Slider가 없는 프리팹이면 Setup/Tick/Reset이 전부 조용히 no-op.
///
/// 빌보드가 필수다 — CameraRig의 yaw는 클램프 없이 무제한 회전하므로(CameraInput.Rotate),
/// 회전을 안 맞추면 카메라를 90도만 돌려도 바가 선으로 보이고 180도면 뒤집힌다.
/// </summary>
public class EnemyHealthBar
{
    private Slider _slider;
    private Transform _tf;
    private Camera _cam;
    private float _smoothSpeed = 10f;
    private bool _shown;      // 마지막으로 반영한 표시 상태 — 바뀐 프레임에만 SetActive 호출

    public bool IsSetup => _slider != null;

    /// <summary>EnemyBase가 Awake에서 1회 호출. slider가 null이면 이후 모든 호출이 no-op가 된다.</summary>
    public void Setup(Slider slider, float smoothSpeed)
    {
        _slider = slider;
        if (_slider == null) return;

        _tf = _slider.transform;
        _smoothSpeed = Mathf.Max(0.01f, smoothSpeed);

        // 표시 전용으로 고정.
        // Transition을 먼저 None으로 꺼야 한다 — ColorTint 상태에서 interactable=false를 주면
        // Selectable이 Disabled 색(기본 회색·알파 0.5)을 칠해서 바가 흐릿해진다.
        _slider.transition = Selectable.Transition.None;
        _slider.interactable = false;

        // 적마다 UI 레이캐스트가 쌓이면 SpawnerManager의 포탈 클릭까지 가로챌 수 있다.
        foreach (Graphic g in _slider.GetComponentsInChildren<Graphic>(true))
            g.raycastTarget = false;

        Reset();
    }

    /// <summary>
    /// 스폰·스탯 재적용 시 현재 체력으로 즉시 맞춘다(보간 없이).
    /// 풀에서 재사용될 때 이전 개체의 체력이 남아 스르륵 차오르는 걸 막는다.
    /// </summary>
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

    /// <summary>
    /// 매 프레임 LateUpdate에서 호출.
    ///
    /// 표시 규칙:
    ///  - 아직 안 맞은 적(풀피)은 바를 띄우지 않는다. 첫 피해를 입는 순간 풀피 상태에서 등장해 깎이는 게 보인다.
    ///  - 죽을 때는 바로 숨기지 않고 0까지 깎이는 걸 보여준 뒤 사라진다.
    ///  - forceHidden(은신 중)이면 위 조건과 무관하게 무조건 숨긴다.
    /// </summary>
    public void Tick(float hp, float maxHp, bool isDead, bool forceHidden)
    {
        if (_slider == null) return;

        _slider.maxValue = Mathf.Max(1f, maxHp); // 버프로 최대 체력이 바뀌어도 따라가게
        float target = Mathf.Clamp(hp, 0f, _slider.maxValue);

        bool damaged = target < _slider.maxValue - FullEpsilon;
        bool drained = _slider.value <= _slider.maxValue * DrainedRatio;
        // 사망 중엔 다 깎일 때까지 계속 보여준다. 다 깎이면 숨긴다(사망 애니가 남아 있어도 빈 바를 띄워두지 않음).
        bool visible = !forceHidden && (isDead ? !drained : damaged);

        if (_shown != visible)
        {
            _shown = visible;
            _slider.gameObject.SetActive(visible);
        }
        if (!visible) return;

        // 프레임률에 독립적인 지수 감쇠 보간. Lerp(a,b,dt*speed)는 fps에 따라 속도가 달라진다.
        _slider.value = Mathf.Lerp(_slider.value, target, 1f - Mathf.Exp(-_smoothSpeed * Time.deltaTime));

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
