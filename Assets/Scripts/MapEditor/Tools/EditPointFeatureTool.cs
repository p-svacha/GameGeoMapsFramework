using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EditPointFeatureTool : EditorTool
{
    public override EditorToolId Id => EditorToolId.EditPointFeatureTool;
    public override string Name => "Edit Point Feature";

    private PointFeature SelectedFeature;

    [Header("Elements")]
    public GameObject NoFeatureSelectedInfo;
    public GameObject FeaturePropertiesContainer;
    public TextMeshProUGUI IdText;
    public TMP_Dropdown TypeDropdown;
    public TMP_InputField LabelInput;
    public Button DeleteButton;

    // Moving point
    private Point DraggedPoint;
    private Vector2 PointDrag_InitialMouseWorldPosition;
    private Vector2 PointDrag_InitialPointPosition;

    public override void Init(MapEditor editor)
    {
        base.Init(editor);

        // UI
        TypeDropdown.ClearOptions();
        List<string> typeOptions = DefDatabase<PointFeatureDef>.AllDefs.Select(x => x.LabelCap).ToList();
        TypeDropdown.AddOptions(typeOptions);
        TypeDropdown.onValueChanged.AddListener(TypeDropdown_OnValueChanged);

        LabelInput.onValueChanged.AddListener(LabelInput_OnValueChanged);

        DeleteButton.onClick.AddListener(DeleteButton_OnClick);
    }

    private void TypeDropdown_OnValueChanged(int value)
    {
        SelectedFeature.SetType(DefDatabase<PointFeatureDef>.AllDefs[value]);
    }

    private void LabelInput_OnValueChanged(string value)
    {
        SelectedFeature.SetLabel(value);
    }

    private void DeleteButton_OnClick()
    {
        Map.DeletePointFeature(SelectedFeature);
        Editor.SelectTool(EditorToolId.SelectFeatureTool);
    }

    public override void OnSelect()
    {
        // Reset selected feature
        SelectFeature(null);

        // Display and hover options
        MouseHoverInfo.SetShowPointSnapIndicator(false);
        ResetHoverableFeatures();

        SetStandardInstructions();
    }

    public override void OnDeselect()
    {
        SelectFeature(null);

        // Reset hover
        MouseHoverInfo.ResetPointSelectionOptions();
        MouseHoverInfo.ResetFeatureSelectionOptions();
    }

    /// <summary>
    /// Sets hoverable features to all point features on the map.
    /// </summary>
    private void ResetHoverableFeatures()
    {
        if (Map.PointFeatures.Count == 0)
        {
            MouseHoverInfo.SetCheckFeatureSelection(false);
        }
        else
        {
            MouseHoverInfo.SetCheckFeatureSelection(true);
            MouseHoverInfo.SetFeatureSelectionOptions(Map.PointFeatures.Values.Select(x => (MapFeature)x).ToList());
        }
    }

    public void SelectFeature(PointFeature feature)
    {
        // Abort current action
        if (DraggedPoint != null) AbortDrag();

        // Unselect previous selected feature
        if (SelectedFeature != null) SelectedFeature.HideSelectionIndicator(removeForced: true);

        // Select new feature
        SelectedFeature = feature;

        if (SelectedFeature != null)
        {
            // Show correct content in tool window
            NoFeatureSelectedInfo.SetActive(false);
            FeaturePropertiesContainer.SetActive(true);

            // Highlight selected features
            SelectedFeature.ShowSelectionIndicator(forced: true);

            // Values
            IdText.text = feature.Id.ToString();
            TypeDropdown.SetValueWithoutNotify(DefDatabase<PointFeatureDef>.AllDefs.IndexOf(SelectedFeature.Def));
            LabelInput.SetTextWithoutNotify(SelectedFeature.Label);
        }
        else // No selected feature
        {
            NoFeatureSelectedInfo.SetActive(true);
            FeaturePropertiesContainer.SetActive(false);
        }
    }

    private void SetStandardInstructions()
    {
        Editor.SetInstructionsText("Click on a point feature to select it.\nUse the property window on the left to change the selected features attributes.");
    }

    #region Input Handling

    public override void HandleLeftClick()
    {
        if (DraggedPoint != null) return;

        // Initiate point dragging
        else if (SelectedFeature != null && MouseHoverInfo.HoveredPoint == SelectedFeature.Point)
        {
            InitiatePointDrag(MouseHoverInfo.HoveredPoint);
        }

        // Select a feature
        else if (MouseHoverInfo.HoveredMapFeature != null)
        {
            PointFeature pointFeatureToSelect = (PointFeature)MouseHoverInfo.HoveredMapFeature;
            SelectFeature(pointFeatureToSelect);
        }
    }

    public override void HandleRightClick()
    {
        if (DraggedPoint != null) AbortDrag();
    }

    #endregion

    #region Point Drag

    private void InitiatePointDrag(Point p)
    {
        DraggedPoint = p;
        PointDrag_InitialMouseWorldPosition = MouseHoverInfo.WorldPosition;
        PointDrag_InitialPointPosition = DraggedPoint.Position;
        DraggedPoint.SetDisplayColor(Color.green);

        // Hover settings
        List<Point> hoverablePoints = new List<Point>();
        hoverablePoints.AddRange(Map.Points.Values);
        hoverablePoints.Remove(p);
        MouseHoverInfo.SetPointSelectionOptions(hoverablePoints);
        MouseHoverInfo.SetShowPointSnapIndicator(true);
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

        DraggedPoint = null;

        MouseHoverInfo.ResetPointSelectionOptions();
        MouseHoverInfo.SetShowPointSnapIndicator(false);
        MouseHoverInfo.SetCheckFeatureSelection(true);

        // Unselect feature if it got destroyed by the merge
        if (SelectedFeature.IsDestroyed) SelectFeature(null);
    }

    private void AbortDrag()
    {
        if (DraggedPoint == null) return;

        DraggedPoint.SetDisplayColor(Color.white);
        DraggedPoint.SetPosition(PointDrag_InitialPointPosition);
        DraggedPoint = null;

        MouseHoverInfo.ResetPointSelectionOptions();
        MouseHoverInfo.SetShowPointSnapIndicator(false);
        MouseHoverInfo.SetCheckFeatureSelection(true);
    }

    #endregion
}
