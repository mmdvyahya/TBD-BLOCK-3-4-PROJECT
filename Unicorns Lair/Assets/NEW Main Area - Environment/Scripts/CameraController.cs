using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Drag")]
    [SerializeField] float dragSpeed = 10f;

    [Header("Zoom")]
    [SerializeField] float zoomSpeed = 10f;
    [SerializeField] float pinchSensitivity = 0.05f;
    [SerializeField] float minDistance = 5f;
    [SerializeField] float maxDistance = 80f;

    [Header("Focus")]
    [SerializeField] Vector3 focusPoint = Vector3.zero;

    [Header("Bounds (XZ plane)")]
    [SerializeField] bool useBounds = true;
    [SerializeField] Vector2 minBoundsXZ = new Vector2(-50f, -50f);
    [SerializeField] Vector2 maxBoundsXZ = new Vector2(50f, 50f);

    private float _previousPinchDistance;
    private bool _wasPinching;

    void Update()
    {
        int touchCount = ActiveTouchCount();

        if (touchCount >= 2)
        {
            HandlePinchZoom();
            _wasPinching = true;
        }
        else if (touchCount == 1)
        {
            if (_wasPinching) { _wasPinching = false; return; }
            HandleTouchDrag();
        }
        else
        {
            _wasPinching = false;
            HandleMouseDrag();
            HandleMouseZoom();
        }
    }

    int ActiveTouchCount()
    {
        var ts = Touchscreen.current;
        if (ts == null) return 0;
        int count = 0;
        for (int i = 0; i < ts.touches.Count; i++)
            if (ts.touches[i].press.isPressed) count++;
        return count;
    }

    void HandleMouseDrag()
    {
        if (Mouse.current == null) return;
        if (!Mouse.current.leftButton.isPressed) return;

        Vector2 delta = Mouse.current.delta.ReadValue();
        ApplyDrag(delta);
    }

    void HandleTouchDrag()
    {
        var ts = Touchscreen.current;
        if (ts == null) return;
        if (!ts.primaryTouch.press.isPressed) return;

        Vector2 delta = ts.primaryTouch.delta.ReadValue();
        ApplyDrag(delta);
    }

    void ApplyDrag(Vector2 delta)
    {
        float currentDistance = Vector3.Distance(transform.position, focusPoint);
        float distanceScale = currentDistance / maxDistance;
        float dragSpeedMultiplier = dragSpeed * distanceScale;

        Vector3 desiredMove = new Vector3(-delta.x, 0f, -delta.y) * dragSpeedMultiplier * 0.01f;
        Vector3 desiredFocus = focusPoint + desiredMove;

        if (useBounds)
        {
            desiredFocus.x = Mathf.Clamp(desiredFocus.x, minBoundsXZ.x, maxBoundsXZ.x);
            desiredFocus.z = Mathf.Clamp(desiredFocus.z, minBoundsXZ.y, maxBoundsXZ.y);
        }

        Vector3 actualMove = desiredFocus - focusPoint;
        transform.Translate(actualMove, Space.World);
        focusPoint = desiredFocus;
    }

    void HandleMouseZoom()
    {
        if (Mouse.current == null) return;

        float scroll = Mouse.current.scroll.ReadValue().y;
        if (scroll == 0f) return;

        float zoomInput = Mathf.Sign(scroll);
        ApplyZoom(zoomInput * zoomSpeed);
    }

    void HandlePinchZoom()
    {
        var ts = Touchscreen.current;
        if (ts == null) return;

        Vector2 t0 = ts.touches[0].position.ReadValue();
        Vector2 t1 = ts.touches[1].position.ReadValue();
        float currentPinch = Vector2.Distance(t0, t1);

        if (!_wasPinching)
        {
            _previousPinchDistance = currentPinch;
            return;
        }

        float pinchDelta = currentPinch - _previousPinchDistance;
        _previousPinchDistance = currentPinch;

        if (Mathf.Abs(pinchDelta) > 0.01f)
            ApplyZoom(pinchDelta * pinchSensitivity);
    }

    void ApplyZoom(float zoomAmount)
    {
        float currentDistance = Vector3.Distance(transform.position, focusPoint);
        float newDistance = Mathf.Clamp(currentDistance - zoomAmount, minDistance, maxDistance);

        if (!Mathf.Approximately(newDistance, currentDistance))
        {
            Vector3 direction = (transform.position - focusPoint).normalized;
            transform.position = focusPoint + direction * newDistance;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!useBounds) return;
        Vector3 center = new Vector3(
            (minBoundsXZ.x + maxBoundsXZ.x) * 0.5f,
            focusPoint.y,
            (minBoundsXZ.y + maxBoundsXZ.y) * 0.5f);
        Vector3 size = new Vector3(
            maxBoundsXZ.x - minBoundsXZ.x,
            0.1f,
            maxBoundsXZ.y - minBoundsXZ.y);
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
        Gizmos.DrawWireCube(center, size);
    }
}