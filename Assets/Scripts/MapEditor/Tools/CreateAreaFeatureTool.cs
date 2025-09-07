using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CreateAreaFeatureTool : EditorTool
{
    public override EditorToolId Id => EditorToolId.CreateAreaFeatureTool;
    public override string Name => "Create Area Feature";

    public static float AREA_LINE_WIDTH = 0.5f;

    public List<Point> Points = new List<Point>();

    private LineRenderer FeatureLineRenderer; // Renderer that draws the current area outline
    private CursorLineRenderer2D StartCursorLineRenderer; // Renderer that draws a dynamic line from the first area point to the cursor
    private CursorLineRenderer2D EndCursorLineRenderer; // Renderer that draws a dynamic line from the last area point to the cursor


    /// <summary>
    /// Resets the current line being added while the tool is active.
    /// </summary>
    private void Reset()
    {
        // Clear temporary points
        foreach (Point p in Points.Where(x => !x.IsRegistered)) p.DestroyVisuals();
        Points.Clear();

        // Hide helper renderers
        HideCursorLineRenderers();
        FeatureLineRenderer.positionCount = 0;

        // Hover options
        MouseHoverInfo.ResetPointSelectionOptions();

        // Instructions
        Editor.SetInstructionsText("Click anywhere to start creating a new area.");
    }

    public override void OnSelect()
    {
        // Display options
        Map.Renderer.ShowAllPoints();
        MouseHoverInfo.SetShowPointSnapIndicator(true);
        MouseHoverInfo.SetCheckFeatureSelection(false);

        // Create cursor line renderer from start point
        GameObject startCursorLineObj = new GameObject("StartCursorLineRenderer");

        LineRenderer startCursorLineRenderer = startCursorLineObj.AddComponent<LineRenderer>();
        startCursorLineRenderer.material = ResourceManager.LoadMaterial("Materials/White");
        startCursorLineRenderer.startColor = Color.white;
        startCursorLineRenderer.endColor = Color.white;
        startCursorLineRenderer.startWidth = AREA_LINE_WIDTH;
        startCursorLineRenderer.endWidth = AREA_LINE_WIDTH;
        startCursorLineRenderer.sortingLayerName = "Foreground";

        StartCursorLineRenderer = startCursorLineObj.AddComponent<CursorLineRenderer2D>();
        startCursorLineObj.SetActive(false);

        // Create cursor line renderer from end point
        GameObject endCursorLineObj = new GameObject("StartCursorLineRenderer");

        LineRenderer endCursorLineRenderer = endCursorLineObj.AddComponent<LineRenderer>();
        endCursorLineRenderer.material = ResourceManager.LoadMaterial("Materials/White");
        endCursorLineRenderer.startColor = Color.white;
        endCursorLineRenderer.endColor = Color.white;
        endCursorLineRenderer.startWidth = AREA_LINE_WIDTH;
        endCursorLineRenderer.endWidth = AREA_LINE_WIDTH;
        endCursorLineRenderer.sortingLayerName = "Foreground";

        EndCursorLineRenderer = endCursorLineObj.AddComponent<CursorLineRenderer2D>();
        endCursorLineObj.SetActive(false);

        // Create feature line renderer from start point
        GameObject featureLineObj = new GameObject("CurrentlyAddedAreaLine");
        FeatureLineRenderer = featureLineObj.AddComponent<LineRenderer>();
        FeatureLineRenderer.material = ResourceManager.LoadMaterial("Materials/White");
        FeatureLineRenderer.startColor = Color.white;
        FeatureLineRenderer.endColor = Color.white;
        FeatureLineRenderer.startWidth = AREA_LINE_WIDTH;
        FeatureLineRenderer.endWidth = AREA_LINE_WIDTH;
        FeatureLineRenderer.sortingLayerName = "Foreground";
        featureLineObj.gameObject.SetActive(true);

        // Reset
        Reset();
    }

    public override void OnDeselect()
    {
        GameObject.Destroy(StartCursorLineRenderer.gameObject);
        GameObject.Destroy(FeatureLineRenderer.gameObject);
    }

    public override void HandleLeftClick()
    {
        // Check if we clicked initial point or previously added point
        if (Points.Count > 0 && (MouseHoverInfo.HoveredPoint == Points.First() || MouseHoverInfo.HoveredPoint == Points.Last()))
        {
            // Less than 3 points means invalid area => abort
            if (Points.Count <= 2)
            {
                Reset();
            }

            else // Valid area => confirm
            {
                ConfirmFeature();
            }
        }

        else
        {
            if (MouseHoverInfo.HoveredPoint != null)
            {
                // Add existing point as next line point
                AddPoint(MouseHoverInfo.HoveredPoint);
            }

            else
            {
                // Add a new point at exact current moust position as next point
                Point newPoint = new Point(Map, MouseHoverInfo.WorldPosition);
                AddPoint(newPoint);
            }
        }
    }

    public override void HandleRightClick()
    {
        // Abort
        Reset();
    }

    private void ConfirmFeature()
    {
        // Add area feature to map
        Map.AddAreaFeature(Points);

        // Reset so we can start creating a new area
        Reset();
    }

    private void AddPoint(Point point)
    {
        // Abort if point is already part of area. An area can not include the same point twice
        if (Points.Contains(point)) return;

        // Create area point
        Points.Add(point);

        // Add point to feature line renderer
        FeatureLineRenderer.positionCount++;
        FeatureLineRenderer.SetPosition(FeatureLineRenderer.positionCount - 1, point.Position);

        // Show the cursor line renderer from the first point
        ShowStartCursorLineRenderer(Points[0]);
        ShowEndCursorLineRenderer(point);

        // Update hoverable points
        List<Point> hoverablePoints = new List<Point>();
        hoverablePoints.AddRange(Map.Points.Values);
        foreach (Point p in Points.Where(x => !x.IsRegistered)) hoverablePoints.Add(p);
        MouseHoverInfo.SetPointSelectionOptions(hoverablePoints);

        // Update instructions
        if (Points.Count == 1) Editor.SetInstructionsText("Click anywhere to add a second point to the area.\nRight click abort.");
        else if (Points.Count == 2) Editor.SetInstructionsText("Click anywhere to add a third point to the area.\nRight click abort.");
        else Editor.SetInstructionsText("Click anywhere to add another point to the area.\nClick on the initial point or the previously added point to confirm the area.\nRight click abort.");
    }

    /// <summary>
    /// Draws a line from the given point to the current mouse position, until hidden again.
    /// </summary>
    private void ShowStartCursorLineRenderer(Point anchorPoint)
    {
        StartCursorLineRenderer.Init(anchorPoint.Position);
        StartCursorLineRenderer.gameObject.SetActive(true);
    }

    /// <summary>
    /// Draws a line from the given point to the current mouse position, until hidden again.
    /// </summary>
    private void ShowEndCursorLineRenderer(Point anchorPoint)
    {
        EndCursorLineRenderer.Init(anchorPoint.Position);
        EndCursorLineRenderer.gameObject.SetActive(true);
    }

    private void HideCursorLineRenderers()
    {
        StartCursorLineRenderer.gameObject.SetActive(false);
        EndCursorLineRenderer.gameObject.SetActive(false);
    }
}
