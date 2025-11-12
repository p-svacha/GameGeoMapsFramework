using UnityEngine;

/// <summary>
/// A single unidirectional segment between 2 adjacent points within a Line Feature used for pathfinding.
/// </summary>
public class Transition
{
    public Point From { get; private set; }
    public Point To { get; private set; }
    public LineFeature LineFeature { get; private set; }
    public float Length { get; private set; }

    public Transition(Point from, Point to, LineFeature lineFeature)
    {
        From = from;
        To = to;
        LineFeature = lineFeature;
        Length = Vector2.Distance(from.Position, to.Position);
    }

    /// <summary>
    /// Returns the cost of this transition for an entity.
    /// Entity may be null to get the general cost.
    /// </summary>
    public float GetCost(Entity e)
    {
        // Base cost
        float cost = Length;

        // Surface modifier
        float surfaceSpeed;
        if (e != null) surfaceSpeed = e.GetSurfaceSpeed(LineFeature.Def.Surface);
        else surfaceSpeed = LineFeature.Def.Surface.DefaultSpeed;

        cost /= surfaceSpeed;

        return cost;
    }

    public bool CanPass(Entity e)
    {
        return true;
    }
}
