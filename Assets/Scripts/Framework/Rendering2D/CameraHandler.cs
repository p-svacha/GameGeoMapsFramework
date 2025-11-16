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
    public static CameraHandler Instance;
    public Camera Camera { get; private set; }

    public Vector2 CurrentPosition { get; private set; } // Position focussed by camera

    protected const float ZOOM_STEP = 1.1f;       // Base mouse wheel zoom speed (multiplicative)
    protected const float PAN_SPEED = 60f;        // Base WASD pan speed (world units/sec)
    protected const float MIN_CAMERA_SIZE = 20f;
    protected const float MAX_CAMERA_SIZE = 20000f;
    protected const float INITIAL_CAMERA_SIZE = 200f;
    protected const float DRAG_SPEED = 0.03f;

    protected const float CAMERA_BOUNDS = 200000;

    protected const bool PAN_SPEED_SCALES_WITH_ZOOM = true;
    protected const float PAN_SPEED_ZOOM_SCALE_FACTOR = 0.01f;

    protected const float PAN_SHIFT_MULT = 6.0f;  // WASD speed boost while holding shift
    protected const float ZOOM_SHIFT_MULT = 6.0f; // Zoom speed boost while holding shift

    // Input state
    protected bool IsLeftMouseDown;
    protected bool IsRightMouseDown;
    protected bool IsMouseWheelDown;

    // Size helpers
    private float CameraHeightWorld => Camera.orthographicSize;
    private float CameraWidthWorld => Camera.orthographicSize * Camera.aspect;

    // Bounds (in world coords)
    protected float MinX, MinY, MaxX, MaxY;

    // Pan animation
    public bool IsPanning { get; private set; }
    private float PanDuration;
    private float PanDelay;
    private Vector2 PanSourcePosition;
    private Vector2 PanTargetPosition;
    private Entity PostPanFollowEntity;
    private System.Action OnPanDoneCallback;

    // Follow
    public Entity FollowedEntity { get; private set; }

    public void SetPosition(Vector2 pos)
    {
        CurrentPosition = pos;
    }

    public void SetZoom(float zoom)
    {
        Camera.orthographicSize = Mathf.Clamp(zoom, MIN_CAMERA_SIZE, MAX_CAMERA_SIZE);
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Camera = GetComponent<Camera>();
        if (Camera == null)
        {
            Camera = Camera.main;
        }
        SetBounds(-CAMERA_BOUNDS, -CAMERA_BOUNDS, CAMERA_BOUNDS, CAMERA_BOUNDS);
        Camera.orthographicSize = INITIAL_CAMERA_SIZE;
    }

    private void Update()
    {
        UpdatePanAnimation();
        UpdateFollow();
        HandleInputs();

        // Set transform position (only here)
        Camera.transform.position = new Vector3(CurrentPosition.x, CurrentPosition.y, -10f);
    }

    private void HandleInputs()
    {
        // --- Zoom (mouse wheel) ---
        float wheel = Input.mouseScrollDelta.y;
        if (Mathf.Abs(wheel) > 0.0001f && !HelperFunctions.IsUiFocussed() && !HelperFunctions.IsMouseOverUi())
        {
            // Calculate new camera orthographic size
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            float step = shift ? Mathf.Pow(ZOOM_STEP, ZOOM_SHIFT_MULT) : ZOOM_STEP;
            float scale = Mathf.Pow(step, wheel);
            float newSize = Mathf.Clamp(Camera.orthographicSize / scale, MIN_CAMERA_SIZE, MAX_CAMERA_SIZE);

            // Anchor zoom at the mouse position to keep that point fixed on screen
            Vector3 mouseScreen = Input.mousePosition;
            Vector3 worldBefore = Camera.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, -Camera.transform.position.z));

            Camera.orthographicSize = newSize; // Apply here so anchor works

            if (FollowedEntity == null) // When following, only allow centered zoom
            {
                Vector3 worldAfter = Camera.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, -Camera.transform.position.z));
                Vector3 delta = worldBefore - worldAfter;
                CurrentPosition += new Vector2(delta.x, delta.y);
            }
        }

        // Dragging with right/middle mouse button
        if (Input.GetKeyDown(KeyCode.Mouse2)) IsMouseWheelDown = true;
        if (Input.GetKeyUp(KeyCode.Mouse2)) IsMouseWheelDown = false;
        if (Input.GetKeyDown(KeyCode.Mouse1)) IsRightMouseDown = true;
        if (Input.GetKeyUp(KeyCode.Mouse1)) IsRightMouseDown = false;
        if (IsMouseWheelDown && (Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0))
        {
            float speed = DRAG_SPEED * Camera.orthographicSize;
            float canvasScaleFactor = GameObject.Find("Canvas").GetComponent<Canvas>().scaleFactor;
            CurrentPosition += new Vector2(-Input.GetAxis("Mouse X") * speed / canvasScaleFactor, -Input.GetAxis("Mouse Y") * speed / canvasScaleFactor);
            EndPanOrFollow();
        }

        // --- Panning with WASD (Shift boost) ---
        float panBoost = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) ? PAN_SHIFT_MULT : 1f;
        Vector2 pan = Vector2.zero;
        if (Input.GetKey(KeyCode.W)) pan += Vector2.up;
        if (Input.GetKey(KeyCode.S)) pan += Vector2.down;
        if (Input.GetKey(KeyCode.A)) pan += Vector2.left;
        if (Input.GetKey(KeyCode.D)) pan += Vector2.right;

        if (pan.sqrMagnitude > 0f && !HelperFunctions.IsUiFocussed())
        {
            pan.Normalize();
            float panSpeed = PAN_SPEED * panBoost * Time.deltaTime;
            if (PAN_SPEED_SCALES_WITH_ZOOM) panSpeed *= (Camera.orthographicSize * PAN_SPEED_ZOOM_SCALE_FACTOR);
            CurrentPosition += pan * panSpeed;
            EndPanOrFollow();
        }

        // Drag triggers
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

        // Stay in bounds
        ClampToBounds();
    }

    private void ClampToBounds()
    {
        float realMinX = MinX + CameraWidthWorld - 1f;
        float realMaxX = MaxX - CameraWidthWorld + 1f;
        float realMinY = MinY + CameraHeightWorld - 1f;
        float realMaxY = MaxY - CameraHeightWorld + 1f;

        Vector2 p = CurrentPosition;
        p.x = Mathf.Clamp(p.x, realMinX, realMaxX);
        p.y = Mathf.Clamp(p.y, realMinY, realMaxY);
        CurrentPosition = p;
    }

    public void SetBounds(float minX, float minY, float maxX, float maxY)
    {
        MinX = minX;
        MinY = minY;
        MaxX = maxX;
        MaxY = maxY;
        ClampToBounds();
    }

    #region Pan & Follow

    public void PanTo(Vector2 targetPos, float duration = 0.5f, Entity postPanFollowEntity = null, System.Action callback = null)
    {
        EndPanOrFollow();

        // Init pan
        IsPanning = true;
        PanSourcePosition = CurrentPosition;
        PanTargetPosition = targetPos;
        PanDuration = duration;
        PostPanFollowEntity = postPanFollowEntity;
        PanDelay = 0f;
        OnPanDoneCallback = callback;

        // Immediately end pan if we are already very close to target position
        if (Vector2.Distance(CurrentPosition, targetPos) <= 0.1f)
        {
            //Debug.Log("Panning camera skipped because it already is at target position");
            PanDelay = PanDuration;
        }
    }

    private void UpdatePanAnimation()
    {
        if (IsPanning)
        {
            PanDelay += Time.deltaTime;

            if (PanDelay >= PanDuration) // Pan done
            {
                CurrentPosition = PanTargetPosition;
                FollowedEntity = PostPanFollowEntity;
                IsPanning = false;
                OnPanDoneCallback?.Invoke();
            }

            else // Pan in progress
            {
                Vector2 target = PanTargetPosition;
                if (PostPanFollowEntity != null) target = PostPanFollowEntity.CurrentWorldPosition;
                CurrentPosition = HelperFunctions.SmoothLerp(PanSourcePosition, target, (PanDelay / PanDuration));
            }
        }
    }

    private void UpdateFollow()
    {
        if (FollowedEntity != null) CurrentPosition = FollowedEntity.CurrentWorldPosition;
    }

    public void Unfollow()
    {
        FollowedEntity = null;
    }

    private void EndPanOrFollow()
    {
        IsPanning = false;
        FollowedEntity = null;
    }

    #endregion

    #region Triggers
    protected virtual void OnLeftMouseDragStart() { }
    protected virtual void OnLeftMouseDragEnd() { }
    #endregion
}
