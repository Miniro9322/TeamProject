using UnityEngine;

[CreateAssetMenu(fileName = "SupportRegion", menuName = "Scriptable Objects/SupportRegion")]
public class SupportRegion : ScriptableObject
{
    [Header("해금되는 병종")]
    [SerializeField] private HeroType unlockHero;
    [Header("해금되는 자원")]
    [SerializeField] private ProductionType unlockResource;
    [Header("해금되는 적 종류")]
    [SerializeField] private EnemyTypeList unlockEnemy;

    public HeroType UnlockHero => unlockHero;
    public ProductionType UnlockResource => unlockResource;
    public EnemyTypeList UnlockEnemy => unlockEnemy;
}
