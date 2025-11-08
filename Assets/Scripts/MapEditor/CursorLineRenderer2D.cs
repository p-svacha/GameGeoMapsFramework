using UnityEngine;

/// <summary>
/// Draws a line from a fixed world position to the current mouse position in 2D.
/// Call Init() right after adding this component.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class CursorLineRenderer2D : MonoBehaviour
{
    public LineRenderer LineRenderer;
    private Vector3 StartWorldPosition;

    private void Awake()
    {
        LineRenderer = GetComponent<LineRenderer>();
        LineRenderer.positionCount = 2;
        LineRenderer.useWorldSpace = true;
    }

    /// <summary>
    /// Initializes the line renderer.
    /// </summary>
    /// <param name="fixedPoint">Fixed start position in world space.</param>
    public void Init(Vector3 fixedPoint)
    {
        StartWorldPosition = fixedPoint;
    }

    private void Update()
    {
        if (LineRenderer == null) return;

        LineRenderer.SetPosition(0, StartWorldPosition);
        LineRenderer.SetPosition(1, MouseHoverInfo.WorldPositionWithSnap);
    }
}
