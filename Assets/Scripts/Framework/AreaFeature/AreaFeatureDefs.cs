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

        new AreaFeatureDef()
        {
            DefName = "BuildingBrown",
            Label = "building (brown)",
            Description = "Building of any kind with a brown color.",
            Color = new Color(0.851f, 0.816f, 0.788f),
            OutlineWidth = 0.5f,
            OutlineColor = new Color(0.751f, 0.716f, 0.688f),
        },

        new AreaFeatureDef()
        {
            DefName = "DesertSand",
            Label = "desert",
            Description = "Desert.",
            Color = new Color(0.960f, 0.941f, 0.902f),
        },

        new AreaFeatureDef()
        {
            DefName = "DesertRocky",
            Label = "rocky desert",
            Description = "Rocky desert.",
            Color = new Color(0.949f, 0.906f, 0.831f),
        },

        new AreaFeatureDef()
        {
            DefName = "Mountain",
            Label = "mountain",
            Description = "Mountaneous terrain.",
            Color = new Color(0.921f, 0.913f, 0.898f),
        },

        new AreaFeatureDef()
        {
            DefName = "Mountain2",
            Label = "high mountain",
            Description = "High mountaneous terrain.",
            Color = new Color(0.851f, 0.843f, 0.828f),
        },
    };
}
