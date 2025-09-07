using System.Collections.Generic;
using UnityEngine;

public abstract class EditorTool : MonoBehaviour
{
    protected MapEditor Editor;
    protected Map Map => Editor.Map;
    public abstract EditorToolId Id { get; }
    public abstract string Name { get; }
    public Sprite Icon => ResourceManager.LoadSprite("Sprites/EditorToolIcons/" + Id.ToString());

    /// <summary>
    /// Gets called once when the editor is stared up
    /// </summary>
    public virtual void Init(MapEditor editor)
    {
        Editor = editor;
    }

    /// <summary>
    /// Gets executed when a new world has been set in the editor.
    /// </summary>
    public virtual void OnNewWorld() { }

    /// <summary>
    /// Gets executed every frame before UpdateTool().
    /// </summary>
    public virtual void HandleKeyboardInputs() { }

    /// <summary>
    /// Gets executed every frame after handling inputs.
    /// </summary>
    public virtual void UpdateTool() { }

    public virtual void HandleLeftClick() { }
    public virtual void HandleLeftDrag() { }
    public virtual void HandleStopLeftDrag() { }
    public virtual void HandleRightClick() { }
    public virtual void HandleRightDrag() { }
    public virtual void HandleStopRightDrag() { }
    public virtual void HandleMiddleClick() { }

    /// <summary>
    /// Gets executed when the tool is selected.
    /// </summary>
    public virtual void OnSelect() { }

    /// <summary>
    /// Gets executed when the tool is deselected.
    /// </summary>
    public virtual void OnDeselect() { }
    

    public virtual void OnHoveredPointChanged(Point oldPoint, Point newPoint) { }
    public virtual void OnHoveredPointFeatureChanged(PointFeature oldFeature, PointFeature newFeature) { }
    public virtual void OnHoveredLineFeatureChanged(LineFeature oldFeature, LineFeature newFeature) { }
    public virtual void OnHoveredAreaFeatureChanged(AreaFeature oldFeature, AreaFeature newFeature) { }
}
