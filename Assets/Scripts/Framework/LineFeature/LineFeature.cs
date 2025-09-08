using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LineFeature : MapFeature
{
    public LineFeatureDef Def { get; private set; }
    public List<Point> Points { get; private set; }
    public int RenderLayer { get; private set; }

    public GameObject VisualRoot;
    public GameObject VisualLine;

    public List<Vector2> PointPositions => Points.Select(p => p.Position).ToList();
    public Point StartPoint => Points.First();
    public Point EndPoint => Points.Last();

    public LineFeature(Map map, int id, List<Point> points, LineFeatureDef def, int renderLayer) : base(map, id)
    {
        Points = new List<Point>(points);

        Def = def;
        RenderLayer = renderLayer;

        Init();
    }

    /// <summary>
    /// Called once either after creating or loading the feature.
    /// </summary>
    private void Init()
    {
        Map.Renderer.CreateLineFeatureVisuals(this);
    }

    public void SetType(LineFeatureDef def)
    {
        Def = def;
        Map.Renderer.RedrawFeature(this);
    }

    public void SetRenderLayer(int layer)
    {
        RenderLayer = layer;
        Map.Renderer.RedrawFeature(this);
    }

    public void ShowFeaturePoints()
    {
        foreach (Point p in Points) p.Show();
    }

    public override void SetSelectionIndicatorColor(Color color, bool temporary = false)
    {
        if (!temporary) CurrentSelectionIndicatorColor = color;
        LineRenderer lr = SelectionIndicator.GetComponent<LineRenderer>();
        lr.startColor = color;
        lr.endColor = color;
    }

    public override void ResetSelectionIndicatorColor()
    {
        SetSelectionIndicatorColor(new Color(Def.Color.r, Def.Color.g, Def.Color.b, MapRenderer.LINE_SELECTION_INDICATOR_ALPHA));
    }

    #region Save / Load

    public LineFeature(Map map, LineFeatureData data) : base(map, data.Id)
    {
        if (DefDatabase<LineFeatureDef>.ContainsDef(data.DefName)) Def = DefDatabase<LineFeatureDef>.GetNamed(data.DefName);
        else Def = DefDatabase<LineFeatureDef>.AllDefs.First();

        RenderLayer = data.RenderLayer;
        Points = data.PointIds.Select(x => map.Points[x]).ToList();
        Init();
    }

    public LineFeatureData ToData()
    {
        LineFeatureData data = new LineFeatureData();
        data.Id = Id;
        data.DefName = Def.DefName;
        data.PointIds = Points.Select(x => x.Id).ToList();
        data.RenderLayer = RenderLayer;
        return data;
    }

    #endregion
}
