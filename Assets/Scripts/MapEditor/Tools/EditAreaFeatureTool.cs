using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class EditAreaFeatureTool : EditorTool
{
    public override EditorToolId Id => EditorToolId.EditAreaFeatureTool;
    public override string Name => "Edit Area Feature";

    private AreaFeature SelectedFeature;

    [Header("Elements")]
    public GameObject NoFeatureSelectedInfo;
    public GameObject FeaturePropertiesContainer;
    public TextMeshProUGUI IdText;
    public TMP_Dropdown TypeDropdown;
    public TMP_Dropdown LayerDropdown;
    public Button DeleteButton;

    // Moving & merging points
    private Point DraggedPoint;
    private Vector2 PointDrag_InitialMouseWorldPosition;
    private Vector2 PointDrag_InitialPointPosition;

    // Adding points
    private List<Point> TemporarySplitPoints = new List<Point>();

    public override void Init(MapEditor editor)
    {
        base.Init(editor);

        // UI
        TypeDropdown.ClearOptions();
        List<string> typeOptions = DefDatabase<AreaFeatureDef>.AllDefs.Select(x => x.LabelCap).ToList();
        TypeDropdown.AddOptions(typeOptions);
        TypeDropdown.onValueChanged.AddListener(TypeDropdown_OnValueChanged);

        LayerDropdown.ClearOptions();
        List<string> layerOptions = new List<string>();
        for (int i = 0; i <= 10; i++) layerOptions.Add(i.ToString());
        LayerDropdown.AddOptions(layerOptions);
        LayerDropdown.onValueChanged.AddListener(LayerDropdown_OnValueChanged);

        DeleteButton.onClick.AddListener(DeleteButton_OnClick);
    }

    public override void OnSelect()
    {
        // Reset selected feature
        SelectFeature(null);

        // Display and hover options
        ResetHoverableFeatures();

        SetStandardInstructions();
    }

    public void SelectFeature(AreaFeature feature)
    {
        // Abort current action
        if (DraggedPoint != null) AbortDrag();

        // Unselect previous selected feature
        if (SelectedFeature != null)
        {
            SelectedFeature.HideSelectionIndicator(removeForced: true);

            // Remove split points
            ClearSplitPoints();
        }
        Map.Renderer2D.HideAllPoints();

        // Select new feature
        SelectedFeature = feature;

        if (SelectedFeature != null)
        {
            // Show correct content in tool window
            NoFeatureSelectedInfo.SetActive(false);
            FeaturePropertiesContainer.SetActive(true);

            // Highlight selected features
            SelectedFeature.ShowFeaturePoints();
            SelectedFeature.ShowSelectionIndicator(forced: true);

            // Values
            IdText.text = feature.Id.ToString();
            TypeDropdown.SetValueWithoutNotify(DefDatabase<AreaFeatureDef>.AllDefs.IndexOf(SelectedFeature.Def));
            LayerDropdown.SetValueWithoutNotify(SelectedFeature.RenderLayer);

            // Split points
            RegenerateSplitPoints();

            // Hover settings
            MouseHoverInfo.SetShowPointSnapIndicator(true);
            ResetHoverablePoints();
        }
        else // No selected feature
        {
            NoFeatureSelectedInfo.SetActive(true);
            FeaturePropertiesContainer.SetActive(false);
            MouseHoverInfo.SetShowPointSnapIndicator(false);
            MouseHoverInfo.ResetPointSelectionOptions();
        }
    }

    /// <summary>
    /// Sets hoverable and snappable points to the points of the selected feature + all temporary split points.
    /// </summary>
    private void ResetHoverablePoints()
    {
        List<Point> hoverablePoints = new List<Point>();
        hoverablePoints.AddRange(SelectedFeature.Points);
        hoverablePoints.AddRange(TemporarySplitPoints);
        MouseHoverInfo.SetPointSelectionOptions(hoverablePoints);
    }

    /// <summary>
    /// Sets hoverable features to all line features on the map.
    /// </summary>
    private void ResetHoverableFeatures()
    {
        if (Map.AreaFeatures.Count == 0)
        {
            MouseHoverInfo.SetCheckFeatureSelection(false);
        }
        else
        {
            MouseHoverInfo.SetCheckFeatureSelection(true);
            MouseHoverInfo.SetFeatureSelectionOptions(Map.AreaFeatures.Values.Select(x => (MapFeature)x).ToList());
        }
    }

    #region Input Handling

    public override void HandleLeftClick()
    {
        if (DraggedPoint != null) return;

        // Create new split point
        if (SelectedFeature != null && MouseHoverInfo.HoveredPoint != null && TemporarySplitPoints.Contains(MouseHoverInfo.HoveredPoint))
        {
            SplitLineSegment(MouseHoverInfo.HoveredPoint);
        }

        // Initiate point dragging
        else if (SelectedFeature != null && MouseHoverInfo.HoveredPoint != null && MouseHoverInfo.HoveredPoint.IsRegistered)
        {
            InitiatePointDrag(MouseHoverInfo.HoveredPoint);
        }

        // Select a feature
        else if (MouseHoverInfo.HoveredMapFeature != null)
        {
            AreaFeature areaFeatureToSelect = (AreaFeature)MouseHoverInfo.HoveredMapFeature;
            SelectFeature(areaFeatureToSelect);
        }
    }

    public override void HandleRightClick()
    {
        if (DraggedPoint != null) AbortDrag();
        else
        {
            // Rightclicking a line point removes it
            if (SelectedFeature != null && MouseHoverInfo.HoveredPoint != null && MouseHoverInfo.HoveredPoint.IsRegistered)
            {
                Map.RemoveAreaFeaturePoint(SelectedFeature, MouseHoverInfo.HoveredPoint);

                RegenerateSplitPoints();
                ResetHoverablePoints();

                // Unselect feature if it got destroyed by the merge
                if (SelectedFeature.IsDestroyed) SelectFeature(null);

                else
                {
                    // Refresh visible points
                    Map.Renderer2D.HideAllPoints();
                    SelectedFeature.ShowFeaturePoints();
                }
            }
        }
    }

    #endregion

    private void TypeDropdown_OnValueChanged(int value)
    {
        SelectedFeature.SetType(DefDatabase<AreaFeatureDef>.AllDefs[value]);
    }

    private void LayerDropdown_OnValueChanged(int value)
    {
        SelectedFeature.SetRenderLayer(value);
    }

    private void DeleteButton_OnClick()
    {
        Map.DeleteAreaFeature(SelectedFeature);
        Editor.SelectTool(EditorToolId.SelectFeatureTool);
    }

    public override void OnDeselect()
    {
        SelectFeature(null);

        // Reset hover
        MouseHoverInfo.ResetPointSelectionOptions();
        MouseHoverInfo.ResetFeatureSelectionOptions();
    }

    private void SetStandardInstructions()
    {
        Editor.SetInstructionsText("Click on a line feature to select it.\nUse the property window on the left to change the selected features attributes.");
    }

    #region Point Drag

    private void InitiatePointDrag(Point p)
    {
        DraggedPoint = p;
        PointDrag_InitialMouseWorldPosition = MouseHoverInfo.WorldPosition;
        PointDrag_InitialPointPosition = DraggedPoint.Position;
        DraggedPoint.SetDisplayColor(Color.green);
        ClearSplitPoints();

        // Hover settings
        List<Point> hoverablePoints = new List<Point>();
        hoverablePoints.AddRange(Map.Points.Values);
        hoverablePoints.Remove(p);
        MouseHoverInfo.SetPointSelectionOptions(hoverablePoints);
        MouseHoverInfo.SetCheckFeatureSelection(false);
    }

    public override void HandleLeftDrag()
    {
        if (DraggedPoint == null) return;

        if (MouseHoverInfo.HoveredPoint != null)
        {
            DraggedPoint.SetPosition(MouseHoverInfo.HoveredPoint.Position);
        }

        else
        {
            Vector2 offset = MouseHoverInfo.WorldPosition - PointDrag_InitialMouseWorldPosition;
            Vector2 currentPos = PointDrag_InitialPointPosition + offset;
            DraggedPoint.SetPosition(currentPos);
        }
    }

    public override void HandleStopLeftDrag()
    {
        if (DraggedPoint == null) return;

        // Remove snapped to point if it was adjacent to the drag point
        if (MouseHoverInfo.HoveredPoint != null)
        {
            Map.MergePointIntoPoint(DraggedPoint, MouseHoverInfo.HoveredPoint);
        }
        else
        {
            DraggedPoint.SetDisplayColor(Color.white);
        }

        RegenerateSplitPoints();
        ResetHoverablePoints();
        SelectedFeature.ShowFeaturePoints();
        MouseHoverInfo.SetCheckFeatureSelection(true);

        DraggedPoint = null;

        // Unselect feature if it got destroyed by the merge
        if (SelectedFeature.IsDestroyed) SelectFeature(null);
    }

    private void AbortDrag()
    {
        if (DraggedPoint == null) return;

        DraggedPoint.SetDisplayColor(Color.white);
        DraggedPoint.SetPosition(PointDrag_InitialPointPosition);
        DraggedPoint = null;
        MouseHoverInfo.SetCheckFeatureSelection(true);

        RegenerateSplitPoints();
        ResetHoverablePoints();
    }

    #endregion

    #region Split Points

    /// <summary>
    /// Between every two points in the line, this creates a temporary unregistered point, that if clicked, becomes permanent and splits a line.
    /// Likes this new points can be created on existing lines.
    /// </summary>
    private void GenerateSplitPoints()
    {
        for (int i = 0; i < SelectedFeature.Points.Count; i++)
        {
            Point segmentStart = SelectedFeature.Points[i];
            Point segmentEnd = i == SelectedFeature.Points.Count - 1 ? SelectedFeature.Points[0] : SelectedFeature.Points[i + 1];
            Vector2 splitPosition = new Vector2((segmentStart.Position.x + segmentEnd.Position.x) / 2f, (segmentStart.Position.y + segmentEnd.Position.y) / 2f);

            Point tempSplitPoint = new Point(Map, splitPosition, overrideSprite: ResourceManager.LoadSprite("Sprites/PointPlus"));
            tempSplitPoint.RenderedPoint.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

            TemporarySplitPoints.Add(tempSplitPoint);
        }
    }

    private void ClearSplitPoints()
    {
        foreach (Point p in TemporarySplitPoints.Where(x => !x.IsRegistered)) p.DestroyVisuals();
        if (MouseHoverInfo.HoveredPoint != null && TemporarySplitPoints.Contains(MouseHoverInfo.HoveredPoint)) MouseHoverInfo.ClearHoveredPoint();
        TemporarySplitPoints.Clear();
    }

    private void RegenerateSplitPoints()
    {
        ClearSplitPoints();
        GenerateSplitPoints();

        List<Point> hoverablePoints = new List<Point>();
        hoverablePoints.AddRange(SelectedFeature.Points);
        hoverablePoints.AddRange(TemporarySplitPoints);
        MouseHoverInfo.SetPointSelectionOptions(hoverablePoints);
    }

    public void SplitLineSegment(Point newPoint)
    {
        int index = TemporarySplitPoints.IndexOf(newPoint);
        Map.SplitAreaLineSegment(SelectedFeature, newPoint, index);

        // Re-select feature to update everything
        SelectFeature(SelectedFeature);

        // Instantly initiate a drag to quickly be able to move it
        InitiatePointDrag(newPoint);
    }

    #endregion
}
