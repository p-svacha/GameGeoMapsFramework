using UnityEngine;

public abstract class MapFeature
{
    /// <summary>
    /// Unique identifier within all features of this type (point / line / area).
    /// </summary>
    public int Id { get; protected set; }

    /// <summary>
    /// The map this feature belongs to.
    /// </summary>
    public Map Map { get; protected set; }

    /// <summary>
    /// The GameObject that, if active, shows that this map feature is selected / hovered.
    /// </summary>
    public GameObject SelectionIndicator;

    /// <summary>
    /// If true, the selection indicator is always displayed, even after calling a Hide().
    /// Remove it with Hide(forced: true).
    /// </summary>
    public bool ForcedSelectionIndicator { get; private set; }

    /// <summary>
    /// The current color the selection indicator of this feature has.
    /// </summary>
    public Color CurrentSelectionIndicatorColor { get; protected set; }

    /// <summary>
    /// Flag that this feature's visuals have already been destroyed, useful in remaining references.
    /// </summary>
    public bool IsDestroyed;

    public MapFeature(Map map, int id)
    {
        Map = map;
        Id = id;
    }

    public void ShowSelectionIndicator(bool forced = false)
    {
        if (forced) ForcedSelectionIndicator = true;
        SelectionIndicator.gameObject.SetActive(true);
    }
    public void HideSelectionIndicator(bool removeForced = false)
    {
        if (ForcedSelectionIndicator && !removeForced) return;

        if (removeForced) ForcedSelectionIndicator = false;
        SelectionIndicator.gameObject.SetActive(false);
    }

    public abstract void SetSelectionIndicatorColor(Color color, bool temporary = false);
    public abstract void ResetSelectionIndicatorColor();
}
