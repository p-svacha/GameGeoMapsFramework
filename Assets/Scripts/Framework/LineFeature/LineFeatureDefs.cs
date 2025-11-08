using System.Collections.Generic;
using UnityEngine;

public static class LineFeatureDefs
{
    private static Color PAVED_STREET_COLOR = new Color(0.667f, 0.725f, 0.788f);

    public static List<LineFeatureDef> Defs => new List<LineFeatureDef>()
    {
        new LineFeatureDef()
        {
            DefName = "Street",
            Label = "street",
            Description = "A medium street often acting as connectors in neighbourhoods.",
            Color = PAVED_STREET_COLOR,
            Width = 6f,
            Surface = SurfaceDefOf.Asphalt,
        },

        new LineFeatureDef()
        {
            DefName = "StreetBig",
            Label = "main street",
            Description = "A big street often acting as the main road through a town.",
            Color = PAVED_STREET_COLOR,
            Width = 8f,
            Surface = SurfaceDefOf.Asphalt,
        },

        new LineFeatureDef()
        {
            DefName = "Highway",
            Label = "highway",
            Description = "Very wide and fast street only for cars.",
            Color = PAVED_STREET_COLOR,
            Width = 10f,
            Surface = SurfaceDefOf.Asphalt,
        },

        new LineFeatureDef()
        {
            DefName = "StreetSmall",
            Label = "small street",
            Description = "A small street often found in quartier neighbourhoods.",
            Color = PAVED_STREET_COLOR,
            Width = 4f,
            Surface = SurfaceDefOf.Asphalt,
        },

        new LineFeatureDef()
        {
            DefName = "Footpath",
            Label = "footpath",
            Description = "A paved footpath.",
            Color = PAVED_STREET_COLOR,
            Width = 2f,
            Surface = SurfaceDefOf.Asphalt,
        },

        new LineFeatureDef()
        {
            DefName = "GravelRoad",
            Label = "gravel road",
            Description = "A path or road made of gravel.",
            Color = new Color(0.567f, 0.625f, 0.688f),
            Width = 5f,
            Surface = SurfaceDefOf.Gravel,
            Texture = LineTexture.Specked,
        },

        new LineFeatureDef()
        {
            DefName = "WaterPath",
            Label = "water path",
            Description = "A ferryway or path to swim through a body of water.",
            Color = new Color(0.365f, 0.655f, 0.733f),
            Width = 2f,
            Texture = LineTexture.Dashed,
            RoundedCorners = true,
            StretchFactor = 2f,
            Surface = SurfaceDefOf.Water,
        },

        new LineFeatureDef()
        {
            DefName = "Stream",
            Label = "small stream",
            Description = "A small, flowing water body.",
            Color = new Color(0.565f, 0.855f, 0.933f),
            Width = 1f,
            Surface = SurfaceDefOf.Water,
        },

        new LineFeatureDef()
        {
            DefName = "StreamMedium",
            Label = "medium stream",
            Description = "A medium flowing water body.",
            Color = new Color(0.565f, 0.855f, 0.933f),
            Width = 2f,
            Surface = SurfaceDefOf.Water,
        },

        new LineFeatureDef()
        {
            DefName = "StreamBig",
            Label = "wide stream",
            Description = "A wide flowing water body.",
            Color = new Color(0.565f, 0.855f, 0.933f),
            Width = 4f,
            Surface = SurfaceDefOf.Water,
        },

        new LineFeatureDef()
        {
            DefName = "RiverNarrow",
            Label = "narrow river",
            Description = "A flowing water body.",
            Color = new Color(0.565f, 0.855f, 0.933f),
            Width = 8f,
            Surface = SurfaceDefOf.Water,
        },

        new LineFeatureDef()
        {
            DefName = "HikingTrail",
            Label = "hiking trail",
            Description = "A hiking trail with rough, uneven surfaces.",
            Color = new Color(0.267f, 0.451f, 0.329f),
            Width = 2f,
            Surface = SurfaceDefOf.Trail,
            Texture = LineTexture.Dashed,
            StretchFactor = 2.5f,
            RoundedCorners = true,
        },

        new LineFeatureDef()
        {
            DefName = "DirtRoad",
            Label = "dirt road",
            Description = "A small dirt path only accessible by foot.",
            Color = new Color(0.675f, 0.549f, 0.267f),
            Width = 4f,
            Surface = SurfaceDefOf.Dirt,
        },
    };
}
