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
    public const float DEFAULT_ASPHALT_SPEED = 2.0f;
    public const float DEFAULT_DIRT_SPEED = 1.8f;
    public const float DEFAULT_SWIM_SPEED = 1.5f;

    public string Name;
    public Color Color;
    public float AsphaltSpeed; // units per second
    public float DirtSpeed; // units per second
    public float SwimSpeed; // units per second
}