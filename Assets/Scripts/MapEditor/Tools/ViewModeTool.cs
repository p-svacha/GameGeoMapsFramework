using UnityEngine;

public class ViewModeTool : EditorTool
{
    public override EditorToolId Id => EditorToolId.ViewModeTool;
    public override string Name => "View Mode";

    public override void OnSelect()
    {
        // Display options
        Map.Renderer.HideAllPoints();
        MouseHoverInfo.SetShowPointSnapIndicator(false);
        MouseHoverInfo.SetCheckFeatureSelection(false);
    }
}
