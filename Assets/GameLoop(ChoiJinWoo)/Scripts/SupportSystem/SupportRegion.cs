using UnityEngine;

[CreateAssetMenu(fileName = "SupportRegion", menuName = "Scriptable Objects/SupportRegion")]
public class SupportRegion : ScriptableObject
{
    [Header("해금되는 병종")]
    [SerializeField] private Hero unlockHero;
    [Header("해금되는 자원")]
    [SerializeField] private ProductionType unlockResource;
    [Header("해금되는 적 종류")]
    [SerializeField] private EnemyBase unlockEnemy;
}
