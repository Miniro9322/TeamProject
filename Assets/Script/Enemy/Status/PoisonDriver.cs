using UnityEngine;

/// <summary>
/// PoisonRegistry를 매 프레임 굴리는 구동체.
/// static 클래스에는 Update가 없으므로 최소한의 MonoBehaviour 하나만 둔다.
/// PoisonRegistry.Apply가 첫 호출 시 자동 생성하므로 씬 배치나 VContainer 등록이 필요 없다.
/// </summary>
public class PoisonDriver : MonoBehaviour
{
    private static PoisonDriver instance;

    internal static void Ensure()
    {
        if (instance != null) return;
        var go = new GameObject("[PoisonDriver]");
        instance = go.AddComponent<PoisonDriver>();
        DontDestroyOnLoad(go);
    }

    // 도메인 리로드를 끈 채 플레이 모드를 재진입할 때 파괴된 인스턴스 참조를 끊는다.
    internal static void ResetInstance() => instance = null;

    private void Update() => PoisonRegistry.Tick();
}
