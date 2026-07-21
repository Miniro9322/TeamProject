using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

public class StatContainerTest : MonoBehaviour, IUnit
{
    private readonly StatContainer stat = new();

    [SerializeField] private StatDataSO statData;
    private Modifier atkBuff;
    private Modifier defDebuff;
    private Keyboard keyboard;
    private BuffManager buffManager;
    private SphereCollider trigger;
    [SerializeField] private string context;

    public StatContainer Stats => stat;

    [Inject]
    private void Construct(BuffManager manager)
    {
        buffManager = manager;
    }

    private void Awake()
    {
        stat.AddStat(StatType.ATK, statData.attackPower);
        stat.AddStat(StatType.DEF, statData.defence);
        stat.AddStat(StatType.HP, statData.maxHp);
        stat.AddStat(StatType.SP, statData.maxSp);
        Debug.Log($"[container] atk: {stat[StatType.ATK]}, def: {stat[StatType.DEF]}, hp: {stat[StatType.HP]}, sp: {stat[StatType.SP]}");
        Debug.Log($"[data] atk: {statData.attackPower}, def: {statData.defence}, hp: {statData.maxHp}, sp: {statData.maxSp}");

        stat.AddStat(StatType.DEF, 10);

        atkBuff = new(ModifierType.Flat, 10f, this);
        defDebuff = new(ModifierType.Additive, 0.1f, this);

        keyboard = Keyboard.current;
    }

    private void Start()
    {
        Debug.Log($"ATK: {stat[StatType.ATK]}, DEF: {stat[StatType.DEF]}");
        Debug.Log(buffManager == null);
    }

    private void Update()
    {
        if (keyboard != null && keyboard.digit1Key.wasPressedThisFrame)
        {
            buffManager.ApplyStackingModifier(this, StatType.ATK, ModifierType.Flat, 10f, 5f, 1, this);
            Debug.Log($"ATK: {stat[StatType.ATK]}, DEF: {stat[StatType.DEF]}");
        }
        if (keyboard != null && keyboard.digit2Key.wasPressedThisFrame)
        {
            DEFDeBuff().Forget();
        }

    }


    private async UniTaskVoid DEFDeBuff()
    {
        stat.AddModifier(StatType.DEF, defDebuff);
        Debug.Log("----- 방어력 디버프(3초) -----");
        Debug.Log($"ATK: {stat[StatType.ATK]}, DEF: {stat[StatType.DEF]}");
        await UniTask.WaitForSeconds(3);
        stat.RemoveModifier(StatType.DEF, defDebuff);
        Debug.Log("----- 방어력 디버프 끝남 -----");
        Debug.Log($"ATK: {stat[StatType.ATK]}, DEF: {stat[StatType.DEF]}");
    }

    private void OnTriggerEnter(Collider other)
    {
        var enemy = other.GetComponent<IUnit>();

        Debug.Log($"Id: {context} enemy: {enemy != null}, buffManager: {buffManager != null}, atkBuff: {atkBuff != null}");

        if (enemy != null)
        {
            buffManager.ApplyStackingModifier(enemy, StatType.ATK, ModifierType.Flat, 10f, 3f, 1, this);
        }
    }
}
