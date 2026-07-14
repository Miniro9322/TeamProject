using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Stat
{
    private float baseValue;
    public float BaseValue => baseValue;

    private readonly List<Modifier> modifiers = new();
    private bool isModifierChanged = true;
    private float value;

    public Stat(float value)
    {
        baseValue = value;
    }

    public void AddModifier(Modifier modifier)
    {
        modifiers.Add(modifier);
        isModifierChanged = true;
    }

    public void ResetBase(float value)
    {
        baseValue = value;
        isModifierChanged = true;
    }

    public void RemoveModifier(Modifier modifier)
    {
        modifiers.Remove(modifier);
        isModifierChanged = true;
    }

    public void RemoveModifier(Predicate<Modifier> predicate)
    {
        modifiers.RemoveAll(predicate);
        isModifierChanged = true;
    }

    public void RemoveModifier(object source)
    {
        modifiers.RemoveAll(x => x.Source == source);
        isModifierChanged = true;
    }

    public float Value
    {
        get
        {
            if (isModifierChanged)
            {
                value = baseValue;

                foreach (var mod in modifiers.Where(x => x.Type == ModifierType.Flat))
                    value += mod.Value;

                float additive = modifiers.Where(x => x.Type == ModifierType.Additive).Sum(x => x.Value);

                value *= (1 + additive);

                foreach (var mod in modifiers.Where(x => x.Type == ModifierType.Multiplier))
                    value *= mod.Value;

                isModifierChanged = false;
                return value;
            }
            else
            {
                return value;
            }
        }
    }
}
