using System;
using System.Collections.Generic;

[Serializable]
public class UpgradeSaveData
{
    public List<string> unlockedIds = new();
    public int points;
}

// 업그레이드 해금 상태 + 포인트를 들고 있는 순수 도메인 서비스 (Unity 의존 없음 → 테스트 가능).
// 영속성(저장/불러오기)은 이 클래스가 직접 하지 않는다. 상태가 바뀔 때 Changed 를 쏘면
// UpgradeStatePlayerPrefsStore 가 그걸 구독해 저장하고, 시작 시 Restore()로 복원한다.
// 세이브 시스템(Assets/Script/SaveLoad)이 준비되면 그 store 만 어댑터로 교체하면 된다.
public class UpgradeState
{
    private UpgradeSaveData data = new();

    // 해금/포인트가 바뀔 때마다 발생. 영속성 store 가 구독한다.
    public event Action Changed;

    public bool IsUnlocked(string id) => data.unlockedIds.Contains(id);

    public int Points => data.points;

    public bool CanAfford(int cost) => data.points >= cost;

    // 선행 업그레이드가 전부 해금됐는지. prerequisites 가 null/빈 배열이면 무조건 true.
    public bool ArePrerequisitesMet(BaseUpgradeData upgrade)
    {
        if (upgrade.prerequisites == null) return true;
        foreach (var prerequisite in upgrade.prerequisites)
        {
            if (prerequisite == null) continue;
            if (!IsUnlocked(prerequisite.id)) return false;
        }
        return true;
    }

    // "지금 이 업그레이드를 해금할 수 있는가" 판정을 한 곳에 모은다(기존엔 UpgradeUI 에 흩어져 있었음).
    public bool CanUnlock(BaseUpgradeData upgrade)
        => !IsUnlocked(upgrade.id) && CanAfford(upgrade.cost) && ArePrerequisitesMet(upgrade);

    public void AddPoints(int amount)
    {
        if (amount <= 0) return;
        data.points += amount;
        Changed?.Invoke();
    }

    public bool TrySpendPoints(int cost)
    {
        if (!CanAfford(cost)) return false;
        data.points -= cost;
        Changed?.Invoke();
        return true;
    }

    // 해당 계열(branch)에서 해금된 단계들의 effectAmount를 전부 더한 값
    public float GetTotalEffect(IEnumerable<BaseUpgradeData> branch)
    {
        float total = 0f;
        foreach (var upgrade in branch)
        {
            if (IsUnlocked(upgrade.id))
                total += upgrade.effectAmount;
        }
        return total;
    }

    public void Unlock(string id)
    {
        if (data.unlockedIds.Contains(id)) return;

        data.unlockedIds.Add(id);
        Changed?.Invoke();
    }

    // 해금된 업그레이드들의 cost 합계를 포인트로 환불하고, 해금 상태를 전부 리셋한다(리스펙).
    public void ResetAll(IEnumerable<BaseUpgradeData> allUpgrades)
    {
        int refund = 0;
        foreach (var upgrade in allUpgrades)
        {
            if (IsUnlocked(upgrade.id))
                refund += upgrade.cost;
        }

        data.unlockedIds.Clear();
        data.points += refund;
        Changed?.Invoke();
    }

    // 디버그용 — 자원 환불 없이 해금 상태만 초기화
    public void DebugResetWithoutRefund()
    {
        data.unlockedIds.Clear();
        Changed?.Invoke();
    }

    // 영속성 store 가 호출: 현재 상태 스냅샷 / 저장본으로 덮어쓰기.
    // Restore 는 로드 경로라 Changed 를 쏘지 않는다(자기 자신을 다시 저장하는 것 방지).
    public UpgradeSaveData Capture() => data;

    public void Restore(UpgradeSaveData savedData)
    {
        data = savedData ?? new UpgradeSaveData();
    }
}
