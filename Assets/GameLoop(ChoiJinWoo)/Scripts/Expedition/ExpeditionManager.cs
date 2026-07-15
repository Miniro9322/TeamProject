using UnityEngine;

public class ExpeditionManager : MonoBehaviour
{
    [SerializeField] private JudgeProbability _base;   // ratio >= 1일 때 기준 확률 (fail+normal+great = 1)
    [SerializeField] private float _normalDecayPower;  // normal이 줄어드는 속도 (기본 1)
    [SerializeField] private float _greatDecayPower;   // great가 줄어드는 속도 (기본 2~3, normal보다 커야 더 빨리 줄어듦)
    [SerializeField] private float _epsilon;           // 최소 확률 보장

    private void Reward()
    {
        var result = Random.Range(0f, 1f);

        if(result < _base.fail)
        {
            Debug.Log("원정 실패");
        }
        else if(result < _base.fail + _base.normal)
        {
            Debug.Log("원정 성공");
        }
        else
        {
            Debug.Log("원정 대성공");
        }
    }

    public void Calculate(float userPower, float recommendedPower)
    {
        float ratio = recommendedPower > 0f ? userPower / recommendedPower : 1f;

        if (ratio >= 1f)
            return;

        float t = Mathf.Clamp01(ratio); // 0~1 구간만 조정 대상

        // t가 0에 가까워질수록 great, normal이 epsilon으로 점근 (거듭제곱 곡선)
        float great = _epsilon + (_base.great - _epsilon) * Mathf.Pow(t, _greatDecayPower);
        float normal = _epsilon + (_base.normal - _epsilon) * Mathf.Pow(t, _normalDecayPower);

        // 남는 확률은 전부 fail이 흡수 (정규화 겸 자동 보정)
        float fail = 1f - great - normal;

        _base.fail = fail;
        _base.normal = normal;
        _base.great = great;
    }
    //실패, 성공, 대성공 각각 기초 비율이 있고, 권장 전투력과 원정 인원 총 전투력에 비례하여 각각의 확률이 변동 단 최소치 밑으로는 내려가지 않음
    //원정 인원 총 전투력 > 권장 전투력 기준 실패 : 10%, 성공 : 70%, 대성공 : 20%
    //차이나는 비율의 절반 만큼 대성공 확률 감소 실패, 성공 총 퍼센티지 증가 후 비율 절반의 절반씩 실패 증가 성공 감소
}
