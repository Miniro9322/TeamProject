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
            var modifier = new Modifier(modType, value, duration, StatLayer.Buff, source);
            target.Stats.AddModifier(type, modifier);

            activeBuffs.Add(new ActiveBuff
            {
                Target = target,
                StatType = type,
                Modifiers = new List<Modifier> { modifier },
                Stacks = 1,
                RemainingTime = duration,
                Source = source
            });
            return;
        }

        if (existing.Stacks < maxStacks)
        {
            var modifier = new Modifier(modType, value, duration, StatLayer.Buff, source);
            target.Stats.AddModifier(type, modifier);
            existing.Modifiers.Add(modifier);
            existing.Stacks++;
        }

        existing.RemainingTime = duration;
    }

    public void Tick()
    {
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            var buff = activeBuffs[i];
            buff.RemainingTime -= Time.deltaTime;
            if (buff.RemainingTime <= 0f)
            {
                foreach (var modifier in buff.Modifiers)
                    buff.Target.Stats.RemoveModifier(buff.StatType, modifier);
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
                foreach (var modifier in b.Modifiers)
                    target.Stats.RemoveModifier(b.StatType, modifier);
                return true;
            }
            return false;
        });
    }
}
