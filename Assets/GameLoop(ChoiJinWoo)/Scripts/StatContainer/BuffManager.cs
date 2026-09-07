using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;

public class BuffManager : ITickable
{
    private readonly List<ActiveBuff> activeBuffs = new();

    public void ApplyStackingModifier(IUnit target, StatType type, ModifierType modType, float value, float duration, int maxStacks, object source)
    {
        maxStacks = Mathf.Max(1, maxStacks);

        var existing = activeBuffs.Find(b =>
        b.Target == target &&
        b.StatType == type &&
        b.Source == source);

        if (existing == null)
        {
            var modifier = CreateAndApplyModifier(target, type, modType, value, duration, source);

            activeBuffs.Add(new ActiveBuff
            {
                Target = target,
                StatType = type,
                Modifiers = new List<Modifier> { modifier },
                Stacks = 1,
                RemainingTime = duration,
                Persistent = duration <= 0f,
                Source = source
            });
            return;
        }

        if (existing.Stacks < maxStacks)
        {
            var modifier = CreateAndApplyModifier(target, type, modType, value, duration, source);
            existing.Modifiers.Add(modifier);
            existing.Stacks++;
        }

        existing.RemainingTime = duration;
    }

    private static Modifier CreateAndApplyModifier(IUnit target, StatType type, ModifierType modType, float value, float duration, object source)
    {
        var modifier = new Modifier(modType, value, duration, StatLayer.Buff, source);
        target.Stats.AddModifier(type, modifier);
        return modifier;
    }

    public void Tick()
    {
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            var buff = activeBuffs[i];
            if (buff.Persistent) continue;
            buff.RemainingTime -= Time.deltaTime;
            if (buff.RemainingTime <= 0f)
            {
                foreach (var modifier in buff.Modifiers)
                    buff.Target.Stats.RemoveModifier(buff.StatType, modifier);
                activeBuffs.RemoveAt(i);
            }
        }
    }

    public void RemoveBuff(IUnit target, StatType type, object source)
    {
        int index = activeBuffs.FindIndex(b => b.Target == target && b.StatType == type && b.Source == source);
        if (index < 0) return;

        var buff = activeBuffs[index];
        foreach (var modifier in buff.Modifiers)
            buff.Target.Stats.RemoveModifier(buff.StatType, modifier);
        activeBuffs.RemoveAt(index);
    }

    public void RemoveAllBuffs(IUnit target)
    {
        activeBuffs.RemoveAll(b =>
        {
            if (b.Target == target)
            {
                foreach (var modifier in b.Modifiers)
                    target.Stats.RemoveModifier(b.StatType, modifier);
                return true;
            }
            return false;
        });
    }

    public void RemoveBuffs(IUnit target, object source)
    {
        for (int buffIndex = activeBuffs.Count - 1; buffIndex >= 0; buffIndex--)
        {
            ActiveBuff buff = activeBuffs[buffIndex];
            if (buff.Target != target || buff.Source != source)
            {
                continue;
            }

            for (int modIndex = 0; modIndex < buff.Modifiers.Count; modIndex++)
            {
                target.Stats.RemoveModifier(buff.StatType, buff.Modifiers[modIndex]);
            }

            activeBuffs.RemoveAt(buffIndex);
        }
    }

    public int GetStacks(IUnit target, object source)
        => activeBuffs.Find(b => b.Target == target && b.Source == source)?.Stacks ?? 0;
}
