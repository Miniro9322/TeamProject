using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class StatContainerTest : MonoBehaviour
{
    private StatContainer stat = new();

    private Modifier atkBuff;
    private Modifier defDebuff;
    private Keyboard keyboard;

    private void Awake()
    {
        stat.AddStat(StatType.ATK, 100);
        stat.AddStat(StatType.DEF, 10);

        atkBuff = new(ModifierType.Flat, 10f, this);
        defDebuff = new(ModifierType.Additive, 0.1f, this);

        keyboard = Keyboard.current;
    }

    private void Start()
    {
        Debug.Log($"ATK: {stat[StatType.ATK]}, DEF: {stat[StatType.DEF]}");
    }

    private void Update()
    {
        if (keyboard != null && keyboard.digit1Key.wasPressedThisFrame)
        {
            ATKBuff().Forget();
        }
        if (keyboard != null && keyboard.digit2Key.wasPressedThisFrame)
        {
            DEFDeBuff().Forget();
        }

    }

    private async UniTaskVoid ATKBuff()
    {
        stat.AddModifier(StatType.ATK, atkBuff);
        Debug.Log("----- 공격력 버프(5초) -----");
        Debug.Log($"ATK: {stat[StatType.ATK]}, DEF: {stat[StatType.DEF]}");
        await UniTask.WaitForSeconds(5);
        stat.RemoveModifier(atkBuff);
        Debug.Log("----- 공격력 버프 끝남 -----");
        Debug.Log($"ATK: {stat[StatType.ATK]}, DEF: {stat[StatType.DEF]}");
    }

    private async UniTaskVoid DEFDeBuff()
    {
        stat.AddModifier(StatType.DEF, defDebuff);
        Debug.Log("----- 방어력 디버프(3초) -----");
        Debug.Log($"ATK: {stat[StatType.ATK]}, DEF: {stat[StatType.DEF]}");
        await UniTask.WaitForSeconds(3);
        stat.RemoveModifier(defDebuff);
        Debug.Log("----- 방어력 디버프 끝남 -----");
        Debug.Log($"ATK: {stat[StatType.ATK]}, DEF: {stat[StatType.DEF]}");
    }

}
