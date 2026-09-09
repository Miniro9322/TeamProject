using System;
using UnityEngine;

/// <summary>
/// <see cref="UpgradeState"/> 의 영속성 담당. 생성 시 PlayerPrefs 에서 복원하고,
/// 이후 <see cref="UpgradeState.Changed"/> 를 구독해 바뀔 때마다 저장한다.
///
/// UpgradeState 를 Unity 의존 없는 순수 도메인으로 유지하기 위해 저장 로직만 분리한 것.
/// 세이브 시스템(Assets/Script/SaveLoad)이 준비되면 이 클래스만 그쪽 어댑터로 교체하면 된다.
/// 수명은 관찰 대상 UpgradeState 와 같다(Changed 델리게이트가 참조를 잡음) — 스코프와 함께 GC.
/// </summary>
public class UpgradeStatePlayerPrefsStore : IDisposable
{
    private const string SaveKey = "UpgradeSaveData";

    private readonly UpgradeState state;

    public UpgradeStatePlayerPrefsStore(UpgradeState state)
    {
        this.state = state;
        Load();
        state.Changed += Save;
    }

    private void Load()
    {
        string json = PlayerPrefs.GetString(SaveKey, "");
        state.Restore(string.IsNullOrEmpty(json)
            ? null
            : JsonUtility.FromJson<UpgradeSaveData>(json));
    }

    private void Save()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(state.Capture()));
        PlayerPrefs.Save();
    }

    public void Dispose()
    {
        state.Changed -= Save;
    }
}
