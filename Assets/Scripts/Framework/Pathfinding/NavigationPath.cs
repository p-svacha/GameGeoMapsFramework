using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


/// <summary>
/// An object representing a specific path from one Point to another.
/// <br/>Is not static and be changed.
/// </summary>
public class NavigationPath
{
    /// <summary>
    /// The map this path is for.
    /// </summary>
    public Map Map { get; private set; }

    /// <summary>
    /// All nodes that are visited along the path, including the source and target node.
    /// </summary>
    public List<Point> Points { get; private set; }

    /// <summary>
    /// All transitions that are traversed along the path.
    /// </summary>
    public List<Transition> Transitions { get; private set; }

    /// <summary>
    /// The final destination node of this path.
    /// </summary>
    public Point Target => Points.Last();

    /// <summary>
    /// The GameObject holding all visual elements of this paths preview.
    /// </summary>
    private GameObject PathPreviewObject;

    /// <summary>
    /// Full length of this path in units / meters.
    /// </summary>
    public float Length { get; private set; }

    /// <summary>
    /// Creates a path with the given source node as the starting point and no transitions or target yet.
    /// </summary>
    public NavigationPath(Map map, Point source)
    {
        Map = map;
        Points = new List<Point>() { source };
        Transitions = new List<Transition>();
    }

    /// <summary>
    /// Creates a path that represents a single transition, going from its source to its target.
    /// </summary>
    public NavigationPath(Map map, Transition transition)
    {
        Map = map;
        Points = new List<Point>() { transition.From, transition.To };
        Transitions = new List<Transition>() { transition };

        RecalculateLength();
    }

    /// <summary>
    /// Creates a copy of an existing NavigationPath.
    /// </summary>
    public NavigationPath(Map map,NavigationPath source)
    {
        Map = map;
        Points = new List<Point>();
        Points.AddRange(source.Points);

        Transitions = new List<Transition>();
        Transitions.AddRange(source.Transitions);

        RecalculateLength();
    }

    /// <summary>
    /// Creates a complete path with all the given nodes and transitions.
    /// </summary>
    public NavigationPath(Map map, List<Point> nodes, List<Transition> transitions)
    {
        Map = map;
        Points = nodes;
        Transitions = transitions;

        RecalculateLength();
    }

    #region Change Path

    /// <summary>
    /// Adds the given transition and transition target to this path.
    /// </summary>
    public void AddTransition(Transition t)
    {
        if (t.From != Target) throw new System.Exception("The start point of the given transition doesn't fit the current target of this path.");

        Transitions.Add(t);
        Points.Add(t.To);

        Length += t.Length;
    }

    /// <summary>
    /// Removes the first node, representing that the starting point of the path is now the first element of the transitions list.
    /// </summary>
    public void RemoveFirstNode()
    {
        if (Points.Count != Transitions.Count + 1) throw new System.Exception("Can't remove first node if the current starting point of this path is already a transition.");

        Points.RemoveAt(0);
    }

    /// <summary>
    /// Remove the first transition, representing that the starting point of the path is now the first element of the nodes list.
    /// </summary>
    public void RemoveFirstTransition()
    {
        if (Transitions.Count != Points.Count) throw new System.Exception("Can't remove first transition if the current starting point of this path is already a node.");

        Length -= Transitions[0].Length;

        Transitions.RemoveAt(0);
    }

    /// <summary>
    /// Changes the path so that the new starting point is the given node.
    /// <br/>Only works if the node is part of the path.
    /// </summary>
    public void CutEverythingBefore(Point node)
    {
        if (!Points.Contains(node)) throw new System.Exception($"Can't cut path because {node} is not part of it. Path has {Points.Count} nodes and {Transitions.Count} transitions.");
        while(Points[0] != node)
        {
            RemoveFirstNode();
            RemoveFirstTransition();
        }
    }

    private void RecalculateLength()
    {
        Length = Transitions.Sum(t => t.Length);
    }

    #endregion


    #region Getters

    /// <summary>
    /// Returns the cost for a specified entity to complete this path.
    /// <br/>This function assumes that the entity is allowed and capable of taking this path, it won't check that.
    /// </summary>
    public float GetCost(Entity entity)
    {
        return Transitions.Sum(t => t.GetCost(entity));
    }

    public string GetCostAsTimeString(Entity entity)
    {
        float seconds = GetCost(entity);
        return HelperFunctions.GetDurationString(seconds);
    }

    /// <summary>
    /// Returns if this is a path from one node to another one in a single transition.
    /// </summary>
    public bool IsSingleTransitionPath()
    {
        return Points.Count == 2 && Transitions.Count == 1;
    }

    /// <summary>
    /// Checks for all transitions and nodes in this path if they still exist.
    /// </summary>
    public bool IsValid()
    {
        if (Points.Any(n => !Map.Points.ContainsKey(n.Id))) return false;
        return true;
    }

    /// <summary>
    /// Checks and returns if this path can be fully used by the given entity.
    /// </summary>
    public bool CanPass(Entity e)
    {
        if (!IsValid()) return false;
        if (Transitions.Any(t => !t.CanPass(e))) return false;
        return true;
    }

    #endregion

    #region Preview

    public void ShowPreview(float width, Color color, LineTexture tex)
    {
        // Destroy old preview
        HidePreview();

        // Create new preview
        PathPreviewObject = new GameObject("Path Preview");

        LineRenderer line = PathPreviewObject.AddComponent<LineRenderer>();
        line.material = ResourceManager.LoadMaterial("Materials/LineMaterials/" + tex.ToString());
        line.startWidth = width;
        line.endWidth = width;
        line.startColor = color;
        line.endColor = color;

        line.positionCount = Points.Count;
        for (int i = 0; i < Points.Count; i++)
        {
            line.SetPosition(i, Points[i].Position);
        }

        line.textureMode = LineTextureMode.Tile;
        float textureScale = 0.5f / width;
        line.textureScale = new Vector2(textureScale, 1f);
        line.numCornerVertices = 5;

        Map.Renderer2D.ApplySortingOrder(line, MapZLayer.PathPreview);
    }

    public void HidePreview()
    {
        if(PathPreviewObject != null) GameObject.Destroy(PathPreviewObject);
    }

    #endregion
}

