using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;

public class BuffManager : ITickable
{
    private readonly List<ActiveBuff> activeBuffs = new();

    public void ApplyTimedModifier(IUnit target, StatType type, Modifier modifier, float duration)
    {
        var existing = activeBuffs.Find(b =>
        b.Target == target &&
        b.StatType == type &&
        b.Source == modifier.Source);

        if (existing != null)
        {
            existing.RemainingTime = duration;
            return;
        }

        target.Stats.AddModifier(type, modifier);

        Debug.Log($"{target.Stats[type]}");

        activeBuffs.Add(new ActiveBuff
        {
            Target = target,
            StatType = type,
            Modifier = modifier,
            RemainingTime = duration,
            Source = modifier.Source
        });
    }

    public void Tick()
    {
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            var buff = activeBuffs[i];
            buff.RemainingTime -= Time.deltaTime;
            if (buff.RemainingTime <= 0f)
            {
                buff.Target.Stats.RemoveModifier(buff.StatType, buff.Modifier);
                Debug.Log($"{buff.StatType} (디)버프 제거됨 {buff.StatType}: {buff.Target.Stats[buff.StatType]}");
                activeBuffs.RemoveAt(i);
            }
        }
    }

    public void RemoveAllBuffs(IUnit target)
    {
        activeBuffs.RemoveAll(b =>
        {
            if (b.Target == target)
            {
                target.Stats.RemoveModifier(b.StatType, b.Modifier);
                return true;
            }
            return false;
        });
    }
}