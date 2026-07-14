using UnityEngine;

/// <summary>
/// 영역 확장 버튼의 외부 호출부(이벤트 기반).
/// 버튼 클릭 → AreaExpand에 "확장 요청"만 보내고, 실제 확장 성공은
/// OnAreaSizeChanged 이벤트로 통지받아 반응.
/// 테스트를 위한 스크립트이며 추후 통합 ui시스템 관련 스크립트에 이전 필요성.
/// </summary>
public class AreaExpandButton : MonoBehaviour
{
    [SerializeField] private AreaExpand _area; // 인스펙터 주입.

    // 확장 성공을 이벤트로 통지받아 구독한다(짝 맞춰 OnDisable에서 해제 → 중복구독·누수 방지).
    private void OnEnable()
    {
        _area.OnAreaSizeChanged += OnAreaChanged;
    }

    private void OnDisable()
    {
        _area.OnAreaSizeChanged -= OnAreaChanged;  
    }
 
    public void Expand()
    {
        if (_area != null)
        {
            _area.TryExpandArea();
        }
    }

    // 확장이 실제로 성공했을 때만 이벤트로 호출.
    private void OnAreaChanged(int size)
    {
        Debug.Log($" 영역 확장 {size}칸.", this);
    }
}
