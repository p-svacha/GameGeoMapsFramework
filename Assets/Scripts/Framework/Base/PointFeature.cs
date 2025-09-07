using UnityEngine;

public class PointFeature : MapFeature
{
    public PointFeature(Map map, int id) : base(map, id) { }

    public override void ResetSelectionIndicatorColor()
    {
        throw new System.NotImplementedException();
    }

    public override void SetSelectionIndicatorColor(Color color, bool temporary = false)
    {
        throw new System.NotImplementedException();
    }
}
