using UnityEngine;

// 튜토리얼 진행 상태. UpgradeState.cs와 같은 컨벤션으로 PlayerPrefs에 저장한다(값이 int 2개뿐이라 JSON 불필요).
// 나중에 진짜 세이브 시스템이 생기면 여기 저장 위치만 바꾸면 된다.
public class TutorialState
{
    private const string SeenKey = "TutorialSeen";
    private const string StepKey = "TutorialCurrentStep";

    public bool Seen => PlayerPrefs.GetInt(SeenKey, 0) == 1;
    public int CurrentStepIndex => PlayerPrefs.GetInt(StepKey, 0);

    public void SaveProgress(int stepIndex)
    {
        PlayerPrefs.SetInt(StepKey, stepIndex);
        PlayerPrefs.Save();
    }

    public void MarkSeen()
    {
        PlayerPrefs.SetInt(SeenKey, 1);
        PlayerPrefs.Save();
    }

    // 테스트용 - UpgradeState.DebugResetWithoutRefund()와 같은 컨벤션.
    public void Reset()
    {
        PlayerPrefs.DeleteKey(SeenKey);
        PlayerPrefs.DeleteKey(StepKey);
        PlayerPrefs.Save();
    }
}
