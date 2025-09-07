using UnityEngine;

/// <summary>
/// Draws a line from a fixed world position to the current mouse position in 2D.
/// Call Init() right after adding this component.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class CursorLineRenderer2D : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private Vector3 startWorldPosition;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
    }

    /// <summary>
    /// Initializes the line renderer.
    /// </summary>
    /// <param name="fixedPoint">Fixed start position in world space.</param>
    public void Init(Vector3 fixedPoint)
    {
        startWorldPosition = fixedPoint;
    }

    private void Update()
    {
        if (lineRenderer == null) return;

        lineRenderer.SetPosition(0, startWorldPosition);
        lineRenderer.SetPosition(1, MouseHoverInfo.WorldPositionWithSnap);
    }
}
