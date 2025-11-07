using UnityEngine;

public class SelectFeatureTool : EditorTool
{
    public override EditorToolId Id => EditorToolId.SelectFeatureTool;
    public override string Name => "Select Feature";

    public override void OnSelect()
    {
        Map.Renderer2D.HideAllPoints();

        MouseHoverInfo.SetShowPointSnapIndicator(false);
        MouseHoverInfo.SetCheckFeatureSelection(true);
        MouseHoverInfo.ResetFeatureSelectionOptions();
    }

    public override void HandleLeftClick()
    {
        if (MouseHoverInfo.HoveredMapFeature != null)
        {
            if (MouseHoverInfo.HoveredMapFeature is PointFeature pointFeature)
            {
                Editor.SelectTool(EditorToolId.EditPointFeatureTool);
                Editor.EditPointFeatureTool.SelectFeature(pointFeature);
            }
            if (MouseHoverInfo.HoveredMapFeature is AreaFeature area)
            {
                Editor.SelectTool(EditorToolId.EditAreaFeatureTool);
                Editor.EditAreaFeatureTool.SelectFeature(area);
            }
            if (MouseHoverInfo.HoveredMapFeature is LineFeature line)
            {
                Editor.SelectTool(EditorToolId.EditLineFeatureTool);
                Editor.EditLineFeatureTool.SelectFeature(line);
            }
        }
    }
}
