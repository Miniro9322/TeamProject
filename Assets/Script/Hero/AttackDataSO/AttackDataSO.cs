using UnityEngine;

public enum AnimSelectMode { Sequential, Random }

[CreateAssetMenu(fileName = "AttackData", menuName = "HeroAttack/AttackData")]
public class AttackDataSO : ScriptableObject
{
    public float attackPer = 1f;
    public int range = 3;
    public bool square = false;

    public AttackType attackType = AttackType.Single;
    public int attackCount = 1;
    public int targetCount = 1;
    public float shotInterval = 0.1f;
    public int splashRange = 1;
    public bool splashSquare = false;

    public AnimSelectMode selectMode = AnimSelectMode.Sequential;
    public string[] animTriggers = { "Attack" };
    public Projectile projectilePrefab;

    // 이 공격이 재생하는 애니메이션 클립의 기본 길이(초). 0이면 배속을 걸지 않는다(안전 폴백).
    // executor가 이 값과 공격 간격(1/AS)의 비율로 animator.speed를 스케일해
    // 클립이 정확히 간격 안에서 끝나도록 맞춘다.
    public float clipLength = 0f;
}

public enum AttackType
{
    Single,
    Multiple,
    Splash
}
