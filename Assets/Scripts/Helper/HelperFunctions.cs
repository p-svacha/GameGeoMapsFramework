using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class HelperFunctions
{
    #region GameObject

    public static void ApplyRandomRotation(GameObject obj)
    {
        obj.transform.rotation = Quaternion.Euler(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f));
    }

    public static GameObject GetChild(GameObject parent, string name)
    {
        for(int i = 0; i < parent.transform.childCount; i++)
        {
            if (parent.transform.GetChild(i).gameObject.name == name) return parent.transform.GetChild(i).gameObject;
        }
        return null;
    }

    /// <summary>
    /// Recursively sets the layer of the given GameObject and all of its children.
    /// </summary>
    /// <param name="root">The root GameObject whose layer (and all descendants’) will be changed.</param>
    /// <param name="layer">The layer index to assign.</param>
    public static void SetLayerRecursively(GameObject root, int layer)
    {
        if (root == null) return;
        root.layer = layer;
        foreach (Transform child in root.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    #endregion

    #region String

    //(HH):MM:SS
    public static string GetDurationString(float seconds, bool includeMilliseconds = false)
    {
        bool isNegative = seconds < 0;
        System.TimeSpan ts = System.TimeSpan.FromSeconds(System.Math.Abs(seconds));

        string core = ts.TotalHours >= 1
            ? $"{(int)ts.TotalHours:00}:{ts.Minutes:00}:{ts.Seconds:00}"
            : $"{(int)ts.TotalMinutes:00}:{ts.Seconds:00}";

        if (includeMilliseconds) core += "." + ts.Milliseconds;

        return isNegative ? "-" + core : core;
    }

    #endregion

    #region Enum

    public static string GetEnumDescription(System.Enum value)
    {
        var fieldInfo = value.GetType().GetField(value.ToString());
        if (fieldInfo == null)
            return value.ToString();

        var attribute = fieldInfo
            .GetCustomAttributes(typeof(DescriptionAttribute), false)
            .FirstOrDefault() as DescriptionAttribute;

        return attribute?.Description ?? value.ToString();
    }

    public static T GetRandomEnumValue<T>() where T : System.Enum
    {
        var values = System.Enum.GetValues(typeof(T));
        return (T)values.GetValue(new System.Random().Next(values.Length));
    }

    #endregion

    #region Math

    /// <summary>
    /// Modulo that can handle negative ints.
    /// </summary>
    public static int Mod(int x, int m)
    {
        return (x % m + m) % m;
    }

    /// <summary>
    /// Modulo that can handle negative floats.
    /// </summary>
    public static float Mod(float x, float m)
    {
        return (x % m + m) % m;
    }

    public static float SmoothLerp(float start, float end, float t)
    {
        t = Mathf.Clamp01(t);
        t = t * t * (3f - 2f * t);
        return Mathf.Lerp(start, end, t);
    }

    public static Vector3 SmoothLerp(Vector3 start, Vector3 end, float t)
    {
        t = Mathf.Clamp01(t);
        t = t * t * (3f - 2f * t);
        return Vector3.Lerp(start, end, t);
    }

    /// <summary>
    /// Returns the world angle (in degrees) from start to end.
    /// 0° = North (positive Y), 90° = East, 180° = South, 270° = West.
    /// </summary>
    public static float GetWorldAngle(Vector2 start, Vector2 end)
    {
        Vector2 dir = end - start;
        if (dir == Vector2.zero)
            return 0f;

        // atan2 gives angle in radians relative to +X axis; convert to degrees
        float angle = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;

        // Normalize to [0, 360)
        if (angle < 0f) angle += 360f;
        return angle;
    }

    /// <summary>
    /// Rasterizes a line between two points using Bresenham's line algorithm.
    /// Returns a list of all grid cells that should be filled, considering the specified line thickness.
    /// </summary>
    public static List<Vector2Int> RasterizeLine(Vector2 start, Vector2 end, int lineThickness)
    {
        List<Vector2Int> points = new List<Vector2Int>();

        float x0 = start.x;
        float y0 = start.y;
        float x1 = end.x;
        float y1 = end.y;

        float dx = Mathf.Abs(x1 - x0);
        float dy = Mathf.Abs(y1 - y0);
        float sx = x0 < x1 ? 1f : -1f;
        float sy = y0 < y1 ? 1f : -1f;
        float err = dx - dy;

        // Calculate half thickness
        float additionalWidthOnEachSide = ((lineThickness - 1f) / 2f);

        while (true)
        {
            // Add points around the main point to achieve the desired thickness
            for (float tx = -additionalWidthOnEachSide; tx <= additionalWidthOnEachSide; tx += 0.1f)
            {
                for (float ty = -additionalWidthOnEachSide; ty <= additionalWidthOnEachSide; ty += 0.1f)
                {
                    // Add point only if it's within the square around the thickness radius
                    if (Mathf.Abs(tx) + Mathf.Abs(ty) <= additionalWidthOnEachSide)
                    {
                        Vector2Int point = new Vector2Int(Mathf.RoundToInt(x0 + tx), Mathf.RoundToInt(y0 + ty));
                        if (!points.Contains(point))
                        {
                            points.Add(point);
                        }
                    }
                }
            }

            if (Mathf.Abs(x0 - x1) <= 1f && Mathf.Abs(y0 - y1) <= 1f)
                break;

            float e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }

        return points;
    }

    public static void SetAsMirrored(GameObject obj)
    {
        obj.transform.localScale = new Vector3(obj.transform.localScale.x * -1f, obj.transform.localScale.y, obj.transform.localScale.z);
    }

    /// <summary>
    /// Creates a list of points along a parabolic arc between start and end.
    /// </summary>
    /// <param name="start">Starting point of the arc.</param>
    /// <param name="end">Ending point of the arc.</param>
    /// <param name="height">Maximum height of the arc.</param>
    /// <param name="segments">Number of segments the arc is divided into.</param>
    /// <returns>A list of Vector3 points along the arc.</returns>
    public static List<Vector3> CreateArc(Vector3 start, Vector3 end, float height, int segments)
    {
        List<Vector3> arcPoints = new List<Vector3>();

        // Add the start point
        arcPoints.Add(start);

        // Calculate the arc
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments; // Normalized parameter (0 to 1)

            // Linear interpolation between start and end
            Vector3 linearPoint = Vector3.Lerp(start, end, t);

            // Add vertical parabolic height
            float parabolicHeight = 4 * height * t * (1 - t); // Parabolic equation
            linearPoint.y += parabolicHeight;

            arcPoints.Add(linearPoint);
        }

        return arcPoints;
    }

    #endregion

    #region Geometry

    public static float Sigmoid(float value)
    {
        return (float)(1.0 / (1.0 + System.Math.Pow(System.Math.E, -value)));
    }

    /// <summary>
    /// Returns true if line segment 'p1q1' and 'p2q2' intersect.
    /// </summary>
    public static bool DoLineSegmentsIntersect(Vector2 p1, Vector2 q1, Vector2 p2, Vector2 q2)
    {
        // Find the four orientations needed for general and 
        // special cases 
        int o1 = orientation(p1, q1, p2);
        int o2 = orientation(p1, q1, q2);
        int o3 = orientation(p2, q2, p1);
        int o4 = orientation(p2, q2, q1);

        // General case 
        if (o1 != o2 && o3 != o4)
            return true;

        // Special Cases 
        // p1, q1 and p2 are colinear and p2 lies on segment p1q1 
        if (o1 == 0 && onSegment(p1, p2, q1)) return true;

        // p1, q1 and q2 are colinear and q2 lies on segment p1q1 
        if (o2 == 0 && onSegment(p1, q2, q1)) return true;

        // p2, q2 and p1 are colinear and p1 lies on segment p2q2 
        if (o3 == 0 && onSegment(p2, p1, q2)) return true;

        // p2, q2 and q1 are colinear and q1 lies on segment p2q2 
        if (o4 == 0 && onSegment(p2, q1, q2)) return true;

        return false; // Doesn't fall in any of the above cases 
    }

    /// <summary>
    /// Returns the area of a polygon with the given points
    /// </summary>
    public static float GetPolygonArea(List<Vector2> points)
    {
        // Add the first point to the end.
        int num_points = points.Count;
        Vector2[] pts = new Vector2[num_points + 1];
        for (int i = 0; i < points.Count; i++) pts[i] = points[i];
        pts[num_points] = points[0];

        // Get the areas.
        float area = 0;
        for (int i = 0; i < num_points; i++)
        {
            area +=
                (pts[i + 1].x - pts[i].x) *
                (pts[i + 1].y + pts[i].y) / 2;
        }

        // Return the result.
        return Mathf.Abs(area);
    }

    /// <summary>
    /// Returns how much apart two degree values are. For example the degree distance between 350° and 10° would return 20°.
    /// </summary>
    public static int DegreeDistance(int deg1, int deg2)
    {
        int absDistance = deg1 > deg2 ? deg1 - deg2 : deg2 - deg1;
        return absDistance <= 180 ? absDistance : 360 - absDistance;
    }

    /// <summary>
    /// Returns a the rotated vector of a given vector by x degrees.
    /// </summary>
    public static Vector2 RotateVector(Vector2 v, float degrees)
    {
        if (degrees == 90) return new Vector2(-v.y, v.x);
        if (degrees == -90) return new Vector2(v.y, -v.x);

        float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
        float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);

        float tx = v.x;
        float ty = v.y;

        float vx = (cos * tx) - (sin * ty);
        float vy = (sin * tx) + (cos * ty);
        return new Vector2(vx, vy);
    }

    /// <summary>
    /// Returns if the polygon with the given points is in clockwise or anticlockwise rotation
    /// </summary>
    public static bool IsClockwise(List<Vector2> points)
    {
        int num_points = points.Count;
        Vector2[] pts = new Vector2[num_points + 1];
        for (int i = 0; i < points.Count; i++) pts[i] = points[i];
        pts[num_points] = points[0];

        // Get the areas.
        float area = 0;
        for (int i = 0; i < num_points; i++)
        {
            area +=
                (pts[i + 1].x - pts[i].x) *
                (pts[i + 1].y + pts[i].y) / 2;
        }

        return area < 0;
    }

    /// <summary>
    /// Determines if the given point is inside the polygon
    /// </summary>
    public static bool IsPointInPolygon4(List<Vector2> polygon, Vector2 testPoint)
    {
        bool result = false;
        int j = polygon.Count - 1;
        for (int i = 0; i < polygon.Count; i++)
        {
            if (polygon[i].y < testPoint.y && polygon[j].y >= testPoint.y || polygon[j].y < testPoint.y && polygon[i].y >= testPoint.y)
            {
                if (polygon[i].x + (testPoint.y - polygon[i].y) / (polygon[j].y - polygon[i].y) * (polygon[j].x - polygon[i].x) < testPoint.x)
                {
                    result = !result;
                }
            }
            j = i;
        }
        return result;
    }

    public static Vector2 GetOffsetIntersection(Vector2 prevPoint, Vector2 thisPoint, Vector2 nextPoint, float prevOffset, float nextOffset)
    {
        TryGetOffsetIntersection(prevPoint, thisPoint, nextPoint, prevOffset, nextOffset, out var p);
        return p;
    }

    /// <summary>
    /// Intersection of two offset lines built from segments (prev->this) and (this->next).
    /// Offsets are measured along the CCW (left) normal of each segment; pass negative to push to the right.
    /// Returns true if an intersection exists (non-parallel), false otherwise (outPoint = thisPoint shifted by averaged offsets).
    /// </summary>
    public static bool TryGetOffsetIntersection(
        Vector2 prevPoint, Vector2 thisPoint, Vector2 nextPoint,
        float prevOffset, float nextOffset,
        out Vector2 outPoint, float epsilon = 1e-6f)
    {
        // Directions of the two segments
        Vector2 d1 = (thisPoint - prevPoint);
        Vector2 d2 = (nextPoint - thisPoint);

        float len1 = d1.magnitude;
        float len2 = d2.magnitude;

        // Handle degenerate segments
        if (len1 <= epsilon && len2 <= epsilon)
        {
            outPoint = thisPoint;
            return false;
        }
        if (len1 <= epsilon) d1 = (nextPoint - thisPoint).normalized; else d1 /= len1;
        if (len2 <= epsilon) d2 = (thisPoint - prevPoint).normalized; else d2 /= len2;

        // CCW (left) normals
        Vector2 n1 = new Vector2(-d1.y, d1.x);
        Vector2 n2 = new Vector2(-d2.y, d2.x);

        // Anchor points of the two offset lines
        Vector2 p1 = thisPoint + n1 * prevOffset;
        Vector2 p2 = thisPoint + n2 * nextOffset;

        // Solve p1 + t*d1 = p2 + u*d2  ->  t = cross((p2 - p1), d2) / cross(d1, d2)
        float denom = Cross(d1, d2);

        if (Mathf.Abs(denom) <= epsilon)
        {
            // Parallel (or nearly): fall back to average offset along a consistent side
            outPoint = thisPoint + (n1 * prevOffset + n2 * nextOffset) * 0.5f;
            return false;
        }

        float t = Cross((p2 - p1), d2) / denom;
        outPoint = p1 + d1 * t;
        return true;
    }

    // 2D scalar cross product
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private static float Cross(in Vector2 a, in Vector2 b) => a.x * b.y - a.y * b.x;

    /// <summary>
    /// Returns if the two vectors p and q are close to parallel
    /// </summary>
    public static bool IsParallel(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2)
    {
        if (Mathf.Abs(p1.x - p2.x) < 0.001f && Mathf.Abs(q1.x - q2.x) < 0.001f) return true;

        Vector2 p = p2 - p1;
        Vector2 q = q2 - q1;

        Vector2 pn = p.normalized;
        Vector2 qn = q.normalized;
        float xDiff = Mathf.Abs(pn.x - qn.x);
        float yDiff = Mathf.Abs(pn.y - qn.y);
        return ((xDiff + yDiff) < 0.002f);
    }

    public static bool IsPointOnLineSegment(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
    {
        float AB = Mathf.Sqrt((lineEnd.x - lineStart.x) * (lineEnd.x - lineStart.x) + (lineEnd.y - lineStart.y) * (lineEnd.y - lineStart.y));
        float AP = Mathf.Sqrt((point.x - lineStart.x) * (point.x - lineStart.x) + (point.y - lineStart.y) * (point.y - lineStart.y));
        float PB = Mathf.Sqrt((lineEnd.x - point.x) * (lineEnd.x - point.x) + (lineEnd.y - point.y) * (lineEnd.y - point.y));
        return Mathf.Abs(AB - (AP + PB)) < 0.001f;
    }

    #endregion

    #region Private functions

    // Given three colinear Vector2s p, q, r, the function checks if 
    // Vector2 q lies on line segment 'pr' 
    private static bool onSegment(Vector2 p, Vector2 q, Vector2 r)
    {
        if (q.x <= Mathf.Max(p.x, r.x) && q.x >= Mathf.Min(p.x, r.x) &&
            q.y <= Mathf.Max(p.y, r.y) && q.y >= Mathf.Min(p.y, r.y))
            return true;

        return false;
    }

    // To find orientation of ordered triplet (p, q, r). 
    // The function returns following values 
    // 0 --> p, q and r are colinear 
    // 1 --> Clockwise 
    // 2 --> Counterclockwise 
    private static int orientation(Vector2 p, Vector2 q, Vector2 r)
    {
        // See https://www.geeksforgeeks.org/orientation-3-ordered-Vector2s/ 
        // for details of below formula. 
        float val = (q.y - p.y) * (r.x - q.x) - (q.x - p.x) * (r.y - q.y);

        if (val == 0) return 0; // colinear 

        return (val > 0) ? 1 : 2; // clock or counterclock wise 
    }

    // Find the point of intersection between
    // the lines p1 --> p2 and p3 --> p4.
    private static Vector2 FindIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
    {
        bool segments_intersect = false;
        Vector2 intersection = new Vector2(0, 0);

        // Check parallel
        if (Mathf.Abs(p1.x - p2.x) < 0.001f && Mathf.Abs(p3.x - p4.x) < 0.001f)
        {
            Debug.LogWarning("WARNING: A PARALLEL LINE HAS BEEN FOUND IN THE SECOND CHECK THAT HAS BEEN SKIPPED IN FIRST CHECK. RETURNING 0/0 VECTOR");
            intersection = new Vector2(0, 0);
            return intersection;
        }
        float a1 = (p1.y - p2.y) / (p1.x - p2.x);
        float a2 = (p3.y - p4.y) / (p3.x - p4.x);
        if (Mathf.Abs(a1 - a2) < 0.00001f)
        {
            intersection = new Vector2(0, 0);
            return intersection;
        }

        // Get the segments' parameters.
        float dx12 = p2.x - p1.x;
        float dy12 = p2.y - p1.y;
        float dx34 = p4.x - p3.x;
        float dy34 = p4.y - p3.y;

        // Solve for t1 and t2
        float denominator = (dy12 * dx34 - dx12 * dy34);

        float t1 =
            ((p1.x - p3.x) * dy34 + (p3.y - p1.y) * dx34)
                / denominator;
        if (float.IsInfinity(t1))
        {
            // The lines are parallel (or close enough to it).
            segments_intersect = false;
            intersection = new Vector2(0, 0);
            return intersection;
        }

        float t2 =
            ((p3.x - p1.x) * dy12 + (p1.y - p3.y) * dx12)
                / -denominator;

        // Find the point of intersection.
        intersection = new Vector2(p1.x + dx12 * t1, p1.y + dy12 * t1);

        // The segments intersect if t1 and t2 are between 0 and 1.
        segments_intersect =
            ((t1 >= 0) && (t1 <= 1) &&
             (t2 >= 0) && (t2 <= 1));

        // Find the closest points on the segments.
        if (t1 < 0)
        {
            t1 = 0;
        }
        else if (t1 > 1)
        {
            t1 = 1;
        }

        if (t2 < 0)
        {
            t2 = 0;
        }
        else if (t2 > 1)
        {
            t2 = 1;
        }

        return intersection;
    }

    #endregion

    #region Random

    public static T GetWeightedRandomElement<T>(Dictionary<T, int> weightDictionary)
    {
        int probabilitySum = weightDictionary.Sum(x => x.Value);
        int rng = Random.Range(0, probabilitySum);
        int tmpSum = 0;
        foreach (KeyValuePair<T, int> kvp in weightDictionary)
        {
            tmpSum += kvp.Value;
            if (rng < tmpSum) return kvp.Key;
        }
        throw new System.Exception();
    }
    public static T GetWeightedRandomElement<T>(Dictionary<T, float> weightDictionary)
    {
        float probabilitySum = weightDictionary.Sum(x => x.Value);
        float rng = Random.Range(0, probabilitySum);
        float tmpSum = 0;
        foreach (KeyValuePair<T, float> kvp in weightDictionary)
        {
            tmpSum += kvp.Value;
            if (rng < tmpSum) return kvp.Key;
        }
        throw new System.Exception();
    }

    /// <summary>
    /// Returns a random number in a gaussian distribution. About 2/3 of generated numbers are within the standard deviation of the mean.
    /// </summary>
    public static float NextGaussian(float mean, float standard_deviation)
    {
        return mean + NextGaussian() * standard_deviation;
    }
    private static float NextGaussian()
    {
        float v1, v2, s;
        do
        {
            v1 = 2.0f * Random.Range(0f, 1f) - 1.0f;
            v2 = 2.0f * Random.Range(0f, 1f) - 1.0f;
            s = v1 * v1 + v2 * v2;
        } while (s >= 1.0f || s == 0f);
        s = Mathf.Sqrt((-2.0f * Mathf.Log(s)) / s);

        return v1 * s;
    }
    public static Vector2Int GetRandomNearPosition(Vector2Int pos, float standard_deviation)
    {
        float x = NextGaussian(pos.x, standard_deviation);
        float y = NextGaussian(pos.y, standard_deviation);

        return new Vector2Int(Mathf.RoundToInt(x), Mathf.RoundToInt(y));
    }

    public static Direction GetRandomSide() => GetSides().RandomElement();
    public static Direction GetRandomCorner() => GetCorners().RandomElement();

    #endregion

    #region Direction

    public static Direction GetNextDirection8(Direction dir)
    {
        return dir switch
        {
            Direction.N => Direction.NE,
            Direction.NE => Direction.E,
            Direction.E => Direction.SE,
            Direction.SE => Direction.S,
            Direction.S => Direction.SW,
            Direction.SW => Direction.W,
            Direction.W => Direction.NW,
            Direction.NW => Direction.N,
            _ => throw new System.Exception("Direction " + dir.ToString() + " not handled")
        };
    }
    public static Direction GetPreviousDirection8(Direction dir)
    {
        return dir switch
        {
            Direction.N => Direction.NW,
            Direction.NW => Direction.W,
            Direction.W => Direction.SW,
            Direction.SW => Direction.S,
            Direction.S => Direction.SE,
            Direction.SE => Direction.E,
            Direction.E => Direction.NE,
            Direction.NE => Direction.N,
            _ => throw new System.Exception("Direction " + dir.ToString() + " not handled")
        };
    }

    public static Direction GetNextSideDirection(Direction dir)
    {
        return dir switch
        {
            Direction.N => Direction.E,
            Direction.E => Direction.S,
            Direction.S => Direction.W,
            Direction.W => Direction.N,
            _ => throw new System.Exception("Direction " + dir.ToString() + " not handled")
        };
    }
    public static Direction GetPreviousSideDirection(Direction dir)
    {
        return dir switch
        {
            Direction.N => Direction.W,
            Direction.E => Direction.N,
            Direction.S => Direction.E,
            Direction.W => Direction.S,
            _ => throw new System.Exception("Direction " + dir.ToString() + " not handled")
        };
    }

    public static Direction GetAdjacentDirection(Vector2Int from, Vector2Int to)
    {
        if (to == from) return Direction.None;

        if (to == from + new Vector2Int(1, 0)) return Direction.E;
        if (to == from + new Vector2Int(-1, 0)) return Direction.W;
        if (to == from + new Vector2Int(0, 1)) return Direction.N;
        if (to == from + new Vector2Int(0, -1)) return Direction.S;

        if (to == from + new Vector2Int(1, 1)) return Direction.NE;
        if (to == from + new Vector2Int(-1, 1)) return Direction.NW;
        if (to == from + new Vector2Int(1, -1)) return Direction.SE;
        if (to == from + new Vector2Int(-1, -1)) return Direction.SW;

        throw new System.Exception("The two given coordinates are not equal or adjacent to each other.");
    }

    public static Direction GetOppositeDirection(Direction dir)
    {
        return dir switch
        {
            Direction.N => Direction.S,
            Direction.E => Direction.W,
            Direction.S => Direction.N,
            Direction.W => Direction.E,
            Direction.NE => Direction.SW,
            Direction.NW => Direction.SE,
            Direction.SW => Direction.NE,
            Direction.SE => Direction.NW,
            Direction.None => Direction.None,
            _ => throw new System.Exception("Direction " + dir.ToString() + " not handled")
        };
    }

    public static Direction GetDirection8FromAngle(float angle, float offset = 0)
    {
        // Normalize angle to the [0, 360) range
        float angleDegrees = angle + offset;
        angleDegrees %= 360f;
        if (angleDegrees < 0)
        {
            angleDegrees += 360f;
        }

        // Offset by 22.5 so that each 45° segment is centered
        double segmentOffset = (angleDegrees + 22.5) % 360f;

        // Determine which 45° segment the angle belongs in
        int segment = (int)(segmentOffset / 45.0);

        switch (segment)
        {
            case 0: return Direction.N;
            case 1: return Direction.NE;
            case 2: return Direction.E;
            case 3: return Direction.SE;
            case 4: return Direction.S;
            case 5: return Direction.SW;
            case 6: return Direction.W;
            case 7: return Direction.NW;
            default: return Direction.None;
        }
    }

    public static Direction GetDirection4FromAngle(float angle, float offset = 0)
    {
        // Normalize angle to the [0, 360) range
        float angleDegrees = angle + offset;
        angleDegrees %= 360f;
        if (angleDegrees < 0)
        {
            angleDegrees += 360f;
        }

        // Offset by 45 so that each 90° segment is centered
        double segmentOffset = (angleDegrees + 45) % 360f;

        // Determine which 90° segment the angle belongs in
        int segment = (int)(segmentOffset / 90.0);

        switch (segment)
        {
            case 0: return Direction.N;
            case 1: return Direction.E;
            case 2: return Direction.S;
            case 3: return Direction.W;
            default: return Direction.None;
        }
    }


    private static List<Direction> _Directions8 = new List<Direction>() { Direction.N, Direction.NE, Direction.E, Direction.SE, Direction.S, Direction.SW, Direction.W, Direction.NW };
    public static List<Direction> GetAllDirections8() => _Directions8;
    
    private static List<Direction> _Directions9 = new List<Direction>() { Direction.None, Direction.N, Direction.NE, Direction.E, Direction.SE, Direction.S, Direction.SW, Direction.W, Direction.NW };
    public static List<Direction> GetAllDirections9() => _Directions9;
    
    private static List<Direction> _Corners = new List<Direction>() { Direction.SW, Direction.SE, Direction.NE, Direction.NW };
    public static List<Direction> GetCorners() => _Corners;

    private static List<Direction> _Sides = new List<Direction>() { Direction.N, Direction.E, Direction.S, Direction.W };
    public static List<Direction> GetSides() => _Sides;
    public static bool IsCorner(Direction dir) => GetCorners().Contains(dir);
    public static bool IsSide(Direction dir) => GetSides().Contains(dir);

    public static Vector2Int GetCoordinatesInDirection(Vector2Int coordinates, Direction dir)
    {
        return coordinates + GetDirectionVectorInt(dir);
    }
    public static Vector2Int GetDirectionVectorInt(Direction dir, int distance = 1)
    {
        if (dir == Direction.N) return new Vector2Int(0, distance);
        if (dir == Direction.E) return new Vector2Int(distance, 0);
        if (dir == Direction.S) return new Vector2Int(0, -distance);
        if (dir == Direction.W) return new Vector2Int(-distance, 0);
        if (dir == Direction.NE) return new Vector2Int(distance, distance);
        if (dir == Direction.NW) return new Vector2Int(-distance, distance);
        if (dir == Direction.SE) return new Vector2Int(distance, -distance);
        if (dir == Direction.SW) return new Vector2Int(-distance, -distance);
        return new Vector2Int(0, 0);
    }

    public static Vector2 GetDirectionVectorFloat(Direction dir, float distance = 1f)
    {
        if (dir == Direction.N) return new Vector2(0, distance);
        if (dir == Direction.E) return new Vector2(distance, 0);
        if (dir == Direction.S) return new Vector2(0, -distance);
        if (dir == Direction.W) return new Vector2(-distance, 0);
        if (dir == Direction.NE) return new Vector2(distance, distance);
        if (dir == Direction.NW) return new Vector2(-distance, distance);
        if (dir == Direction.SE) return new Vector2(distance, -distance);
        if (dir == Direction.SW) return new Vector2(-distance, -distance);
        return new Vector2(0, 0);
    }

    /// <summary>
    /// Returns the corner directions that are relevant for a given direction.
    /// </summary>
    public static List<Direction> GetAffectedCorners(Direction dir)
    {
        if (dir == Direction.None) return new List<Direction> { Direction.NE, Direction.NW, Direction.SW, Direction.SE };
        if (dir == Direction.N) return new List<Direction> { Direction.NE, Direction.NW };
        if (dir == Direction.E) return new List<Direction> { Direction.NE, Direction.SE };
        if (dir == Direction.S) return new List<Direction> { Direction.SW, Direction.SE };
        if (dir == Direction.W) return new List<Direction> { Direction.SW, Direction.NW };
        if (dir == Direction.NW) return new List<Direction>() { Direction.NW };
        if (dir == Direction.NE) return new List<Direction>() { Direction.NE };
        if (dir == Direction.SE) return new List<Direction>() { Direction.SE };
        if (dir == Direction.SW) return new List<Direction>() { Direction.SW };
        throw new System.Exception("Direction " + dir.ToString() + " not handled");
    }
    public static bool DoAffectedCornersOverlap(Direction dir1, Direction dir2) => GetAffectedCorners(dir1).Intersect(GetAffectedCorners(dir2)).Any();

    public static List<Direction> GetAffectedSides(Direction dir)
    {
        if (dir == Direction.None) return new List<Direction> { Direction.N, Direction.E, Direction.S, Direction.W };
        if (dir == Direction.N) return new List<Direction> { Direction.N };
        if (dir == Direction.E) return new List<Direction> { Direction.E };
        if (dir == Direction.S) return new List<Direction> { Direction.S };
        if (dir == Direction.W) return new List<Direction> { Direction.W };
        if (dir == Direction.NW) return new List<Direction>() { Direction.N, Direction.W };
        if (dir == Direction.NE) return new List<Direction>() { Direction.N, Direction.E };
        if (dir == Direction.SE) return new List<Direction>() { Direction.S, Direction.E };
        if (dir == Direction.SW) return new List<Direction>() { Direction.S, Direction.W };
        throw new System.Exception("Direction " + dir.ToString() + " not handled");
    }

    public static List<Direction> GetAffectedDirections(Direction dir)
    {
        if (dir == Direction.None) return GetAllDirections8();
        if (dir == Direction.N) return new List<Direction> { Direction.NW, Direction.N, Direction.NE };
        if (dir == Direction.E) return new List<Direction> { Direction.NE, Direction.E, Direction.SE };
        if (dir == Direction.S) return new List<Direction> { Direction.SW, Direction.S, Direction.SE };
        if (dir == Direction.W) return new List<Direction> { Direction.NW, Direction.W, Direction.SW };
        if (dir == Direction.NW) return new List<Direction>() { Direction.NW, Direction.N, Direction.W };
        if (dir == Direction.NE) return new List<Direction>() { Direction.NE, Direction.N, Direction.E };
        if (dir == Direction.SE) return new List<Direction>() { Direction.SE, Direction.S, Direction.E };
        if (dir == Direction.SW) return new List<Direction>() { Direction.SW, Direction.S, Direction.W };
        throw new System.Exception("Direction " + dir.ToString() + " not handled");
    }

    public static Direction GetMirroredCorner(Direction dir, Direction axis)
    {
        if(axis == Direction.N || axis == Direction.S) // east,west stays the same
        {
            if (dir == Direction.NE) return Direction.SE;
            if (dir == Direction.NW) return Direction.SW;
            if (dir == Direction.SW) return Direction.NW;
            if (dir == Direction.SE) return Direction.NE;
        }
        if (axis == Direction.E || axis == Direction.W) // north,south stays the same
        {
            if (dir == Direction.NE) return Direction.NW;
            if (dir == Direction.NW) return Direction.NE;
            if (dir == Direction.SW) return Direction.SE;
            if (dir == Direction.SE) return Direction.SW;
        }
        throw new System.Exception("axis " + axis.ToString() + " not handled or direction " + dir.ToString() + " not handled");
    }

    /// <summary>
    /// Returns the heights for a flat surface based on its height.
    /// </summary>
    public static Dictionary<Direction, int> GetFlatHeights(int height)
    {
        Dictionary<Direction, int> heights = new Dictionary<Direction, int>();
        foreach (Direction dir in GetCorners()) heights.Add(dir, height);
        return heights;
    }

    /// <summary>
    /// Returns the heights for a sloped surface based on its upwards direction and base height.
    /// </summary>
    public static Dictionary<Direction, int> GetSlopeHeights(int baseHeight, Direction dir)
    {
        Dictionary<Direction, int> heights = new Dictionary<Direction, int>();
        foreach (Direction corner in GetAffectedCorners(dir))
        {
            heights.Add(corner, baseHeight + 1);
            heights.Add(GetOppositeDirection(corner), baseHeight);
        }
        return heights;
    }

    public static float GetDirectionAngle(Direction dir)
    {
        if (dir == Direction.N) return 0f;
        if (dir == Direction.NE) return 45f;
        if (dir == Direction.E) return 90f;
        if (dir == Direction.SE) return 135f;
        if (dir == Direction.S) return 180f;
        if (dir == Direction.SW) return 225f;
        if (dir == Direction.W) return 270f;
        if (dir == Direction.NW) return 315f;
        return 0f;
    }

    public static Quaternion Get2dRotationByDirection(Direction dir)
    {
        return Quaternion.Euler(0f, GetDirectionAngle(dir), 0f);
    }

    /// <summary>
    /// Returns the cell coordinates when going from source coordinates into a 2d direction on the same altitude level.
    /// </summary>
    public static Vector3Int GetAdjacentCellCoordinates(Vector3Int cellCoordinates, Direction dir)
    {
        Vector2Int dirVector = GetDirectionVectorInt(dir);
        return new Vector3Int(cellCoordinates.x + dirVector.x, cellCoordinates.y, cellCoordinates.z + dirVector.y);
    }

    /// <summary>
    /// Returns the global cell coordinates of the wall that is to the left/right/above/below a wall piece with the given source coordinates and side.
    /// <br/> dir refers which direction we want to search, whereas (N = Above, S = Below, W = Left, E = Right).
    /// </summary>
    public static Vector3Int GetAdjacentWallCellCoordinates(Vector3Int sourceCoordinates, Direction sourceSide, Direction dir)
    {
        if (dir == Direction.N) return new Vector3Int(sourceCoordinates.x, sourceCoordinates.y + 1, sourceCoordinates.z);
        if (dir == Direction.S) return new Vector3Int(sourceCoordinates.x, sourceCoordinates.y - 1, sourceCoordinates.z);

        Vector3Int offset = Vector3Int.zero;
        if (GetAffectedSides(dir).Contains(Direction.N)) offset = new Vector3Int(0, 1, 0);
        if (GetAffectedSides(dir).Contains(Direction.S)) offset = new Vector3Int(0, -1, 0);

        if (GetAffectedSides(dir).Contains(Direction.W))
        {
            return sourceSide switch
            {
                Direction.N => offset + new Vector3Int(sourceCoordinates.x + 1, sourceCoordinates.y, sourceCoordinates.z),
                Direction.S => offset + new Vector3Int(sourceCoordinates.x - 1, sourceCoordinates.y, sourceCoordinates.z),
                Direction.W => offset + new Vector3Int(sourceCoordinates.x, sourceCoordinates.y, sourceCoordinates.z + 1),
                Direction.E => offset + new Vector3Int(sourceCoordinates.x, sourceCoordinates.y, sourceCoordinates.z - 1),
                _ => throw new System.Exception("direction not handled")
            };
        }

        if (GetAffectedSides(dir).Contains(Direction.E))
        {
            return sourceSide switch
            {
                Direction.N => offset + new Vector3Int(sourceCoordinates.x - 1, sourceCoordinates.y, sourceCoordinates.z),
                Direction.S => offset + new Vector3Int(sourceCoordinates.x + 1, sourceCoordinates.y, sourceCoordinates.z),
                Direction.W => offset + new Vector3Int(sourceCoordinates.x, sourceCoordinates.y, sourceCoordinates.z - 1),
                Direction.E => offset + new Vector3Int(sourceCoordinates.x, sourceCoordinates.y, sourceCoordinates.z + 1),
                _ => throw new System.Exception("direction not handled")
            };
        }
        throw new System.Exception("direction not handled");
    }

    public static Vector2Int GetCornerCoordinates(Vector2Int dimensions, Direction corner)
    {
        if (corner == Direction.SW) return new Vector2Int(0, 0);
        if (corner == Direction.SE) return new Vector2Int(dimensions.x - 1, 0);
        if (corner == Direction.NE) return new Vector2Int(dimensions.x - 1, dimensions.y - 1);
        if (corner == Direction.NW) return new Vector2Int(0, dimensions.y - 1);
        throw new System.Exception($"{corner} is not a valid corner direction.");
    }

    /// <summary>
    /// Groups the given coordinates into clusters based on 4-directional connectivity.
    /// </summary>
    /// <param name="coordinates">A set of 2D coordinates.</param>
    /// <returns>A list of clusters, each cluster represented as a HashSet of connected coordinates.</returns>
    public static List<HashSet<Vector2Int>> GetConnectedClusters(HashSet<Vector2Int> coordinates)
    {
        var clusters = new List<HashSet<Vector2Int>>();
        var visited = new HashSet<Vector2Int>();

        // For each coordinate, if it's not yet visited, perform a BFS or DFS to find its cluster
        foreach (var coord in coordinates)
        {
            if (!visited.Contains(coord))
            {
                var cluster = new HashSet<Vector2Int>();
                var queue = new Queue<Vector2Int>();

                // Start BFS
                visited.Add(coord);
                queue.Enqueue(coord);

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    cluster.Add(current);

                    // Check all 4 possible neighbors
                    foreach (var neighbor in GetNeighbors(current))
                    {
                        // If neighbor is part of the original set and not yet visited, add it
                        if (coordinates.Contains(neighbor) && !visited.Contains(neighbor))
                        {
                            visited.Add(neighbor);
                            queue.Enqueue(neighbor);
                        }
                    }
                }

                clusters.Add(cluster);
            }
        }

        return clusters;
    }

    /// <summary>
    /// Returns the 4 orthogonal neighbors for a given coordinate.
    /// </summary>
    private static IEnumerable<Vector2Int> GetNeighbors(Vector2Int coord)
    {
        yield return new Vector2Int(coord.x + 1, coord.y);
        yield return new Vector2Int(coord.x - 1, coord.y);
        yield return new Vector2Int(coord.x, coord.y + 1);
        yield return new Vector2Int(coord.x, coord.y - 1);
    }

    #endregion

    #region UI

    /// <summary>
    /// Destroys all children of a GameObject immediately.
    /// </summary>
    public static void DestroyAllChildredImmediately(GameObject obj, int skipElements = 0)
    {
        int numChildren = obj.transform.childCount;
        for (int i = skipElements; i < numChildren; i++) GameObject.DestroyImmediate(obj.transform.GetChild(skipElements).gameObject);
    }

    public static Sprite TextureToSprite(Texture tex) => TextureToSprite((Texture2D)tex);
    public static Sprite TextureToSprite(Texture2D tex)
    {
        if (tex == null) return null;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
    }
    public static Sprite GetAssetPreviewSprite(string path)
    {
#if UNITY_EDITOR
        // Only executes in the Unity Editor
        UnityEngine.Object asset = Resources.Load(path);
        if (asset == null)
            throw new System.Exception($"Could not find asset with path {path}.");

        // The AssetPreview class is also editor-only
        Texture2D assetPreviewTexture = UnityEditor.AssetPreview.GetAssetPreview(asset);
        // if (assetPreviewTexture == null) 
        //    throw new System.Exception($"Could not create asset preview texture of {asset} ({path}).");

        return TextureToSprite(assetPreviewTexture);
#else
    // Always returns null in builds
    return null;
#endif
    }

    public static Sprite TextureToSprite(string resourcePath)
    {
        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        return TextureToSprite(texture);
    }

    /// <summary>
    /// Sets the Left, Right, Top and Bottom attribute of a RectTransform
    /// </summary>
    public static void SetRectTransformMargins(RectTransform rt, float left, float right, float top, float bottom)
    {
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    public static void SetLeft(RectTransform rt, float left)
    {
        rt.offsetMin = new Vector2(left, rt.offsetMin.y);
    }

    public static void SetRight(RectTransform rt, float right)
    {
        rt.offsetMax = new Vector2(-right, rt.offsetMax.y);
    }

    public static void SetTop(RectTransform rt, float top)
    {
        rt.offsetMax = new Vector2(rt.offsetMax.x, -top);
    }

    public static void SetBottom(RectTransform rt, float bottom)
    {
        rt.offsetMin = new Vector2(rt.offsetMin.x, bottom);
    }

    /// <summary>
    /// Unfocusses any focussed button/dropdown/toggle UI element so that keyboard inputs don't get 'absorbed' by the UI element.
    /// </summary>
    public static void UnfocusNonInputUiElements()
    {
        if (EventSystem.current.currentSelectedGameObject != null && (
            EventSystem.current.currentSelectedGameObject.GetComponent<Button>() != null ||
            EventSystem.current.currentSelectedGameObject.GetComponent<TMP_Dropdown>() != null ||
            EventSystem.current.currentSelectedGameObject.GetComponent<Toggle>() != null
            ))
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    /// <summary>
    /// Returns if any ui element is currently focussed.
    /// </summary>
    public static bool IsUiFocussed()
    {
        return EventSystem.current.currentSelectedGameObject != null;
    }

    /// <summary>
    /// Returns is the mouse is currently hovering over a UI element.
    /// </summary>
    public static bool IsMouseOverUi()
    {
        return EventSystem.current.IsPointerOverGameObject();
    }

    public static bool IsPointerOverUIWithComponent<T>() where T : UnityEngine.Component
    {
        EventSystem eventSystem = EventSystem.current;
        GraphicRaycaster raycaster = UnityEngine.Object.FindFirstObjectByType<GraphicRaycaster>();

        PointerEventData pointerEventData = new PointerEventData(eventSystem)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        raycaster.Raycast(pointerEventData, results);

        foreach (var result in results)
        {
            if (result.gameObject.GetComponent<T>() != null)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if the mouse is currently over a UI element, excluding certain UI objects
    /// and all their children.
    /// </summary>
    /// <param name="excludedUiElements">
    /// Optional list of UI GameObjects to ignore in the check (including any of their children).
    /// </param>
    /// <returns>
    /// True if mouse is over a UI element that is not excluded; false otherwise.
    /// </returns>
    public static bool IsMouseOverUiExcept(params GameObject[] excludedUiElements)
    {
        // Quick check: if pointer isn't over *any* UI elements, we can stop.
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            return false;
        }

        // Perform a UI raycast from the mouse pointer
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        // If no UI elements are hit, we can stop
        if (results.Count == 0)
        {
            return false;
        }

        // Check each UI element that was hit by the raycast
        foreach (RaycastResult result in results)
        {
            GameObject hitObject = result.gameObject;

            // If the hit object is not in the excluded list and not a child of an excluded object,
            // then we consider the mouse to be over a "meaningful" UI element.
            if (!IsExcluded(hitObject, excludedUiElements))
            {
                return true;
            }
        }

        // If we only hit excluded objects, return false
        return false;
    }

    /// <summary>
    /// Returns true if the given object is the same as one of the excluded objects
    /// or is a child of one of them.
    /// </summary>
    private static bool IsExcluded(GameObject candidate, GameObject[] excludedUiElements)
    {
        foreach (GameObject excluded in excludedUiElements)
        {
            if (excluded == null) continue;

            // If candidate is the excluded object itself or is a descendant
            if (candidate.transform == excluded.transform ||
                candidate.transform.IsChildOf(excluded.transform))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Returns how many consecutive bottom rows (from the sprite's rect) are fully transparent.
    /// Notes:
    /// - The texture must be Read/Write enabled in import settings.
    /// - If the sprite was tightly packed/trimmed, the cropped pixels are gone; this will
    ///   only analyze the remaining rect in the atlas.
    /// </summary>
    public static int CountBottomTransparentRows(Sprite sprite)
    {
        if (sprite == null) throw new System.ArgumentNullException(nameof(sprite));
        Texture2D tex = sprite.texture;
        if (tex == null) throw new System.ArgumentException("Sprite has no texture.", nameof(sprite));

        // Use the sprite's rect (pixel coords inside the texture)
        Rect r = sprite.rect;
        int w = Mathf.RoundToInt(r.width);
        int h = Mathf.RoundToInt(r.height);
        int x0 = Mathf.RoundToInt(r.x);
        int y0 = Mathf.RoundToInt(r.y);

        // Read only the sub-rect to avoid copying the whole texture.
        // Requires TextureImporter.isReadable = true.
        Color[] sub = tex.GetPixels(x0, y0, w, h);

        int transparentRowCount = 0;

        // Colors from GetPixels are laid out row-major from bottom to top.
        // Row y starts at index y * w, for y in [0, h-1].
        for (int y = 0; y < h; y++)
        {
            bool rowIsFullyTransparent = true;
            int rowStart = y * w;

            for (int x = 0; x < w; x++)
            {
                // "Fully transparent" means alpha == 0.0f exactly.
                // If you want to tolerate tiny non-zero values, use <= someEpsilon instead.
                if (sub[rowStart + x].a > 0f)
                {
                    rowIsFullyTransparent = false;
                    break;
                }
            }

            if (rowIsFullyTransparent)
                transparentRowCount++;
            else
                break; // first non-transparent row encountered; we’re done
        }

        return transparentRowCount;
    }

    #endregion

    #region Color

    public static Color GetColorFromRgb255(int r, int g, int b)
    {
        return new Color(r / 255f, g / 255f, b / 255f);
    }

    public static Color SmoothLerpColor(Color c1, Color c2, float t)
    {
        t = Mathf.Clamp01(t); // Ensure t is in the range [0, 1]
        return Color.Lerp(c1, c2, SmoothStep(t));
    }

    // SmoothStep function for smoother interpolation
    private static float SmoothStep(float t)
    {
        return t * t * (3f - 2f * t);
    }

    #endregion

    #region Raycast

    /// <summary>
    /// Sorts the given array of raycast hits by distance, whereas the first hit (closest to source position) is first.
    /// </summary>
    public static void OrderRaycastHitsByDistance(RaycastHit[] hits)
    {
        System.Array.Sort(hits, (a, b) => (a.distance.CompareTo(b.distance)));
    }

    #endregion

    #region Network

    public static string GetLocalIPv4()
    {
        string localIP = string.Empty;
        string hostName = Dns.GetHostName();       // Get the name of the host running the application
        IPHostEntry hostEntry = Dns.GetHostEntry(hostName);

        foreach (var ip in hostEntry.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                localIP = ip.ToString();
                break; // If you only want the first IPv4
            }
        }

        return localIP;
    }

    #endregion
}
