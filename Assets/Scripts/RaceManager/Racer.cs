using UnityEngine;

public class Racer : Entity
{
    public const float BASE_STAMINA_DRAIN = 2f; // per minute

    public bool IsFinished;
    public float Stamina;

    public const float MAX_STAMINA = 120f;
    public float CurrentStaminaDrain; // Stamina drain per tick

    public Racer(Map map, string name, Color color, Point p) : base(map, name, color, p)
    {
        Stamina = MAX_STAMINA;
    }

    protected override void OnTick()
    {
        CurrentStaminaDrain = 0f;
        if (IsMoving)
        {
            float drainPerMinute = BASE_STAMINA_DRAIN;
            drainPerMinute *= CurrentSurface.EnergyDrainFactor;
            float drainPerSecond = drainPerMinute / 60f;
            float drainPerTick = drainPerSecond / GameLoop.TPS;
            CurrentStaminaDrain = drainPerTick;

            ReduceStamina(CurrentStaminaDrain);
        }
    }

    private void ReduceStamina(float value)
    {
        Stamina -= value;
        if (Stamina < 0f) Stamina = 0f;
    }
}
