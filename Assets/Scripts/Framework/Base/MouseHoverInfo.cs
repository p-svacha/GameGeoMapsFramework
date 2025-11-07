using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Static class containing information about what the mouse is currently hovering.
/// </summary>
public static class MouseHoverInfo
{
    private static int POINT_SNAP_RANGE = 25; // in pixels
    private static int AREA_SELECTION_RANGE = 60; // maximum distance in pixels to the border of an area when inside the area
    private static int LINE_SELECTION_RANGE = 30; // maximum distance in pixels to a line
    private static int POINT_SELECTION_RANGE = 30; // maximum distance in pixels to a point

    public static Vector2 ScreenPosition { get; private set; }
    public static Vector2 WorldPosition { get; private set; }
    public static Vector2 WorldPositionWithSnap { get; private set; }

    public static bool ShowPointSnapIndicator { get; private set; }
    /// <summary>
    /// The MapFeature that is closest to the mouse and within the point snap range. May be null.
    /// </summary>
    public static Point HoveredPoint { get; private set; }
    /// <summary>
    /// If this list isn't empty, only the points in this list will be considered when checking the point to snap/hover.
    /// </summary>
    private static List<Point> PointSelectionOptions;

    public static bool DoCheckFeatureSelection { get; private set; }
    /// <summary>
    /// The MapFeature that is closest to the mouse and within the hover/selection range. May be null.
    /// </summary>
    public static MapFeature HoveredMapFeature { get; private set; }
    /// <summary>
    /// If this list isn't empty, only the features in this list will be considered when checking the map feature to hover.
    /// </summary>
    private static List<MapFeature> FeatureSelectionOptions;

    /// <summary>
    /// If not null, the hovered map feature will always be highlighted with this color instead of it's own color.
    /// </summary>
    private static Color? ForcedFeatureHighlightColor;

    public static void AwakeReset()
    {
        HoveredPoint = null;
        HoveredMapFeature = null;
        FeatureSelectionOptions = new List<MapFeature>();
        PointSelectionOptions = new List<Point>();
    }

    public static void ClearHoveredPoint() => HoveredPoint = null;
    public static void ClearHoveredMapFeature() => HoveredMapFeature = null;

    public static void Update(Map map)
    {
        if (map == null) return;

        Camera mainCam = Camera.main;

        // Calculate base screen and world position of mouse
        ScreenPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        Vector3 mouseWorldPosition3d = mainCam.ScreenToWorldPoint(ScreenPosition);
        WorldPosition = new Vector2(mouseWorldPosition3d.x, mouseWorldPosition3d.y);
        WorldPositionWithSnap = WorldPosition;

        // Point hover
        if (HoveredPoint != null) HoveredPoint.HideSnapIndicator();
        HoveredPoint = null;

        float minPointSnapDistance = float.MaxValue;

        // Check only selectable points
        List<Point> pointsToCheck = PointSelectionOptions.Count > 0 ? PointSelectionOptions : map.Points.Values.ToList();
        foreach (Point p in pointsToCheck)
        {
            Vector2 pointScreenPosition = mainCam.WorldToScreenPoint(p.Position);
            float pixelDistance = Vector2.Distance(ScreenPosition, pointScreenPosition);
            if (pixelDistance <= POINT_SNAP_RANGE && pixelDistance < minPointSnapDistance)
            {
                HoveredPoint = p;
                WorldPositionWithSnap = p.Position;
                minPointSnapDistance = pixelDistance;
            }
        }
        if (HoveredPoint != null && ShowPointSnapIndicator) HoveredPoint.ShowSnapIndicator();


        // Feature hover
        if (HoveredMapFeature != null && !HoveredMapFeature.IsDestroyed)
        {
            HoveredMapFeature.SetSelectionIndicatorColor(HoveredMapFeature.CurrentSelectionIndicatorColor);
            HoveredMapFeature.HideSelectionIndicator();
        }

        HoveredMapFeature = null;

        if (DoCheckFeatureSelection)
        {
            float minAreaSelectionDistance = float.MaxValue;

            // Area features
            foreach (AreaFeature area in map.AreaFeatures.Values)
            {
                // Skip if this feature is not one we can currently select
                if (FeatureSelectionOptions.Count > 0 && !FeatureSelectionOptions.Contains(area)) continue;

                // Skip if mouse not within area polygon
                bool isInsidePolygon = GeometryFunctions.IsPointInPolygon(area.PointPositions, WorldPosition);
                if (!isInsidePolygon) continue;

                // Convert area points to screen space so we can make pixel distance check
                List<Vector2> areaPointsInScreenPos = new List<Vector2>();
                foreach (Vector2 p in area.PointPositions)
                {
                    Vector3 screenPos = mainCam.WorldToScreenPoint(p);
                    areaPointsInScreenPos.Add(new Vector2(screenPos.x, screenPos.y));
                }

                // Check 
                float distanceToPolygonInPixels = GeometryFunctions.DistanceToLine(areaPointsInScreenPos, ScreenPosition, isClosed: true);
                if (distanceToPolygonInPixels <= AREA_SELECTION_RANGE && distanceToPolygonInPixels < minAreaSelectionDistance)
                {
                    HoveredMapFeature = area;
                    minAreaSelectionDistance = distanceToPolygonInPixels;
                }
            }

            // Line features
            foreach (LineFeature line in map.LineFeatures.Values)
            {
                // Skip if this feature is not one we can currently select
                if (FeatureSelectionOptions.Count > 0 && !FeatureSelectionOptions.Contains(line)) continue;

                // Convert area points to screen space so we can make pixel distance check
                List<Vector2> areaPointsInScreenPos = new List<Vector2>();
                foreach (Vector2 p in line.PointPositions)
                {
                    Vector3 screenPos = mainCam.WorldToScreenPoint(p);
                    areaPointsInScreenPos.Add(new Vector2(screenPos.x, screenPos.y));
                }

                // Check 
                float distanceToPolygonInPixels = GeometryFunctions.DistanceToLine(areaPointsInScreenPos, ScreenPosition, isClosed: false);
                if (distanceToPolygonInPixels <= LINE_SELECTION_RANGE && distanceToPolygonInPixels < minAreaSelectionDistance)
                {
                    HoveredMapFeature = line;
                    minAreaSelectionDistance = distanceToPolygonInPixels;
                }
            }

            // Point features
            foreach(PointFeature pointFeature in map.PointFeatures.Values)
            {
                // Skip if this feature is not one we can currently select
                if (FeatureSelectionOptions.Count > 0 && !FeatureSelectionOptions.Contains(pointFeature)) continue;

                // Check distance to point
                Vector2 featureScreenPosition = mainCam.WorldToScreenPoint(pointFeature.Point.Position);
                float pixelDistance = Vector2.Distance(ScreenPosition, featureScreenPosition);
                if (pixelDistance <= POINT_SELECTION_RANGE && pixelDistance < minAreaSelectionDistance)
                {
                    HoveredMapFeature = pointFeature;
                    minAreaSelectionDistance = pixelDistance;
                }
            }

            if (HoveredMapFeature != null && DoCheckFeatureSelection)
            {
                if (ForcedFeatureHighlightColor.HasValue) HoveredMapFeature.SetSelectionIndicatorColor(ForcedFeatureHighlightColor.Value, temporary: true);
                HoveredMapFeature.ShowSelectionIndicator();
            }
        }
    }

    public static void SetShowPointSnapIndicator(bool value)
    {
        ShowPointSnapIndicator = value;
        if (!value && HoveredPoint != null) HoveredPoint.HideSnapIndicator();
    }

    public static void SetCheckFeatureSelection(bool value)
    {
        DoCheckFeatureSelection = value;
        if (!value && HoveredMapFeature != null) HoveredMapFeature.HideSelectionIndicator();
    }

    public static void SetPointSelectionOptions(List<Point> points) => PointSelectionOptions = new List<Point>(points);
    public static void ResetPointSelectionOptions() => PointSelectionOptions.Clear();

    public static void SetFeatureSelectionOptions(List<MapFeature> features) => FeatureSelectionOptions = features;
    public static void ResetFeatureSelectionOptions() => FeatureSelectionOptions.Clear();

    public static void SetForcedHightlightColor(Color color) => ForcedFeatureHighlightColor = color;
    public static void ResetForcedHightlightColor() => ForcedFeatureHighlightColor = null;
}
