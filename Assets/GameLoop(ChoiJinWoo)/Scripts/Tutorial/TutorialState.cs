using UnityEngine;

// 튜토리얼 진행 상태. UpgradeState.cs와 같은 컨벤션으로 PlayerPrefs에 저장한다.
// 나중에 진짜 세이브 시스템이 생기면 여기 저장 위치만 바꾸면 된다.
//
// 세이브 시스템이 없는 프로젝트라 앱을 다시 켜면 0일차 게임 상태(자원/건물/시민/영웅)가 전부
// 초기화된다 - 그래서 "몇 번째 단계까지 진행했는지"는 저장하지 않는다. 중간에 앱이 꺼졌다 켜지면
// 게임 상태는 이미 백지로 돌아가 있으므로, 튜토리얼도 항상 처음부터 다시 시작해야 앞뒤가 맞는다.
public class TutorialState
{
    private const string SeenKey = "TutorialSeen";

    public bool Seen => PlayerPrefs.GetInt(SeenKey, 0) == 1;

    public void MarkSeen()
    {
        PlayerPrefs.SetInt(SeenKey, 1);
        PlayerPrefs.Save();
    }

    // 테스트용 - UpgradeState.DebugResetWithoutRefund()와 같은 컨벤션.
    public void Reset()
    {
        PlayerPrefs.DeleteKey(SeenKey);
        PlayerPrefs.Save();
    }
}
