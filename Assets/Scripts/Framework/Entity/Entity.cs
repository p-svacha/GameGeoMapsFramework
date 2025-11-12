using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// An entity represents a moving actor whose position is always somewhere on the LineFeature network graph.
/// </summary>
public class Entity
{
    public Map Map { get; private set; }
    public int Id { get; private set; }
    public string Name { get; private set; }
    public Color Color { get; private set; }
    public float CurrentSpeed { get; private set; }

    /// <summary>
    /// The exact tick (including decimal) when this entity has reached its previous target.
    /// </summary>
    public float LastArrivalTick { get; private set; }

    public float GeneralSpeedModifier = 1f;
    public Dictionary<SurfaceDef, float> SurfaceSpeedModifiers = new Dictionary<SurfaceDef, float>();

    // Position & Movement
    public Point Point; // The point the entity has last visited. If CurrentTransition is null or CurrentTransitionPosition is 0, the entity is exactly on this point.
    public NavigationPath CurrentPath; // The path this entity is currently following.
    public Transition CurrentTransition; // The transition this entity is currently taking.
    public float CurrentTransitionPositionRelative; // Relative position (0..1) within the current transition.
    public bool IsMoving => CurrentTransition != null;
    public SurfaceDef CurrentSurface => IsMoving ? CurrentTransition.LineFeature.Surface : null;

    // Visual Display
    public GameObject VisualRoot;
    public SpriteRenderer VisualSprite;
    public SpriteRenderer SelectionIndicator;

    // World position snapshots for render interpolation
    private Vector2 PrevTickWorldPos;
    public Vector2 CurrentWorldPosition { get; private set; }

    public Entity(Map map, string name, Color color, Point p)
    {
        Map = map;
        Name = name;
        Color = color;

        if (p != null)
        {
            Map.Renderer2D.CreateEntityVisuals(this);
            SetPosition(p);
        }
    }

    public void OnRegistered(int id)
    {
        Id = id;
    }

    /// <summary>
    /// Called every tick.
    /// </summary>
    public void Tick()
    {
        // Rest values
        CurrentSpeed = 0;

        // Save previous world position for render interpolation
        PrevTickWorldPos = GetWorldPosition();

        // Check if new path has been assigned
        if (!IsMoving && CurrentPath != null)
        {
            CurrentTransition = CurrentPath.Transitions[0];
        }

        // Move along transition
        if (IsMoving)
        {
            CurrentSpeed = GetSurfaceSpeed(CurrentTransition.LineFeature.Surface);
            float distance = CurrentSpeed * GameLoop.TickDeltaTime; // Get travelled distance this tick in units (meters)
            MoveDistance(distance, tickFraction: 1f);
        }

        // Save new world position for render interpolation
        CurrentWorldPosition = GetWorldPosition();

        OnTick();
    }

    protected virtual void OnTick() { }

    /// <summary>
    /// Called every frame.
    /// </summary>
    public void Render(float alpha)
    {
        VisualRoot.transform.position = Vector2.Lerp(PrevTickWorldPos, CurrentWorldPosition, alpha);
    }

    /// <summary>
    /// Moves the entity along the current path for the given distance.
    /// <param name="tickFraction">The fraction of the tick still to move.</param>
    /// </summary>
    private void MoveDistance(float distance, float tickFraction)
    {
        float relDistanceOnCurrentTransition = distance / CurrentTransition.Length; // Get relative distance on current transition
        if (CurrentTransitionPositionRelative + relDistanceOnCurrentTransition < 1f)
        {
            // Just move along transition
            CurrentTransitionPositionRelative += relDistanceOnCurrentTransition;
        }
        else
        {
            // We will reach end of transition
            float remainingTransitionDistanceRel = 1f - CurrentTransitionPositionRelative;
            float remainingTransitionDistanceAbs = CurrentTransition.Length * remainingTransitionDistanceRel;
            float remaniningTickFraction = (1f - (remainingTransitionDistanceAbs / distance)) * tickFraction;

            // Get the absolute distance we will move after having reached end of current transition
            float distanceAfterTransitionEnd = distance - remainingTransitionDistanceAbs;

            // Update current transition and move further
            CurrentPath.CutEverythingBefore(CurrentPath.Points[1]);
            Point = CurrentPath.Points[0];
            if(CurrentPath.Transitions.Count > 0)
            {
                CurrentTransition = CurrentPath.Transitions[0];
                CurrentTransitionPositionRelative = 0f;
                MoveDistance(distanceAfterTransitionEnd, remaniningTickFraction);
            }
            else
            {
                // We reached end of path
                CurrentPath = null;
                CurrentTransition = null;
                CurrentTransitionPositionRelative = 0f;
                OnTargetReached(remaniningTickFraction);
            }
        }
    }

    /// <summary>
    /// Called when the entity has reached the end of its CurrentPath.
    /// </summary>
    /// <param name="remainingTickFraction">Describes the fraction of the tick (where the entity arrived) that was unused.</param>
    protected virtual void OnTargetReached(float remainingTickFraction) { }

    #region Position & Movement

    public void SetPosition(Point p)
    {
        Point = p;
        CurrentTransition = null;
        CurrentPath = null;
        CurrentTransitionPositionRelative = 0f;

        Vector3 worldPos = GetWorldPosition();
        PrevTickWorldPos = worldPos;
        CurrentWorldPosition = worldPos;
    }

    public void SetPath(NavigationPath path)
    {
        CurrentPath = path;
    }

    /// <summary>
    /// Returns the world position of this entity based on its Point, CurrentTransition and CurrentTransitionPositionRelative.
    /// </summary>
    private Vector2 GetWorldPosition()
    {
        if (CurrentTransition == null || CurrentTransitionPositionRelative == 0f) return Point.Position;

        return Vector2.Lerp(
            CurrentTransition.From.Position,
            CurrentTransition.To.Position,
            CurrentTransitionPositionRelative
        );
    }

    #endregion

    public void SetSurfaceSpeedModififer(SurfaceDef surface, float modifier)
    {
        SurfaceSpeedModifiers[surface] = modifier;
    }

    public void ShowAsSelected(bool value) => Map.Renderer2D.ShowEntityAsSelected(this, value);


    #region Getters

    /// <summary>
    /// Returns this entity's speed in units per second on the given surface.
    /// </summary>
    public float GetSurfaceSpeed(SurfaceDef surface)
    {
        return surface.DefaultSpeed * GetSurfaceSpeedModifier(surface) * GeneralSpeedModifier;
    }

    private float GetSurfaceSpeedModifier(SurfaceDef surface)
    {
        if (SurfaceSpeedModifiers.TryGetValue(surface, out float modifier)) return modifier;
        else return 1.0f;
    }

    #endregion
}