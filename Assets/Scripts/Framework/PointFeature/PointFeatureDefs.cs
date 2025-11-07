using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class PointFeatureDefs
{
    public static List<PointFeatureDef> Defs = new List<PointFeatureDef>()
    {
        new PointFeatureDef()
        {
            DefName = "Town",
            Label = "town",
            Description = "A small urban area.",
            MinZoom = 100,
            MaxZoom = 1000,
            LabelFontSize = 26,
        },

        new PointFeatureDef()
        {
            DefName = "RestStop",
            Label = "rest stop",
            Description = "A place to rest.",
            Icon = Resources.Load<Sprite>("Sprites/PointFeatureIcons/RestStop"),
            IconSize = 24,
            MaxZoom = 800,
            LabelFontSize = 16,
        },
    };
}