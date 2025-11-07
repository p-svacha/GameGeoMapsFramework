using UnityEngine;

/// <summary>
/// A single segment between 2 adjacent points within a Line Feature used for pathfinding.
/// </summary>
public class Transition
{
    public Point From;
    public Point To;
    public LineFeature LineFeature;
    private float Length;

    public Transition(Point from, Point to, LineFeature lineFeature)
    {
        From = from;
        To = to;
        LineFeature = lineFeature;
        Length = Vector2.Distance(from.Position, to.Position);
    }

    public float GetCost(Entity e)
    {
        // Base cost
        float cost = Length;

        // Surface modifier
        cost /= e.GetSurfaceSpeed(LineFeature.Def.Surface);

        return cost;
    }

    public bool CanPass(Entity e)
    {
        return true;
    }
}
