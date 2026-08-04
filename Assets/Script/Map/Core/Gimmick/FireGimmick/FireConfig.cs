using UnityEngine;

// 불 피해 수치를 한 곳에서 저작한다. 실제 진행은 BurnClock에 맡긴다.
[DisallowMultipleComponent]
public class FireConfig : MonoBehaviour
{
    [Tooltip("불 칸 위 유닛이 한 번에 받는 피해량.")]
    [SerializeField] private int damagePerHit = 5;

    [Tooltip("피해를 주는 간격(초).")]
    [SerializeField] private float hitInterval = 1f;

    [Tooltip("불 칸을 벗어난 뒤 피해가 유지되는 시간(초).")]
    [SerializeField] private float burnDuration = 3f;

    public int DamagePerHit => damagePerHit;
    public float HitInterval => hitInterval;
    public float BurnDuration => burnDuration;

    private void Awake()
    {
        BurnClock.Ensure(this);
    }
}
