using System.Collections.Generic;
using UnityEngine;

public static class AreaFeatureDefs
{
    public static List<AreaFeatureDef> Defs = new List<AreaFeatureDef>()
    {
        new AreaFeatureDef()
        {
            DefName = "Grassland",
            Label = "grassland",
            Description = "Basic land with no further special features.",
            Color = new Color(0.827f, 0.973f, 0.886f),
        },

        new AreaFeatureDef()
        {
            DefName = "Water",
            Label = "water",
            Description = "Water in any shape or form.",
            Color = new Color(0.565f, 0.855f, 0.933f),
        },

        new AreaFeatureDef()
        {
            DefName = "Forest",
            Label = "forest",
            Description = "Many trees.",
            Color = new Color(0.670f, 0.929f, 0.784f),
        },

        new AreaFeatureDef()
        {
            DefName = "Urban",
            Label = "urban",
            Description = "Urbanized area like a city or town.",
            Color = new Color(0.969f, 0.969f, 0.969f),
        },

        new AreaFeatureDef()
        {
            DefName = "Building",
            Label = "building",
            Description = "Building of any kind.",
            Color = new Color(0.910f, 0.914f, 0.929f),
            OutlineWidth = 0.5f,
            OutlineColor = new Color(0.775f, 0.782f, 0.818f),
        },
    };
}
