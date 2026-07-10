using UnityEngine;

public class Modifier
{
    private ModifierType type;
    private float value;
    private object source;
    public ModifierType Type => type;
    public float Value => value;
    public object Source => source;

    public Modifier(ModifierType type, float value, object source)
    {
        this.type = type;
        this.value = value;
        this.source = source;
    }
}
