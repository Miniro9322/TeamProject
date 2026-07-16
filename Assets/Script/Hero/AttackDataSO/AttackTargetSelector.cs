using System.Collections.Generic;
using UnityEngine;

public static class AttackTargetSelector
{
    // enemies: 사거리 내 적 Transform 목록
    // shotCount: 총 발사 수 (attackCount)
    // distinctCap: 서로 다른 적 최대 수 (targetCount)
    // 반환: 길이 shotCount 의 타겟 목록. 서로 다른 적을 distinctCap명까지 우선 배치하고,
    //       적이 부족하면 라운드로빈으로 중복시킨다.
    public static List<Transform> SelectTargets(List<Transform> enemies, int shotCount, int distinctCap)
    {
        var result = new List<Transform>(shotCount);
        if (enemies == null || enemies.Count == 0 || shotCount <= 0)
            return result;

        int poolSize = Mathf.Min(distinctCap, enemies.Count);
        for (int i = 0; i < shotCount; i++)
            result.Add(enemies[i % poolSize]);

        return result;
    }
}
