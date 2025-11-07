using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class CreateLineFeatureTool : EditorTool
{
    public override EditorToolId Id => EditorToolId.CreateLineFeatureTool;
    public override string Name => "Create Line Feature";

    public List<Point> Points = new List<Point>();

    private LineRenderer FeatureLineRenderer; // Renderer that draws the current line
    private CursorLineRenderer2D CursorLineRenderer; // Renderer that draws a dynamic line from the last line point to the cursor

    [Header("Elements")]
    public TMP_Dropdown TypeDropdown;
    public TMP_Dropdown LayerDropdown;

    public override void Init(MapEditor editor)
    {
        base.Init(editor);

        // UI
        TypeDropdown.ClearOptions();
        List<string> typeOptions = DefDatabase<LineFeatureDef>.AllDefs.Select(x => x.LabelCap).ToList();
        TypeDropdown.AddOptions(typeOptions);
        TypeDropdown.onValueChanged.AddListener(TypeDropdown_OnValueChanged);

        LayerDropdown.ClearOptions();
        List<string> layerOptions = new List<string>();
        for (int i = 0; i <= Map.FEATURE_LAYERS; i++) layerOptions.Add(i.ToString());
        LayerDropdown.AddOptions(layerOptions);
        LayerDropdown.value = Map.FEATURE_DEFAULT_LAYER;
    }

    /// <summary>
    /// Resets the current line being added while the tool is active.
    /// </summary>
    private void Reset()
    {
        // Clear temporary points
        foreach (Point p in Points.Where(x => !x.IsRegistered)) p.DestroyVisuals();
        Points.Clear();

        // Hide helper renderers
        HideCursorLineRenderer();
        FeatureLineRenderer.positionCount = 0;

        // Hover options
        MouseHoverInfo.ResetPointSelectionOptions();

        // Instructions
        Editor.SetInstructionsText("Click anywhere to start creating a new line.");
    }

    public override void OnSelect()
    {
        // Diplay options
        Map.Renderer2D.ShowAllPoints();
        MouseHoverInfo.SetShowPointSnapIndicator(true);
        MouseHoverInfo.SetCheckFeatureSelection(false);

        // Create cursor line renderer
        GameObject clrGo = new GameObject("CursorLineRenderer");

        LineRenderer cursorLineRenderer = clrGo.AddComponent<LineRenderer>();
        cursorLineRenderer.material = ResourceManager.LoadMaterial("Materials/Line");
        cursorLineRenderer.startColor = Color.white;
        cursorLineRenderer.endColor = Color.white;
        cursorLineRenderer.startWidth = 1f;
        cursorLineRenderer.endWidth = 1f;
        cursorLineRenderer.sortingLayerName = "Foreground";

        CursorLineRenderer = clrGo.AddComponent<CursorLineRenderer2D>();
        clrGo.SetActive(false);

        // Create feature line renderer
        GameObject featureLineObj = new GameObject("CurrentlyAddedLineLine");
        FeatureLineRenderer = featureLineObj.AddComponent<LineRenderer>();
        FeatureLineRenderer.material = ResourceManager.LoadMaterial("Materials/Line");
        FeatureLineRenderer.startColor = Color.white;
        FeatureLineRenderer.endColor = Color.white;
        FeatureLineRenderer.startWidth = 1f;
        FeatureLineRenderer.endWidth = 1f;
        FeatureLineRenderer.sortingLayerName = "Foreground";
        featureLineObj.gameObject.SetActive(true);

        // Set line width according to selected def
        TypeDropdown_OnValueChanged(TypeDropdown.value);

        // Reset
        Reset();
    }

    public override void OnDeselect()
    {
        GameObject.Destroy(CursorLineRenderer.gameObject);
        GameObject.Destroy(FeatureLineRenderer.gameObject);
    }

    public override void HandleLeftClick()
    {
        // Check if we re-clicked previously added point
        if (Points.Count > 0 && (MouseHoverInfo.HoveredPoint == Points.Last()))
        {
            // Less than 2 points means invalid line => abort
            if (Points.Count <= 1)
            {
                Reset();
            }

            else // Valid line => confirm
            {
                ConfirmFeature();
            }
        }

        else
        {
            if (MouseHoverInfo.HoveredPoint != null)
            {
                // Add existing point as next line point
                bool canAddPoint = true;

                if (Points.Contains(MouseHoverInfo.HoveredPoint)) canAddPoint = false; // Can't reuse a point on a line feature

                if (canAddPoint) AddPoint(MouseHoverInfo.HoveredPoint);
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

    private void TypeDropdown_OnValueChanged(int value)
    {
        LineFeatureDef selectedDef = DefDatabase<LineFeatureDef>.AllDefs[TypeDropdown.value];

        FeatureLineRenderer.startWidth = selectedDef.Width;
        FeatureLineRenderer.endWidth = selectedDef.Width;

        LineRenderer cursorLineRenderer = CursorLineRenderer.GetComponent<LineRenderer>();
        cursorLineRenderer.startWidth = selectedDef.Width;
        cursorLineRenderer.endWidth = selectedDef.Width;
    }

    /// <summary>
    /// Draws a line from the given point to the current mouse position, until hidden again.
    /// </summary>
    private void ShowCursorLineRenderer(Point anchorPoint)
    {
        CursorLineRenderer.Init(anchorPoint.Position);
        CursorLineRenderer.gameObject.SetActive(true);
    }

    private void HideCursorLineRenderer()
    {
        CursorLineRenderer.gameObject.SetActive(false);
    }

    private void ConfirmFeature()
    {
        // Add line feature to map
        Map.AddLineFeature(Points, DefDatabase<LineFeatureDef>.AllDefs[TypeDropdown.value], LayerDropdown.value);

        // Reset so we can start creating a new line
        Reset();
    }

    private void AddPoint(Point point)
    {
        // Create line point
        Points.Add(point);

        // Add point to feature line renderer
        FeatureLineRenderer.positionCount++;
        FeatureLineRenderer.SetPosition(FeatureLineRenderer.positionCount - 1, point.Position);

        // Show the cursor line renderer from the first point
        ShowCursorLineRenderer(point);

        // Update instructions
        if (Points.Count == 1) Editor.SetInstructionsText("Click anywhere to add a second point to the line.\nRight click abort.");
        else Editor.SetInstructionsText("Click anywhere to add another point to the line.\nClick on the previously added point to confirm the line.\nRight click abort.");

        // Update hoverable points
        List<Point> hoverablePoints = new List<Point>();
        hoverablePoints.AddRange(Map.Points.Values);
        foreach (Point p in Points.Where(x => !x.IsRegistered)) hoverablePoints.Add(p);
        MouseHoverInfo.SetPointSelectionOptions(hoverablePoints);
    }
}
