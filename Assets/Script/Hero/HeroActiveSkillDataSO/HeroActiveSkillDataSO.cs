using System.Collections.Generic;
using UnityEngine;

public enum SkillTargetScope { Self, AnywhereOnBoard }

[CreateAssetMenu(fileName = "HeroActiveSkillData", menuName = "HeroSkill/ActiveSkillData")]
public class HeroActiveSkillDataSO : ScriptableObject
{
    public SkillTargetScope targetScope = SkillTargetScope.Self;
    public List<BuffEffect> buffList;      // 자기 버프. AttackDamageUtil.ApplySelfBuffs가 isTargetToOther==false만 적용.
    public GroundZoneDataSO groundZone;    // 선택 칸에 소환할 장판(없으면 null).

    [Header("즉시 피해(장판 아님)")]
    [Tooltip("0 = 없음. sc[ATK] * instantDamagePer 만큼 대상 칸의 적 전원에게 즉시 피해.")]
    public float instantDamagePer = 0f;
    public GameObject instantHitEffect;
    public float instantHitEffectLifetime = 1f;

    [Header("쿨타임")]
    [Tooltip("스킬 사용 후 재사용까지 걸리는 시간(초). Stat(CDR)은 아직 적용되지 않음.")]
    public float cooldown = 0f;
}
