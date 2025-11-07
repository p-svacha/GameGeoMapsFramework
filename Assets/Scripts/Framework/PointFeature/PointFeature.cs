using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PointFeature : MapFeature
{
    public PointFeatureDef Def { get; private set; }
    public Point Point { get; private set; }
    public string Label { get; private set; }

    // Rendered objects (all screen space / UI elements)
    public GameObject VisualRoot;
    public Image VisualIcon;
    public TextMeshProUGUI VisualLabel;

    public PointFeature(Map map, int id, Point point, PointFeatureDef def, string label) : base(map, id)
    {
        Def = def;
        Point = point;
        Label = label;

        Init();
    }

    /// <summary>
    /// Called once either after creating or loading the feature.
    /// </summary>
    private void Init()
    {
        Map.Renderer2D.CreatePointFeatureVisuals(this);
    }

    public void SetPoint(Point point)
    {
        Point = point;
    }

    public void SetType(PointFeatureDef def)
    {
        Def = def;
        Map.Renderer2D.RedrawPointFeature(this);
    }

    public void SetLabel(string label)
    {
        Label = label;
        Map.Renderer2D.RedrawPointFeature(this);
    }

    public override void SetSelectionIndicatorColor(Color color, bool temporary = false) { }
    public override void ResetSelectionIndicatorColor() { }

    #region Save / Load

    public PointFeature(Map map, PointFeatureData data) : base(map, data.Id)
    {
        if (DefDatabase<PointFeatureDef>.ContainsDef(data.DefName)) Def = DefDatabase<PointFeatureDef>.GetNamed(data.DefName);
        else Def = DefDatabase<PointFeatureDef>.AllDefs.First();
        Point = map.Points[data.PointId];
        Label = data.Label;
        Init();
    }

    public PointFeatureData ToData()
    {
        PointFeatureData data = new PointFeatureData();
        data.Id = Id;
        data.DefName = Def.DefName;
        data.PointId = Point.Id;
        data.Label = Label;
        return data;
    }

    #endregion
}
