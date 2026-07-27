using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public enum AnimSelectMode { Sequential, Random }

[CreateAssetMenu(fileName = "AttackData", menuName = "HeroAttack/AttackData")]
public class AttackDataSO : ScriptableObject
{
    public float attackPer = 1f;
    public int range = 3;
    public RangeShape rangeShape = RangeShape.Diamond;

    public AttackType attackType = AttackType.Single;
    public TargetMode targetMode = TargetMode.SameTarget;
    public int attackCount = 1;
    public int targetCount = 1;
    public float shotInterval = 0.1f;

    public AreaShape areaShape = AreaShape.Diamond;
    public int areaRange = 1;
    public int lineLength = 2;
    public int chainRange = 2;
    public int chainCount = 3;
    public float chainFalloff = 0.8f;

    public AnimSelectMode selectMode = AnimSelectMode.Sequential;
    public string[] animTriggers = { "Attack" };
    public Projectile projectilePrefab;

    // 이 공격이 재생하는 애니메이션 클립의 기본 길이(초). 0이면 배속을 걸지 않는다(안전 폴백).
    // executor가 이 값과 공격 간격(1/AS)의 비율로 animator.speed를 스케일해
    // 클립이 정확히 간격 안에서 끝나도록 맞춘다.
    public float clipLength = 0f;
    public List<BuffEffect> buffList;
    public GroundZoneDataSO groundZone; // null이면 이 공격은 장판을 깔지 않음

    [Header("아군 힐 / 피흡")]
    public float lifestealPercent = 0f; // 0 = 없음. 가한 데미지의 N%만큼 공격자 회복
    public float allyHealAmount = 0f;                      // 0 = 없음. 적중 시 범위 내 최저 체력 아군 1명 회복
    public int allyHealRange = 2;
    public RangeShape allyHealRangeShape = RangeShape.Diamond;
}

// Single: 대상 하나(또는 targetMode==DifferentEnemies면 서로 다른 적)에게 비범위 피해.
// Area: areaShape(Diamond/Square/Line/Chain)로 정의된 범위에 피해.
public enum AttackType
{
    Single,
    Area
}

// SameTarget: attackCount 만큼의 타격/캐스트가 전부 같은 대상(또는 같은 지점)에 적용.
// DifferentEnemies: attackCount 만큼의 타격/캐스트를 targetCount 이내의 서로 다른 적에게 분산(라운드로빈).
public enum TargetMode
{
    SameTarget,
    DifferentEnemies
}

// attackType == Area일 때의 범위 모양. Line/Chain은 targetMode와 무관하게 자체 타겟팅 모델로 처리된다.
public enum AreaShape
{
    Diamond,
    Square,
    Line,
    Chain
}
[System.Serializable]
public struct BuffEffect
{
    public StatType statType;
    public ModifierType modifierType;
    public float value;
    public float duration;
    public bool isTargetToOther; // false = 공격자 자신, true = 맞은 적
    public int maxStacks;        // 1 = 스택 없이 지속시간만 갱신, N>1 = 스택형. Unity 역직렬화 기본값은 0 — 적용부에서 Mathf.Max(1, maxStacks)로 보정
}