using UnityEngine;

public class MovementModeDef : Def
{
    public string Verb { get; init; }
    public Color Color { get; init; }
    public char Char { get; init; }
    public float SpeedModifier { get; init; }
    public float StaminaDrainModifier { get; init; }
}
