using UnityEngine;

public class Racer : Entity
{
    public RaceSimulation Race { get; private set; }

    public const float BASE_STAMINA_DRAIN = 2f; // per minute

    public bool IsFinished { get; private set; }
    public float FinishTick { get; private set; }
    public float FinishTime { get; private set; } // In seconds
    public int FinishRank { get; private set; }
    public float Stamina;

    public const float MAX_STAMINA = 120f;

    // Current values
    public float CurrentStaminaDrain; // Stamina drain per tick
    public float CurrentDistanceToFinish;
    public int CurrentRank;

    public Racer(RaceSimulation race, Map map, string name, Color color, Point p) : base(map, name, color, p)
    {
        Race = race;
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

            CurrentDistanceToFinish = CurrentPath.Length - (CurrentTransitionPositionRelative * CurrentTransition.Length);
            // todo: replace current path with best general path (but performant) (calculate from next crossroads and only recalculate when next crossroads changes AND changes differently from calculated path)
            // todo: maybe needs a more performant pathfinder
            // with a new NetworkTransition class that sums up all transitions between two crossroads
            // crossroads would then act as the nodes in this bigger network
        }
    }

    protected override void OnTargetReached(float remainingTickFraction)
    {
        IsFinished = true;
        FinishTick = Race.TickNumber - remainingTickFraction;
        FinishTime = FinishTick * GameLoop.TickDeltaTime;
        FinishRank = CurrentRank;
        string timeString = HelperFunctions.GetDurationString(FinishTime, includeMilliseconds: true);
        Debug.Log($"{Name} has reached the finish on rank {FinishRank} in {timeString}.");
    }

    private void ReduceStamina(float value)
    {
        Stamina -= value;
        if (Stamina < 0f) Stamina = 0f;
    }
}
