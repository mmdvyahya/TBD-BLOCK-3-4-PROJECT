using UnityEngine;
using UnityEngine.InputSystem;

public class PrairieDogHoleRaycaster : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;

    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void Update()
    {
        bool pressed = false;
        Vector2 screenPosition = Vector2.zero;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            pressed = true;
            screenPosition = Mouse.current.position.ReadValue();
        }

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            pressed = true;
            screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
        }

        if (!pressed || mainCamera == null)
            return;

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            Debug.Log("Clicked object: " + hit.collider.name);

            PrairieDogHole hole = hit.collider.GetComponentInParent<PrairieDogHole>();

            if (hole != null)
                hole.NotifyPressedFromRaycast();
        }
    }
}