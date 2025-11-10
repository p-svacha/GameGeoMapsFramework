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
            MaxZoom = 4000,
            LabelFontSize = 26,
        },

        new PointFeatureDef()
        {
            DefName = "Landmark",
            Label = "landmark",
            Description = "A natural or artifical landmark.",
            MinZoom = 100,
            MaxZoom = 2500,
            LabelFontSize = 20,
        },

        new PointFeatureDef()
        {
            DefName = "RestStop",
            Label = "rest stop",
            Description = "A place to rest.",
            Icon = Resources.Load<Sprite>("Sprites/PointFeatureIcons/RestStop"),
            IconSize = 20,
            MaxZoom = 1400,
            LabelFontSize = 12,
        },

        new PointFeatureDef()
        {
            DefName = "Pin",
            Label = "pin",
            Description = "General pin that's always visible to mark any point of interest.",
            Icon = Resources.Load<Sprite>("Sprites/PointFeatureIcons/Pin"),
            IconSize = 64,
            LabelFontSize = 20,
        },
    };
}