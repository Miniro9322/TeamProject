using System;
using System.Collections.Generic;
using System.Linq;

public class Stat
{
    private float baseValue;
    public float BaseValue => baseValue;

    private readonly List<Modifier> modifiers = new();

    public Stat(float value)
    {
        baseValue = value;
    }

    public void AddModifier(Modifier modifier)
    {
        modifiers.Add(modifier);
    }

    public void ResetBase(float value)
    {
        baseValue = value;
    }

    public void RemoveModifier(Modifier modifier)
    {
        modifiers.Remove(modifier);
    }

    public void RemoveModifier(Predicate<Modifier> predicate)
    {
        modifiers.RemoveAll(predicate);
    }

    public void RemoveModifier(object source)
    {
        modifiers.RemoveAll(x => x.Source == source);
    }

    public float Value
    {
        get
        {
            float result = baseValue;

            foreach (var mod in modifiers.Where(x => x.Type == ModifierType.Flat))
                result += mod.Value;

            float additive = modifiers.Where(x => x.Type == ModifierType.Additive).Sum(x => x.Value);

            result *= (1 + additive);

            foreach (var mod in modifiers.Where(x => x.Type == ModifierType.Multiplier))
                result *= mod.Value;

            return result;
        }
    }
}
