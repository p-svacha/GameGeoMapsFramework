using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Pathfinder
{
    private static Map Map;

    #region A*

    // A* algorithm implementation. https://pavcreations.com/tilemap-based-a-star-algorithm-implementation-in-unity-game/
    /// <summary>
    /// Returns the shortest path from a source node to a target node for the given entity.
    /// <br/>Returned path includes both source and target.
    /// </summary>
    public static NavigationPath GetPath(Map map, Entity entity, Point from, Point to, List<LineFeature> forbiddenLineFeatures = null)
    {
        Map = map;
        if (Map == null || from == null || to == null) return null;

        PriorityQueue<Point> openSet = new PriorityQueue<Point>(); // Nodes that are queued for searching
        HashSet<Point> closedSet = new HashSet<Point>(); // Nodes that have already been searched

        Dictionary<Point, float> gCosts = new Dictionary<Point, float>(); // G-Costs are the accumulated real costs from the source node to any other node
        Dictionary<Point, float> fCosts = new Dictionary<Point, float>(); // F-Costs are the combined cost (assumed(h) + real(g)) from the source node to any other node
        Dictionary<Point, Transition> transitionToNodes = new Dictionary<Point, Transition>(); // Stores for each node which transition comes before it to get there the shortest way
        Dictionary<Transition, Transition> transitionToTransitions = new Dictionary<Transition, Transition>(); // Stores for each transition which transition comes before it to get there the shortest way

        // Initialize start node
        gCosts[from] = 0;
        fCosts[from] = GetHCost(from, to);
        openSet.Enqueue(from, fCosts[from]);  // priority = F cost

        while (openSet.Count > 0)
        {
            // Grab the node with the smallest F cost
            Point currentNode = openSet.Dequeue();

            // If it's already in closedSet, it might be a stale entry
            if (closedSet.Contains(currentNode)) continue;

            // If we've reached the goal
            if (currentNode == to)
            {
                return GetFinalPath(to, transitionToNodes, transitionToTransitions);
            }

            closedSet.Add(currentNode);

            // Explore neighbours
            foreach (Transition transition in currentNode.GetTransitions())
            {
                Debug.Log($"Point {currentNode.Id} has {currentNode.GetTransitions().Count} transitions.");

                Point neighbour = transition.To;

                // Skip any invalid or closed neighbours
                if (closedSet.Contains(neighbour)) continue;
                if (!transition.CanPass(entity)) continue;
                if (forbiddenLineFeatures != null && forbiddenLineFeatures.Contains(transition.LineFeature)) continue;

                float tentativeGCost = gCosts[currentNode] + GetCCost(transition, entity);

                // If this neighbor has never been visited or we found a cheaper path
                if (!gCosts.ContainsKey(neighbour) || tentativeGCost < gCosts[neighbour])
                {
                    gCosts[neighbour] = tentativeGCost;
                    float newF = tentativeGCost + GetHCost(neighbour, to);
                    fCosts[neighbour] = newF;

                    transitionToNodes[neighbour] = transition;
                    if (currentNode != from)
                    {
                        transitionToTransitions[transition] = transitionToNodes[transition.From];
                    }

                    // Enqueue or update priority
                    openSet.Enqueue(neighbour, newF);
                }
            }
        }

        // Out of tiles -> no path
        Debug.Log($"[Pathfinder] Couldn't find path {from} -> {to} for {entity?.Name} after checking all transitions.");
        return null;
    }

    /// <summary>
    /// Returns the cost of going from any one node to any other for a specified entity when taking the cheapest possible path.
    /// </summary>
    public static float GetPathCost(Map map, Entity entity, Point from, Point to, List<LineFeature> forbiddenLineFeatures = null)
    {
        NavigationPath path = GetPath(map, entity, from, to, forbiddenLineFeatures);
        if (path == null) return float.MaxValue;
        else return path.GetCost(entity);
    }

    /// <summary>
    /// Assumed cost of that path. This function is not allowed to overestimate the cost. The real cost must be >= this cost.
    /// </summary>
    private static float GetHCost(Point from, Point to)
    {
        return Vector2.Distance(from.Position, to.Position) / 10f;
    }

    /// <summary>
    /// Real cost of going from one node to another.
    /// </summary>
    private static float GetCCost(Transition t, Entity e)
    {
        return t.GetCost(e);
    }

    /// <summary>
    /// Returns the final path to the given target node with all the intermediary steps that have been cached.
    /// </summary>
    private static NavigationPath GetFinalPath(Point to, Dictionary<Point, Transition> transitionToNodes, Dictionary<Transition, Transition> transitionToTransitions)
    {
        List<Point> nodes = new List<Point>(); // reversed list of traversed nodes
        List<Transition> transitions = new List<Transition>(); // reversed list of traversed transitions

        nodes.Add(to);
        Transition currentTransition = transitionToNodes[to];
        transitions.Add(currentTransition);

        while (transitionToTransitions.ContainsKey(currentTransition))
        {
            nodes.Add(currentTransition.From);
            transitions.Add(transitionToTransitions[currentTransition]);
            currentTransition = transitionToTransitions[currentTransition];
        }
        nodes.Add(currentTransition.From);

        nodes.Reverse();
        transitions.Reverse();

        return new NavigationPath(Map, nodes, transitions);
    }

    #endregion
}

