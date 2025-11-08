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
    public string Name;
    public Color Color;

    public Dictionary<SurfaceDef, float> SurfaceSpeedModifiers = new Dictionary<SurfaceDef, float>();

    #region Getters

    /// <summary>
    /// Returns this entity's speed in units per second on the given surface.
    /// </summary>
    public float GetSurfaceSpeed(SurfaceDef surface)
    {
        return surface.DefaultSpeed * GetSurfaceSpeedModifier(surface);
    }

    private float GetSurfaceSpeedModifier(SurfaceDef surface)
    {
        if (SurfaceSpeedModifiers.TryGetValue(surface, out float modifier)) return modifier;
        else return 1.0f;
    }

    #endregion
}