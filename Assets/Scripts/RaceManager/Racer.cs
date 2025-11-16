using System.Linq;
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

    public Racer(RaceSimulation race, Map map, string name, Color color, Point p) : base(map, name, color, p)
    {
        Race = race;
        Stamina = MAX_STAMINA;
    }

    public void OnRaceStart()
    {
        
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

            float distanceLeftOnTransition;
            // Inverse distance left on current transition if we move in opposite direction of best general path
            if (BestGeneralPathIsFromTransitionStart) distanceLeftOnTransition = CurrentTransitionPositionRelative * CurrentTransition.Length;
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
            NavigationPath bestPathFromTransitionStart = Race.GetBestPathToFin(CurrentTransition.From);
            NavigationPath bestPathFromTransitionEnd = Race.GetBestPathToFin(CurrentTransition.To);

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
        string timeString = HelperFunctions.GetDurationString(FinishTime, includeMilliseconds: true);
        Debug.Log($"{Name} has reached the finish on rank {FinishRank} in {timeString}.");
    }

    private void ReduceStamina(float value)
    {
        Stamina -= value;
        if (Stamina < 0f) Stamina = 0f;
    }
}
