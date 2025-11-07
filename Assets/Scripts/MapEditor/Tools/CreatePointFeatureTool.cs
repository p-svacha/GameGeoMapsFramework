using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class CreatePointFeatureTool : EditorTool
{
    public override EditorToolId Id => EditorToolId.CreatePointFeatureTool;
    public override string Name => "Create Point Feature";

    [Header("Elements")]
    public TMP_Dropdown TypeDropdown;
    public TMP_InputField LabelInput;

    public override void Init(MapEditor editor)
    {
        base.Init(editor);

        // UI
        TypeDropdown.ClearOptions();
        List<string> typeOptions = DefDatabase<PointFeatureDef>.AllDefs.Select(x => x.LabelCap).ToList();
        TypeDropdown.AddOptions(typeOptions);

        LabelInput.text = "";

        Editor.SetInstructionsText("Click on an existing point (without a Point Feature) or anywhere on the map to create a new Point Feature with the selected attributes.");
    }

    public override void OnSelect()
    {
        // Diplay options
        Map.Renderer2D.ShowAllPoints();
        MouseHoverInfo.SetShowPointSnapIndicator(true);
        MouseHoverInfo.SetCheckFeatureSelection(false);
    }

    public override void HandleLeftClick()
    {
        if (MouseHoverInfo.HoveredPoint != null && MouseHoverInfo.HoveredPoint.HasPointFeature) return; // Each point can only have one point feature

        // Take hovered point as base
        Point newFeaturePoint = MouseHoverInfo.HoveredPoint;

        // Create new point if no existing point was clicked
        if (newFeaturePoint == null) newFeaturePoint = new Point(Map, MouseHoverInfo.WorldPosition);

        // Create feature
        Map.AddPointFeature(newFeaturePoint, DefDatabase<PointFeatureDef>.AllDefs[TypeDropdown.value], LabelInput.text);
    }
}
