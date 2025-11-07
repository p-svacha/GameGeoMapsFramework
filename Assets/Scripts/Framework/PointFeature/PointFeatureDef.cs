using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PointFeatureDef : Def
{
    /// <summary>
    /// Icon to display the point feature. May be null.
    /// </summary>
    public Sprite Icon { get; init; } = null;

    /// <summary>
    /// Icon size in pixels.
    /// </summary>
    public int IconSize { get; init; } = 50;

    /// <summary>
    /// When the camera orthographic size is less than this value, the icon and label will be hidden.
    /// </summary>
    public float MinZoom { get; init; } = 0;

    /// <summary>
    /// When the camera orthographic size is greater than this value, the icon and label will be hidden.
    /// </summary>
    public float MaxZoom { get; init; } = float.MaxValue;

    public int LabelFontSize { get; init; }
}