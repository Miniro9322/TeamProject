using UnityEngine;

// 화면 밖(카메라 뷰포트 밖, 마진 포함)에 스폰될 순수 연출용(VFX/파티클) 이펙트의 인스턴스화를 건너뛰기
// 위한 판정 유틸. EnemySoundManager.IsOffscreen과 동일한 뷰포트 투영 판정을, MonoBehaviour 인스턴스가
// 없는 호출부(Hero)에서도 쓸 수 있게 정적 클래스로 옮겨 둔 것.
//
// 절대 게임플레이(피해/디버프/힐/장판 틱)를 소유하는 스폰 경로에는 쓰지 말 것 — 스폰된 GameObject
// 자체가 TakeDamage/Heal/버프 적용의 주체인 경우(GroundZoneEffect, Projectile.Launch 등)는 대상 제외.
public static class VfxVisibility
{
    public const float DefaultMargin = 0.3f;

    // 문제 진단(A/B)용 전역 스위치. 끄면 아무것도 컷하지 않는다.
    public static bool Enabled = true;

    private static Camera mainCam;
    private static Camera MainCam => mainCam != null ? mainCam : (mainCam = Camera.main);

    // 카메라가 오빗+줌(distance 5~120, pitch 5~89°)이라 절대 거리로는 판정할 수 없어 뷰포트로
    // 투영한다 — 줌/팬/피치/종횡비가 전부 반영된다. 탑다운이라 높낮이도 투영이 알아서 처리한다.
    public static bool IsOffscreen(Vector3 worldPos, float margin = DefaultMargin)
    {
        if (!Enabled) return false;
        Camera cam = MainCam;
        // 카메라를 못 찾으면 컷하지 않는다(fail-open) — 판정 불가 상황에서 안 보이게 되는 것보다
        // 한 번 더 스폰되는 게 낫다. EnemySoundManager.IsOffscreen과 동일한 원칙.
        if (cam == null) return false;

        Vector3 vp = cam.WorldToViewportPoint(worldPos);
        if (vp.z < 0f) return true; // 카메라 뒤
        return vp.x < -margin || vp.x > 1f + margin
            || vp.y < -margin || vp.y > 1f + margin;
    }
}
