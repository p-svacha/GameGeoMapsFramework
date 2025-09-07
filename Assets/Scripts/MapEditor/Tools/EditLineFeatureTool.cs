using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EditLineFeatureTool : EditorTool
{
    public override EditorToolId Id => EditorToolId.EditLineFeatureTool;
    public override string Name => "Edit Line Feature";

    private LineFeature SelectedFeature;

    private static Color MERGE_OPTION_COLOR = new Color(0f, 1f, 0f, 0.5f);
    private static Color MERGE_OPTION_HIGHLIGHTED_COLOR = new Color(0f, 1f, 0f, 0.9f);

    [Header("Elements")]
    public GameObject NoFeatureSelectedInfo;
    public GameObject FeaturePropertiesContainer;
    public TextMeshProUGUI IdText;
    public TMP_Dropdown TypeDropdown;
    public TMP_Dropdown LayerDropdown;

    public Button MergeButton;
    public Button SplitButton;
    public Button DeleteButton;

    private bool IsMerging;
    private bool IsSplitting;

    private Dictionary<LineFeature, Point> MergeOptions = new Dictionary<LineFeature, Point>(); // Key is the line that we can merge with, value is the merge point
    private List<Point> SplitOptions = new List<Point>();

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
        List<string> typeOptions = DefDatabase<LineFeatureDef>.AllDefs.Select(x => x.LabelCap).ToList();
        TypeDropdown.AddOptions(typeOptions);
        TypeDropdown.onValueChanged.AddListener(TypeDropdown_OnValueChanged);

        LayerDropdown.ClearOptions();
        List<string> layerOptions = new List<string>();
        for (int i = 0; i <= 10; i++) layerOptions.Add(i.ToString());
        LayerDropdown.AddOptions(layerOptions);
        LayerDropdown.onValueChanged.AddListener(LayerDropdown_OnValueChanged);

        MergeButton.onClick.AddListener(MergeButton_OnClick);
        SplitButton.onClick.AddListener(SplitButton_OnClick);
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

    public void SelectFeature(LineFeature feature)
    {
        // Abort current action
        if (IsMerging) AbortMerge();
        if (IsSplitting) AbortSplit();
        if (DraggedPoint != null) AbortDrag();

        // Unselect previous selected feature
        if (SelectedFeature != null)
        {
            SelectedFeature.HideSelectionIndicator(removeForced: true);

            // Remove split points
            ClearSplitPoints();
        }
        Map.Renderer.HideAllPoints();

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
            TypeDropdown.SetValueWithoutNotify(DefDatabase<LineFeatureDef>.AllDefs.IndexOf(SelectedFeature.Def));
            LayerDropdown.SetValueWithoutNotify(SelectedFeature.RenderLayer);

            // Actions
            RefreshMergeOptions();
            RefreshSplitOptions();

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
        MouseHoverInfo.SetCheckFeatureSelection(true);
        MouseHoverInfo.SetFeatureSelectionOptions(Map.LineFeatures.Values.Select(x => (MapFeature)x).ToList());
    }

    #region Input Handling

    public override void HandleLeftClick()
    {
        if (DraggedPoint != null) return;

        if (IsMerging)
        {
            if (MouseHoverInfo.HoveredMapFeature != null)
            {
                LineFeature toMerge = (LineFeature)MouseHoverInfo.HoveredMapFeature;
                ConfirmMerge(toMerge, MergeOptions[toMerge]);
            }
        }
        else if(IsSplitting)
        {
            if (MouseHoverInfo.HoveredPoint != null)
            {
                ConfirmSplit(MouseHoverInfo.HoveredPoint);
            }
        }
        else
        {
            // Create new split point
            if (SelectedFeature != null && MouseHoverInfo.HoveredPoint != null && TemporarySplitPoints.Contains(MouseHoverInfo.HoveredPoint))
            {
                SplitLineSegment(MouseHoverInfo.HoveredPoint);
            }

            // Initiate point dragging
            else if(SelectedFeature != null && MouseHoverInfo.HoveredPoint != null && MouseHoverInfo.HoveredPoint.IsRegistered)
            {
                InitiatePointDrag(MouseHoverInfo.HoveredPoint);
            }

            // Select a feature
            else if (MouseHoverInfo.HoveredMapFeature != null)
            {
                LineFeature lineFeatureToSelect = (LineFeature)MouseHoverInfo.HoveredMapFeature;
                SelectFeature(lineFeatureToSelect);
            }
        }
    }

    public override void HandleRightClick()
    {
        if (IsMerging) AbortMerge();
        else if (IsSplitting) AbortSplit();
        else if (DraggedPoint != null) AbortDrag();
        else
        {
            // Rightclicking a line point removes it
            if (SelectedFeature != null && MouseHoverInfo.HoveredPoint != null && MouseHoverInfo.HoveredPoint.IsRegistered)
            {
                Map.RemoveLineFeaturePoint(SelectedFeature, MouseHoverInfo.HoveredPoint);

                RegenerateSplitPoints();
                RefreshMergeOptions();
                RefreshSplitOptions();
                ResetHoverablePoints();

                // Unselect feature if it got destroyed by the merge
                if (SelectedFeature.IsDestroyed) SelectFeature(null);

                else
                {
                    // Refresh visible points
                    Map.Renderer.HideAllPoints();
                    SelectedFeature.ShowFeaturePoints();
                }
            }
        }
    }

    #endregion

    #region UI Listeners

    private void TypeDropdown_OnValueChanged(int value)
    {
        SelectedFeature.SetType(DefDatabase<LineFeatureDef>.AllDefs[value]);
    }

    private void LayerDropdown_OnValueChanged(int value)
    {
        SelectedFeature.SetRenderLayer(value);
    }
    
    private void DeleteButton_OnClick()
    {
        Map.DeleteLineFeature(SelectedFeature);
        Editor.SelectTool(EditorToolId.SelectFeatureTool);
    }

    #endregion

    #region Merge

    private void MergeButton_OnClick()
    {
        if (IsMerging)
        {
            AbortMerge();
            return;
        }

        if (MergeOptions.Count == 0) return;
        if (IsSplitting) return;

        StartMerge();
    }

    private void StartMerge()
    {
        IsMerging = true;
        MergeButton.GetComponent<Image>().color = Color.yellow;

        Editor.SetInstructionsText("Click on a highlighted line to merge this line with.\nRight click to abort.");
        foreach (LineFeature mergeOption in MergeOptions.Keys)
        {
            mergeOption.SetSelectionIndicatorColor(MERGE_OPTION_COLOR);
            mergeOption.ShowSelectionIndicator(forced: true);
        }
        MouseHoverInfo.SetFeatureSelectionOptions(MergeOptions.Keys.Select(x => (MapFeature)x).ToList());
        MouseHoverInfo.SetForcedHightlightColor(MERGE_OPTION_HIGHLIGHTED_COLOR);
    }
    private void ConfirmMerge(LineFeature otherLine, Point mergePoint)
    {
        if (!IsMerging) return;
        if (otherLine == null) return;

        MouseHoverInfo.ClearHoveredMapFeature();
        Map.MergeLines(SelectedFeature, otherLine, mergePoint);

        // Re-select feature to update everything
        SelectFeature(SelectedFeature);
    }
    private void AbortMerge()
    {
        if (!IsMerging) return;

        IsMerging = false;
        MergeButton.GetComponent<Image>().color = Color.white;
        SetStandardInstructions();

        foreach (LineFeature mergeOption in MergeOptions.Keys)
        {
            mergeOption.ResetSelectionIndicatorColor();
            mergeOption.HideSelectionIndicator(removeForced: true);
        }
        ResetHoverableFeatures();
        MouseHoverInfo.ResetForcedHightlightColor();

        // Re-select feature to update everything
        SelectFeature(SelectedFeature);

    }

    private void RefreshMergeOptions()
    {
        MergeOptions.Clear();

        foreach (LineFeature otherLine in Map.LineFeatures.Values)
        {
            if (otherLine == SelectedFeature) continue;
            if (MergeOptions.ContainsKey(otherLine)) continue;

            if (otherLine.StartPoint == SelectedFeature.StartPoint)
            {
                MergeOptions.Add(otherLine, otherLine.StartPoint);
                continue;
            }
            if (otherLine.EndPoint == SelectedFeature.StartPoint)
            {
                MergeOptions.Add(otherLine, otherLine.EndPoint);
                continue;
            }
            if (otherLine.StartPoint == SelectedFeature.EndPoint)
            {
                MergeOptions.Add(otherLine, otherLine.StartPoint);
                continue;
            }
            if (otherLine.EndPoint == SelectedFeature.EndPoint)
            {
                MergeOptions.Add(otherLine, otherLine.EndPoint);
                continue;
            }
        }

        if (MergeOptions.Count == 0) MergeButton.interactable = false;
        else MergeButton.interactable = true;
    }

    #endregion

    #region Split Feature

    private void SplitButton_OnClick()
    {
        if (IsSplitting)
        {
            AbortSplit();
            return;
        }

        if (SplitOptions.Count == 0) return;
        if (IsMerging) return;

        StartSplit();
    }

    private void StartSplit()
    {
        IsSplitting = true;
        SplitButton.GetComponent<Image>().color = Color.yellow;
        Editor.SetInstructionsText("Click on a highlighted point to split the line at that point.\nRight click to abort.");

        // Highlight options
        foreach (Point p in SplitOptions) p.SetDisplayColor(Color.green);

        // Hover settings
        MouseHoverInfo.SetPointSelectionOptions(SplitOptions);
        MouseHoverInfo.SetShowPointSnapIndicator(true);
        MouseHoverInfo.SetCheckFeatureSelection(false);
    }
    private void ConfirmSplit(Point splitPoint)
    {
        if (!IsSplitting) return;
        if (splitPoint == null) return;

        Map.SplitLine(SelectedFeature, splitPoint);

        // Re-select feature to update everything
        SelectFeature(SelectedFeature);

        // Update selectable features because there is a new one now
        ResetHoverableFeatures();
    }
    private void AbortSplit()
    {
        if (!IsSplitting) return;

        IsSplitting = false;
        SplitButton.GetComponent<Image>().color = Color.white;
        SetStandardInstructions();

        // Unhighlight options
        foreach (Point p in SplitOptions) p.SetDisplayColor(Color.white);

        // Hover settings
        MouseHoverInfo.ResetPointSelectionOptions();
        MouseHoverInfo.SetShowPointSnapIndicator(false);
        MouseHoverInfo.SetCheckFeatureSelection(true);

        // Re-select feature to update everything
        SelectFeature(SelectedFeature);
    }

    private void RefreshSplitOptions()
    {
        SplitOptions.Clear();

        SplitOptions = new List<Point>(SelectedFeature.Points);
        SplitOptions.RemoveAt(0);
        SplitOptions.RemoveAt(SplitOptions.Count - 1);

        if (SplitOptions.Count == 0) SplitButton.interactable = false;
        else SplitButton.interactable = true;
    }

    #endregion

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
        RefreshMergeOptions();
        RefreshSplitOptions();
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
        for(int i = 0; i < SelectedFeature.Points.Count - 1; i++)
        {
            Point segmentStart = SelectedFeature.Points[i];
            Point segmentEnd = SelectedFeature.Points[i + 1];
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
    }

    public void SplitLineSegment(Point newPoint)
    {
        int index = TemporarySplitPoints.IndexOf(newPoint);
        Map.SplitLineSegment(SelectedFeature, newPoint, index);

        // Re-select feature to update everything
        SelectFeature(SelectedFeature);

        // Instantly initiate a drag to quickly be able to move it
        InitiatePointDrag(newPoint);
    }

    #endregion

    public override void OnDeselect()
    {
        if (IsMerging) AbortMerge();
        if (IsSplitting) AbortSplit();
        if (DraggedPoint != null) AbortDrag();

        SelectFeature(null);

        // Reset hover
        MouseHoverInfo.ResetPointSelectionOptions();
        MouseHoverInfo.ResetFeatureSelectionOptions();
    }

    private void SetStandardInstructions()
    {
        Editor.SetInstructionsText("Click on a line feature to select it.\nUse the property window on the left to change the selected features attributes.");
    }
}
