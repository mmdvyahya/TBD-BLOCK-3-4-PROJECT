// To do: Add mobile input support

using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Drag")]
    [SerializeField] float dragSpeed = 10f;

    [Header("Zoom")]
    [SerializeField] float zoomSpeed = 10f;
    [SerializeField] float minDistance = 5f;
    [SerializeField] float maxDistance = 80f;
    
    [Header("Focus")]
    [SerializeField] Vector3 focusPoint = Vector3.zero;

    void Update()
    {
        HandleDrag();
        HandleZoom();
    }

    void HandleDrag()
    {
        if (Mouse.current == null) return; // Prevents error for mobile platforms without mouse input

        if (Mouse.current.leftButton.isPressed)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            
            // Scale the drag speed based on the current distance to the focus point
            float currentDistance = Vector3.Distance(transform.position, focusPoint);
            float distanceScale = currentDistance / maxDistance;
            float dragSpeedMultiplier = dragSpeed * distanceScale;
            
            Vector3 move = new Vector3(-delta.x, 0, -delta.y) * dragSpeedMultiplier * 0.01f;
            transform.Translate(move, Space.World);
            focusPoint += move;
        }
    }

    void HandleZoom()
    {
        if (Mouse.current == null) return; // Prevents error for mobile platforms without mouse input

        float scroll = Mouse.current.scroll.ReadValue().y;
        if (scroll == 0) return;

        float zoomInput = Mathf.Sign(scroll);
        float zoomAmount = zoomInput * zoomSpeed;

        float currentDistance = Vector3.Distance(transform.position, focusPoint);
        float newDistance = Mathf.Clamp(currentDistance - zoomAmount, minDistance, maxDistance);

        if (!Mathf.Approximately(newDistance, currentDistance))
        {
            Vector3 direction = (transform.position - focusPoint).normalized;
            transform.position = focusPoint + direction * newDistance;
        }
    }
}