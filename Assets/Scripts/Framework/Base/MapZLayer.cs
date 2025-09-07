using UnityEngine;

/// <summary>
/// Enum containing all layers used in map rendering. Map.ZLayers holds the sorting information.
/// </summary>
public enum MapZLayer
{
    Point,
    PointSnapIndicator,
    AreaPolygon,
    Line,
    LineSelectionIndicator,
    AreaSelectionIndicator,
}
