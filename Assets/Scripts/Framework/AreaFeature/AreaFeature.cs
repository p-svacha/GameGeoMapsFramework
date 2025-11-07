using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AreaFeature : MapFeature
{
    public AreaFeatureDef Def { get; private set; }
    public List<Point> Points { get; private set; }
    public bool IsClockwise { get; private set; }
    public int RenderLayer { get; private set; }

    // Rendered objects
    public GameObject VisualRoot;
    public GameObject VisualPolygon;
    public GameObject VisualOutline; // optional

    public List<Vector2> PointPositions => Points.Select(p => p.Position).ToList();

    public AreaFeature(Map map, int id, List<Point> points, AreaFeatureDef def, int renderLayer) : base(map, id)
    {
        Points = new List<Point>(points);

        Def = def;
        RenderLayer = renderLayer;

        Init();
    }

    public void SetType(AreaFeatureDef def)
    {
        Def = def;
        Map.Renderer2D.RedrawFeature(this);
    }

    public void SetRenderLayer(int layer)
    {
        RenderLayer = layer;
        Map.Renderer2D.RedrawFeature(this);
    }

    /// <summary>
    /// Called once either after creating or loading the feature.
    /// </summary>
    private void Init()
    {
        RecalculateClockwise();

        Map.Renderer2D.CreateAreaFeatureVisuals(this);
    }

    public void RecalculateClockwise()
    {
        IsClockwise = GeometryFunctions.IsClockwise(Points.Select(p => p.Position).ToList());
    }

    public void ShowFeaturePoints()
    {
        foreach (Point p in Points) p.Show();
    }

    public override void SetSelectionIndicatorColor(Color color, bool temporary = false)
    {
        if (!temporary) CurrentSelectionIndicatorColor = color;
        SelectionIndicator.GetComponent<MeshRenderer>().material.color = color;
    }

    public override void ResetSelectionIndicatorColor()
    {
        SetSelectionIndicatorColor(MapRenderer2D.AREA_SELECTION_INDICATOR_COLOR);
    }

    #region Save / Load

    public AreaFeature(Map map, AreaFeatureData data) : base(map, data.Id)
    {
        if (DefDatabase<AreaFeatureDef>.ContainsDef(data.DefName)) Def = DefDatabase<AreaFeatureDef>.GetNamed(data.DefName);
        else Def = DefDatabase<AreaFeatureDef>.AllDefs.First();
        RenderLayer = data.RenderLayer;
        Points = data.PointIds.Select(x => map.Points[x]).ToList();
        Init();
    }

    public AreaFeatureData ToData()
    {
        AreaFeatureData data = new AreaFeatureData();
        data.Id = Id;
        data.DefName = Def.DefName;
        data.PointIds = Points.Select(x => x.Id).ToList();
        data.RenderLayer = RenderLayer;
        return data;
    }

    #endregion
}
