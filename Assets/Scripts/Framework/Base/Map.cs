using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class Map
{
    public string Name { get; private set; }

    public Dictionary<int, Point> Points;
    public Dictionary<int, PointFeature> PointFeatures;
    public Dictionary<int, LineFeature> LineFeatures;
    public Dictionary<int, AreaFeature> AreaFeatures;

    public int NextPointId;
    public int NextPointFeatureId;
    public int NextLineFeatureId;
    public int NextAreaFeatureId;

    public MapRenderer2D Renderer2D { get; private set; }
    public MapRenderer3D Renderer3D { get; private set; }

    public const int FEATURE_LAYERS = 20; // Line and Area features have this many layers available to render-sort the features (to decide which one drawn on top)
    public const int FEATURE_DEFAULT_LAYER = 10;

    public Map()
    {
        Points = new Dictionary<int, Point>();
        PointFeatures = new Dictionary<int, PointFeature>();
        LineFeatures = new Dictionary<int, LineFeature>();
        AreaFeatures = new Dictionary<int, AreaFeature>();

        NextPointId = 1;
        NextPointFeatureId = 1;
        NextLineFeatureId = 1;
        NextAreaFeatureId = 1;

        Renderer2D = new MapRenderer2D(this);
        Renderer3D = new MapRenderer3D(this);
    }

    /// <summary>
    /// Called every frame.
    /// </summary>
    public void Render()
    {
        Renderer2D.Update();
    }

    #region Points

    private void RegisterNewPoint(Point p)
    {
        p.OnRegistered(NextPointId++);
        Points.Add(p.Id, p);
    }

    private void DeletePoint(Point p)
    {
        if (p.IsConnectedToAnyFeature) throw new System.Exception("Can't remove a point that is still connected to features.");

        // Destroy point visually
        p.DestroyVisuals();

        // Delete point from database
        Points.Remove(p.Id);
    }

    /// <summary>
    /// Removes a point if it is not connected to any features.
    /// </summary>
    private void DeletePointIfOrphaned(Point p)
    {
        if (!p.IsConnectedToAnyFeature)
        {
            DeletePoint(p);
        }
    }

    /// <summary>
    /// Moves p1 onto p2, removing p1 and transfering all it's feature references onto p2.
    /// </summary>
    public void MergePointIntoPoint(Point p1, Point p2)
    {
        // Change references in all features that were connected to p1 to p2
        List<LineFeature> linesStagedToRemove = new List<LineFeature>();
        List<AreaFeature> areasStagedToRemove = new List<AreaFeature>();

        // Can't merge 2 points that both had a point feature
        if (p1.HasPointFeature && p2.HasPointFeature) return;

        // Move connected features from p1 to p2
        foreach (MapFeature feat in p1.ConnectedFeatures)
        {
            if (feat is PointFeature point)
            {
                p1.PointFeature.SetPoint(p2);
            }

            if (feat is LineFeature line)
            {
                int removedPointIndex = line.Points.IndexOf(p1);

                // Check if points where neighbours on the line
                bool pointsWhereNeighbours = false;
                if (line.Points.Contains(p2))
                {
                    // Check if points where neighbours
                    int mergeTargetIndex = line.Points.IndexOf(p2);

                    if (Mathf.Abs(removedPointIndex - mergeTargetIndex) == 1) // If true, points are next to each other
                    {
                        pointsWhereNeighbours = true;
                    }
                }
                
                // If points where neighbours on the line, simply remove the merged point from the line
                if (pointsWhereNeighbours)
                {
                    if (line.Points.Count == 2) linesStagedToRemove.Add(line); // Delete the whole line if it would now have less than 2 points
                    else line.Points.RemoveAt(removedPointIndex);
                }

                // Else update the reference to target the merged to point
                else line.Points[removedPointIndex] = p2;
            }


            if (feat is AreaFeature area)
            {
                int removedPointIndex = area.Points.IndexOf(p1);

                // Check if points where neighbours on the area
                bool pointsWhereNeighbours = false;
                if (area.Points.Contains(p2))
                {
                    // Check if points where neighbours
                    int mergeTargetIndex = area.Points.IndexOf(p2);

                    if (Mathf.Abs(removedPointIndex - mergeTargetIndex) == 1 || Mathf.Abs(removedPointIndex - mergeTargetIndex) == area.Points.Count - 1) // If true, points are next to each other
                    {
                        pointsWhereNeighbours = true;
                    }
                }

                // If points where neighbours on the line, simply remove the merged point from the area
                if (pointsWhereNeighbours)
                {
                    if (area.Points.Count == 3) areasStagedToRemove.Add(area); // Delete the whole area if it would now have less than 2 points
                    else area.Points.RemoveAt(removedPointIndex);
                }

                // Else update the reference to target the merged to point
                else area.Points[removedPointIndex] = p2;
            }

            p2.AddConnectedFeature(feat);
        }

        // Delete all features that became invalid due to too few points
        foreach (LineFeature toRemove in linesStagedToRemove) DeleteLineFeature(toRemove);
        foreach (AreaFeature toRemove in areasStagedToRemove) DeleteAreaFeature(toRemove);

        // Remove p1
        p1.ConnectedFeatures.Clear();
        DeletePoint(p1);

        // Remove duplicate points on all features
        List<MapFeature> featuresToCheck = new List<MapFeature>(p2.ConnectedFeatures);
        foreach (MapFeature feat in featuresToCheck)
        {
            if (feat is LineFeature line) RemoveDuplicateLinePoints(line);
            if (feat is AreaFeature area) RemoveDuplicateAreaPoints(area);
        }

        // Recalculate all transitions from line features now connected to p2
        foreach (LineFeature line in p2.LineFeatures) line.RecalculateTransitions();

        // Redraw all features now connected to p2
        foreach (MapFeature feat in p2.ConnectedFeatures) Renderer2D.RedrawFeature(feat);
    }

    #endregion

    #region Line Features

    public LineFeature AddLineFeature(List<Point> points, LineFeatureDef def, int renderLayer)
    {
        // Register all unregistered points added in line
        foreach (Point p in points)
        {
            if (!p.IsRegistered) RegisterNewPoint(p);
        }

        // Create new line feature
        LineFeature newFeature = new LineFeature(this, NextLineFeatureId++, points, def, renderLayer);
        LineFeatures.Add(newFeature.Id, newFeature);

        // Add feature reference to all points
        foreach (Point p in points) p.AddConnectedFeature(newFeature);

        // Calculate transitions
        newFeature.RecalculateTransitions();

        return newFeature;
    }

    public void DeleteLineFeature(LineFeature line)
    {
        // Remove feature reference from all points
        foreach (Point p in line.Points) p.RemoveConnectedFeature(line);

        // Remove line visually
        GameObject.Destroy(line.VisualRoot);
        line.IsDestroyed = true;

        // Remove line from database
        LineFeatures.Remove(line.Id);

        // Recalculate transitions in points of deleted line feature
        foreach (Point p in line.Points) p.RecalculateTransitions();

        // Remove orphaned points
        foreach (Point p in line.Points)
        {
            DeletePointIfOrphaned(p);
        }
    }

    /// <summary>
    /// Merges two line features at a specific point so they become one. All properties from line1 will be carried over.
    /// </summary>
    public void MergeLines(LineFeature line1, LineFeature line2, Point mergePoint)
    {
        if (mergePoint == line1.StartPoint)
        {
            if (mergePoint == line2.StartPoint)
            {
                for(int i = 1; i < line2.Points.Count; i++)
                {
                    Point pointToAddToLine1 = line2.Points[i];
                    pointToAddToLine1.AddConnectedFeature(line1);
                    line1.Points.Insert(0, pointToAddToLine1);
                }
            }
            if (mergePoint == line2.EndPoint)
            {
                for (int i = 0; i < line2.Points.Count - 1; i++)
                {
                    Point pointToAddToLine1 = line2.Points[i];
                    pointToAddToLine1.AddConnectedFeature(line1);
                    line1.Points.Insert(i, pointToAddToLine1);
                }
            }
        }
        if (mergePoint == line1.EndPoint)
        {
            if (mergePoint == line2.StartPoint)
            {
                for (int i = 1; i < line2.Points.Count; i++)
                {
                    Point pointToAddToLine1 = line2.Points[i];
                    pointToAddToLine1.AddConnectedFeature(line1);
                    line1.Points.Add(pointToAddToLine1);
                }
            }
            if (mergePoint == line2.EndPoint)
            {
                for (int i = line2.Points.Count - 1; i >= 0; i--)
                {
                    Point pointToAddToLine1 = line2.Points[i];
                    pointToAddToLine1.AddConnectedFeature(line1);
                    line1.Points.Add(pointToAddToLine1);
                }
            }
        }

        // Update line1 visuals
        Renderer2D.RedrawFeature(line1);

        // Remove line2
        DeleteLineFeature(line2);

        // Remove duplicates in line1
        RemoveDuplicateLinePoints(line1);

        // Recalculate transitions
        line1.RecalculateTransitions();
    }

    public void SplitLine(LineFeature existingLine, Point splitPoint)
    {
        if (!LineFeatures.ContainsKey(existingLine.Id)) throw new System.Exception("Line to split must exist and be registered on the map.");
        if (!existingLine.Points.Contains(splitPoint)) throw new System.Exception("Line must contain split point.");
        if (splitPoint == existingLine.StartPoint) throw new System.Exception("Split point cannot be start or end of line");
        if (splitPoint == existingLine.EndPoint) throw new System.Exception("Split point cannot be start or end of line");

        // Identify points the new line will have
        int pointIndex = existingLine.Points.IndexOf(splitPoint);
        List<Point> newLinePoints = new List<Point>();
        for (int i = pointIndex; i < existingLine.Points.Count; i++) newLinePoints.Add(existingLine.Points[i]);

        // Identify the points the existing line will lose
        List<Point> removedPoints = new List<Point>(newLinePoints);
        removedPoints.Remove(splitPoint);

        // Remove points from existing line
        foreach (Point pointToRemove in removedPoints) existingLine.Points.Remove(pointToRemove);

        // Create new line
        LineFeature splitLine = AddLineFeature(newLinePoints, existingLine.Def, existingLine.RenderLayer);
        splitLine.SetType(existingLine.Def);
        splitLine.SetRenderLayer(existingLine.RenderLayer);

        // Recalculate transitions
        existingLine.RecalculateTransitions();
        splitLine.RecalculateTransitions();

        // Redraw both lines
        Renderer2D.RedrawFeature(existingLine);
        Renderer2D.RedrawFeature(splitLine);
    }

    public void SplitLineSegment(LineFeature line, Point newSplitPoint, int splitIndex)
    {
        // Register new point
        RegisterNewPoint(newSplitPoint);
        newSplitPoint.AddConnectedFeature(line);

        // Insert point into line
        line.Points.Insert(splitIndex + 1, newSplitPoint);

        // Redraw line
        Renderer2D.RedrawFeature(line);

        // Recalculate transitions
        line.RecalculateTransitions();
    }

    public void ExpandLineAtStart(LineFeature line, Point newStartPoint)
    {
        // Register new point
        RegisterNewPoint(newStartPoint);
        newStartPoint.AddConnectedFeature(line);

        // Insert point into line
        line.Points.Insert(0, newStartPoint);

        // Redraw line
        Renderer2D.RedrawFeature(line);

        // Recalculate transitions
        line.RecalculateTransitions();
    }

    public void ExpandLineAtEnd(LineFeature line, Point newEndPoint)
    {
        // Register new point
        RegisterNewPoint(newEndPoint);
        newEndPoint.AddConnectedFeature(line);

        // Insert point into line
        line.Points.Add(newEndPoint);

        // Redraw line
        Renderer2D.RedrawFeature(line);

        // Recalculate transitions
        line.RecalculateTransitions();
    }

    public void RemoveLineFeaturePoint(LineFeature line, Point point)
    {
        // If line only contains 2 points, remove it completely
        if (line.Points.Count == 2)
        {
            DeleteLineFeature(line);
            return;
        }

        // Remove point from line
        line.Points.Remove(point);
        point.RemoveConnectedFeature(line);

        // Remove point if orphaned
        DeletePointIfOrphaned(point);

        // Redraw line
        Renderer2D.RedrawFeature(line);

        // Recalculate transitions
        line.RecalculateTransitions();
    }

    /// <summary>
    /// Removes all points in a line feature that are used more than once.
    /// </summary>
    public void RemoveDuplicateLinePoints(LineFeature line)
    {
        // If less than 3 unique points, just remove it
        if (line.Points.Distinct().Count() < 2)
        {
            DeleteLineFeature(line);
            return;
        }

        // Remove duplicate points
        List<Point> pointsToRemove = new List<Point>();
        List<Point> visitedPoints = new List<Point>();
        foreach (Point p in line.Points)
        {
            if (visitedPoints.Contains(p)) pointsToRemove.Add(p);
            visitedPoints.Add(p);
        }

        foreach (Point pointToRemove in pointsToRemove)
        {
            Debug.Log($"Removing point with id {pointToRemove.Id} from line {line.Id} because it is a duplicate.");

            // Remove last instance
            int lastIndex = line.Points.LastIndexOf(pointToRemove);
            line.Points.RemoveAt(lastIndex);
        }

        // Redraw
        Renderer2D.RedrawFeature(line);

        // Recalculate transitions
        line.RecalculateTransitions();
    }

    #endregion

    #region Area Features

    public AreaFeature AddAreaFeature(List<Point> points, AreaFeatureDef def, int renderLayer)
    {
        // Register all unregistered points added in line
        foreach (Point p in points)
        {
            if (!p.IsRegistered) RegisterNewPoint(p);
        }

        // Create new line feature
        AreaFeature newFeature = new AreaFeature(this, NextAreaFeatureId++, points, def, renderLayer);
        AreaFeatures.Add(newFeature.Id, newFeature);

        // Add feature reference to all points
        foreach (Point p in points) p.AddConnectedFeature(newFeature);

        return newFeature;
    }

    public void DeleteAreaFeature(AreaFeature area)
    {
        // Remove feature reference from all points
        foreach (Point p in area.Points) p.RemoveConnectedFeature(area);

        // Remove line visually
        GameObject.Destroy(area.VisualRoot);
        area.IsDestroyed = true;

        // Remove line from database
        AreaFeatures.Remove(area.Id);

        // Remove orphaned points
        foreach (Point p in area.Points)
        {
            DeletePointIfOrphaned(p);
        }
    }

    public void SplitAreaLineSegment(AreaFeature area, Point newSplitPoint, int splitIndex)
    {
        // Register new point
        RegisterNewPoint(newSplitPoint);
        newSplitPoint.AddConnectedFeature(area);

        // Insert point into line
        area.Points.Insert(splitIndex + 1, newSplitPoint);

        // Redraw line
        Renderer2D.RedrawFeature(area);
    }

    public void RemoveAreaFeaturePoint(AreaFeature area, Point point)
    {
        // If area only contains 3 points, remove it completely
        if (area.Points.Count == 3)
        {
            DeleteAreaFeature(area);
            return;
        }

        // Remove point from line
        area.Points.Remove(point);
        point.RemoveConnectedFeature(area);

        // Remove point if orphaned
        DeletePointIfOrphaned(point);

        // Redraw line
        Renderer2D.RedrawFeature(area);
    }

    /// <summary>
    /// Removes all points in an area feature that are used more than once.
    /// </summary>
    public void RemoveDuplicateAreaPoints(AreaFeature area)
    {
        // If less than 3 unique points, just remove it
        if(area.Points.Distinct().Count() < 3)
        {
            DeleteAreaFeature(area);
            return;
        }

        // Remove duplicate points
        List<Point> pointsToRemove = new List<Point>();
        List<Point> visitedPoints = new List<Point>();
        foreach(Point p in area.Points)
        {
            if (visitedPoints.Contains(p)) pointsToRemove.Add(p);
            visitedPoints.Add(p);
        }

        foreach (Point pointToRemove in pointsToRemove)
        {
            Debug.Log($"Removing point with id {pointToRemove.Id} from area {area.Id} because it is a duplicate.");

            // Remove last instance
            int lastIndex = area.Points.LastIndexOf(pointToRemove);
            area.Points.RemoveAt(lastIndex);
        }

        // Redraw
        Renderer2D.RedrawFeature(area);
    }

    #endregion

    #region Point Features

    public PointFeature AddPointFeature(Point point, PointFeatureDef def, string label)
    {
        // Register point if not already
        if (!point.IsRegistered) RegisterNewPoint(point);

        // Create new point feature
        PointFeature newFeature = new PointFeature(this, NextPointFeatureId++, point, def, label);
        PointFeatures.Add(newFeature.Id, newFeature);

        // Add feature reference to point
        point.AddConnectedFeature(newFeature);

        return newFeature;
    }

    public void DeletePointFeature(PointFeature feature)
    {
        // Remove feature reference from point
        feature.Point.RemoveConnectedFeature(feature);

        // Remove feature visuals
        GameObject.Destroy(feature.VisualRoot);
        feature.IsDestroyed = true;

        // Remove feature from database
        PointFeatures.Remove(feature.Id);

        // Remove point if orphaned
        DeletePointIfOrphaned(feature.Point);
    }

    public List<Point> GetNavigationNetworkPoints() => Points.Values.Where(p => p.HasLineFeature).ToList();

    #endregion

    #region Rendering

    /// <summary>
    /// Changes display settings to disable all interactions with the map and hiding all points.
    /// </summary>
    public void SetDisplayToViewMode()
    {
        Renderer2D.HideAllPoints();
        MouseHoverInfo.SetShowPointSnapIndicator(false);
        MouseHoverInfo.SetCheckFeatureSelection(false);
    }

    #endregion

    public void SetName(string name)
    {
        Name = name;
    }

    public void DestroyAllVisuals()
    {
        GameObject.Destroy(Renderer2D.MapRoot);
        GameObject.Destroy(Renderer2D.PointFeatureContainer.gameObject);
    }

    #region Save / Load

    /// <summary>
    /// Loads the map in the default map data directory with the given name.
    /// </summary>
    public static Map LoadMap(string name)
    {
        MapData loadedData = JsonUtilities.LoadData<MapData>(name);
        Map loadedMap = new Map(loadedData);
        return loadedMap;
    }

    public Map(MapData data)
    {
        Points = new Dictionary<int, Point>();
        PointFeatures = new Dictionary<int, PointFeature>();
        LineFeatures = new Dictionary<int, LineFeature>();
        AreaFeatures = new Dictionary<int, AreaFeature>();

        // Renderer
        Renderer2D = new MapRenderer2D(this);
        Renderer3D = new MapRenderer3D(this);

        // Load points
        foreach (PointData pointData in data.Points)
        {
            Point point = new Point(this, pointData);
            Points.Add(point.Id, point);
        }

        // Load point features
        if (data.PointFeatures != null)
        {
            foreach (PointFeatureData featureData in data.PointFeatures)
            {
                PointFeature feature = new PointFeature(this, featureData);
                PointFeatures.Add(feature.Id, feature);
            }
        }

        // Load line features
        foreach (LineFeatureData featureData in data.LineFeatures)
        {
            LineFeature feature = new LineFeature(this, featureData);
            LineFeatures.Add(feature.Id, feature);
        }

        // Load area features
        foreach (AreaFeatureData featureData in data.AreaFeatures)
        {
            AreaFeature feature = new AreaFeature(this, featureData);
            AreaFeatures.Add(feature.Id, feature);
        }

        // Add feature references to points
        foreach (PointFeature feat in PointFeatures.Values)
        {
            feat.Point.AddConnectedFeature(feat);
        }
        foreach (LineFeature feat in LineFeatures.Values)
        {
            foreach (Point p in feat.Points) p.AddConnectedFeature(feat);
        }
        foreach (AreaFeature feat in AreaFeatures.Values)
        {
            foreach (Point p in feat.Points) p.AddConnectedFeature(feat);
        }

        // Calculate transitions
        foreach (LineFeature feat in LineFeatures.Values) feat.RecalculateTransitions();

        // Database ID's
        NextPointId = Points.Count == 0 ? 1 : Points.Max(x => x.Key) + 1;
        NextPointFeatureId = PointFeatures.Count == 0 ? 1 : PointFeatures.Max(x => x.Key) + 1;
        NextLineFeatureId = LineFeatures.Count == 0 ? 1 : LineFeatures.Max(x => x.Key) + 1;
        NextAreaFeatureId = AreaFeatures.Count == 0 ? 1 : AreaFeatures.Max(x => x.Key) + 1;

        // Log
        Debug.Log($"Successfully loaded Map {data.Name}: Loaded {Points.Count} points, {PointFeatures.Count} point features, {LineFeatures.Count} line features, {AreaFeatures.Count} area features.");
    }

    /// <summary>
    /// Saves the current simulation state
    /// </summary>
    public void Save()
    {
        JsonUtilities.SaveData<MapData>(ToData(), Name);

        // Save a backup
        int rng = (int)(Random.value * 15);
        string backupName = Name + "_backup_" + rng;
        JsonUtilities.SaveData<MapData>(ToData(), backupName, isBackup: true);
    }

    public MapData ToData()
    {
        MapData data = new MapData();
        data.Name = Name;
        data.Points = Points.Values.Select(x => x.ToData()).ToList();
        data.PointFeatures = PointFeatures.Values.Select(x => x.ToData()).ToList();
        data.LineFeatures = LineFeatures.Values.Select(x => x.ToData()).ToList();
        data.AreaFeatures = AreaFeatures.Values.Select(x => x.ToData()).ToList();

        return data;
    }

    #endregion
}
