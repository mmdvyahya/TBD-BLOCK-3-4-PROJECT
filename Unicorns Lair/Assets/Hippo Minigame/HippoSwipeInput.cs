using UnityEngine;
using UnityEngine.InputSystem;

public class HippoSwipeInput : MonoBehaviour
{
    [Header("Drag Settings")]
    [SerializeField] private float minReleaseDistance = 1.2f;

    public bool IsDragging { get; private set; }
    public bool ReleasedLeftThisFrame { get; private set; }
    public bool ReleasedRightThisFrame { get; private set; }
    public Vector3 DragWorldPosition { get; private set; }

    private Camera mainCamera;
    private Vector3 dragStartWorldPosition;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        ReleasedLeftThisFrame = false;
        ReleasedRightThisFrame = false;

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
            if (Mouse.current.leftButton.wasPressedThisFrame)
                pressedThisFrame = true;

            if (Mouse.current.leftButton.wasReleasedThisFrame)
                releasedThisFrame = true;

            if (Mouse.current.leftButton.isPressed)
                isPressed = true;

            screenPosition = Mouse.current.position.ReadValue();
        }

        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;

            if (touch.press.wasPressedThisFrame)
                pressedThisFrame = true;

            if (touch.press.wasReleasedThisFrame)
                releasedThisFrame = true;

            if (touch.press.isPressed)
                isPressed = true;

            screenPosition = touch.position.ReadValue();
        }

        if (mainCamera == null)
            return;

        Vector3 worldPosition = ScreenToWorldOnPlane(screenPosition);

        if (pressedThisFrame)
        {
            IsDragging = true;
            dragStartWorldPosition = worldPosition;
            DragWorldPosition = worldPosition;
        }

        if (IsDragging && isPressed)
        {
            DragWorldPosition = worldPosition;
        }

        if (IsDragging && releasedThisFrame)
        {
            float deltaX = worldPosition.x - dragStartWorldPosition.x;

            if (deltaX <= -minReleaseDistance)
                ReleasedLeftThisFrame = true;
            else if (deltaX >= minReleaseDistance)
                ReleasedRightThisFrame = true;

            IsDragging = false;
        }
    }

    private Vector3 ScreenToWorldOnPlane(Vector2 screenPosition)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);

        Plane plane = new Plane(Vector3.up, Vector3.up * 1f);

        if (plane.Raycast(ray, out float distance))
            return ray.GetPoint(distance);

        return Vector3.zero;
    }
}