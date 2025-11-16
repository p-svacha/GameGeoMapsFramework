using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

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
    public MovementModeDef CurrentMovementMode { get; private set; }
    public float CurrentStaminaDrain; // Stamina drain per tick


    /// <summary>
    /// The best general (non-entity-specific) path from the current transitions end point to the finish.
    /// <br/>Used for calculating the CurrentDistanceToFinish to get an intuitive ranking.
    /// </summary>
    public NavigationPath BestGeneralPathToFin;
    /// <summary>
    /// Flag is true, if the best general path is cheaper from the current transitions start vs the end, 
    /// meaning the racer is currently moving in the opposite direction of the best general path.
    /// </summary>
    public bool BestGeneralPathIsFromTransitionStart;
    public float CurrentDistanceToFinish;
    public int CurrentRank;

    public bool IsCurrentlyFirst() => CurrentRank == 1;
    public bool IsCurrentlyLast() => CurrentRank == Race.Racers.Count;

    public Racer CurrentRacerInFront;
    public Racer CurrentRacerInBack;

    public float CurrentDistanceToRacerInFront => CurrentDistanceToFinish - CurrentRacerInFront.CurrentDistanceToFinish; // In meters
    public float CurrentDistanceToRacerInBack => CurrentRacerInBack.CurrentDistanceToFinish - CurrentDistanceToFinish; // In meters

    public float CurrentTimeGapToRacerInFront => CurrentDistanceToRacerInFront / CurrentSpeed; // In seconds (takes speed of this raceras reference speed)
    public float CurrentTimeGapToRacerInBack => CurrentDistanceToRacerInBack / CurrentSpeed; // In seconds (takes speed of this raceras reference speed)

    // Test attributes
    public float GeneralSpeedModifier = 1f;
    public Dictionary<SurfaceDef, float> SurfaceSpeedModifiers = new Dictionary<SurfaceDef, float>();

    public Racer(RaceSimulation race, Map map, string name, Color color, Point p) : base(map, name, color, p)
    {
        Race = race;
        Stamina = MAX_STAMINA;

        InitAttributes();
        InitStats();
    }

    private void InitAttributes()
    {
        Attributes = new Dictionary<AttributeDef, Attribute>();
    }

    private void InitStats()
    {
        Stats = new Dictionary<StatDef, Stat>();
    }

    /// <summary>
    /// Called when the race starts.
    /// </summary>
    public void OnRaceStart()
    {
        CurrentMovementMode = MovementModeDefOf.Jog;
    }

    public override float GetCurrentSpeed()
    {
        float speed = GetSurfaceSpeed(CurrentSurface); ;
        speed *= CurrentMovementMode.SpeedModifier;
        return speed;
    }

    public override float GetSurfaceCostModifier(SurfaceDef surface)
    {
        return 1f / GetSurfaceSpeed(surface);
    }

    private float GetSurfaceSpeed(SurfaceDef surface)
    {
        float speed = surface.DefaultSpeed;
        speed *= GeneralSpeedModifier;
        speed *= GetSurfaceSpeedModifier(surface);
        return speed;
    }

    private float GetSurfaceSpeedModifier(SurfaceDef surface)
    {
        if (SurfaceSpeedModifiers.TryGetValue(surface, out float modifier)) return modifier;
        else return 1.0f;
    }

    protected override void OnTick()
    {
        CurrentStaminaDrain = 0f;

        // Change movement mode randomly
        if (Random.value < 0.0001f)
        {
            CurrentMovementMode = DefDatabase<MovementModeDef>.AllDefs.RandomElement();
        }

        // Only allow walk on 0 stamina
        if (Stamina == 0f && CurrentMovementMode != MovementModeDefOf.Walk)
        {
            CurrentMovementMode = MovementModeDefOf.Walk;
        }

        // General
        if (IsMoving)
        {
            // Stamina drain
            float drainPerMinute = BASE_STAMINA_DRAIN;
            drainPerMinute *= CurrentSurface.EnergyDrainFactor;
            float drainPerSecond = drainPerMinute / 60f;
            float drainPerTick = drainPerSecond / GameLoop.TPS;
            drainPerTick *= CurrentMovementMode.StaminaDrainModifier;
            CurrentStaminaDrain = drainPerTick;

            ReduceStamina(CurrentStaminaDrain);

            // Distance to finish
            float distanceLeftOnTransition;
            if (BestGeneralPathIsFromTransitionStart) distanceLeftOnTransition = CurrentTransitionPositionRelative * CurrentTransition.Length; // Inverse distance left on current transition if we move in opposite direction of best general path
            else distanceLeftOnTransition = (1f - CurrentTransitionPositionRelative) * CurrentTransition.Length;

            float bestPathLength = BestGeneralPathToFin != null ? BestGeneralPathToFin.Length : 0f;

            CurrentDistanceToFinish = bestPathLength + distanceLeftOnTransition;
        }
    }

    protected override void OnTransitionStarted()
    {
        // If we just move along the best general path, simply update it to next transition
        if (BestGeneralPathToFin != null && BestGeneralPathToFin.Transitions.Count > 1 && CurrentTransition == BestGeneralPathToFin.Transitions[1] && !BestGeneralPathIsFromTransitionStart)
        {
            BestGeneralPathToFin.CutEverythingBefore(BestGeneralPathToFin.Points[1]);
        }
        else
        {
            if (Race.EndPoint == CurrentTransition.From) throw new System.Exception("Why are me moving away from the race end point? This should never happen.");

            // No path needed if we are on final transition
            if (Race.EndPoint == CurrentTransition.To)
            {
                BestGeneralPathIsFromTransitionStart = false;
                BestGeneralPathToFin = null;
                return;
            }


            // Else 
            Profiler.BeginSample("GetBestPathToFin");
            NavigationPath bestPathFromTransitionStart = Race.GetBestPathToFin(CurrentTransition.From);
            NavigationPath bestPathFromTransitionEnd = Race.GetBestPathToFin(CurrentTransition.To);
            Profiler.EndSample();

            if (bestPathFromTransitionStart.Length < bestPathFromTransitionEnd.Length)
            {
                // We move away from best path
                BestGeneralPathToFin = bestPathFromTransitionStart;
                BestGeneralPathIsFromTransitionStart = true;
            }
            else
            {
                // We move along best path
                BestGeneralPathToFin = bestPathFromTransitionEnd;
                BestGeneralPathIsFromTransitionStart = false;
            }
        }
    }

    protected override void OnTargetReached(float remainingTickFraction)
    {
        IsFinished = true;
        FinishTick = Race.TickNumber - remainingTickFraction;
        FinishTime = FinishTick * GameLoop.TickDeltaTime;
        FinishRank = CurrentRank;
        string timeString = FinishTime.GetAsDuration(millisecondDigits: 3);
        Debug.Log($"{Name} has reached the finish on rank {FinishRank} in {timeString}.");
    }

    private void ReduceStamina(float value)
    {
        Stamina -= value;
        if (Stamina < 0f) Stamina = 0f;
    }
}
