using System.Collections.Generic;

public class ActiveBuff
{
    public IUnit Target;
    public StatType StatType;
    public List<Modifier> Modifiers = new();
    public int Stacks;
    public float RemainingTime;
    public bool Persistent;
    public object Source;
}
