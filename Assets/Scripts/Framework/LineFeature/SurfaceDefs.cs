using System.Collections.Generic;
using UnityEngine;

public static class SurfaceDefs
{
    public static List<SurfaceDef> Defs => new List<SurfaceDef>()
    {
        new SurfaceDef()
        {
            DefName = "Asphalt",
            Label = "asphalt",
            Description  = "",
            DefaultDisplayColor = new Color(0.667f, 0.725f, 0.788f),
            DefaultSpeed = 2.0f,
            EnergyDrainFactor = 1.0f,
        },

        new SurfaceDef()
        {
            DefName = "Dirt",
            Label = "dirt",
            Description = "",
            DefaultDisplayColor = new Color(0.675f, 0.549f, 0.267f),
            DefaultSpeed = 1.8f,
            EnergyDrainFactor = 1.1f,
        },

        new SurfaceDef()
        {
            DefName = "Gravel",
            Label = "gravel",
            Description = "",
            DefaultDisplayColor = new Color(0.567f, 0.625f, 0.688f),
            DefaultSpeed = 1.6f,
            EnergyDrainFactor = 1.1f,
        },

        new SurfaceDef()
        {
            DefName = "Trail",
            Label = "trail",
            Description = "",
            DefaultDisplayColor = new Color(0.575f, 0.549f, 0.367f),
            DefaultSpeed = 1.4f,
            EnergyDrainFactor = 1.5f,
        },

        new SurfaceDef()
        {
            DefName = "Sand",
            Label = "sand",
            Description = "",
            DefaultDisplayColor = new Color(0.749f, 0.706f, 0.631f),
            DefaultSpeed = 1.2f,
            EnergyDrainFactor = 2.0f,
        },

        new SurfaceDef()
        {
            DefName = "Water",
            Label = "water",
            Description = "",
            DefaultDisplayColor = new Color(0.565f, 0.855f, 0.933f),
            DefaultSpeed = 1.0f,
            EnergyDrainFactor = 3.0f,
        },
    };
}
