using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Default camera controls for a 2D orthographic world map.
/// Attach to the main camera.
/// - Middle (or right) mouse drag: world-anchored panning (cursor stays on the same world point)
/// - Mouse wheel: zoom (Shift to boost)
/// - WASD: pan (Shift to boost)
/// - Bounds clamped based on current ortho size
/// </summary>
public class CameraHandler : MonoBehaviour
{
    public Camera Camera { get; private set; }

    // Tunables
    protected const float ZOOM_SPEED = 6f;        // Base mouse wheel zoom speed
    protected const float PAN_SPEED = 60f;        // Base WASD pan speed (world units/sec)
    protected const float MIN_CAMERA_SIZE = 10f;
    protected const float MAX_CAMERA_SIZE = 500f;
    protected const float DRAG_SPEED = 0.03f;

    // Shift boosts
    protected const float PAN_SHIFT_MULT = 6.0f;  // WASD speed boost while holding Shift
    protected const float ZOOM_SHIFT_MULT = 5.0f; // Zoom speed boost while holding Shift

    // Input state
    protected bool IsLeftMouseDown;
    protected bool IsRightMouseDown;
    protected bool IsMouseWheelDown;

    // Size helpers
    private float CameraHeightWorld => Camera.orthographicSize;
    private float CameraWidthWorld => Camera.orthographicSize * Camera.aspect;

    // Bounds (in world coords)
    protected float MinX, MinY, MaxX, MaxY;

    public void SetPosition(Vector2 pos)
    {
        transform.position = new Vector3(pos.x, pos.y, transform.position.z);
    }

    public void SetZoom(float zoom)
    {
        Camera.orthographicSize = Mathf.Clamp(zoom, MIN_CAMERA_SIZE, MAX_CAMERA_SIZE);
    }

    private void Start()
    {
        Camera = GetComponent<Camera>();
        if (Camera == null)
        {
            Camera = Camera.main;
        }
        SetBounds(-3000, -3000, 3000, 3000);
    }

    private void Update()
    {
        // --- Zoom (mouse wheel) ---
        float wheel = Input.mouseScrollDelta.y;
        if (Mathf.Abs(wheel) > 0.0001f)
        {
            float boost = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) ? ZOOM_SHIFT_MULT : 1f;
            float delta = -wheel * ZOOM_SPEED * boost;
            Camera.orthographicSize = Mathf.Clamp(Camera.orthographicSize + delta, MIN_CAMERA_SIZE, MAX_CAMERA_SIZE);
        }

        // Dragging with right/middle mouse button
        if (Input.GetKeyDown(KeyCode.Mouse2)) IsMouseWheelDown = true;
        if (Input.GetKeyUp(KeyCode.Mouse2)) IsMouseWheelDown = false;
        if (Input.GetKeyDown(KeyCode.Mouse1)) IsRightMouseDown = true;
        if (Input.GetKeyUp(KeyCode.Mouse1)) IsRightMouseDown = false;
        if (IsMouseWheelDown)
        {
            float speed = DRAG_SPEED * Camera.orthographicSize;
            float canvasScaleFactor = GameObject.Find("Canvas").GetComponent<Canvas>().scaleFactor;
            transform.position += new Vector3(-Input.GetAxis("Mouse X") * speed / canvasScaleFactor, -Input.GetAxis("Mouse Y") * speed / canvasScaleFactor, 0f);
        }

        // --- Panning with WASD (Shift boost) ---
        float panBoost = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) ? PAN_SHIFT_MULT : 1f;
        Vector3 pan = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) pan += Vector3.up;
        if (Input.GetKey(KeyCode.S)) pan += Vector3.down;
        if (Input.GetKey(KeyCode.A)) pan += Vector3.left;
        if (Input.GetKey(KeyCode.D)) pan += Vector3.right;

        if (pan.sqrMagnitude > 0f)
        {
            pan.Normalize();
            transform.position += pan * (PAN_SPEED * panBoost * Time.deltaTime);
        }

        // --- Left-mouse drag triggers (kept from your original) ---
        if (Input.GetKeyDown(KeyCode.Mouse0) && !IsLeftMouseDown)
        {
            IsLeftMouseDown = true;
            OnLeftMouseDragStart();
        }
        if (Input.GetKeyUp(KeyCode.Mouse0) && IsLeftMouseDown)
        {
            IsLeftMouseDown = false;
            OnLeftMouseDragEnd();
        }

        // --- Clamp to bounds (account for current camera size/aspect) ---
        ClampToBounds();
    }

    private void ClampToBounds()
    {
        float realMinX = MinX + CameraWidthWorld - 1f;
        float realMaxX = MaxX - CameraWidthWorld + 1f;
        float realMinY = MinY + CameraHeightWorld - 1f;
        float realMaxY = MaxY - CameraHeightWorld + 1f;

        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, realMinX, realMaxX);
        p.y = Mathf.Clamp(p.y, realMinY, realMaxY);
        transform.position = p;
    }

    public void SetBounds(float minX, float minY, float maxX, float maxY)
    {
        MinX = minX;
        MinY = minY;
        MaxX = maxX;
        MaxY = maxY;
        // Optional: snap inside immediately
        ClampToBounds();
    }

    #region Triggers
    protected virtual void OnLeftMouseDragStart() { }
    protected virtual void OnLeftMouseDragEnd() { }
    #endregion
}
