using UnityEngine;

public class Racer : Entity
{
    public bool IsFinished;

    public Racer(Map map, string name, Color color, Point p) : base(map, name, color, p)
    {
    }
}
