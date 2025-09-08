using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A point is the most fundamental point of map. It marks a specific position on a map. Every feature on the map is made out of one or multiple points.
/// </summary>
public class Point
{
    public int Id { get; private set; }

    /// <summary>
    /// The map this point belongs to.
    /// </summary>
    public Map Map { get; private set; }

    /// <summary>
    /// Flag if the point is registered as part of the map.
    /// </summary>
    public bool IsRegistered { get; private set; }

    /// <summary>
    /// The exact position of the point.
    /// </summary>
    public Vector2 Position { get; private set; }

    /// <summary>
    /// Reference to the GameObject representing this point visually.
    /// </summary>
    public GameObject RenderedPoint { get; private set; }

    /// <summary>
    /// Reference to the GameObject representing the snap indicator of the point.
    /// </summary>
    public GameObject SnapIndicator { get; private set; }

    /// <summary>
    /// List containing all features that include this point.
    /// </summary>
    public List<MapFeature> ConnectedFeatures { get; private set; }

    /// <summary>
    /// Flag that this point has been destroyed. Used for ghost references.
    /// </summary>
    public bool IsDestroyed { get; private set; }


    /// <summary>
    /// Create a temporary unregistered point.
    /// </summary>
    public Point(Map map, Vector2 position, Sprite overrideSprite = null)
    {
        Map = map;
        IsRegistered = false;
        Position = position;
        Init(overrideSprite);
        Show();
    }

    public void OnRegistered(int id)
    {
        Id = id;
        IsRegistered = true;
        RenderedPoint.GetComponent<SpriteRenderer>().sprite = MapRenderer.DEFAULT_POINT_SPRITE;
    }

    /// <summary>
    /// Called once either after creating or loading the feature.
    /// </summary>
    private void Init(Sprite overrideSprite = null)
    {
        ConnectedFeatures = new List<MapFeature>();
        InitVisuals(overrideSprite);
    }
    private void InitVisuals(Sprite overrideSprite = null)
    {
        RenderedPoint = Map.Renderer.DrawPoint(this, overrideSprite);
        SnapIndicator = Map.Renderer.DrawPointSnapIndicator(this);
    }

    public void SetPosition(Vector2 position)
    {
        Position = position;
        Map.Renderer.RedrawPointAndAllConnectedFeatures(this);
    }

    public void SetDisplayColor(Color c)
    {
        RenderedPoint.GetComponent<SpriteRenderer>().color = c;
    }

    public void AddConnectedFeature(MapFeature feature)
    {
        if (ConnectedFeatures.Contains(feature)) return;
        ConnectedFeatures.Add(feature);
    }
    public void RemoveConnectedFeature(MapFeature feature) => ConnectedFeatures.Remove(feature);

    public void Show() => RenderedPoint.SetActive(true);
    public void Hide()=> RenderedPoint.SetActive(false);
    public void ShowSnapIndicator() => SnapIndicator.gameObject.SetActive(true);
    public void HideSnapIndicator()
    {
        if(IsDestroyed) return;
        SnapIndicator.gameObject.SetActive(false);
    }
    public void DestroyVisuals()
    {
        GameObject.Destroy(RenderedPoint);
        IsDestroyed = true;
    }

    public bool IsConnectedToAnyFeature => ConnectedFeatures.Count > 0;

    #region Save / Load

    public Point(Map map, PointData data)
    {
        Map = map;
        Id = data.Id;
        Position = new Vector2(data.X, data.Y);
        IsRegistered = true;
        Init();
    }

    public PointData ToData()
    {
        PointData data = new PointData();
        data.Id = Id;
        data.X = Position.x;
        data.Y = Position.y;
        return data;
    }

    #endregion

    public override string ToString()
    {
        return Id.ToString();
    }
}
