using UnityEngine;

// FireDamage 장부를 매 프레임 굴리는 구동체. 불 피해량과 간격은 여기서 저작한다.
[DisallowMultipleComponent]
public class FireDriver : MonoBehaviour
{
    [Tooltip("불 칸 위 유닛이 한 번에 받는 피해량.")]
    [SerializeField] private int damagePerHit = 5;

    [Tooltip("피해를 주는 간격(초).")]
    [SerializeField] private float hitInterval = 1f;

    private void Awake()
    {
        SendDamage();
    }

    // 인스펙터에서 숫자를 만지면 플레이 중에도 곧바로 반영한다.
    private void OnValidate()
    {
        SendDamage();
    }

    private void Update()
    {
        FireDamage.Tick();
    }

    private void SendDamage()
    {
        FireDamage.SetDamage(damagePerHit, hitInterval);
    }
}
