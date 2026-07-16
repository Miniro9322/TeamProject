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
}

public enum AttackType
{
    Single,
    Multiple,
    Splash
}
