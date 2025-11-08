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
    public Map Map;
    public string Name;
    public Color Color;

    public float GeneralSpeedModifier = 1f;
    public Dictionary<SurfaceDef, float> SurfaceSpeedModifiers = new Dictionary<SurfaceDef, float>();

    // Position & Movement
    public Point Point; // The point the entity has last visited. If CurrentTransition is null or CurrentTransitionPosition is 0, the entity is exactly on this point.
    public NavigationPath CurrentPath; // The path this entity is currently following.
    public Transition CurrentTransition; // The transition this entity is currently taking.
    public float CurrentTransitionPositionRelative; // Relative position (0..1) within the current transition.

    // Visual Display
    public GameObject VisualRoot;
    public GameObject VisualSprite;

    // World position snapshots for render interpolation
    private Vector2 PrevTickWorldPos;
    private Vector2 CurrentTickWorldPos;

    public Entity(Map map, string name, Color color, Point p)
    {
        Map = map;
        Name = name;
        Color = color;

        if (p != null)
        {
            InitVisuals();
            SetPosition(p);
        }
    }

    /// <summary>
    /// Called every tick.
    /// </summary>
    public void Tick()
    {
        // Save previous world position for render interpolation
        PrevTickWorldPos = GetWorldPosition();

        // Check if new path is assigned
        if (CurrentTransition == null && CurrentPath != null)
        {
            CurrentTransition = CurrentPath.Transitions[0];
        }

        // Move along transition
        if (CurrentTransition != null)
        {
            float transitionSpeed = GetSurfaceSpeed(CurrentTransition.LineFeature.Surface);
            float distance = transitionSpeed * GameLoop.TickDeltaTime; // Get travelled distance this tick in units (meters)
            MoveDistance(distance);
        }

        // Save new world position for render interpolation
        CurrentTickWorldPos = GetWorldPosition();
    }

    /// <summary>
    /// Called every frame.
    /// </summary>
    public void Render(float alpha)
    {
        VisualRoot.transform.position = Vector2.Lerp(PrevTickWorldPos, CurrentTickWorldPos, alpha);
    }

    /// <summary>
    /// Moves the entity along the current path for the given distance.
    /// </summary>
    private void MoveDistance(float distance)
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

            // Get the absolute distance we will move after having reached end of current transition
            float distanceAfterTransitionEnd = distance - remainingTransitionDistanceAbs;

            // Update current transition and move further
            CurrentPath.CutEverythingBefore(CurrentPath.Points[1]);
            Point = CurrentPath.Points[0];
            if(CurrentPath.Transitions.Count > 0)
            {
                CurrentTransition = CurrentPath.Transitions[0];
                CurrentTransitionPositionRelative = 0f;
                MoveDistance(distanceAfterTransitionEnd);
            }
            else
            {
                // We reached end of path
                CurrentPath = null;
                CurrentTransition = null;
                CurrentTransitionPositionRelative = 0f;
            }
        }
    }

    private void InitVisuals()
    {
        VisualRoot = new GameObject("Entity_" + Name);
        VisualRoot.transform.SetParent(Map.Renderer2D.MapRoot.transform);

        VisualSprite = new GameObject("Sprite");
        VisualSprite.transform.SetParent(VisualRoot.transform);
        SpriteRenderer sr = VisualSprite.AddComponent<SpriteRenderer>();
        sr.sprite = ResourceManager.LoadSprite("Sprites/Point");
        sr.color = Color;
        sr.sortingLayerName = "Entity";
        VisualSprite.transform.localScale = new Vector3(5f, 5f, 1f);
    }

    #region Position & Movement

    public void SetPosition(Point p)
    {
        Point = p;
        CurrentTransition = null;
        CurrentPath = null;
        CurrentTransitionPositionRelative = 0f;

        Vector3 worldPos = GetWorldPosition();
        PrevTickWorldPos = worldPos;
        CurrentTickWorldPos = worldPos;
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