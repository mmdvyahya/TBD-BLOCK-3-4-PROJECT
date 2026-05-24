using UnityEngine;
using UnityEngine.InputSystem;

public class HippoSwipeInput : MonoBehaviour
{
    [Header("Camera")]
    [Tooltip("Leave empty to auto-find. Falls back to Camera.main, then any camera in the scene.")]
    [SerializeField] private Camera mainCamera;

    [Header("Drag Settings")]
    [Tooltip("World plane height the drag visual is projected onto. Set this near the height your food items sit at.")]
    [SerializeField] private float planeHeight = 1f;
    [Tooltip("How far the finger/mouse must move sideways (as a fraction of screen width) before a release counts as a swipe. 0.12 = 12% of the screen.")]
    [SerializeField] private float minSwipeScreenFraction = 0.12f;
    [Tooltip("Flip this if left/right ever feel reversed for your setup.")]
    [SerializeField] private bool invertSwipe = false;

    [Header("Debug")]
    [SerializeField] private bool logDebug = false;

    public bool IsDragging { get; private set; }
    public bool ReleasedLeftThisFrame { get; private set; }
    public bool ReleasedRightThisFrame { get; private set; }
    public Vector3 DragWorldPosition { get; private set; }

    private Vector2 dragStartScreenPosition;

    private void Start()
    {
        ResolveCamera();
    }

    private void ResolveCamera()
    {
        if (mainCamera != null) return;
        mainCamera = Camera.main;
        if (mainCamera == null) mainCamera = FindFirstObjectByType<Camera>();
        if (mainCamera == null && logDebug)
            Debug.LogWarning("[HippoSwipeInput] No camera found. Tag your camera 'MainCamera' or drag it into the Main Camera slot.");
    }

    private void Update()
    {
        ReleasedLeftThisFrame = false;
        ReleasedRightThisFrame = false;

        if (mainCamera == null) ResolveCamera();

        HandleInput();
    }

    private void HandleInput()
    {
        bool pressedThisFrame = false;
        bool releasedThisFrame = false;
        bool isPressed = false;
        Vector2 screenPosition = Vector2.zero;

        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame) pressedThisFrame = true;
            if (Mouse.current.leftButton.wasReleasedThisFrame) releasedThisFrame = true;
            if (Mouse.current.leftButton.isPressed) isPressed = true;
            screenPosition = Mouse.current.position.ReadValue();
        }

        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;
            if (touch.press.isPressed)
            {
                if (touch.press.wasPressedThisFrame) pressedThisFrame = true;
                isPressed = true;
                screenPosition = touch.position.ReadValue();
            }
            else if (touch.press.wasReleasedThisFrame)
            {
                releasedThisFrame = true;
                screenPosition = touch.position.ReadValue();
            }
        }

        if (mainCamera == null)
            return;

        DragWorldPosition = ScreenToWorldOnPlane(screenPosition);

        if (pressedThisFrame)
        {
            IsDragging = true;
            dragStartScreenPosition = screenPosition;
            if (logDebug) Debug.Log("[HippoSwipeInput] Drag started at screen " + screenPosition);
        }

        if (IsDragging && releasedThisFrame)
        {
            float deltaX = screenPosition.x - dragStartScreenPosition.x;
            if (invertSwipe) deltaX = -deltaX;

            float threshold = Screen.width * minSwipeScreenFraction;

            if (deltaX <= -threshold) ReleasedLeftThisFrame = true;
            else if (deltaX >= threshold) ReleasedRightThisFrame = true;

            if (logDebug) Debug.Log($"[HippoSwipeInput] Released. screenDeltaX={deltaX:F0}px, threshold={threshold:F0}px, left={ReleasedLeftThisFrame}, right={ReleasedRightThisFrame}");

            IsDragging = false;
        }
    }

    private Vector3 ScreenToWorldOnPlane(Vector2 screenPosition)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        Plane plane = new Plane(Vector3.up, Vector3.up * planeHeight);
        if (plane.Raycast(ray, out float distance))
            return ray.GetPoint(distance);
        return Vector3.zero;
    }
}