using UnityEngine;
using UnityEngine.InputSystem;

public class HabitatClickRaycaster : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private HabitatInteractionController controller;

    private void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (controller == null) controller = FindFirstObjectByType<HabitatInteractionController>();
    }

    private void Update()
    {
        if (mainCamera == null || controller == null) return;
        if (controller.IsBusy) return;

        bool pressed = false;
        Vector2 screenPos = Vector2.zero;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            pressed = true;
            screenPos = Mouse.current.position.ReadValue();
        }
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            pressed = true;
            screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
        }

        if (!pressed) return;
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 9999f))
        {
            var habitat = hit.collider.GetComponentInParent<InspectableHabitat>()
                       ?? hit.collider.GetComponent<InspectableHabitat>();
            if (habitat != null)
                controller.OpenHabitat(habitat);
        }
    }
}