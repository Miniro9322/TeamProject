using UnityEngine;

// 얼음 지대 데이터 보유. Get만 제공 — 실제 적용은 ZoneEffectApplier가 한다
public class IceZone : MonoBehaviour
{
    [SerializeField] private ZoneStatEffect[] effects;

    public ZoneStatEffect[] Effects => effects;
}
