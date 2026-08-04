using UnityEngine;

// 불이 얼마나 아픈지 저작하는 씬 부품. 숫자만 들고 있고 굴리는 일은 BurnClock에 맡긴다.
[DisallowMultipleComponent]
public class FireConfig : MonoBehaviour
{
    [Tooltip("불 칸 위 유닛이 한 번에 받는 피해량.")]
    [SerializeField] private int damagePerHit = 5;

    [Tooltip("피해를 주는 간격(초).")]
    [SerializeField] private float hitInterval = 1f;

    public int DamagePerHit => damagePerHit;
    public float HitInterval => hitInterval;

    private void Awake()
    {
        BurnClock.Ensure(this);
    }
}
